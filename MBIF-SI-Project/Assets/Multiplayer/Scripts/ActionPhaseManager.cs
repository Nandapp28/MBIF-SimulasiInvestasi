// File: ActionPhaseManager.cs (Versi Final dengan Logika Giliran Round-Robin)
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using TMPro;

[RequireComponent(typeof(PhotonView))]
public class ActionPhaseManager : MonoBehaviourPunCallbacks
{
    public static ActionPhaseManager Instance;

    [Header("Game Data")]
    public List<CardPoolEntry> allCardsPool;

    [Header("UI Setup")]
    public GameObject actionCardPrefab;
    public Transform cardContainer;
    public GameObject actionButtonsPanel;
    public Button toggleCardContainerButton;

    [Header("Action Buttons References")]
    public Button primaryActionButton;
    public TextMeshProUGUI primaryActionButtonText;
    public Button activateButton; // Referensi untuk tombol Activate
    public Button skipButton;     // Referensi untuk tombol Skip

    [Header("Turn Timer")]
    public GameObject localTimerPanel;
    public Image localTimerBar;
    public TextMeshProUGUI localTimerText;
    private Coroutine turnTimerCoroutine;
    private const float TURN_DURATION = 15.0f;
    private const float ACTION_DURATION = 20.0f;
    

    [Header("Layout")]
    public List<Transform> cardPositions;

    [Header("Trade Fee UI")]
    public GameObject tradeFeePanel; // Panel UI baru Anda
    public Transform tradeFeeContainer; // Tempat untuk menampung baris (Vertical Layout)
    public GameObject tradeFeeRowPrefab; // Prefab baris (SALINAN DARI SellingPhaseManager)
    public Button tradeFeeConfirmButton; // Tombol konfirmasi di panel baru
    private Dictionary<string, int> localTradeFeeInputs = new Dictionary<string, int>();
    // Di bagian State Variables:
    private bool isInFlashbuyMode = false;
    private int flashbuyActivatorActorNumber = -1; // Tambahkan ini untuk melacak siapa pengaktif
    private List<int> flashbuySelectedCardIds = new List<int>(); // Kartu yang dipilih di sesi Flashbuy
    private Coroutine flashbuyTimerCoroutine; // Untuk manajemen timer (opsional tapi disarankan)

    
    private string tenderOfferCardColor; // Kita masih butuh ini

    // Variabel State
    private List<Player> turnOrder;
    private int currentTurnIndex = -1;
    private int currentPlayerActorNumber = -1;
    private int cardsTaken = 0; // KEMBALI MENGGUNAKAN INI untuk melacak progres
    private int totalCardsOnTable = 0;
    private int consecutiveSkipCount = 0;
    private bool isInTenderOfferMode = false;
    private HashSet<int> disconnectedPlayerActorNumbers = new HashSet<int>();

    // Variabel Lokal UI & Data
    private int selectedCardId = -1;
    private Dictionary<int, CardMultiplayer> cardsOnTable = new Dictionary<int, CardMultiplayer>();
    private List<GameObject> instantiatedCards = new List<GameObject>();
    private GameObject currentlySelectedCardObject = null;
    private Vector3 defaultCardScale;
    

    #region Unity & Setup Methods
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); } else { Instance = this; }
    }

    void Start()
    {
        if (actionButtonsPanel != null) actionButtonsPanel.SetActive(false);
        if (localTimerPanel != null) localTimerPanel.SetActive(false);
        if (tradeFeePanel != null) tradeFeePanel.SetActive(false);
        if (toggleCardContainerButton != null)
        {
            toggleCardContainerButton.gameObject.SetActive(false); // Sembunyikan di awal
            toggleCardContainerButton.onClick.AddListener(OnToggleCardContainerClicked);
        }
    }
    

    public void StartActionPhase()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Player[] players = PhotonNetwork.PlayerList;
            turnOrder = players.OrderBy(p => (int)p.CustomProperties[PlayerProfileMultiplayer.TURN_ORDER_KEY]).ToList();

            cardsTaken = 0; // Reset penghitung kartu yang diambil
            totalCardsOnTable = PhotonNetwork.CurrentRoom.PlayerCount * 5;
            currentTurnIndex = -1;
            disconnectedPlayerActorNumbers.Clear();

            CreateDeck();
            AdvanceToNextTurn();
        }
    }

    private void CreateDeck()
    {
        if (allCardsPool.Count < totalCardsOnTable)
        {
            Debug.LogError($"GAGAL MEMBUAT DEK: Tidak cukup kartu di 'allCardsPool'.");
            return;
        }
        List<int> possibleIndices = Enumerable.Range(0, allCardsPool.Count).ToList();
        System.Random rnd = new System.Random();
        List<int> shuffledIndices = possibleIndices.OrderBy(x => rnd.Next()).ToList();
        List<int> cardIndicesToSend = shuffledIndices.Take(totalCardsOnTable).ToList();
        photonView.RPC("Rpc_SetupCardsOnTable", RpcTarget.All, cardIndicesToSend.ToArray());
    }
    #endregion

    #region Turn Management (Logika Baru)

    public static void SetPublicTurnTimer(bool isActive, Player player = null, float duration = TURN_DURATION)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Hashtable turnProps = new Hashtable();
        if (isActive && player != null)
        {
            turnProps[PlayerProfileMultiplayer.TURN_START_TIME_KEY] = PhotonNetwork.Time;
            turnProps[PlayerProfileMultiplayer.TURN_ACTOR_KEY] = player.ActorNumber;
            // Tambahkan durasi ke properti
            turnProps[PlayerProfileMultiplayer.TURN_DURATION_KEY] = duration;
        }
        else
        {
            turnProps[PlayerProfileMultiplayer.TURN_ACTOR_KEY] = -1;
        }
        PhotonNetwork.CurrentRoom.SetCustomProperties(turnProps);
    }
    private void AdvanceToNextTurn()
{
    if (!PhotonNetwork.IsMasterClient) return;
    HideAndResetSelection(); // Sembunyikan tombol normal jika ada

    // Pastikan tidak ada mode Flashbuy yang masih aktif di MasterClient
    if (isInFlashbuyMode && flashbuyActivatorActorNumber != -1) {
        Player activator = PhotonNetwork.CurrentRoom.GetPlayer(flashbuyActivatorActorNumber);
        // Cek apakah pengaktif flashbuy masih ada DAN tidak disconnect
        if (activator != null && !disconnectedPlayerActorNumbers.Contains(activator.ActorNumber)) {
            Debug.LogWarning($"[Flashbuy] MasterClient memajukan giliran karena Flashbuy belum selesai oleh {activator.NickName}.");
            photonView.RPC("Rpc_SubmitFlashbuyChoices", RpcTarget.MasterClient, new int[0]); // Paksa submit pilihan kosong
        }
    }

    // Fase berakhir jika semua kartu di meja sudah diambil
    if (cardsTaken >= totalCardsOnTable)
    {
        Debug.Log("✅ Semua kartu telah diambil. Transisi ke Fase Penjualan dalam 1.5 detik...");
        GameStatusUI.Instance.photonView.RPC("UpdateStatusText", RpcTarget.All, "Fase Aksi Selesai! Mempersiapkan Penjualan...");
        SetPublicTurnTimer(false);
        StartCoroutine(EndActionPhaseSequence());
        return;
    }

    // --- LOGIKA "SEMUA SKIP" YANG DIPERBAIKI ---
    // Hitung jumlah pemain yang masih aktif
    int activePlayers = turnOrder.Count - disconnectedPlayerActorNumbers.Count;
    
    // Jika tidak ada pemain aktif (semua disconnect), akhiri fase
    if (activePlayers <= 0 && turnOrder.Count > 0)
    {
         Debug.Log($"[All Disconnect] Semua pemain telah disconnect. Mengakhiri fase aksi.");
         consecutiveSkipCount = 0; 
         ClearAllRemainingCards(); 
         AdvanceToNextTurn(); // Panggil lagi untuk memicu 'cardsTaken'
         return;
    }

    // Jika jumlah skip >= jumlah pemain aktif (dan ada pemain aktif)
    // Ini hanya akan dipicu oleh Rpc_RequestSkipTurn (skip manual)
    if (consecutiveSkipCount >= activePlayers && activePlayers > 0)
    {
         Debug.Log($"[All Skip] Semua pemain aktif ({activePlayers} pemain) telah skip. Mengakhiri fase aksi.");
         consecutiveSkipCount = 0; 
         ClearAllRemainingCards(); 
         AdvanceToNextTurn(); // Panggil lagi untuk memicu 'cardsTaken'
         return;
    }

    // --- AKHIR LOGIKA "SEMUA SKIP" ---

    Player nextPlayer = null; // Inisialisasi ke null untuk perbaikan error CS0165
    int safetyBreak = 0; // Menghindari infinite loop
    
    do
    {
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        if (turnOrder.Count == 0) {
            Debug.LogError("[ActionPhaseManager] TurnOrder kosong! Mengakhiri fase.");
            safetyBreak = 999;
            break;
        }
        nextPlayer = turnOrder[currentTurnIndex];
        safetyBreak++;
        
        // Jika pemain berikutnya adalah pemain yang disconnect
        if (disconnectedPlayerActorNumbers.Contains(nextPlayer.ActorNumber))
        {
            Debug.Log($"[ActionPhaseManager] Melompati giliran {nextPlayer.NickName} (disconnect)...");
            
            // (Logika 'consecutiveSkipCount++' telah dihapus dari sini untuk memperbaiki bug)
        }

        if (safetyBreak > turnOrder.Count * 2)
        {
            Debug.LogError("[ActionPhaseManager] Terjebak di loop AdvanceToNextTurn! Mengakhiri fase paksa.");
            cardsTaken = totalCardsOnTable; // Paksa akhir fase
            AdvanceToNextTurn(); // Panggil lagi untuk memicu akhir
            return;
        }
        
    } while (disconnectedPlayerActorNumbers.Contains(nextPlayer.ActorNumber)); // Ulangi HANYA jika pemain disconnect
    // --- AKHIR LOGIKA LOOP ---

    // Cek null untuk keamanan jika 'break' terjadi
    if (nextPlayer != null)
    {
        SetPublicTurnTimer(true, nextPlayer, TURN_DURATION);
        GameStatusUI.Instance.photonView.RPC("UpdateStatusText", RpcTarget.All, $"Giliran {nextPlayer.NickName} untuk memilih kartu.");
        photonView.RPC("Rpc_SyncCurrentPlayerTurn", RpcTarget.All, nextPlayer.ActorNumber);
    }
    else
    {
        Debug.LogError("[ActionPhaseManager] GAGAL menemukan nextPlayer yang valid. Fase aksi mungkin macet.");
        // (Ini seharusnya tidak terjadi kecuali 'turnOrder' kosong)
    }
}

    public void ForceNextTurn()
    {
        // Fungsi ini hanya sebagai "pintu" publik yang aman
        // agar skrip lain bisa memicu pergantian giliran.
        if (PhotonNetwork.IsMasterClient)
        {
            AdvanceToNextTurn();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // --- MODIFIKASI --- Gunakan konstanta dari PlayerProfile
        if (propertiesThatChanged.ContainsKey(PlayerProfileMultiplayer.TURN_ACTOR_KEY))
        {
            StopLocalTimer();

            int turnActorNumber = (int)propertiesThatChanged[PlayerProfileMultiplayer.TURN_ACTOR_KEY];

            if (turnActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerProfileMultiplayer.IS_BOT_MODE_KEY)
                    && (bool)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.IS_BOT_MODE_KEY])
                {
                    Debug.Log("[Bot Mode] Action: Otomatis skip giliran.");
                    OnSkipTurnClicked(); // Langsung panggil skip
                    return; // Lewati sisa fungsi (jangan mulai timer)
                }

                // Tampilkan tombol Skip HANYA jika kita TIDAK sedang dalam mode Flashbuy
                if (skipButton != null && !isInFlashbuyMode) 
                {
                    skipButton.gameObject.SetActive(true);
                }
                float duration = propertiesThatChanged.ContainsKey(PlayerProfileMultiplayer.TURN_DURATION_KEY) 
                    ? (float)propertiesThatChanged[PlayerProfileMultiplayer.TURN_DURATION_KEY] 
                    : TURN_DURATION;
                // --- MODIFIKASI --- Mulai timer dengan durasi normal
                turnTimerCoroutine = StartCoroutine(StartLocalTurnTimer(duration));
            }
            else
            {
                if (localTimerPanel != null)
                {
                    localTimerPanel.SetActive(false);
                }
                if (skipButton != null)
                {
                    skipButton.gameObject.SetActive(false);
                }
            }
        }
    }

    private IEnumerator StartLocalTurnTimer(float duration)
    {
        if (localTimerPanel != null) localTimerPanel.SetActive(true);
        
        float timeLeft = duration;

        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            if (localTimerText != null)
            {
                localTimerText.text = Mathf.CeilToInt(timeLeft).ToString();
            }
            
            if (localTimerBar != null)
            {
                // --- MODIFIKASI --- Bagi dengan durasi yang benar
                localTimerBar.fillAmount = Mathf.Clamp01(timeLeft / duration);
            }

            yield return null; 
        }

        if (localTimerPanel != null) localTimerPanel.SetActive(false);

        Debug.Log("Waktu giliran habis! Otomatis skip.");
        BotModeManager.SetBotMode(true);
        OnSkipTurnClicked(); // Panggil fungsi skip
    }

    // --- BARU --- Fungsi helper untuk menghentikan timer lokal
    private void StopLocalTimer()
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
        }
        if (localTimerPanel != null)
        {
            localTimerPanel.SetActive(false);
        }
        if (skipButton != null)
        {
            skipButton.gameObject.SetActive(false);
        }
    }

    [PunRPC]
    private void Rpc_SyncCurrentPlayerTurn(int actorNumber)
    {
        this.currentPlayerActorNumber = actorNumber;
    }
    #endregion

    #region Tender Offer Logic

    [PunRPC]
    private void Rpc_RequestTenderOfferTarget(int[] validTargetActorNumbers, string cardColorStr, int activatorActorNumber)
    {
        // Logika lama memanggil animasi untuk SEMUA pemain di sini

        // Hanya pemain pengaktif yang memunculkan tombol DAN animasi
        if (PhotonNetwork.LocalPlayer.ActorNumber == activatorActorNumber)
        {
            // --- PINDAHKAN LOGIKA ANIMASI KE SINI ---
            if (MultiplayerManager.Instance != null)
            {
                MultiplayerManager.Instance.AnimatePlayerContainers(true);
            }
            // --- AKHIR PEMINDAHAN ---

            isInTenderOfferMode = true;
            tenderOfferCardColor = cardColorStr; 
            List<int> validTargetsList = new List<int>(validTargetActorNumbers);
            
            PlayerProfileMultiplayer[] allPlayerProfiles = FindObjectsOfType<PlayerProfileMultiplayer>();
            foreach (PlayerProfileMultiplayer profile in allPlayerProfiles)
            {
                if (profile.photonView.Owner != null && validTargetsList.Contains(profile.photonView.Owner.ActorNumber))
                {
                    profile.SetupTenderOfferButton(true);
                }
            }

            StopLocalTimer();
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(true);
            }
            turnTimerCoroutine = StartCoroutine(StartLocalTurnTimer(ACTION_DURATION));
        }
        else
        {
            // Pemain lain tidak melakukan apa-apa (sesuai permintaan "hanya hidecard")
            // "hidecard" sudah diurus oleh Rpc_SetActionPhaseUIVisibility(false) di CardEffectManagerMultiplayer
            return;
        }
    }
    public void OnTenderOfferTargetClicked(Player targetPlayer)
    {
        StopLocalTimer();

        // Kirim pilihan ke server
        photonView.RPC("Rpc_SubmitTenderOfferTarget", RpcTarget.MasterClient, targetPlayer.ActorNumber, tenderOfferCardColor);
    }
    // RPC ini berjalan di MasterClient, menerima pilihan final dari pemain
   [PunRPC]
    private void Rpc_SubmitTenderOfferTarget(int targetActorNumber, string cardColor, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player activator = info.Sender;
        if (activator == null) return;
        Player target = PhotonNetwork.CurrentRoom.GetPlayer(targetActorNumber);
        if (target == null)
        {
            Debug.Log($"[Tender Offer] {activator.NickName} skip (atau target tidak valid). Melanjutkan giliran.");
            consecutiveSkipCount = 0; // Tetap reset skip count karena ini adalah "aksi"
            photonView.RPC("Rpc_CleanupTenderOfferVisuals", RpcTarget.All);
            AdvanceToNextTurn();
            return; // Hentikan eksekusi di sini
        }
        consecutiveSkipCount = 0;

        // --- LOGIKA BARU: BELI DARI BANK ---
        Debug.Log($"[Tender Offer] MasterClient memproses: {activator.NickName} membeli kartu {cardColor} (diaktifkan oleh {target.NickName}).");

        // 1. Hitung harga pembelian (setengah dari harga jual penuh)
        int fullPrice = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(cardColor);
        int purchasePrice = Mathf.CeilToInt(fullPrice / 2.0f);

        // 2. Ambil data Investpoint dan kartu aktivator
        int activatorInvestPoin = (int)activator.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];
        string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(cardColor);
        int activatorCardCount = activator.CustomProperties.ContainsKey(cardKey) ? (int)activator.CustomProperties[cardKey] : 0;
        int targetCardCount = target.CustomProperties.ContainsKey(cardKey) ? (int)target.CustomProperties[cardKey] : 0;
        // Ambil juga InvestPoin target
        int targetInvestPoin = (int)target.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];
        // 3. Validasi jika pengaktif mampu membayar
        if (activatorInvestPoin >= purchasePrice)
        {
            // 4. Siapkan properti baru untuk AKTIVATOR SAJA
            Hashtable activatorProps = new Hashtable
            {
                { PlayerProfileMultiplayer.INVESTPOINT_KEY, activatorInvestPoin - purchasePrice },
                { cardKey, activatorCardCount + 1 } // Dapat 1 kartu baru
            };

            // 5. Kirim pembaruan ke jaringan
            activator.SetCustomProperties(activatorProps);

            Hashtable targetProps = new Hashtable
            {
                { PlayerProfileMultiplayer.INVESTPOINT_KEY, targetInvestPoin + purchasePrice }, // Target menerima uang
                { cardKey, targetCardCount - 1 } // Kehilangan 1 kartu
            };
            target.SetCustomProperties(targetProps);
            Debug.Log($"[Tender Offer] Transaksi berhasil. {activator.NickName} membayar {purchasePrice} InvestPoin dan mendapat 1 kartu {cardColor}.");
        }
        else
        {
            Debug.LogWarning($"[Tender Offer] Transaksi dibatalkan oleh server, Investpoint {activator.NickName} tidak cukup.");
        }

        // 6. Perintahkan SEMUA klien untuk membersihkan visual
        photonView.RPC("Rpc_CleanupTenderOfferVisuals", RpcTarget.All);

        AdvanceToNextTurn();
    }
    private void CleanupTenderOfferButtons()
    {
        // --- INI LOGIKA BARUNYA ---
        // Temukan semua profil dan nonaktifkan tombol mereka
        PlayerProfileMultiplayer[] allPlayerProfiles = FindObjectsOfType<PlayerProfileMultiplayer>();
        foreach (PlayerProfileMultiplayer profile in allPlayerProfiles)
        {
            // Suruh profil untuk MENONAKTIFKAN tombolnya
            profile.SetupTenderOfferButton(false);
        }
        // --- AKHIR LOGIKA BARU ---
    }
    [PunRPC]
    private void Rpc_CleanupTenderOfferVisuals()
    {
        // Logika lama memanggil animasi untuk SEMUA pemain

        // --- PERUBAHAN LOGIKA ---
        // Hanya pemain LOKAL yang sedang dalam mode Tender Offer yang mengembalikan animasi
        // (yaitu, si pengaktif)
        if (isInTenderOfferMode)
        {
            if (MultiplayerManager.Instance != null)
            {
                MultiplayerManager.Instance.AnimatePlayerContainers(false);
            }
        }

        isInTenderOfferMode = false; // Reset state untuk semua pemain

        // SEMUA pemain (termasuk non-aktivator) memunculkan kembali container kartu
        if (cardContainer != null)
        {
            cardContainer.gameObject.SetActive(true);
        }

        // Hanya pemain LOKAL yang secara fisik memiliki tombol yang membersihkannya
        CleanupTenderOfferButtons();
    }
    #endregion

    #region Trade Fee Logic

    [PunRPC]
    private void Rpc_RequestTradeFeeInput()
    {
        // 1. Setup Panel
        tradeFeePanel.SetActive(true);
        localTradeFeeInputs.Clear();
        foreach (Transform child in tradeFeeContainer) Destroy(child.gameObject);

        Player localPlayer = PhotonNetwork.LocalPlayer;
        string[] colors = { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };

        // 2. Buat setiap baris (row)
        for (int i = 0; i < colors.Length; i++)
        {
            string colorName = colors[i];
            string colorKey = PlayerProfileMultiplayer.GetCardKeyFromColor(colorName);
            int ownedCards = localPlayer.CustomProperties.ContainsKey(colorKey) ? (int)localPlayer.CustomProperties[colorKey] : 0;

            localTradeFeeInputs[colorName] = 0; // Inisialisasi
            GameObject row = Instantiate(tradeFeeRowPrefab, tradeFeeContainer);

            // 3. Ambil referensi dari prefab (Gunakan Find, asumsi nama komponen sama dgn prefab penjualan)
            Text colorLabel = row.transform.Find("ColorLabel").GetComponent<Text>();
            Text priceLabel = row.transform.Find("PriceLabel").GetComponent<Text>();
            Text valueText = row.transform.Find("ValueText").GetComponent<Text>();
            Button plusButton = row.transform.Find("PlusButton").GetComponent<Button>();
            Button minusButton = row.transform.Find("MinusButton").GetComponent<Button>();

            // 4. Isi data
            if (colorLabel) colorLabel.text = colorName;
            if (priceLabel) priceLabel.text = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(colorName).ToString();
            if (valueText) valueText.text = "0";

            // 5. Atur Listeners
            if (plusButton != null) plusButton.interactable = true;
            if (minusButton != null) minusButton.interactable = true;
            plusButton.onClick.AddListener(() =>
            {
                if (localTradeFeeInputs[colorName] < ownedCards)
                {
                    localTradeFeeInputs[colorName]++;
                    if (valueText) valueText.text = localTradeFeeInputs[colorName].ToString();
                }
            });
            minusButton.onClick.AddListener(() =>
            {
                if (localTradeFeeInputs[colorName] > 0)
                {
                    localTradeFeeInputs[colorName]--;
                    if (valueText) valueText.text = localTradeFeeInputs[colorName].ToString();
                }
            });
        }

        // 6. Setup Tombol Konfirmasi
        tradeFeeConfirmButton.gameObject.SetActive(true);
        tradeFeeConfirmButton.interactable = true;
        tradeFeeConfirmButton.onClick.RemoveAllListeners();
        tradeFeeConfirmButton.onClick.AddListener(OnTradeFeeConfirm); // Panggil fungsi di bawah

        // 7. Mulai Timer
        StopLocalTimer();
        turnTimerCoroutine = StartCoroutine(StartLocalTurnTimer(ACTION_DURATION));
    }

    // Fungsi ini dipanggil dari tombol konfirmasi di UI baru
    public void OnTradeFeeConfirm()
    {
        StopLocalTimer();

        // Kumpulkan semua input dari dictionary
        Hashtable sellDecision = new Hashtable();
        foreach (var entry in localTradeFeeInputs)
        {
            if (entry.Value > 0) sellDecision.Add(entry.Key, entry.Value);
        }

        Debug.Log($"[Trade Fee] Mengkonfirmasi penjualan. Mengirim {sellDecision.Count} entri ke MasterClient...");
        photonView.RPC("Rpc_SubmitTradeFeeDecision", RpcTarget.MasterClient, sellDecision);
        
        tradeFeePanel.SetActive(false);
    }

    // RPC ini berjalan di MasterClient, menerima pilihan final dari pemain
    [PunRPC]
    private void Rpc_SubmitTradeFeeDecision(Hashtable sellDecision, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player activator = info.Sender;
        if (activator == null) return;
        
        consecutiveSkipCount = 0;
        Debug.Log($"[Trade Fee] MasterClient memproses: {activator.NickName} menjual {sellDecision.Count} jenis kartu.");

        int totalEarnings = 0;
        Hashtable propsToSet = new Hashtable();

        // Loop melalui Hashtable keputusan
        foreach (var entry in sellDecision)
        {
            string colorName = (string)entry.Key;
            int quantityToSell = (int)entry.Value;
            if (quantityToSell <= 0) continue;

            // Dapatkan harga jual penuh saat ini
            int pricePerCard = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(colorName);
            totalEarnings += quantityToSell * pricePerCard;

            // Siapkan untuk mengurangi kartu
            string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(colorName);
            int currentCards = (int)activator.CustomProperties[cardKey];
            propsToSet[cardKey] = currentCards - quantityToSell;
        }

        // Tambahkan pendapatan ke InvestPoin
        int currentInvestpoint = (int)activator.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];
        propsToSet[PlayerProfileMultiplayer.INVESTPOINT_KEY] = currentInvestpoint + totalEarnings;

        // Kirim pembaruan ke jaringan
        activator.SetCustomProperties(propsToSet);
        Debug.Log($"[Trade Fee] Transaksi berhasil. {activator.NickName} mendapatkan {totalEarnings} InvestPoin.");
        
        AdvanceToNextTurn();
    }

    #endregion

    #region Flashbuy Logic
    [PunRPC]
    private void Rpc_StartFlashbuyMode(int activatorActorNumber)
    {
        // Mode ini aktif untuk semua pemain, tapi hanya pengaktif yang bisa berinteraksi
        this.currentPlayerActorNumber = activatorActorNumber; // Pastikan giliran diset ke pengaktif
        this.flashbuyActivatorActorNumber = activatorActorNumber;

        if (PhotonNetwork.LocalPlayer.ActorNumber == activatorActorNumber)
        {
            isInFlashbuyMode = true;
            flashbuySelectedCardIds.Clear();

            Debug.Log($"[Flashbuy] Anda mengaktifkan Flashbuy! Pilih 2 kartu GRATIS.");
            if (actionButtonsPanel != null) actionButtonsPanel.SetActive(true);
            if (primaryActionButtonText != null) primaryActionButtonText.text = "Confirm Selection";
            if (activateButton != null) activateButton.gameObject.SetActive(false);
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(false); 
            }
            UpdateFlashbuyAffordability();

            StopLocalTimer();
            turnTimerCoroutine = StartCoroutine(StartLocalTurnTimer(ACTION_DURATION));
        }
        else
        {
            Player activatorPlayer = PhotonNetwork.CurrentRoom.GetPlayer(activatorActorNumber);
            if (activatorPlayer != null && GameStatusUI.Instance != null)
            {
                GameStatusUI.Instance.photonView.RPC("UpdateStatusText", RpcTarget.All, $"{activatorPlayer.NickName} mengaktifkan Flashbuy! Dia akan memilih kartu.");
            }
        }
    }

    [PunRPC]
    private void Rpc_SubmitFlashbuyChoices(int[] chosenCardIds, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Player activator = info.Sender;

        // ... (Validasi tetap sama) ...
        if (activator.ActorNumber != flashbuyActivatorActorNumber || chosenCardIds.Length > 2)
        {
            Debug.LogError($"[Flashbuy Bug] Validasi gagal untuk {activator.NickName}.");
            return;
        }

        // Hitung total biaya DULU
        int totalCost = 0;
        foreach (int cardId in chosenCardIds)
        {
            CardMultiplayer cardData = GetCardFromTable(cardId);
            if (cardData != null)
            {
                int fullPrice = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(cardData.color.ToString());
                totalCost += cardData.baseValue + fullPrice;
            }
        }

        int currentInvestpoint = (int)activator.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];

        // Cek apakah pemain mampu membayar
        if (currentInvestpoint >= totalCost)
        {
            Debug.Log($"[Flashbuy] MasterClient memproses pilihan {chosenCardIds.Length} kartu dari {activator.NickName} seharga {totalCost} InvestPoin.");
            consecutiveSkipCount = 0;
            Hashtable playerPropsToUpdate = new Hashtable();
            playerPropsToUpdate[PlayerProfileMultiplayer.INVESTPOINT_KEY] = currentInvestpoint - totalCost;

            foreach (int cardId in chosenCardIds)
            {
                CardMultiplayer cardData = GetCardFromTable(cardId);
                if (cardData != null)
                {
                    string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(cardData.color.ToString());
                    if (!string.IsNullOrEmpty(cardKey))
                    {
                        // SOLUSI: Cek dulu di data sementara (playerPropsToUpdate), baru ke data asli.
                        int currentCards = 0;
                        if (playerPropsToUpdate.ContainsKey(cardKey))
                        {
                            // Jika sudah ada di transaksi ini, gunakan nilai itu.
                            currentCards = (int)playerPropsToUpdate[cardKey];
                        }
                        else if (activator.CustomProperties.ContainsKey(cardKey))
                        {
                            // Jika tidak, baru ambil dari data asli pemain.
                            currentCards = (int)activator.CustomProperties[cardKey];
                        }

                        // Tambahkan 1 ke nilai yang benar.
                        playerPropsToUpdate[cardKey] = currentCards + 1;
                    }
                    photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);
                    cardsTaken++;
                }
            }

            // Update properti pemain SATU KALI dengan semua perubahan
            activator.SetCustomProperties(playerPropsToUpdate);

            // Reset state dan lanjutkan giliran HANYA JIKA SUKSES
            this.isInFlashbuyMode = false;
            this.flashbuyActivatorActorNumber = -1;
            AdvanceToNextTurn(); // <-- TAMBAHKAN KEMBALI BARIS INI
        }
        else
        {
            // Jika gagal, kirim notifikasi dan jangan lanjutkan giliran (ini sudah benar)
            Debug.LogWarning($"[Flashbuy] {activator.NickName} tidak mampu membayar...");
            photonView.RPC("Rpc_FlashbuyFailed", activator);
        }
    }

    [PunRPC]
    private void Rpc_FlashbuyFailed()
    {
        // Pastikan ini hanya berjalan untuk pemain yang mengaktifkan Flashbuy
        if (!isInFlashbuyMode || PhotonNetwork.LocalPlayer.ActorNumber != flashbuyActivatorActorNumber) return;

        Debug.LogError("Pembelian Flashbuy GAGAL: InvestPoin tidak cukup. Silakan pilih lagi.");

        // Tampilkan notifikasi kepada pemain.
        // Anda bisa menggunakan sistem notifikasi UI yang sudah ada, atau buat yang simpel.
        if (GameStatusUI.Instance != null)
        {
            GameStatusUI.Instance.ShowTemporaryNotification("Pembelian Gagal! Poin tidak cukup.", 3.0f);
        }

        // Beri pemain kesempatan memilih lagi dengan mengosongkan pilihan sebelumnya
        // dan memperbarui tampilan tombol.
        foreach (int cardId in flashbuySelectedCardIds)
        {
            GameObject cardObject = instantiatedCards.ElementAtOrDefault(cardId);
            if (cardObject != null)
            {
                cardObject.transform.localScale = defaultCardScale;
            }
        }
        flashbuySelectedCardIds.Clear();
        UpdateFlashbuyAffordability(); // Perbarui status tombol 'Confirm'
    }

    private void UpdateFlashbuyAffordability()
    {
        // Pastikan kita berada dalam mode Flashbuy dan ini adalah giliran kita
        if (!isInFlashbuyMode || PhotonNetwork.LocalPlayer.ActorNumber != flashbuyActivatorActorNumber)
        {
            return;
        }

        // Hitung total biaya dari kartu yang dipilih
        int totalCost = 0;
        foreach (int cardId in flashbuySelectedCardIds)
        {
            CardMultiplayer cardData = GetCardFromTable(cardId);
            if (cardData != null)
            {
                // Ambil harga pasar saat ini
                int marketPrice = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(cardData.color.ToString());
                totalCost += cardData.baseValue + marketPrice;
            }
        }
        if (primaryActionButtonText != null)
        {
            primaryActionButtonText.text = $"Confirm Selection [{totalCost}]";
        }

        // Ambil InvestPoin pemain saat ini
        int currentInvestPoin = (int)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];

        // Bandingkan dan atur status tombol
        if (currentInvestPoin >= totalCost)
        {
            primaryActionButton.interactable = true;
            Debug.Log($"[Flashbuy Check] Biaya: {totalCost}, Uang: {currentInvestPoin}. Cukup untuk membeli.");
        }
        else
        {
            primaryActionButton.interactable = false;
            Debug.LogWarning($"[Flashbuy Check] Biaya: {totalCost}, Uang: {currentInvestPoin}. TIDAK cukup untuk membeli!");
        }
    }

    private void ExitFlashbuyMode()
    {
        isInFlashbuyMode = false;
        flashbuyActivatorActorNumber = -1; // Reset pengaktif
        flashbuySelectedCardIds.Clear(); // Kosongkan daftar pilihan

        if (primaryActionButtonText != null) primaryActionButtonText.text = "Save";
        if (activateButton != null) activateButton.gameObject.SetActive(true);
        if (skipButton != null) skipButton.gameObject.SetActive(false);
        if (actionButtonsPanel != null) actionButtonsPanel.SetActive(false);

        foreach (GameObject cardObject in instantiatedCards)
        {
            if (cardObject != null)
            {
                cardObject.transform.localScale = defaultCardScale;
            }
        }
        // Pastikan tidak ada kartu yang masih "dipilih" secara visual dari Flashbuy.
        currentlySelectedCardObject = null;
        selectedCardId = -1;
    }

    public void OnSkipTurnClicked()
    {
        StopLocalTimer();
        if (isInFlashbuyMode && PhotonNetwork.LocalPlayer.ActorNumber == flashbuyActivatorActorNumber)
        {
            Debug.Log("[Flashbuy] Pemain mengklik Skip di mode Flashbuy.");
            // Kirim pilihan kosong ke MasterClient (Artinya tidak memilih kartu)
            photonView.RPC("Rpc_SubmitFlashbuyChoices", RpcTarget.MasterClient, new int[0]);
            ExitFlashbuyMode(); // Keluar dari mode di klien
        }
        else if (tradeFeePanel != null && tradeFeePanel.activeInHierarchy)
        {
            Debug.Log("[Trade Fee] Pemain memilih untuk skip (atau waktu habis).");
            tradeFeePanel.SetActive(false); // Tutup panel
            OnTradeFeeConfirm(); // Kirim skip normal
        }
// --- PERBAIKAN BUG 2: TAMBAHKAN BLOK INI ---
        else if (isInTenderOfferMode)
        {
            Debug.Log("[Tender Offer] Pemain memilih untuk skip (atau waktu habis).");
            
            // Kirim RPC dengan target -1 untuk menandakan "skip"
            // Server (yang sudah kita ubah) akan menanganinya dengan benar.
            photonView.RPC("Rpc_SubmitTenderOfferTarget", RpcTarget.MasterClient, -1, tenderOfferCardColor);
        }
        else // Skip normal
        {
            Debug.Log("Tombol Skip diklik.");
            photonView.RPC("Rpc_RequestSkipTurn", RpcTarget.MasterClient);
            HideAndResetSelection();
        }
    }

    [PunRPC]
    private void Rpc_RequestSkipTurn(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Pastikan yang meminta adalah pemain yang sedang giliran
        if (info.Sender.ActorNumber == this.currentPlayerActorNumber)
        {
            Debug.Log($"[Skip] {info.Sender.NickName} melewati gilirannya.");

            
            
            consecutiveSkipCount++; // Tambahkan skip count
            Debug.Log($"[Skip] Skip count: {consecutiveSkipCount}/{turnOrder.Count}");

            // Cek apakah semua pemain sudah skip
            if (consecutiveSkipCount >= turnOrder.Count)
            {
                Debug.Log($"[All Skip] Semua pemain telah skip secara berurutan. Mengakhiri fase aksi.");
                consecutiveSkipCount = 0; // Reset
                ClearAllRemainingCards(); // Hapus semua kartu
            }

            // Panggil giliran berikutnya (yang akan cek 'cardsTaken' dan end phase jika perlu)
            AdvanceToNextTurn();
        }
    }
    #endregion

    #region Insider Trade Logic
    // RPC BARU: Untuk menyembunyikan atau menampilkan UI fase aksi untuk semua pemain.
    [PunRPC]
    private void Rpc_SetActionPhaseUIVisibility(bool isVisible)
    {
        if (cardContainer != null)
        {
            cardContainer.gameObject.SetActive(isVisible);
        }
        if (toggleCardContainerButton != null)
        {
            toggleCardContainerButton.gameObject.SetActive(isVisible);
        }

        // Jika UI disembunyikan, pastikan panel tombol juga ikut tersembunyi.
        if (!isVisible && actionButtonsPanel != null)
        {
            actionButtonsPanel.SetActive(false);
        }
    }

    // RPC BARU: Menerima sinyal dari pemain bahwa animasi Insider Trade telah selesai.
    [PunRPC]
    private void Rpc_SignalInsiderTradeAnimationComplete(PhotonMessageInfo info)
    {
        // Hanya MasterClient yang perlu menanggapi sinyal ini.
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"[MasterClient] Menerima sinyal dari {info.Sender.NickName} bahwa animasi Insider Trade telah selesai.");

            // 1. Tampilkan kembali UI fase aksi untuk semua pemain.
            photonView.RPC("Rpc_SetActionPhaseUIVisibility", RpcTarget.All, true);

            // 2. Lanjutkan permainan ke giliran berikutnya.
            AdvanceToNextTurn();
        }
    }

    #endregion


    #region Player Actions
    public void OnCardSelected(int cardId)
    {
        // Hanya pemain pengaktif Flashbuy yang bisa memilih kartu dalam mode ini
        if (isInFlashbuyMode)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber != flashbuyActivatorActorNumber) return; // Bukan giliran Anda

            GameObject cardObject = instantiatedCards.ElementAtOrDefault(cardId);
            // Penting: Pastikan kartu valid dan belum 'nullified' secara visual atau data
            if (cardObject == null || !cardsOnTable.ContainsKey(cardId)) return;

            if (flashbuySelectedCardIds.Contains(cardId))
            {
                // Deselect kartu
                flashbuySelectedCardIds.Remove(cardId);
                cardObject.transform.localScale = defaultCardScale;
            }
            else
            {
                // Pilih kartu baru jika belum mencapai batas 2 kartu
                if (flashbuySelectedCardIds.Count < 2)
                {
                    flashbuySelectedCardIds.Add(cardId);
                    cardObject.transform.localScale = defaultCardScale * 1.1f;
                }
                else
                {
                    Debug.LogWarning("Anda hanya bisa memilih maksimal 2 kartu untuk Flashbuy.");
                }
            }
            UpdateFlashbuyAffordability();
            return;
        }


        // Pastikan ini adalah giliran pemain lokal
        if (PhotonNetwork.LocalPlayer.ActorNumber == this.currentPlayerActorNumber)
        {
            GameObject clickedCardObject = instantiatedCards.ElementAtOrDefault(cardId);
            // Hentikan jika kartu tidak valid atau jika mengklik kartu yang sama lagi
            if (clickedCardObject == null || currentlySelectedCardObject == clickedCardObject) return;

            // Jika ada kartu lain yang sedang dipilih, kembalikan ukurannya
            if (currentlySelectedCardObject != null)
            {
                currentlySelectedCardObject.transform.localScale = defaultCardScale;
            }

            // Tetapkan kartu yang baru diklik sebagai kartu yang dipilih saat ini
            currentlySelectedCardObject = clickedCardObject;
            // Perbesar ukurannya
            clickedCardObject.transform.localScale = defaultCardScale * 1.1f;
            this.selectedCardId = cardId;

            // Tampilkan panel tombol aksi
            actionButtonsPanel.SetActive(true);
        }
    }


    public void OnPrimaryActionButtonClicked()
    {
        StopLocalTimer();

        if (isInFlashbuyMode)
        {
            OnConfirmFlashbuySelection();
        }
        else
        {
            // Logika save kartu normal
            if (selectedCardId != -1 && PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerActorNumber)
            {
                photonView.RPC("RequestSaveCard", RpcTarget.MasterClient, selectedCardId, PhotonNetwork.LocalPlayer);
                HideAndResetSelection(); // Reset UI setelah mengirim permintaan
            }
        }
    }
    public void OnToggleCardContainerClicked()
    {
        if (cardContainer == null) return;

        // 1. Dapatkan status visibilitas BARU (apa yang akan terjadi)
        bool isNowVisible = !cardContainer.gameObject.activeInHierarchy;
        cardContainer.gameObject.SetActive(isNowVisible);

        // 2. Reset seleksi kartu (sesuai permintaan)
        // Ini akan selalu menyembunyikan panel tombol, sesuai logika lama.
        HideAndResetSelection();

        // --- TAMBAHAN SOLUSI ---
        // 3. Jika panel kartu baru saja DITAMPILKAN (isNowVisible == true)
        //    DAN kita sedang dalam mode Flashbuy...
        if (isNowVisible && isInFlashbuyMode && actionButtonsPanel != null)
        {
            // ...kita harus paksa panel tombol aksi untuk muncul kembali,
            //    khusus untuk pemain yang sedang giliran.
            if (PhotonNetwork.LocalPlayer.ActorNumber == flashbuyActivatorActorNumber)
            {
                actionButtonsPanel.SetActive(true);
            }
        }
    }

    // Buat fungsi baru ini untuk menangani konfirmasi Flashbuy
    private void OnConfirmFlashbuySelection()
    {
        StopLocalTimer();
        
        if (!isInFlashbuyMode) return; // Pastikan kita memang dalam mode Flashbuy
        if (PhotonNetwork.LocalPlayer.ActorNumber != flashbuyActivatorActorNumber) return; // Hanya pengaktif yang bisa konfirmasi

        Debug.Log($"[Flashbuy] Mengkonfirmasi pilihan: {flashbuySelectedCardIds.Count} kartu.");
        
        // Kirim pilihan kartu ke MasterClient
        photonView.RPC("Rpc_SubmitFlashbuyChoices", RpcTarget.MasterClient, flashbuySelectedCardIds.ToArray());
        
        // Keluar dari mode flashbuy di sisi klien setelah mengirim data
        ExitFlashbuyMode();
    }

    public void OnActivateButtonClicked()
    {
        StopLocalTimer();
        if (selectedCardId == -1) return;
        

        // Ambil data kartu yang dipilih untuk memeriksa namanya
        CardMultiplayer cardData = GetCardFromTable(selectedCardId);
        if (cardData == null) return;

        // Kirim permintaan aktivasi ke server
        photonView.RPC("RequestActivateCard", RpcTarget.MasterClient, selectedCardId, PhotonNetwork.LocalPlayer);

        // Cek nama kartu. Hanya sembunyikan panel untuk kartu yang efeknya instan.
        string cardName = cardData.cardName;
        if (cardName == "StockSplit" || cardName == "InsiderTrade" || cardName == "TenderOffer" || cardName == "TradeFee")
        {
            HideAndResetSelection();
        }
    }

    private void HideAndResetSelection()
    {
        if (actionButtonsPanel != null) actionButtonsPanel.SetActive(false);
        if (currentlySelectedCardObject != null)
        {
            // Gunakan skala default untuk mereset
            currentlySelectedCardObject.transform.localScale = defaultCardScale;
            currentlySelectedCardObject = null;
        }
        selectedCardId = -1;
    }
    #endregion

    #region RPC Handlers
    [PunRPC]
    private void RequestSaveCard(int cardId, Player requestingPlayer)
    {
        if (!PhotonNetwork.IsMasterClient || requestingPlayer.ActorNumber != this.currentPlayerActorNumber) return;
        CardMultiplayer cardData = GetCardFromTable(cardId);
        if (cardData == null) return;

        // --- PERUBAHAN LOGIKA BIAYA ---
        int fullPrice = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(cardData.color.ToString());
        int totalCost = cardData.baseValue + fullPrice;
        int currentInvestpoint = (int)requestingPlayer.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];

        if (currentInvestpoint >= totalCost)
        {
            consecutiveSkipCount = 0;
            Hashtable propsToSet = new Hashtable();
            // Kurangi INVESTPOINT, bukan FINPOINT
            propsToSet.Add(PlayerProfileMultiplayer.INVESTPOINT_KEY, currentInvestpoint - totalCost);
        // --- AKHIR PERUBAHAN ---

            string cardColorKey = PlayerProfileMultiplayer.GetCardKeyFromColor(cardData.color.ToString());
            if (!string.IsNullOrEmpty(cardColorKey))
            {
                int currentCardCount = 0;
                if (requestingPlayer.CustomProperties.ContainsKey(cardColorKey))
                    currentCardCount = (int)requestingPlayer.CustomProperties[cardColorKey];
                propsToSet.Add(cardColorKey, currentCardCount + 1);
            }
            requestingPlayer.SetCustomProperties(propsToSet);

            cardsTaken++;
            photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);
            AdvanceToNextTurn();
        }
        else
        {
            Debug.LogWarning($"[SAVE GAGAL] {requestingPlayer.NickName} tidak punya cukup InvestPoin (butuh {totalCost}).");
            SetPublicTurnTimer(true, requestingPlayer, TURN_DURATION); // Reset timer publik
            photonView.RPC("Rpc_ActionFailedToActivator", requestingPlayer);
        }
    }

    [PunRPC]
    private void RequestActivateCard(int cardId, Player requestingPlayer)
    {
        Debug.Log($"[MC-CHECK] Request from: '{requestingPlayer.NickName}' ({requestingPlayer.ActorNumber}). Current Turn is for Actor: {this.currentPlayerActorNumber}. IsInFlashbuyMode: {this.isInFlashbuyMode}");
        // Periksa apakah MasterClient dan apakah ini giliran pemain yang benar.
        if (!PhotonNetwork.IsMasterClient || requestingPlayer.ActorNumber != this.currentPlayerActorNumber || isInFlashbuyMode) return;

        CardMultiplayer cardData = GetCardFromTable(cardId);
        if (cardData == null)
        {
            Debug.LogWarning($"[ACTIVATE GAGAL] Kartu ID {cardId} tidak valid atau sudah diambil.");
            AdvanceToNextTurn(); // Majukan giliran jika kartu tidak valid agar permainan tidak macet.
            return;
        }

        // --- PERUBAHAN LOGIKA BIAYA ---
        int fullPrice = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(cardData.color.ToString());
        int totalCost = cardData.baseValue;
        int currentInvestpoint = (int)requestingPlayer.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];

        if (currentInvestpoint >= totalCost)
        {
            consecutiveSkipCount = 0;
            // Kurangi INVESTPOINT, bukan FINPOINT
            Hashtable props = new Hashtable { { PlayerProfileMultiplayer.INVESTPOINT_KEY, currentInvestpoint - totalCost } };
            requestingPlayer.SetCustomProperties(props);
            // --- AKHIR PERUBAHAN ---

            cardsTaken++;

            string cardName = cardData.cardName;
            photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);

            if (cardName == "StockSplit" || cardName == "InsiderTrade")
            {
                // Ini kartu "Event", hentikan timer publik
                SetPublicTurnTimer(false);
            }
            else if (cardName == "Flashbuy" || cardName == "TenderOffer" || cardName == "TradeFee")
            {
                // Ini kartu "Aksi Tambahan", reset timer publik ke durasi aksi
                SetPublicTurnTimer(true, requestingPlayer, ACTION_DURATION);
            }
            StartCoroutine(CardEffectManagerMultiplayer.ApplyEffect(cardData.cardName, requestingPlayer, cardData.color));
        }
        else
        {
            Debug.LogWarning($"[ACTIVATE GAGAL] {requestingPlayer.NickName} tidak punya cukup InvestPoin (butuh {totalCost}).");
            AdvanceToNextTurn();
        }
    }
    
    [PunRPC]
    private void Rpc_ActionFailedToActivator()
    {
        // Tampilkan notifikasi
        if (GameStatusUI.Instance != null)
        {
            GameStatusUI.Instance.ShowTemporaryNotification("Aksi Gagal! InvestPoin tidak cukup.", 3.0f);
        }
        
        // Tampilkan kembali tombol aksi (jika ada kartu yang terseleksi)
        if (selectedCardId != -1)
        {
            if (actionButtonsPanel != null) actionButtonsPanel.SetActive(true);
        }
        
        // Timer lokal akan otomatis dimulai ulang oleh OnRoomPropertiesUpdate
    }

    [PunRPC]
    private void Rpc_RemoveCardFromTable(int cardId)
    {
        cardsOnTable.Remove(cardId);
        if (instantiatedCards.Count > cardId && instantiatedCards[cardId] != null)
        {
            Destroy(instantiatedCards[cardId]);
            instantiatedCards[cardId] = null;
        }
    }

    [PunRPC]
    private void Rpc_SetupCardsOnTable(int[] cardIndices)
    {
        if (cardContainer != null) cardContainer.gameObject.SetActive(true);
        foreach (GameObject oldCard in instantiatedCards) { Destroy(oldCard); }
        instantiatedCards.Clear();
        cardsOnTable.Clear();

        if (toggleCardContainerButton != null)
        {
            toggleCardContainerButton.gameObject.SetActive(true);
        }

        for (int i = 0; i < cardIndices.Length; i++)
        {
            if (i >= cardPositions.Count || cardPositions[i] == null) continue;
            int poolIndex = cardIndices[i];
            CardPoolEntry blueprint = allCardsPool[poolIndex];
            int baseValue = GetBaseValueForCard(blueprint.cardName);

            // --- PERUBAHAN: Membuat objek CardMultiplayer ---
            CardMultiplayer newCard = new CardMultiplayer(blueprint.cardName, "", baseValue, blueprint.color, blueprint.cardSprite);

            cardsOnTable.Add(i, newCard);
            GameObject cardObj = Instantiate(actionCardPrefab, cardContainer);
            cardObj.transform.position = cardPositions[i].position;
            cardObj.transform.localScale = cardPositions[i].localScale;
            instantiatedCards.Add(cardObj);

            // --- PERUBAHAN: Mengirim CardMultiplayer ke Setup ---
            cardObj.GetComponent<ActionCardUI>().Setup(newCard, i, this);
        }

        if (instantiatedCards.Count > 0 && instantiatedCards[0] != null)
        {
            this.defaultCardScale = instantiatedCards[0].transform.localScale;
            Debug.Log($"Ukuran default kartu telah diatur secara dinamis ke: {this.defaultCardScale}");
        }
    }
    #endregion

    #region Helper Methods
    private int GetBaseValueForCard(string cardName)
    {
        switch (cardName)
        {
            case "TenderOffer": return 0;
            case "TradeFee": return 1;
            case "StockSplit": return 0;
            case "InsiderTrade": return 0; // <-- TAMBAHKAN KEMBALI BARIS INI
            case "Flashbuy": return 0;
            default: return 0;
        }
    }

    public CardMultiplayer GetCardFromTable(int cardId)
    {
        return cardsOnTable.ContainsKey(cardId) ? cardsOnTable[cardId] : null;
    }
    private void ClearAllRemainingCards()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("[MasterClient] Semua pemain skip. Menghapus semua kartu tersisa di meja...");
        
        // Salin keys untuk menghindari error modifikasi koleksi saat iterasi
        List<int> remainingCardIds = cardsOnTable.Keys.ToList();
        
        foreach (int cardId in remainingCardIds)
        {
            photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);
        }

        // Atur 'cardsTaken' agar 'AdvanceToNextTurn' tahu fase harus berakhir
        this.cardsTaken = this.totalCardsOnTable; 
    }

    private IEnumerator EndActionPhaseSequence()
    {
        this.currentPlayerActorNumber = -1;
        photonView.RPC("Rpc_SetActionPhaseUIVisibility", RpcTarget.All, false);
        // --- AKHIR TAMBAHAN ---
        int currentSemester = (int)PhotonNetwork.CurrentRoom.CustomProperties[MultiplayerManager.SEMESTER_KEY];

        if (currentSemester > 1)
        {
            // LANGKAH 1: Transisi ke Fase Testing
            MultiplayerManager.Instance.photonView.RPC("Rpc_StartFadeTransition", RpcTarget.All, MultiplayerManager.TransitionType.Testing);
            yield return new WaitForSeconds(2.0f);

            // LANGKAH 2: Mulai Fase Testing dan serahkan kendali.
            if (TestingCardManagerMultiplayer.Instance != null)
            {
                Debug.Log($"[GAME FLOW] Menyerahkan kendali ke TestingCardManager untuk Semester {currentSemester}...");
                TestingCardManagerMultiplayer.Instance.BeginTestingPhase();
            }
            // COROUTINE BERHENTI DI SINI. Tidak ada lagi logika menunggu.
        }
        else
        {
            // Alur untuk semester 1 tidak berubah, langsung ke penjualan.
            MultiplayerManager.Instance.photonView.RPC("Rpc_StartFadeTransition", RpcTarget.All, MultiplayerManager.TransitionType.Selling);
            yield return new WaitForSeconds(2.0f);
            if (SellingPhaseManagerMultiplayer.Instance != null)
            {
                SellingPhaseManagerMultiplayer.Instance.StartSellingPhase(this.turnOrder);
            }
        }
    }

    public void ProceedToSellingPhaseAfterTesting()
    {
        // Pastikan hanya MasterClient yang bisa menjalankan ini.
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("[ActionPhaseManager] Menerima callback dari TestingCardManager. Memulai transisi ke Fase Penjualan.");
        StartCoroutine(TransitionToSellingSequence());
    }

    // --- COROUTINE BARU ---
    // Coroutine ini berisi logika yang sebelumnya ada di akhir EndActionPhaseSequence.
    private IEnumerator TransitionToSellingSequence()
    {
        // Transisi ke Fase Penjualan
        MultiplayerManager.Instance.photonView.RPC("Rpc_StartFadeTransition", RpcTarget.All, MultiplayerManager.TransitionType.Selling);
        yield return new WaitForSeconds(2.0f);

        // Mulai Fase Penjualan
        if (SellingPhaseManagerMultiplayer.Instance != null)
        {
            SellingPhaseManagerMultiplayer.Instance.StartSellingPhase(this.turnOrder);
        }
    }
    
    public List<int> GetRandomCardIndices(int count)
    {
        if (allCardsPool.Count < count) return new List<int>();
        List<int> possibleIndices = Enumerable.Range(0, allCardsPool.Count).ToList();
        System.Random rnd = new System.Random();
        return possibleIndices.OrderBy(x => rnd.Next()).Take(count).ToList();
    }
    public void HandlePlayerDisconnect(Player disconnectedPlayer)
    {
        // Hanya MasterClient & hanya jika fase aksi sedang berjalan
        if (!PhotonNetwork.IsMasterClient || currentPlayerActorNumber == -1)
    {
        // currentPlayerActorNumber == -1 berarti Fase Aksi tidak sedang berjalan.
        // Jangan lakukan apa-apa.
        return;
    }
        // Cek apakah pemain ini ada di daftar giliran fase ini
        if (turnOrder.Contains(disconnectedPlayer))
        {   
            int actorNum = disconnectedPlayer.ActorNumber;
            Debug.Log($"[ActionPhaseManager] Menandai {disconnectedPlayer.NickName} (Actor {actorNum}) sebagai disconnect.");
            
            if (!disconnectedPlayerActorNumbers.Contains(actorNum))
            {
                 disconnectedPlayerActorNumbers.Add(actorNum);
            }

            // Cek apakah ini giliran mereka SEKARANG?
            if (actorNum == this.currentPlayerActorNumber)
            {
                Debug.Log($"[ActionPhaseManager] Itu adalah giliran pemain yang disconnect. Memajukan paksa...");
                
                // Hentikan timer publik
                SetPublicTurnTimer(false); 
                
                // Cek mode khusus (Flashbuy/TenderOffer)
                if (isInFlashbuyMode && actorNum == flashbuyActivatorActorNumber)
                {
                     // Panggil logika dari Rpc_SubmitFlashbuyChoices untuk pilihan kosong (0 kartu)
                     Debug.Log($"[ActionPhaseManager] {disconnectedPlayer.NickName} disconnect saat Flashbuy. Submit 0 kartu.");
                     consecutiveSkipCount = 0; // Memilih 0 kartu dihitung sbg aksi, bukan skip
                     this.isInFlashbuyMode = false;
                     this.flashbuyActivatorActorNumber = -1;
                     AdvanceToNextTurn();
                }
                else if (isInTenderOfferMode)
                {
                     Debug.Log($"[ActionPhaseManager] {disconnectedPlayer.NickName} disconnect saat Tender Offer. Submit 'skip'.");
                     consecutiveSkipCount = 0; // Aksi skip di Tender Offer dihitung sbg aksi
                     photonView.RPC("Rpc_CleanupTenderOfferVisuals", RpcTarget.All);
                     AdvanceToNextTurn();
                }
                // (Kita tidak perlu cek TradeFee karena itu di-handle oleh timer skip biasa)
                else
                {
                    // Giliran normal, panggil AdvanceToNextTurn.
                    // AdvanceToNextTurn akan otomatis melompati pemain ini.
                    AdvanceToNextTurn();
                }
            }
        }
    }
    #endregion
}