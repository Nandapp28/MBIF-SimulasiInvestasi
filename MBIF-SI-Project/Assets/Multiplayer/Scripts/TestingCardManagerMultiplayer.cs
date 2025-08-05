// File: TestingCardManagerMultiplayer.cs (Versi Modifikasi - Independen)
using UnityEngine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class TestingCardManagerMultiplayer : MonoBehaviourPunCallbacks
{
    public static TestingCardManagerMultiplayer Instance;

    [Header("Game Data References")]
    // --- PERUBAHAN 1: Menggunakan List dari TestingCardData, BUKAN CardPoolEntry ---
    public List<TestingCardData> testingCardsPool; 

    [Header("UI Setup")]
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
            // Pengecekan sekarang ke testingCardsPool
            if (testingCardsPool == null || testingCardsPool.Count == 0)
            {
                Debug.LogError("'Testing Cards Pool' belum di-assign atau kosong di TestingCardManager!");
                return;
            }

            // Ambil kartu acak dari pool yang baru
            int randomIndex = Random.Range(0, testingCardsPool.Count);
            photonView.RPC("Rpc_ShowTestingCard", RpcTarget.All, randomIndex);
        }
    }

    [PunRPC]
    private void Rpc_ShowTestingCard(int cardIndex)
    {
        if (instantiatedCard != null) Destroy(instantiatedCard);

        // --- PERUBAHAN 2: Mengambil data dari testingCardsPool ---
        TestingCardData cardData = testingCardsPool[cardIndex];
        
        instantiatedCard = Instantiate(testingCardPrefab, cardDisplayContainer);
        instantiatedCard.transform.localPosition = Vector3.zero;

        TestingCardUI cardUI = instantiatedCard.GetComponent<TestingCardUI>();
        if (cardUI != null)
        {
            // --- PERUBAHAN 3: Mengirim data tipe baru ke UI ---
            cardUI.Setup(cardData);
        }

        StartCoroutine(AnimateCardDisplay());
    }

    // Fungsi AnimateCardDisplay tidak perlu diubah, karena hanya mengatur animasi fade.
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

        // Logika untuk melanjutkan permainan setelah fase ini selesai (tidak berubah)
        if (PhotonNetwork.IsMasterClient)
        {
            yield return new WaitForSeconds(1.0f);
            if (MultiplayerManager.Instance != null)
            {
                MultiplayerManager.Instance.StartNewSemester();
            }
        }
    }
}