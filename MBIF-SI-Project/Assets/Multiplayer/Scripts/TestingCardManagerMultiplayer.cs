// File: TestingCardManagerMultiplayer.cs (Versi Final dengan Efek Cardtest1 & Cardtest2)
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

public class TestingCardManagerMultiplayer : MonoBehaviourPunCallbacks
{
    public static TestingCardManagerMultiplayer Instance;

    [Header("Game Data References")]
    public List<TestingCardData> testingCardsPool;

    [Header("UI Setup")]
    public GameObject testingCardPrefab;
    public Transform cardDisplayContainer;
    public CanvasGroup containerCanvasGroup;

    [Header("Interactive UI (Sem 2-4)")]
    public GameObject interactiveButtonsPanel;
    public Button activateButton;
    public Button skipButton;
    public TextMeshProUGUI statusText;

    [Header("Cardtest1 Effect UI")]
    public GameObject sectorChoicePanel;
    public Button konsumerChoiceButton;
    public Button infrastrukturChoiceButton;
    public Button keuanganChoiceButton;
    public Button tambangChoiceButton;

    private bool playerHasMadeChoice = false;
    private GameObject instantiatedCard;
    private List<int> playersFinishedInteraction = new List<int>();

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        if (activateButton != null) activateButton.onClick.AddListener(OnActivateButtonClicked);
        if (skipButton != null) skipButton.onClick.AddListener(OnSkipButtonClicked);
        if (konsumerChoiceButton != null) konsumerChoiceButton.onClick.AddListener(() => OnSectorChosenForPreview("Konsumer"));
        if (infrastrukturChoiceButton != null) infrastrukturChoiceButton.onClick.AddListener(() => OnSectorChosenForPreview("Infrastruktur"));
        if (keuanganChoiceButton != null) keuanganChoiceButton.onClick.AddListener(() => OnSectorChosenForPreview("Keuangan"));
        if (tambangChoiceButton != null) tambangChoiceButton.onClick.AddListener(() => OnSectorChosenForPreview("Tambang"));
        if (interactiveButtonsPanel != null) interactiveButtonsPanel.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (sectorChoicePanel != null) sectorChoicePanel.SetActive(false);
    }
    
    [PunRPC]
    private void Rpc_ShowMyTestingCard(int cardIndex)
    {
        int currentSemester = (int)PhotonNetwork.CurrentRoom.CustomProperties[MultiplayerManager.SEMESTER_KEY];
        if (currentSemester > 1)
        {
            StartCoroutine(InteractiveCardSequence(cardIndex));
        }
        else
        {
            StartCoroutine(AnimateSimpleCard(cardIndex));
        }
    }

    // --- PERBAIKAN: Logika efek untuk Cardtest2 ditambahkan di sini ---
    [PunRPC]
    private void Rpc_ApplyTestingCardEffect(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Player activator = info.Sender;
        if (activator == null) return;

        int cardIndex = (int)activator.CustomProperties[PlayerProfileMultiplayer.TESTING_CARD_INDEX_KEY];
        TestingCardData cardData = testingCardsPool[cardIndex];
        Debug.Log($"[MasterClient] Menerapkan efek dari '{cardData.cardType}' untuk pemain {activator.NickName}.");

        switch (cardData.cardType)
        {
            case TestingCardType.Cardtest1:
                // Tidak ada aksi di server untuk Cardtest1
                break;

            case TestingCardType.Cardtest2:
                Debug.Log($"[Cardtest2 Effect] Mengurangi InvestPoin semua pemain kecuali {activator.NickName}.");
                foreach (Player targetPlayer in PhotonNetwork.PlayerList)
                {
                    if (targetPlayer == activator) continue;

                    int totalCards = 0;
                    totalCards += (int)targetPlayer.CustomProperties[PlayerProfileMultiplayer.KONSUMER_CARDS_KEY];
                    totalCards += (int)targetPlayer.CustomProperties[PlayerProfileMultiplayer.INFRASTRUKTUR_CARDS_KEY];
                    totalCards += (int)targetPlayer.CustomProperties[PlayerProfileMultiplayer.KEUANGAN_CARDS_KEY];
                    totalCards += (int)targetPlayer.CustomProperties[PlayerProfileMultiplayer.TAMBANG_CARDS_KEY];

                    if (totalCards > 0)
                    {
                        // --- PERUBAHAN DI SINI: Hitung penalti (jumlah kartu dikali 2) ---
                        int penalty = totalCards * 2;

                        int currentInvestPoin = (int)targetPlayer.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];
                        int newInvestPoin = Mathf.Max(0, currentInvestPoin - penalty); // Gunakan 'penalty'

                        Hashtable propsToSet = new Hashtable { { PlayerProfileMultiplayer.INVESTPOINT_KEY, newInvestPoin } };
                        targetPlayer.SetCustomProperties(propsToSet);

                        // --- PERBAIKAN LOG: Tampilkan nilai penalti yang benar ---
                        Debug.Log($"[Cardtest2 Effect] {targetPlayer.NickName} memiliki {totalCards} kartu, InvestPoin berkurang sebesar {penalty}. Sisa: {newInvestPoin}.");
                    }
                }
                break;
            case TestingCardType.Cardtest3:
            case TestingCardType.Cardtest4:
                // 1. Siapkan daftar sektor yang bisa menjadi target
                string[] sectors = { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };

                // 2. Pilih satu sektor secara acak
                string randomSector = sectors[Random.Range(0, sectors.Length)];

                // 3. Pilih nilai penurunan IPO secara acak (-1, -2, atau -3)
                int randomDecrease = Random.Range(1, 4) * -1; // Menghasilkan 1,2,3 lalu dikali -1

                Debug.Log($"[{cardData.cardType} Effect] Menurunkan IPO Sektor '{randomSector}' sebesar {randomDecrease}.");

                // 4. Panggil fungsi di SellingPhaseManager untuk menerapkan perubahan
                SellingPhaseManagerMultiplayer.Instance.ModifyIPOIndex(randomSector, randomDecrease);
                break;
            case TestingCardType.Cardtest5:
                Debug.Log($"[{cardData.cardType} Effect] Mereset semua harga IPO ke posisi awal (5).");

                // 1. Siapkan Hashtable untuk menampung semua perubahan properti.
                Hashtable props = new Hashtable();
                string[] allSectors = { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };

                // 2. Loop melalui setiap sektor dan atur indeks serta bonusnya ke 0.
                foreach (string sector in allSectors)
                {
                    props["ipo_index_" + sector] = 0; // Indeks 0 = harga 5
                    props["ipo_bonus_" + sector] = 0; // Reset bonus juga untuk memastikan
                }

                // 3. Kirim semua perubahan dalam satu panggilan jaringan.
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                break;
        }
    }
    
    // --- PERBAIKAN: Fungsi ini sekarang menangani Cardtest1 dan Cardtest2 ---
    public void OnActivateButtonClicked()
    {
        Hashtable props = new Hashtable { { PlayerProfileMultiplayer.TESTING_CARD_USED_KEY, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        int cardIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.TESTING_CARD_INDEX_KEY];
        TestingCardData cardData = testingCardsPool[cardIndex];

        if (interactiveButtonsPanel != null) interactiveButtonsPanel.SetActive(false);
        
        switch (cardData.cardType)
        {
            case TestingCardType.Cardtest1:
                if (sectorChoicePanel != null) sectorChoicePanel.SetActive(true);
                break;
            
            case TestingCardType.Cardtest2:
                playerHasMadeChoice = true;
                break;
            case TestingCardType.Cardtest3:
            case TestingCardType.Cardtest4:
                playerHasMadeChoice = true;
                break;
            case TestingCardType.Cardtest5:
                playerHasMadeChoice = true;
                break;
            
            default:
                playerHasMadeChoice = true;
                break;
        }
        
        photonView.RPC("Rpc_ApplyTestingCardEffect", RpcTarget.MasterClient);
    }
    
    private void OnSectorChosenForPreview(string sectorName)
    {
        if (sectorChoicePanel != null) sectorChoicePanel.SetActive(false);
        StartCoroutine(PrivateRumorPreviewAnimation(sectorName));
    }

    private IEnumerator PrivateRumorPreviewAnimation(string sectorName)
    {
        if (RumorPhaseManagerMultiplayer.Instance != null)
        {
            yield return StartCoroutine(RumorPhaseManagerMultiplayer.Instance.AnimatePrivateRumorPreview(sectorName));
        }
        playerHasMadeChoice = true;
    }
    
    public void OnSkipButtonClicked()
    {
        playerHasMadeChoice = true;
        if (interactiveButtonsPanel != null) interactiveButtonsPanel.SetActive(false);
    }

    #region Interactive Flow (Semester 2, 3, 4)
    public void BeginTestingPhase()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            playersFinishedInteraction.Clear();
            Hashtable props = new Hashtable { { "AllPlayersFinishedTesting", false } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            if (testingCardsPool == null || testingCardsPool.Count == 0) return;
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.CustomProperties.ContainsKey(PlayerProfileMultiplayer.TESTING_CARD_INDEX_KEY))
                {
                    int savedCardIndex = (int)p.CustomProperties[PlayerProfileMultiplayer.TESTING_CARD_INDEX_KEY];
                    if (savedCardIndex != -1)
                    {
                        photonView.RPC("Rpc_ShowMyTestingCard", p, savedCardIndex);
                    }
                }
            }
        }
    }
    private IEnumerator InteractiveCardSequence(int cardIndex)
    {
        if (instantiatedCard != null) Destroy(instantiatedCard);
        instantiatedCard = Instantiate(testingCardPrefab, cardDisplayContainer);
        instantiatedCard.GetComponent<TestingCardUI>().Setup(testingCardsPool[cardIndex]);
        float fadeDuration = 0.7f;
        float timer = 0f;
        while (timer < fadeDuration) { containerCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeDuration); timer += Time.deltaTime; yield return null; }
        containerCanvasGroup.alpha = 1;
        bool hasUsedCard = (bool)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.TESTING_CARD_USED_KEY];
        if (hasUsedCard)
        {
            if (statusText != null) { statusText.text = "Card Already Used"; statusText.gameObject.SetActive(true); }
            yield return new WaitForSeconds(2.0f);
            playerHasMadeChoice = true; 
        }
        else
        {
            playerHasMadeChoice = false;
            interactiveButtonsPanel.SetActive(true);
            yield return new WaitUntil(() => playerHasMadeChoice);
        }
        photonView.RPC("Rpc_SignalInteractionComplete", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
        if (statusText != null) statusText.gameObject.SetActive(false);
        if (interactiveButtonsPanel != null) interactiveButtonsPanel.SetActive(false);
        timer = 0f;
        while (timer < fadeDuration) { containerCanvasGroup.alpha = Mathf.Lerp(1, 0, timer / fadeDuration); timer += Time.deltaTime; yield return null; }
        containerCanvasGroup.alpha = 0;
        if (instantiatedCard != null) Destroy(instantiatedCard);
    }
    [PunRPC]
    private void Rpc_SignalInteractionComplete(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!playersFinishedInteraction.Contains(actorNumber))
        {
            playersFinishedInteraction.Add(actorNumber);
        }
        if (playersFinishedInteraction.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            if (ActionPhaseManager.Instance != null)
            {
                ActionPhaseManager.Instance.ProceedToSellingPhaseAfterTesting();
            }
        }
    }
    #endregion

    #region Automatic Flow (Semester 1)
    public IEnumerator ShowCardAndWait()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (testingCardsPool.Count > 0)
            {
                foreach (Player p in PhotonNetwork.PlayerList)
                {
                    int randomIndex = Random.Range(0, testingCardsPool.Count);
                    Hashtable props = new Hashtable { { PlayerProfileMultiplayer.TESTING_CARD_INDEX_KEY, randomIndex } };
                    p.SetCustomProperties(props);
                    photonView.RPC("Rpc_ShowMyTestingCard", p, randomIndex);
                }
            }
        }
        float totalWaitTime = 0.7f + 3.0f + 0.7f + 1.0f;
        yield return new WaitForSeconds(totalWaitTime);
    }
    private IEnumerator AnimateSimpleCard(int cardIndex)
    {
        if (instantiatedCard != null) Destroy(instantiatedCard);
        instantiatedCard = Instantiate(testingCardPrefab, cardDisplayContainer);
        instantiatedCard.GetComponent<TestingCardUI>().Setup(testingCardsPool[cardIndex]);
        float fadeDuration = 0.7f;
        float holdDuration = 3.0f;
        float timer;
        timer = 0f;
        while (timer < fadeDuration) { containerCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeDuration); timer += Time.deltaTime; yield return null; }
        containerCanvasGroup.alpha = 1;
        yield return new WaitForSeconds(holdDuration);
        timer = 0f;
        while (timer < fadeDuration) { containerCanvasGroup.alpha = Mathf.Lerp(1, 0, timer / fadeDuration); timer += Time.deltaTime; yield return null; }
        containerCanvasGroup.alpha = 0;
        if (instantiatedCard != null) Destroy(instantiatedCard);
    }
    #endregion
}