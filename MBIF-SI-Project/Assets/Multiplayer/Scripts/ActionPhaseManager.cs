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
    public GameObject insiderTradePanel;
    public Text insiderTradeText;

    [Header("Action Buttons References")]
    public Button primaryActionButton;
    public TextMeshProUGUI primaryActionButtonText;
    public Button activateButton; // Referensi untuk tombol Activate
    public Button skipButton;     // Referensi untuk tombol Skip

    [Header("Layout")]
    public List<Transform> cardPositions;

    [Header("Tender Offer UI")]
    public GameObject tenderOfferPanel;
    public Transform targetButtonContainer;
    public GameObject targetButtonPrefab;

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
            StartCoroutine(TransitionToSellingPhase());
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

    #region Insider Trade Logic
    [PunRPC]
    private void Rpc_ShowInsiderTrade(string rumorDescription)
    {
        // Fungsi ini hanya akan berjalan di komputer pemain yang dituju
        Debug.Log($"[INSIDER TRADE] Anda menerima bocoran: {rumorDescription}");
        StartCoroutine(ShowInsiderTradePanel(rumorDescription));
    }

    private IEnumerator ShowInsiderTradePanel(string description)
    {
        if (insiderTradePanel != null && insiderTradeText != null)
        {
            insiderTradeText.text = description;
            insiderTradePanel.SetActive(true);

            // Tampilkan panel selama 5 detik, lalu sembunyikan lagi
            yield return new WaitForSeconds(5f);

            insiderTradePanel.SetActive(false);
        }
    }

    [PunRPC]
    private void Rpc_ShowInsiderTradePrediction(string cardName, string colorName)
    {
        // Fungsi ini berjalan di klien pemain yang mengaktifkan kartu
        Debug.Log($"[INSIDER TRADE] Anda menerima bocoran visual untuk kartu: {cardName} [{colorName}]");
        StartCoroutine(AnimateInsiderTradePrediction(cardName, colorName));
    }

    private IEnumerator AnimateInsiderTradePrediction(string cardName, string colorName)
    {
        // Coroutine ini menjalankan seluruh sekuens animasi secara lokal
        if (RumorPhaseManagerMultiplayer.Instance == null) // Periksa instance langsung
        {
            Debug.LogError("Instance RumorPhaseManagerMultiplayer tidak ditemukan di scene!");
            // Jika animasi tidak bisa diputar, kita tetap HARUS lapor ke MasterClient agar game tidak macet.
            photonView.RPC("Rpc_ConfirmInsiderTradeComplete", RpcTarget.MasterClient);
            yield break;
        }

        // Ganti semua 'rumorManager' dengan 'RumorPhaseManagerMultiplayer.Instance'
        GameObject cardObject = RumorPhaseManagerMultiplayer.Instance.GetCardObjectByColor(colorName);
        Texture cardTexture = RumorPhaseManagerMultiplayer.Instance.GetCardTextureByName(cardName);

        if (cardObject == null || cardTexture == null)
        {
            Debug.LogError($"Tidak dapat menemukan objek kartu atau tekstur untuk {cardName} [{colorName}]");
            photonView.RPC("Rpc_ConfirmInsiderTradeComplete", RpcTarget.MasterClient);
            yield break;
        }
        
        Renderer cardRenderer = cardObject.GetComponentInChildren<Renderer>();
        
        // Simpan state asli kartu
        Vector3 originalPosition = cardObject.transform.position;
        Quaternion originalRotation = cardObject.transform.rotation;
        
        // Pindahkan kartu ke panggung prediksi
        cardObject.transform.position = RumorPhaseManagerMultiplayer.Instance.predictionCardStage.position;
        cardObject.transform.rotation = RumorPhaseManagerMultiplayer.Instance.predictionCardStage.rotation;
        cardRenderer.material.mainTexture = cardTexture;
        
        // JALANKAN ANIMASI FLIP (DURASI 0.5 DETIK)
        float flipDuration = 0.5f;
        yield return StartCoroutine(RumorPhaseManagerMultiplayer.Instance.FlipCard(cardObject, flipDuration));
        
        // Tunggu sejenak agar pemain bisa melihat kartu
        yield return new WaitForSeconds(3.5f); // Total jeda = 0.5s (animasi) + 3.5s (tunggu) = 4 detik
        
        // Sembunyikan dan kembalikan kartu ke posisi semula
        cardObject.SetActive(false); // Sembunyikan agar tidak terlihat saat kembali
        cardObject.transform.position = originalPosition;
        cardObject.transform.rotation = originalRotation;
        
        Debug.Log("[Insider Trade] Animasi selesai. Mengirim konfirmasi ke MasterClient.");

        // KIRIM KONFIRMASI KEMBALI KE MASTERCLIENT
        photonView.RPC("Rpc_ConfirmInsiderTradeComplete", RpcTarget.MasterClient);
    }

    [PunRPC]
    private void Rpc_ConfirmInsiderTradeComplete(PhotonMessageInfo info)
    {
        // Fungsi ini HANYA berjalan di MasterClient
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log($"[MasterClient] Konfirmasi Insider Trade dari {info.Sender.NickName} diterima. Melanjutkan giliran.");
        ForceNextTurn(); // Lanjutkan ke giliran berikutnya
    }

    #endregion

    #region Tender Offer Logic
    // Fungsi ini dipanggil dari Tombol Target yang kita klik
    public void OnTenderOfferTargetSelected(int targetActorNumber, string cardColor)
    {
        Debug.Log($"Anda memilih target #{targetActorNumber} untuk kartu {cardColor}. Mengirim pilihan ke MasterClient...");
        // Kirim pilihan final ke MasterClient, sekarang dengan data warna
        photonView.RPC("Rpc_SubmitTenderOfferTarget", RpcTarget.MasterClient, targetActorNumber, cardColor);

        tenderOfferPanel.SetActive(false);
    }

    // RPC ini berjalan di pemain yang mengaktifkan kartu, untuk menampilkan pilihan
    [PunRPC]
    private void Rpc_RequestTenderOfferTarget(int[] validTargetActorNumbers, string cardColorStr)
    {
        tenderOfferPanel.SetActive(true);
        foreach (Transform child in targetButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (int actorNumber in validTargetActorNumbers)
        {
            Player targetPlayer = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (targetPlayer != null)
            {
                GameObject buttonObj = Instantiate(targetButtonPrefab, targetButtonContainer);
                // Panggil Setup dengan parameter warna yang baru
                buttonObj.GetComponent<TargetPlayerButton>().Setup(targetPlayer.NickName, targetPlayer.ActorNumber, cardColorStr, this);
            }
        }
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

        // 2. Ambil data Finpoint dan kartu saat ini dari kedua pemain
        int activatorFP = (int)activator.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY];
        int targetFP = (int)target.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY];

        string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(cardColor);
        int activatorCardCount = activator.CustomProperties.ContainsKey(cardKey) ? (int)activator.CustomProperties[cardKey] : 0;
        int targetCardCount = target.CustomProperties.ContainsKey(cardKey) ? (int)target.CustomProperties[cardKey] : 0;

        // 3. Validasi sekali lagi jika pengaktif mampu membayar
        if (activatorFP >= purchasePrice)
        {
            // 4. Siapkan properti baru untuk kedua pemain
            Hashtable activatorProps = new Hashtable
            {
                { PlayerProfileMultiplayer.FINPOINT_KEY, activatorFP - purchasePrice },
                { cardKey, activatorCardCount + 1 }
            };

            Hashtable targetProps = new Hashtable
            {
                { PlayerProfileMultiplayer.FINPOINT_KEY, targetFP + purchasePrice },
                { cardKey, targetCardCount - 1 }
            };

            // 5. Kirim pembaruan ke jaringan
            activator.SetCustomProperties(activatorProps);
            target.SetCustomProperties(targetProps);

            Debug.Log($"[Tender Offer] Transaksi berhasil. {activator.NickName} membayar {purchasePrice} FP ke {target.NickName}.");
        }
        else
        {
            Debug.LogWarning($"[Tender Offer] Transaksi dibatalkan oleh server, Finpoint {activator.NickName} tidak cukup.");
        }
        AdvanceToNextTurn();
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
        int currentFP = (int)activator.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY];

        // Siapkan properti baru
        Hashtable propsToSet = new Hashtable
    {
        { cardKey, currentCards - quantity },
        { PlayerProfileMultiplayer.FINPOINT_KEY, currentFP + totalEarnings }
    };

        // Kirim pembaruan
        activator.SetCustomProperties(propsToSet);
        Debug.Log($"[Trade Fee] Transaksi berhasil. {activator.NickName} mendapatkan {totalEarnings} FP.");
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
            if(actionButtonsPanel != null) actionButtonsPanel.SetActive(true);
            if(primaryActionButtonText != null) primaryActionButtonText.text = "Confirm Selection";
            if(activateButton != null) activateButton.gameObject.SetActive(false);
            if(skipButton != null) skipButton.gameObject.SetActive(true);
        } else {
            Player activatorPlayer = PhotonNetwork.CurrentRoom.GetPlayer(activatorActorNumber);
            if (activatorPlayer != null && GameStatusUI.Instance != null) {
                GameStatusUI.Instance.photonView.RPC("UpdateStatusText", RpcTarget.All, $"{activatorPlayer.NickName} mengaktifkan Flashbuy! Dia akan memilih kartu.");
            }
        }
    }

    [PunRPC]
    private void Rpc_SubmitFlashbuyChoices(int[] chosenCardIds, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Player activator = info.Sender;

        // VALIDASI MASTERCLIENT SANGAT PENTING DI SINI
        if (activator.ActorNumber != flashbuyActivatorActorNumber)
        {
            Debug.LogError($"[Flashbuy Bug] Pemain yang bukan pengaktif ({activator.NickName}) mencoba mengirim pilihan Flashbuy!");
            return;
        }
        if (chosenCardIds.Length > 2)
        {
            Debug.LogError($"[Flashbuy Bug] {activator.NickName} mencoba memilih lebih dari 2 kartu Flashbuy!");
            return;
        }
        
        Debug.Log($"[Flashbuy] MasterClient memproses pilihan {chosenCardIds.Length} kartu dari {activator.NickName}.");

        Hashtable playerPropsToUpdate = new Hashtable();
        // Salin properti yang sudah ada dari pemain ke Hashtable baru
        foreach (DictionaryEntry entry in activator.CustomProperties)
        {
            playerPropsToUpdate.Add(entry.Key, entry.Value);
        }

        foreach (int cardId in chosenCardIds)
        {
            // Ambil data kartu dari state meja MasterClient
            CardMultiplayer cardData = GetCardFromTable(cardId); 
            
            // Pastikan kartu itu masih ada di meja dan belum diambil oleh efek lain secara bersamaan
            if (cardData != null)
            {
                Debug.Log($"[Flashbuy] Memberikan kartu {cardData.cardName} ke {activator.NickName}.");

                // Tambahkan kartu ke inventaris pemain (tanpa biaya)
                string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(cardData.color.ToString());
                if (!string.IsNullOrEmpty(cardKey))
                {
                    int currentCards = playerPropsToUpdate.ContainsKey(cardKey) ? (int)playerPropsToUpdate[cardKey] : 0;
                    playerPropsToUpdate[cardKey] = currentCards + 1;
                }

                // Hapus kartu dari meja di semua klien
                photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);
                cardsTaken++; // Update jumlah kartu yang telah diambil
            } else {
                Debug.LogWarning($"[Flashbuy] Kartu dengan ID {cardId} tidak ditemukan di meja MasterClient. Mungkin sudah diambil/dihapus.");
            }
        }

        // Hanya update properti pemain sekali setelah semua kartu diproses
        if (playerPropsToUpdate.Count > 0) {
            activator.SetCustomProperties(playerPropsToUpdate);
        }
        
        // Setelah selesai memproses pilihan Flashbuy, paksa giliran berikutnya
        AdvanceToNextTurn();
    }

    private void ExitFlashbuyMode()
    {
        isInFlashbuyMode = false;
        flashbuyActivatorActorNumber = -1; // Reset pengaktif
        flashbuySelectedCardIds.Clear(); // Kosongkan daftar pilihan

        if(primaryActionButtonText != null) primaryActionButtonText.text = "Save";
        if(activateButton != null) activateButton.gameObject.SetActive(true);
        if(skipButton != null) skipButton.gameObject.SetActive(false);
        if(actionButtonsPanel != null) actionButtonsPanel.SetActive(false);

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
        // Jika dalam mode Flashbuy dan ini pemain pengaktif
        if (isInFlashbuyMode && PhotonNetwork.LocalPlayer.ActorNumber == flashbuyActivatorActorNumber)
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
            } else {
                Debug.LogWarning("Anda hanya bisa memilih maksimal 2 kartu untuk Flashbuy.");
            }
        }
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
        if (selectedCardId != -1) // Tambahkan pemeriksaan sederhana
        {
            photonView.RPC("RequestActivateCard", RpcTarget.MasterClient, selectedCardId, PhotonNetwork.LocalPlayer);
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

        int currentFinpoint = (int)requestingPlayer.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY];

        if (currentFinpoint >= cardData.value)
        {
            Hashtable propsToSet = new Hashtable();
            propsToSet.Add(PlayerProfileMultiplayer.FINPOINT_KEY, currentFinpoint - cardData.value);
            string cardColorKey = PlayerProfileMultiplayer.GetCardKeyFromColor(cardData.color.ToString()); if (!string.IsNullOrEmpty(cardColorKey))
            {
                int currentCardCount = 0;
                if (requestingPlayer.CustomProperties.ContainsKey(cardColorKey))
                    currentCardCount = (int)requestingPlayer.CustomProperties[cardColorKey];
                propsToSet.Add(cardColorKey, currentCardCount + 1);
            }
            requestingPlayer.SetCustomProperties(propsToSet);

            cardsTaken++; // Tandai satu kartu telah diambil

            photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);
            AdvanceToNextTurn();
        }
        else
        {
            Debug.LogWarning($"[SAVE GAGAL] {requestingPlayer.NickName} tidak punya cukup Finpoint.");
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
        
        int currentFinpoint = (int)requestingPlayer.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY];

        if (currentFinpoint >= cardData.value)
        {
            // Kurangi finpoint pemain
            Hashtable props = new Hashtable { { PlayerProfileMultiplayer.FINPOINT_KEY, currentFinpoint - cardData.value } };
            requestingPlayer.SetCustomProperties(props);
            
            // Tandai kartu sudah diambil dan hapus dari meja
            cardsTaken++;
            photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);
        
            // PENTING: Panggil coroutine efek kartu.
            // Sekarang, setiap efek bertanggung jawab untuk melanjutkan giliran.
            // - InsiderTrade akan lanjut setelah RPC konfirmasi.
            // - StockSplit akan lanjut dari dalam efeknya sendiri.
            // - TenderOffer/TradeFee/Flashbuy akan lanjut setelah input dari pemain.
            StartCoroutine(CardEffectManagerMultiplayer.ApplyEffect(cardData.cardName, requestingPlayer, cardData.color));
        }
        else
        {
            Debug.LogWarning($"[ACTIVATE GAGAL] {requestingPlayer.NickName} tidak punya cukup Finpoint.");
            // Jika gagal karena tidak cukup uang, kita perlu beritahu klien untuk reset UI
            // dan tidak melanjutkan giliran agar pemain bisa memilih aksi lain.
            // Untuk kesederhanaan saat ini, kita biarkan pemain 'kehilangan' gilirannya jika mencoba cheat.
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
            case "InsiderTrade": return 2;
            case "Flashbuy": return 3;
            default: return 0;
        }
    }

    public CardMultiplayer GetCardFromTable(int cardId)
    {
        return cardsOnTable.ContainsKey(cardId) ? cardsOnTable[cardId] : null;
    }

    private IEnumerator TransitionToSellingPhase()
    {
        // Beri jeda 1.5 detik agar semua properti pemain tersinkronisasi
        yield return new WaitForSeconds(1.5f);

        // Setelah jeda, baru mulai fase penjualan
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

    // RPC untuk menerima kartu dari Flashbuy
    [PunRPC]
    private void Rpc_AddCardsToPlayer(int[] cardIndices)
    {
        Debug.Log($"[Flashbuy] Anda menerima {cardIndices.Length} kartu baru!");
        Hashtable propsToSet = new Hashtable();
        foreach (int index in cardIndices)
        {
            CardPoolEntry cardData = allCardsPool[index];
            string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(cardData.color.ToString());
            if (!string.IsNullOrEmpty(cardKey))
            {
                int currentCards = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(cardKey) ? (int)PhotonNetwork.LocalPlayer.CustomProperties[cardKey] : 0;
                propsToSet[cardKey] = currentCards + 1;
            }
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(propsToSet);
    }
    #endregion
}