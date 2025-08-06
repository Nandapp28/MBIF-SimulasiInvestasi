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

    [Header("Action Buttons References")]
    public Button primaryActionButton;
    public TextMeshProUGUI primaryActionButtonText;
    public Button activateButton; // Referensi untuk tombol Activate
    public Button skipButton;     // Referensi untuk tombol Skip

    [Header("Layout")]
    public List<Transform> cardPositions;

    [Header("Trade Fee UI")]
    public GameObject tradeFeePanel;
    public Text tradeFeeInfoText;
    public Text tradeFeeQuantityText;
    public Button tradeFeePlusButton;
    public Button tradeFeeMinusButton;
    public Button tradeFeeConfirmButton;

    // Di bagian State Variables:
    private bool isInFlashbuyMode = false;
    private int flashbuyActivatorActorNumber = -1; // Tambahkan ini untuk melacak siapa pengaktif
    private List<int> flashbuySelectedCardIds = new List<int>(); // Kartu yang dipilih di sesi Flashbuy
    private Coroutine flashbuyTimerCoroutine; // Untuk manajemen timer (opsional tapi disarankan)

    [Header("Tender Offer UI")]
    public GameObject tenderOfferClickButtonPrefab; // Prefab tombol "klik" Anda

    // Tambahkan variabel ini untuk melacak tombol yang aktif
    private List<GameObject> activeTenderOfferButtons = new List<GameObject>();
    private string tenderOfferCardColor; // Kita masih butuh ini

    // Variabel State
    private List<Player> turnOrder;
    private int currentTurnIndex = -1;
    private int currentPlayerActorNumber = -1;
    private int cardsTaken = 0; // KEMBALI MENGGUNAKAN INI untuk melacak progres
    private int totalCardsOnTable = 0;

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
    }

    public void StartActionPhase()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Player[] players = PhotonNetwork.PlayerList;
            turnOrder = players.OrderBy(p => (int)p.CustomProperties[PlayerProfileMultiplayer.TURN_ORDER_KEY]).ToList();

            cardsTaken = 0; // Reset penghitung kartu yang diambil
            totalCardsOnTable = PhotonNetwork.CurrentRoom.PlayerCount * 2;
            currentTurnIndex = -1;

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
    private void AdvanceToNextTurn()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        HideAndResetSelection(); // Sembunyikan tombol normal jika ada

        // Pastikan tidak ada mode Flashbuy yang masih aktif di MasterClient
        // Ini penting jika pemain pengaktif Flashbuy keluar atau koneksi terputus.
        if (isInFlashbuyMode && flashbuyActivatorActorNumber != -1) {
            Player activator = PhotonNetwork.CurrentRoom.GetPlayer(flashbuyActivatorActorNumber);
            if (activator != null) {
                Debug.LogWarning($"[Flashbuy] MasterClient memajukan giliran karena Flashbuy belum selesai oleh {activator.NickName}.");
                // Mungkin kirim pilihan kosong atau paksa ExitFlashbuyMode() di semua klien
                photonView.RPC("Rpc_SubmitFlashbuyChoices", RpcTarget.MasterClient, new int[0]); // Paksa submit pilihan kosong
            }
        }

        // Fase berakhir jika semua kartu di meja sudah diambil
        if (cardsTaken >= totalCardsOnTable)
        {
            Debug.Log("✅ Semua kartu telah diambil. Transisi ke Fase Penjualan dalam 1.5 detik...");
            GameStatusUI.Instance.photonView.RPC("UpdateStatusText", RpcTarget.All, "Fase Aksi Selesai! Mempersiapkan Penjualan...");

            // --- PERBAIKAN: HANYA PANGGIL COROUTINE ---
            // Panggil coroutine untuk transisi yang mulus dan HENTIKAN eksekusi.
            StartCoroutine(EndActionPhaseSequence());
            return;
            // Baris `return` ini penting untuk memastikan tidak ada kode lain yang berjalan.
        }

        // Logika untuk memutar giliran (round-robin)
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        Player nextPlayer = turnOrder[currentTurnIndex];

        GameStatusUI.Instance.photonView.RPC("UpdateStatusText", RpcTarget.All, $"Giliran {nextPlayer.NickName} untuk memilih kartu.");
        photonView.RPC("Rpc_SyncCurrentPlayerTurn", RpcTarget.All, nextPlayer.ActorNumber);
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

    [PunRPC]
    private void Rpc_SyncCurrentPlayerTurn(int actorNumber)
    {
        this.currentPlayerActorNumber = actorNumber;
    }
    #endregion

    #region Tender Offer Logic

    [PunRPC]
    private void Rpc_RequestTenderOfferTarget(int[] validTargetActorNumbers, string cardColorStr)
    {
        // Hanya pemain pengaktif yang memunculkan tombol
        if (PhotonNetwork.LocalPlayer.ActorNumber != this.currentPlayerActorNumber) return;

        tenderOfferCardColor = cardColorStr;
        CleanupTenderOfferButtons(); // Bersihkan tombol lama jika ada

        // Cari semua profil pemain yang ada di scene
        PlayerProfileMultiplayer[] allPlayerProfiles = FindObjectsOfType<PlayerProfileMultiplayer>();
        List<int> validTargetsList = new List<int>(validTargetActorNumbers);

        foreach (PlayerProfileMultiplayer profile in allPlayerProfiles)
        {
            // Cek apakah pemilik profil ini adalah target yang valid
            if (profile.photonView.Owner != null && validTargetsList.Contains(profile.photonView.Owner.ActorNumber))
            {
                // Dapatkan posisi dari UI profil pemain target
                Transform targetTransform = profile.transform;

                // Buat tombol "klik" sebagai child dari transform target agar posisinya mengikuti
                GameObject buttonObj = Instantiate(tenderOfferClickButtonPrefab, targetTransform);
                buttonObj.transform.localPosition = Vector3.zero; // Atur posisi relatif ke tengah

                Button clickButton = buttonObj.GetComponentInChildren<Button>();
                if (clickButton != null)
                {
                    Player targetPlayer = profile.photonView.Owner;
                    clickButton.onClick.AddListener(() => OnTenderOfferTargetClicked(targetPlayer));
                }

                activeTenderOfferButtons.Add(buttonObj);
            }
        }

        // Tampilkan tombol Skip untuk jaga-jaga
        if (skipButton != null)
        {
            actionButtonsPanel.SetActive(true);
            primaryActionButton.gameObject.SetActive(false);
            activateButton.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(true);
        }
    }

    private void OnTenderOfferTargetClicked(Player targetPlayer)
    {
        // Kirim pilihan ke server
        photonView.RPC("Rpc_SubmitTenderOfferTarget", RpcTarget.MasterClient, targetPlayer.ActorNumber, tenderOfferCardColor);

        // Setelah memilih, bersihkan semua tombol "klik" dan tombol skip
        CleanupTenderOfferButtons();
    }
    
    // RPC ini berjalan di MasterClient, menerima pilihan final dari pemain
    [PunRPC]
    private void Rpc_SubmitTenderOfferTarget(int targetActorNumber, string cardColor, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player activator = info.Sender;
        Player target = PhotonNetwork.CurrentRoom.GetPlayer(targetActorNumber);

        if (activator == null || target == null) return;

        Debug.Log($"[Tender Offer] MasterClient memproses: {activator.NickName} mengambil kartu {cardColor} dari {target.NickName}.");

        // 1. Hitung harga pembelian (setengah dari harga jual penuh)
        // Asumsi SellingPhaseManagerMultiplayer sudah ada di scene
        int fullPrice = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(cardColor);
        int purchasePrice = Mathf.CeilToInt(fullPrice / 2.0f);

        // 2. Ambil data Investpoint dan kartu saat ini dari kedua pemain
        int activatorInvestPoin = (int)activator.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];
        int targetInvestPoin = (int)target.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];

        string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(cardColor);
        int activatorCardCount = activator.CustomProperties.ContainsKey(cardKey) ? (int)activator.CustomProperties[cardKey] : 0;
        int targetCardCount = target.CustomProperties.ContainsKey(cardKey) ? (int)target.CustomProperties[cardKey] : 0;

        // 3. Validasi sekali lagi jika pengaktif mampu membayar
        if (activatorInvestPoin >= purchasePrice)
        {
            // 4. Siapkan properti baru untuk kedua pemain
            Hashtable activatorProps = new Hashtable
            {
                { PlayerProfileMultiplayer.INVESTPOINT_KEY, activatorInvestPoin - purchasePrice },
                { cardKey, activatorCardCount + 1 }
            };

            Hashtable targetProps = new Hashtable
            {
                { PlayerProfileMultiplayer.INVESTPOINT_KEY, targetInvestPoin + purchasePrice },
                { cardKey, targetCardCount - 1 }
            };

            // 5. Kirim pembaruan ke jaringan
            activator.SetCustomProperties(activatorProps);
            target.SetCustomProperties(targetProps);

            Debug.Log($"[Tender Offer] Transaksi berhasil. {activator.NickName} membayar {purchasePrice} InvestPoin ke {target.NickName}.");
        }
        else
        {
            Debug.LogWarning($"[Tender Offer] Transaksi dibatalkan oleh server, Investpoint {activator.NickName} tidak cukup.");
        }
        AdvanceToNextTurn();
    }

    private void CleanupTenderOfferButtons()
    {
        // Hancurkan semua tombol "klik" yang aktif
        foreach (GameObject btn in activeTenderOfferButtons)
        {
            if (btn != null) Destroy(btn);
        }
        activeTenderOfferButtons.Clear();

        // Sembunyikan kembali panel tombol aksi (yang berisi tombol skip)
        actionButtonsPanel.SetActive(false);
        // Kembalikan visibilitas tombol normal
        primaryActionButton.gameObject.SetActive(true);
        activateButton.gameObject.SetActive(true);
        skipButton.gameObject.SetActive(false);
    }
    #endregion

    #region Trade Fee Logic

    [PunRPC]
    private void Rpc_RequestTradeFeeInput(string color, int maxQuantity)
    {
        tradeFeePanel.SetActive(true);

        // Konfigurasi UI
        tradeFeeInfoText.text = $"Jual Kartu Sektor {color}?";
        int quantityToSell = maxQuantity; // Defaultnya adalah menjual semua
        tradeFeeQuantityText.text = quantityToSell.ToString();

        // Hapus listener lama untuk mencegah penumpukan
        tradeFeePlusButton.onClick.RemoveAllListeners();
        tradeFeeMinusButton.onClick.RemoveAllListeners();
        tradeFeeConfirmButton.onClick.RemoveAllListeners();

        // Atur fungsi tombol + dan -
        tradeFeePlusButton.onClick.AddListener(() =>
        {
            if (quantityToSell < maxQuantity)
            {
                quantityToSell++;
                tradeFeeQuantityText.text = quantityToSell.ToString();
            }
        });

        tradeFeeMinusButton.onClick.AddListener(() =>
        {
            if (quantityToSell > 0)
            {
                quantityToSell--;
                tradeFeeQuantityText.text = quantityToSell.ToString();
            }
        });

        // Atur fungsi tombol konfirmasi
        tradeFeeConfirmButton.onClick.AddListener(() =>
        {
            OnTradeFeeConfirm(color, quantityToSell);
        });
    }

    // Fungsi ini dipanggil dari tombol konfirmasi di UI
    public void OnTradeFeeConfirm(string color, int quantity)
    {
        Debug.Log($"Anda memilih untuk menjual {quantity} kartu {color}. Mengirim keputusan ke MasterClient...");
        photonView.RPC("Rpc_SubmitTradeFeeDecision", RpcTarget.MasterClient, color, quantity);
        tradeFeePanel.SetActive(false);
    }

    // RPC ini berjalan di MasterClient, menerima pilihan final dari pemain
    [PunRPC]
    private void Rpc_SubmitTradeFeeDecision(string color, int quantity, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player activator = info.Sender;
        if (activator == null) return;

        Debug.Log($"[Trade Fee] MasterClient memproses: {activator.NickName} menjual {quantity} kartu {color}.");

        // Dapatkan harga jual penuh saat ini
        int pricePerCard = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(color);
        int totalEarnings = quantity * pricePerCard;

        // Ambil data pemain saat ini
        string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(color);
        int currentCards = (int)activator.CustomProperties[cardKey];
        // --- PERBAIKAN NAMA VARIABEL ---
        int currentInvestpoint = (int)activator.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];

            // Siapkan properti baru
            Hashtable propsToSet = new Hashtable
        {
            { cardKey, currentCards - quantity },
            { PlayerProfileMultiplayer.INVESTPOINT_KEY, currentInvestpoint + totalEarnings }
        };

        // Kirim pembaruan
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
            if (skipButton != null) skipButton.gameObject.SetActive(true);
            UpdateFlashbuyAffordability();
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
        if (activeTenderOfferButtons.Count > 0)
        {
            Debug.Log("[Tender Offer] Pemain memilih untuk skip.");
            CleanupTenderOfferButtons(); // Bersihkan UI
            photonView.RPC("Rpc_RequestSkipTurn", RpcTarget.MasterClient); // Tetap kirim skip normal
        }
        // Jika dalam mode Flashbuy dan ini pemain pengaktif
        else if (isInFlashbuyMode && PhotonNetwork.LocalPlayer.ActorNumber == flashbuyActivatorActorNumber)
        {
            Debug.Log("[Flashbuy] Pemain mengklik Skip di mode Flashbuy.");
            // Kirim pilihan kosong ke MasterClient (Artinya tidak memilih kartu)
            photonView.RPC("Rpc_SubmitFlashbuyChoices", RpcTarget.MasterClient, new int[0]);
            ExitFlashbuyMode(); // Keluar dari mode di klien
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
            // Cukup panggil giliran berikutnya
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
        if (isInFlashbuyMode)
        {
            OnConfirmFlashbuySelection();
        }
        else
        {
            // Logika save kartu normal
            if (selectedCardId != -1 && PhotonNetwork.LocalPlayer.ActorNumber == currentPlayerActorNumber) {
                photonView.RPC("RequestSaveCard", RpcTarget.MasterClient, selectedCardId, PhotonNetwork.LocalPlayer);
                HideAndResetSelection(); // Reset UI setelah mengirim permintaan
            }
        }
    }

    // Buat fungsi baru ini untuk menangani konfirmasi Flashbuy
    private void OnConfirmFlashbuySelection()
    {
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
        int totalCost = cardData.baseValue + fullPrice;
        int currentInvestpoint = (int)requestingPlayer.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];

        if (currentInvestpoint >= totalCost)
        {
            // Kurangi INVESTPOINT, bukan FINPOINT
            Hashtable props = new Hashtable { { PlayerProfileMultiplayer.INVESTPOINT_KEY, currentInvestpoint - totalCost } };
            requestingPlayer.SetCustomProperties(props);
        // --- AKHIR PERUBAHAN ---
            
            cardsTaken++;
            photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);
            StartCoroutine(CardEffectManagerMultiplayer.ApplyEffect(cardData.cardName, requestingPlayer, cardData.color));
        }
        else
        {
            Debug.LogWarning($"[ACTIVATE GAGAL] {requestingPlayer.NickName} tidak punya cukup InvestPoin (butuh {totalCost}).");
            AdvanceToNextTurn();
        }
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
            case "TradeFee":
            case "StockSplit": return 1;
            case "InsiderTrade": return 2; // <-- TAMBAHKAN KEMBALI BARIS INI
            case "Flashbuy": return 3;
            default: return 0;
        }
    }

    public CardMultiplayer GetCardFromTable(int cardId)
    {
        return cardsOnTable.ContainsKey(cardId) ? cardsOnTable[cardId] : null;
    }

    private IEnumerator EndActionPhaseSequence()
    {
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
    #endregion
}