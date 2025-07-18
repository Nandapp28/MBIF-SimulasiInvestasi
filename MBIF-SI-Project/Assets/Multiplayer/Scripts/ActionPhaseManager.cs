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

    private bool isInFlashbuyMode = false;
    private List<int> flashbuySelectedCardIds = new List<int>();

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
    private Vector3 defaultCardScale = Vector3.one;

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
        HideAndResetSelection();

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
        this.currentPlayerActorNumber = activatorActorNumber;
        
        if (PhotonNetwork.LocalPlayer.ActorNumber == activatorActorNumber)
        {
            isInFlashbuyMode = true;
            flashbuySelectedCardIds.Clear();
            
            // Ubah UI untuk mode Flashbuy
            actionButtonsPanel.SetActive(true);
            actionButtonsPanel.transform.Find("SaveButton").GetComponentInChildren<Text>().text = "Confirm Selection";
            actionButtonsPanel.transform.Find("ActivateButton").gameObject.SetActive(false);
            // Anda juga perlu menambahkan tombol Skip di sini
            actionButtonsPanel.transform.Find("SkipButton").gameObject.SetActive(true);
        }
    }

    [PunRPC]
    private void Rpc_SubmitFlashbuyChoices(int[] chosenCardIds, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player activator = info.Sender;

        foreach (int cardId in chosenCardIds)
        {
            CardMultiplayer cardData = GetCardFromTable(cardId);
            if (cardData != null)
            {
                Debug.Log($"[Flashbuy] MasterClient memberikan kartu {cardData.cardName} ke {activator.NickName}.");

                // Tambahkan kartu ke inventaris pemain (tanpa biaya)
                string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(cardData.color.ToString());
                if (!string.IsNullOrEmpty(cardKey))
                {
                    int currentCards = activator.CustomProperties.ContainsKey(cardKey) ? (int)activator.CustomProperties[cardKey] : 0;
                    Hashtable props = new Hashtable { { cardKey, currentCards + 1 } };
                    activator.SetCustomProperties(props);
                }

                // Hapus kartu dari meja
                photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);
                cardsTaken++;
            }
        }
        AdvanceToNextTurn();
    }

    private void ExitFlashbuyMode()
    {
        isInFlashbuyMode = false;

        // Kembalikan UI ke kondisi normal
        actionButtonsPanel.transform.Find("SaveButton").GetComponentInChildren<Text>().text = "Save";
        actionButtonsPanel.transform.Find("ActivateButton").gameObject.SetActive(true);
        actionButtonsPanel.SetActive(false); // Sembunyikan lagi

        // Reset visual kartu yang tadi dipilih
        foreach (int cardId in flashbuySelectedCardIds)
        {
            // Gunakan ElementAtOrDefault untuk keamanan
            GameObject cardObject = instantiatedCards.ElementAtOrDefault(cardId);
            if (cardObject != null)
            {
                // Reset ke skala default, bukan originalCardScale yang rapuh
                cardObject.transform.localScale = defaultCardScale;
            }
        }
    }

    public void OnSkipTurnClicked()
    {
        // Kirim permintaan untuk melewati giliran ke MasterClient
        photonView.RPC("Rpc_RequestSkipTurn", RpcTarget.MasterClient);

        // Langsung sembunyikan UI di sisi klien
        if (isInFlashbuyMode)
        {
            ExitFlashbuyMode();
        }
        else
        {
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
        // --- BLOK LOGIKA 1: MODE FLASHBUY ---
        if (isInFlashbuyMode)
        {
            // Hanya pemain yang mengaktifkan yang boleh memilih
            if (PhotonNetwork.LocalPlayer.ActorNumber != this.currentPlayerActorNumber) return;

            // Ambil objek kartu dengan aman
            GameObject cardObject = instantiatedCards.ElementAtOrDefault(cardId);
            if (cardObject == null) return; // Hentikan jika kartu tidak valid atau sudah diambil

            // Cek apakah kartu sudah ada di daftar pilihan
            if (flashbuySelectedCardIds.Contains(cardId))
            {
                // Jika sudah ada, batalkan pilihan (deselect)
                flashbuySelectedCardIds.Remove(cardId);
                // Kembalikan ukurannya ke skala normal
                cardObject.transform.localScale = defaultCardScale;
            }
            else
            {
                // Jika belum ada, pilih kartu baru selama belum mencapai batas 2 kartu
                if (flashbuySelectedCardIds.Count < 2)
                {
                    flashbuySelectedCardIds.Add(cardId);
                    // Perbesar ukurannya sebagai tanda visual
                    cardObject.transform.localScale = defaultCardScale * 1.1f;
                }
            }
            // Setelah logika Flashbuy selesai, hentikan eksekusi fungsi ini
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
            // Jika dalam mode Flashbuy, tombol ini berfungsi sebagai konfirmasi
            OnConfirmFlashbuySelection();
        }
        else
        {
            // Jika tidak, berfungsi sebagai "Save Card" biasa
            photonView.RPC("RequestSaveCard", RpcTarget.MasterClient, selectedCardId, PhotonNetwork.LocalPlayer);
            HideAndResetSelection();
        }
    }

    // Buat fungsi baru ini untuk menangani konfirmasi Flashbuy
    private void OnConfirmFlashbuySelection()
    {
        Debug.Log($"[Flashbuy] Mengkonfirmasi pilihan: {flashbuySelectedCardIds.Count} kartu.");
        // Kirim pilihan kartu ke MasterClient
        photonView.RPC("Rpc_SubmitFlashbuyChoices", RpcTarget.MasterClient, flashbuySelectedCardIds.ToArray());
        
        // Keluar dari mode flashbuy di sisi klien
        ExitFlashbuyMode();
    }

    public void OnActivateButtonClicked()
    {
        photonView.RPC("RequestActivateCard", RpcTarget.MasterClient, selectedCardId, PhotonNetwork.LocalPlayer);
        HideAndResetSelection();
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
        if (!PhotonNetwork.IsMasterClient || requestingPlayer.ActorNumber != this.currentPlayerActorNumber) return;
        CardMultiplayer cardData = GetCardFromTable(cardId);
        if (cardData == null) 
        {
            // Jika kartu tidak valid (misal, sudah diambil), tetap majukan giliran agar tidak macet
            AdvanceToNextTurn();
            return;
        }
        int currentFinpoint = (int)requestingPlayer.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY];

        if (currentFinpoint >= cardData.value)
        {
            Hashtable props = new Hashtable { { PlayerProfileMultiplayer.FINPOINT_KEY, currentFinpoint - cardData.value } };
            requestingPlayer.SetCustomProperties(props);
            cardsTaken++;
            photonView.RPC("Rpc_RemoveCardFromTable", RpcTarget.All, cardId);
            StartCoroutine(CardEffectManagerMultiplayer.ApplyEffect(cardData.cardName, requestingPlayer, cardData.color));
            if (cardData.cardName != "Flashbuy" && 
            cardData.cardName != "TenderOffer" && 
            cardData.cardName != "TradeFee")
            {
                // Untuk kartu dengan efek instan (Stock Split, Insider Trade), langsung majukan giliran.
                AdvanceToNextTurn();
            }
        }
        else
        {
            Debug.LogWarning($"[ACTIVATE GAGAL] {requestingPlayer.NickName} tidak punya cukup Finpoint.");
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