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

    [Header("Timer UI (Shared)")]
    public GameObject timerPanel;
    public Image timerBar;
    public TextMeshProUGUI timerText;
    public const float TESTING_TIME = 30.0f; // Waktu dalam detik
    private Coroutine testingTimerCoroutine;
    private const string TESTING_START_TIME_KEY = "testingStartTime";

    private bool isAnimating = false;
    private bool playerHasMadeChoice = false;
    private GameObject instantiatedCard;
    private List<int> playersFinishedInteraction = new List<int>();
    private Coroutine botTestingCoroutine;

    public bool isInTenderMode = false;

    private string swapSourceSector = ""; 
    private int swapTargetActorNumber = -1;

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
        if (timerPanel != null) timerPanel.SetActive(false);
    }
    private IEnumerator StartTestingTimer(double startTime)
    {
        if (timerPanel != null) timerPanel.SetActive(true);
        float timeLeft = TESTING_TIME;

        while (timeLeft > 0)
        {
            // Hitung sisa waktu berdasarkan waktu server agar sinkron
            double elapsed = PhotonNetwork.Time - startTime;
            timeLeft = TESTING_TIME - (float)elapsed;

            if (timeLeft < 0) timeLeft = 0;

            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(timeLeft).ToString();
            }
            if (timerBar != null)
            {
                timerBar.fillAmount = Mathf.Clamp01(timeLeft / TESTING_TIME);
            }

            yield return null;
        }

        // Waktu habis
        if (timerPanel != null) timerPanel.SetActive(false);

        // Jika pemain ini belum memilih, paksa skip
        if (!playerHasMadeChoice)
        {
            Debug.Log("Waktu Testing habis! Otomatis skip.");
            BotModeManager.SetBotMode(true);
            OnSkipButtonClicked();
        }
    }

    // --- BARU --- Mendengarkan Properti Ruangan untuk memulai timer
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey(TESTING_START_TIME_KEY))
        {
            // Hentikan timer lama jika ada
            if (testingTimerCoroutine != null) StopCoroutine(testingTimerCoroutine);

            double startTime = (double)propertiesThatChanged[TESTING_START_TIME_KEY];
            testingTimerCoroutine = StartCoroutine(StartTestingTimer(startTime));
        }
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
    private void Rpc_ApplyTestingCardEffect(string chosenSector, PhotonMessageInfo info)
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

                string targetSector = chosenSector;

            if (string.IsNullOrEmpty(targetSector))
            {
                Debug.LogWarning("TargetSector kosong! Fallback ke Konsumer.");
                targetSector = "";
            }

                // Pilih nilai penurunan IPO secara acak (-1, -2, atau -3)
                // (Efeknya tetap acak, tapi targetnya sekarang dipilih pemain)
                int randomDecrease = -2;

                Debug.Log($"[{cardData.cardType} Effect] Pemain {activator.NickName} memilih sektor '{targetSector}'. Menurunkan IPO sebesar {randomDecrease}.");

                // Panggil fungsi di SellingPhaseManager untuk menerapkan perubahan
                SellingPhaseManagerMultiplayer.Instance.ModifyIPOIndex(targetSector, randomDecrease);

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
            case TestingCardType.Cardtest6:
                // --- LOGIKA SERVER CARDTEST6 (Tender Offer Setengah Harga) ---
                if (activator.CustomProperties.TryGetValue("TargetActorForCardtest6", out object targetActorObj))
                {
                    int targetActorId = (int)targetActorObj;
                    Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(targetActorId);

                    if (targetPlayer != null && !string.IsNullOrEmpty(chosenSector))
                    {
                        string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(chosenSector);
                        int targetCardCount = targetPlayer.CustomProperties.ContainsKey(cardKey) ? (int)targetPlayer.CustomProperties[cardKey] : 0;

                        Debug.Log($"[Cardtest6] {activator.NickName} mencoba membeli paksa {chosenSector} dari {targetPlayer.NickName}. Jumlah kartu target: {targetCardCount}");

                        if (targetCardCount > 0)
                        {
                            // 1. Hitung Harga (Setengah Harga)
                            int fullPrice = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(chosenSector);
                            int purchasePrice = Mathf.CeilToInt(fullPrice / 2.0f); // Pembulatan ke atas

                            int activatorMoney = (int)activator.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];

                            // 2. Cek Uang
                            if (activatorMoney >= purchasePrice)
                            {
                                // 3. Proses Transaksi
                                // Kurangi kartu target, tambah uang target
                                int targetMoney = (int)targetPlayer.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];
                                Hashtable targetProps = new Hashtable {
                                    { cardKey, targetCardCount - 1 },
                                    { PlayerProfileMultiplayer.INVESTPOINT_KEY, targetMoney + purchasePrice }
                                };
                                targetPlayer.SetCustomProperties(targetProps);

                                // Tambah kartu activator, kurangi uang activator
                                int activatorCardCount = activator.CustomProperties.ContainsKey(cardKey) ? (int)activator.CustomProperties[cardKey] : 0;
                                Hashtable activatorProps = new Hashtable {
                                    { cardKey, activatorCardCount + 1 },
                                    { PlayerProfileMultiplayer.INVESTPOINT_KEY, activatorMoney - purchasePrice }
                                };
                                activator.SetCustomProperties(activatorProps);

                                Debug.Log($"[Cardtest6] Sukses! Dibeli seharga {purchasePrice}.");
                            }
                            else
                            {
                                Debug.LogWarning("[Cardtest6] Gagal: Uang activator tidak cukup.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[Cardtest6] Gagal: {targetPlayer.NickName} tidak punya kartu {chosenSector}. Efek hangus.");
                        }
                    }
                }
                break;
            case TestingCardType.Cardtest7:
        {
            // --- PERBAIKAN LOGIKA SERVER ---
            // Kita pecah data dari string "SektorLawan|SektorKita|ActorIDLawan"
            string[] dataParts = chosenSector.Split('|');

            if (dataParts.Length < 3) 
            {
                Debug.LogError("[Cardtest7] Format data RPC salah atau korup.");
                return;
            }

            string swapTargetSector = dataParts[0]; // Sektor yang mau diambil
            string sourceSector = dataParts[1];     // Sektor yang mau dikasih
            int swapTargetActorId = int.Parse(dataParts[2]); // ID Pemain target

            Player swapTargetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(swapTargetActorId);

            if (swapTargetPlayer != null && !string.IsNullOrEmpty(sourceSector) && !string.IsNullOrEmpty(swapTargetSector))
            {
                Debug.Log($"[Cardtest7] {activator.NickName} menukar 1 {sourceSector} dengan 1 {swapTargetSector} milik {swapTargetPlayer.NickName}");

                // 1. Cek Ketersediaan Kartu
                string sourceKey = PlayerProfileMultiplayer.GetCardKeyFromColor(sourceSector);
                string targetKey = PlayerProfileMultiplayer.GetCardKeyFromColor(swapTargetSector);

                int activatorSourceCount = activator.CustomProperties.ContainsKey(sourceKey) ? (int)activator.CustomProperties[sourceKey] : 0;
                int targetTargetCount = swapTargetPlayer.CustomProperties.ContainsKey(targetKey) ? (int)swapTargetPlayer.CustomProperties[targetKey] : 0;

                if (activatorSourceCount > 0 && targetTargetCount > 0)
                {
                    // 2. Eksekusi Tukar
                    
                    // UPDATE ACTIVATOR: -1 Source, +1 Target
                    // Ambil jumlah kartu target yang dimiliki activator saat ini
                    int actTargetOld = activator.CustomProperties.ContainsKey(targetKey) ? (int)activator.CustomProperties[targetKey] : 0;
                    
                    Hashtable actProps = new Hashtable { 
                        { sourceKey, activatorSourceCount - 1 },
                        { targetKey, actTargetOld + 1 }
                    };
                    activator.SetCustomProperties(actProps);

                    // UPDATE TARGET PLAYER: -1 Target, +1 Source
                    // Ambil jumlah kartu source yang dimiliki target saat ini
                    int tgtSourceOld = swapTargetPlayer.CustomProperties.ContainsKey(sourceKey) ? (int)swapTargetPlayer.CustomProperties[sourceKey] : 0;

                    Hashtable tgtProps = new Hashtable {
                        { targetKey, targetTargetCount - 1 },
                        { sourceKey, tgtSourceOld + 1 }
                    };
                    swapTargetPlayer.SetCustomProperties(tgtProps);

                    Debug.Log("[Cardtest7] Pertukaran Berhasil!");
                }
                else
                {
                    Debug.LogWarning("[Cardtest7] Gagal: Salah satu pemain tidak memiliki kartu yang cukup.");
                }
            }
            break;
            }
        }
    }

    // --- PERBAIKAN: Fungsi ini sekarang menangani Cardtest1 dan Cardtest2 ---
    public void OnActivateButtonClicked()
    {
        Hashtable props = new Hashtable { { PlayerProfileMultiplayer.TESTING_CARD_USED_KEY, true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        int cardIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.TESTING_CARD_INDEX_KEY];
        TestingCardData cardData = testingCardsPool[cardIndex];
        swapSourceSector = "";
        swapTargetActorNumber = -1;

        if (interactiveButtonsPanel != null) interactiveButtonsPanel.SetActive(false);

        switch (cardData.cardType)
        {
            case TestingCardType.Cardtest1:
                if (sectorChoicePanel != null) sectorChoicePanel.SetActive(true);
                break;

            case TestingCardType.Cardtest2:
                playerHasMadeChoice = true;
                photonView.RPC("Rpc_ApplyTestingCardEffect", RpcTarget.MasterClient);
                break;
            case TestingCardType.Cardtest3:
            case TestingCardType.Cardtest4:
                if (sectorChoicePanel != null) sectorChoicePanel.SetActive(true);
                // Kita TIDAK memanggil Rpc_ApplyTestingCardEffect di sini lagi untuk kartu ini,
                // karena harus menunggu pemain memilih sektor dulu.
                break;
            case TestingCardType.Cardtest5:
                playerHasMadeChoice = true;
                photonView.RPC("Rpc_ApplyTestingCardEffect", RpcTarget.MasterClient);
                break;
            case TestingCardType.Cardtest6:
                // --- LOGIKA BARU CARDTEST6 ---
                StartTenderSelectionMode();
                break;
            case TestingCardType.Cardtest7:
                // LANGKAH 1: Tampilkan panel sektor untuk memilih saham SENDIRI yang mau ditukar
                if (sectorChoicePanel != null) 
                {
                    sectorChoicePanel.SetActive(true);
                    if (statusText != null) { statusText.text = "Pilih Sektor ANDA untuk ditukar"; statusText.gameObject.SetActive(true); }
                }
                break;

            default:
                playerHasMadeChoice = true;
                photonView.RPC("Rpc_ApplyTestingCardEffect", RpcTarget.MasterClient);
                break;
        }
    }
    private void StartTenderSelectionMode()
    {
        isInTenderMode = true;
        Debug.Log("[Cardtest6] Memulai mode pemilihan pemain.");

        // Animasikan container pemain online ke tengah (meminjam fungsi dari MultiplayerManager)
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.AnimatePlayerContainers(true);
        }
        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.alpha = 0f;
            containerCanvasGroup.blocksRaycasts = false; // Matikan interaksi
        }

        // Tampilkan tombol "Select" di atas kepala setiap pemain LAIN
        PlayerProfileMultiplayer[] profiles = FindObjectsOfType<PlayerProfileMultiplayer>();
        foreach (var profile in profiles)
        {
            // Jangan tampilkan tombol di diri sendiri
            if (profile.photonView.IsMine) continue;

            // Aktifkan tombol tender di profil tersebut
            
            profile.SetupTenderOfferButton(true);
        }
    }
    public void OnTargetPlayerSelected(Player targetPlayer)
    {
        if (!isInTenderMode) return;

        int cardIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.TESTING_CARD_INDEX_KEY];
        TestingCardData cardData = testingCardsPool[cardIndex];

        // Logika Cardtest6 (Tender Offer)
        if (cardData.cardType == TestingCardType.Cardtest6)
        {
            Debug.Log($"[Cardtest6] Target pemain dipilih: {targetPlayer.NickName}");
            Hashtable props = new Hashtable { { "TargetActorForCardtest6", targetPlayer.ActorNumber } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            CleanupTenderVisuals();
            if (sectorChoicePanel != null) sectorChoicePanel.SetActive(true);
        }
        // Logika Cardtest7 (Swap) - LANGKAH 2
        else if (cardData.cardType == TestingCardType.Cardtest7)
        {
            Debug.Log($"[Cardtest7] Target pemain dipilih: {targetPlayer.NickName}");
            swapTargetActorNumber = targetPlayer.ActorNumber;
            
            CleanupTenderVisuals();

            // Lanjut ke LANGKAH 3: Tampilkan panel sektor lagi untuk memilih sektor lawan
            if (sectorChoicePanel != null)
            {
                sectorChoicePanel.SetActive(true);
                if (statusText != null) { statusText.text = $"Pilih Sektor milik {targetPlayer.NickName} yang diinginkan"; statusText.gameObject.SetActive(true); }
            }
        }
    }
    private void CleanupTenderVisuals()
    {
        isInTenderMode = false;
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.AnimatePlayerContainers(false);
        }

        PlayerProfileMultiplayer[] profiles = FindObjectsOfType<PlayerProfileMultiplayer>();
        foreach (var profile in profiles)
        {
            profile.SetupTenderOfferButton(false);
        }
    }

    private void OnSectorChosenForPreview(string sectorName)
    {
        if (sectorChoicePanel != null) sectorChoicePanel.SetActive(false);

        // Cek kartu apa yang sedang aktif
        int cardIndex = (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.TESTING_CARD_INDEX_KEY];
        TestingCardData cardData = testingCardsPool[cardIndex];

        if (cardData.cardType == TestingCardType.Cardtest1)
        {
            // Logika Lama: Jalankan Animasi Preview
            StartCoroutine(PrivateRumorPreviewAnimation(sectorName));
        }
        else if (cardData.cardType == TestingCardType.Cardtest3 || cardData.cardType == TestingCardType.Cardtest4)
        {

            playerHasMadeChoice = true;

            // INILAH saat yang tepat memanggil RPC, setelah sektor dipilih
            photonView.RPC("Rpc_ApplyTestingCardEffect", RpcTarget.MasterClient, sectorName);
        }
        else if (cardData.cardType == TestingCardType.Cardtest6)
        {
            // --- LOGIKA CARDTEST6 ---
            // Kita sudah punya "TargetActorForCardtest6" di properties (dari langkah 2)
            // Sekarang kita kirim sektornya lewat RPC
            playerHasMadeChoice = true;
            photonView.RPC("Rpc_ApplyTestingCardEffect", RpcTarget.MasterClient, sectorName);
        }
        if (cardData.cardType == TestingCardType.Cardtest7)
    {
        // Cek apakah ini Langkah 1 atau Langkah 3?
        if (string.IsNullOrEmpty(swapSourceSector))
        {
            // INI LANGKAH 1: Pemain baru saja memilih sektor miliknya
            swapSourceSector = sectorName;
            Debug.Log($"[Cardtest7] Langkah 1 Selesai. Sektor Asal: {swapSourceSector}");

            // Lanjut ke LANGKAH 2: Pilih Pemain Target
            StartTenderSelectionMode(); 
            if (statusText != null) { statusText.text = "Pilih PEMAIN TARGET"; statusText.gameObject.SetActive(true); }
        }
        else
        {
            // INI LANGKAH 3: Pemain baru saja memilih sektor milik lawan
            Debug.Log($"[Cardtest7] Langkah 3 Selesai. Sektor Target: {sectorName}");
            
            playerHasMadeChoice = true;
            
            // --- PERBAIKAN DI SINI ---
            // Jangan pakai SetCustomProperties. Kita gabungkan data jadi satu string.
            // Format: "SektorLawan|SektorKita|ActorIDLawan"
            string packedData = $"{sectorName}|{swapSourceSector}|{swapTargetActorNumber}";

            photonView.RPC("Rpc_ApplyTestingCardEffect", RpcTarget.MasterClient, packedData);
        }
    }
    }
    private IEnumerator PrivateRumorPreviewAnimation(string sectorName)
    {
        isAnimating = true;
        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.alpha = 0f;
            containerCanvasGroup.blocksRaycasts = false; // Matikan interaksi
        }
        if (RumorPhaseManagerMultiplayer.Instance != null)
        {
            yield return StartCoroutine(RumorPhaseManagerMultiplayer.Instance.AnimatePrivateRumorPreview(sectorName));
        }
        if (containerCanvasGroup != null)
        {
            containerCanvasGroup.alpha = 1f;
            containerCanvasGroup.blocksRaycasts = true; // Hidupkan kembali interaksi
        }
        isAnimating = false;
        playerHasMadeChoice = true;
    }

    public void OnSkipButtonClicked()
    {
        // PERBAIKAN 1: Cegah skip paksa jika animasi sedang berjalan
        // Jika waktu habis tapi animasi masih jalan, biarkan animasi menyelesaikannya nanti.
        
        if (isAnimating)
        {
            Debug.Log("Sedang animasi, menunggu selesai sebelum skip.");
            return;
        }
        if (isInTenderMode)
        {
            CleanupTenderVisuals();
        }

        // PERBAIKAN 2: Pastikan panel pilihan sektor tertutup jika waktu habis saat memilih
        if (sectorChoicePanel != null && sectorChoicePanel.activeSelf)
        {
            sectorChoicePanel.SetActive(false);
        }

        // Logika standar
        playerHasMadeChoice = true;
        if (interactiveButtonsPanel != null) interactiveButtonsPanel.SetActive(false);

        if (botTestingCoroutine != null)
        {
            StopCoroutine(botTestingCoroutine);
            botTestingCoroutine = null;
        }
    }

    #region Interactive Flow (Semester 2, 3, 4)
    public void BeginTestingPhase()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            playersFinishedInteraction.Clear();
            Hashtable props = new Hashtable { { "AllPlayersFinishedTesting", false } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            Hashtable timerProps = new Hashtable { { TESTING_START_TIME_KEY, PhotonNetwork.Time } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(timerProps);

            if (testingCardsPool == null || testingCardsPool.Count == 0) return;
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.IsInactive)
                {
                    StartCoroutine(HandleDisconnectedPlayerTesting(p));
                }
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

            if (botTestingCoroutine != null) StopCoroutine(botTestingCoroutine); // Hentikan sisa
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerProfileMultiplayer.IS_BOT_MODE_KEY)
                && (bool)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.IS_BOT_MODE_KEY])
            {
                botTestingCoroutine = StartCoroutine(BotTestingCoroutine());
            }

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

    private IEnumerator BotTestingCoroutine()
    {
        Debug.Log("[Bot Mode] Testing: Menunggu 5 detik sebelum skip otomatis...");
        yield return new WaitForSeconds(5.0f);

        // Cek lagi setelah 5 detik
        bool isStillBot = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerProfileMultiplayer.IS_BOT_MODE_KEY) &&
                          (bool)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.IS_BOT_MODE_KEY];

        if (isStillBot && !playerHasMadeChoice) // Pastikan pemain belum memilih
        {
            Debug.Log("[Bot Mode] Testing: Waktu tunggu 5 detik selesai. Masih dalam mode bot. Otomatis skip.");
            OnSkipButtonClicked(); // OnSkipButtonClicked akan set playerHasMadeChoice = true
        }
        else
        {
            Debug.Log("[Bot Mode] Testing: Dibatalkan. Pemain kembali ke mode manual atau sudah memilih.");
        }
        botTestingCoroutine = null;
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
            Debug.Log("MasterClient: Semua pemain telah selesai interaksi Testing Card.");

            // Hentikan timer di semua klien
            photonView.RPC("Rpc_StopTestingTimerAndPhase", RpcTarget.All);

            // Lanjutkan ke fase berikutnya
            if (ActionPhaseManager.Instance != null)
            {
                ActionPhaseManager.Instance.ProceedToSellingPhaseAfterTesting();
            }
        }
    }
    [PunRPC]
    private void Rpc_StopTestingTimerAndPhase()
    {
        // Hentikan coroutine timer lokal
        if (testingTimerCoroutine != null)
        {
            StopCoroutine(testingTimerCoroutine);
            testingTimerCoroutine = null;
        }
        // Sembunyikan panel timer
        if (timerPanel != null)
        {
            timerPanel.SetActive(false);
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
    public void HandlePlayerDisconnect(Player disconnectedPlayer)
    {
        // Cek apakah fase testing sedang aktif dengan melihat properti timer
        object testingTimerProp;
        if (!PhotonNetwork.IsMasterClient ||
            !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(TESTING_START_TIME_KEY, out testingTimerProp) ||
            testingTimerProp == null)
        {
            // Fase testing tidak sedang aktif
            return;
        }

        // Cek apakah properti "AllPlayersFinishedTesting" sudah true
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("AllPlayersFinishedTesting") &&
            (bool)PhotonNetwork.CurrentRoom.CustomProperties["AllPlayersFinishedTesting"] == true)
        {
            return; // Fase sudah selesai diproses
        }

        int actorNumber = disconnectedPlayer.ActorNumber;

        // Cek apakah pemain ini sudah selesai SEBELUM disconnect
        if (playersFinishedInteraction == null || playersFinishedInteraction.Contains(actorNumber))
        {
            return; // Sudah selesai, tidak perlu ditangani
        }

        Debug.Log($"[TestingCardManager] {disconnectedPlayer.NickName} disconnect. Menandai sebagai selesai.");

        // Ini adalah logika yang sama dari Rpc_SignalInteractionComplete
        playersFinishedInteraction.Add(actorNumber);

        // Cek apakah semua pemain (termasuk yang disconnect) sudah selesai
        if (playersFinishedInteraction.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.Log("MasterClient: Semua pemain (termasuk disconnect) telah selesai.");

            // Tandai fase selesai di properti ruangan
            Hashtable props = new Hashtable { { "AllPlayersFinishedTesting", true } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            photonView.RPC("Rpc_StopTestingTimerAndPhase", RpcTarget.All);

            if (ActionPhaseManager.Instance != null)
            {
                ActionPhaseManager.Instance.ProceedToSellingPhaseAfterTesting();
            }
        }
    }
    private IEnumerator HandleDisconnectedPlayerTesting(Player disconnectedPlayer)
    {
        Debug.Log($"[TestingCardManager] Player {disconnectedPlayer.NickName} sudah disconnect. Menunggu 5 detik (Bot Mode) sebelum menandai selesai...");
        yield return new WaitForSeconds(5.0f);

        int actorNumber = disconnectedPlayer.ActorNumber;

        // Cek lagi jika MasterClient masih aktif dan pemain belum diproses
        if (!PhotonNetwork.IsMasterClient || playersFinishedInteraction == null || playersFinishedInteraction.Contains(actorNumber))
        {
            yield break; // Batalkan jika fase selesai atau sudah diproses
        }

        Debug.Log($"[TestingCardManager] Menandai {disconnectedPlayer.NickName} sebagai selesai (Disconnect Bot Mode).");

        // Panggil RPC yang sama dengan yang dipanggil oleh Bot Mode (OnSkipButtonClicked)
        photonView.RPC("Rpc_SignalInteractionComplete", RpcTarget.MasterClient, actorNumber);
    }
    public void ResumeTestingPhase()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Rekonstruksi siapa yang belum selesai
        if (playersFinishedInteraction == null) playersFinishedInteraction = new List<int>();

        // Kita tidak bisa tahu persis siapa yang sudah selesai hanya dari list lokal Host baru,
        // tapi kita bisa mengandalkan timer global.

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("testingStartTime", out object startTimeObj))
        {
            double startTime = (double)startTimeObj;
            double elapsed = PhotonNetwork.Time - startTime;
            float remainingTime = TESTING_TIME - (float)elapsed;

            if (remainingTime > 0)
            {
                StartCoroutine(MasterClientTestingMonitor(remainingTime));
            }
            else
            {
                // Waktu habis, anggap semua selesai
                Rpc_SignalInteractionComplete(0); // Trigger check completion
            }
        }
    }

    private IEnumerator MasterClientTestingMonitor(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("Rpc_StopTestingTimerAndPhase", RpcTarget.All);
            if (ActionPhaseManager.Instance != null)
            {
                ActionPhaseManager.Instance.ProceedToSellingPhaseAfterTesting();
            }
        }
    }
}