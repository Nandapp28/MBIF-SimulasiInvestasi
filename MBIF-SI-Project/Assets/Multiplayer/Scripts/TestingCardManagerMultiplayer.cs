// File: TestingCardManagerMultiplayer.cs (Versi Independen)
using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class TestingCardManagerMultiplayer : MonoBehaviourPunCallbacks
{
    public static TestingCardManagerMultiplayer Instance;

    [Header("Game Data References")]
    public List<CardPoolEntry> allCardsPool;

    [Header("UI Setup")]
    // Menggunakan prefab baru yang didedikasikan untuk fase ini
    public GameObject testingCardPrefab; 
    public Transform cardDisplayContainer;
    public CanvasGroup containerCanvasGroup;

    private GameObject instantiatedCard;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    public void StartTestingPhase()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (allCardsPool == null || allCardsPool.Count == 0)
            {
                Debug.LogError("All Cards Pool belum di-assign di TestingCardManager!");
                return;
            }

            int randomIndex = Random.Range(0, allCardsPool.Count);
            photonView.RPC("Rpc_ShowTestingCard", RpcTarget.All, randomIndex);
        }
    }

    [PunRPC]
    private void Rpc_ShowTestingCard(int cardIndex)
    {
        if (instantiatedCard != null) Destroy(instantiatedCard);

        CardPoolEntry cardData = allCardsPool[cardIndex];
        
        // Buat objek kartu dari prefab baru
        instantiatedCard = Instantiate(testingCardPrefab, cardDisplayContainer);
        instantiatedCard.transform.localPosition = Vector3.zero;

        // Ambil komponen UI baru dan atur tampilannya
        TestingCardUI cardUI = instantiatedCard.GetComponent<TestingCardUI>();
        if (cardUI != null)
        {
            cardUI.Setup(cardData);
        }

        StartCoroutine(AnimateCardDisplay());
    }

    private IEnumerator AnimateCardDisplay()
    {
        float fadeDuration = 0.7f;
        float holdDuration = 3.0f;
        float timer;

        // FADE IN
        timer = 0f;
        while (timer < fadeDuration)
        {
            containerCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        containerCanvasGroup.alpha = 1;

        // HOLD
        yield return new WaitForSeconds(holdDuration);

        // FADE OUT
        timer = 0f;
        while (timer < fadeDuration)
        {
            containerCanvasGroup.alpha = Mathf.Lerp(1, 0, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        containerCanvasGroup.alpha = 0;

        if (instantiatedCard != null) Destroy(instantiatedCard);

        if (PhotonNetwork.IsMasterClient)
        {
            yield return new WaitForSeconds(1.0f);
            MultiplayerManager.Instance.StartNewSemester();
        }
    }
}