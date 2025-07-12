using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
[System.Serializable]
public class CardTextureMapping
{
    public string cardName;
    public string color;
    public Sprite cardSprite;
}
public class GameManager : MonoBehaviour
{
    public TicketManager ticketManager;
    [SerializeField] private SellingPhaseManager sellingManager;
    public RumorPhaseManager rumorPhaseManager;
    public HelpCardPhaseManager helpCardPhaseManager;
    public ResolutionPhaseManager resolutionPhaseManager;

    private CardEffectManager cardEffect;
    private PlayerProfile player;


    [Header("UI References")]
    public GameObject playerEntryPrefab;
    public Transform playerListContainer;
    public GameObject cardPrefab;
    public Transform cardHolderParent;
    public GameObject ticketButtonPrefab;
    public Transform ticketListContainer;
    public GameObject activateButtonPrefab;
    public GameObject saveButtonPrefab;
    public Transform ActiveSaveContainer;
    public GameObject leaderboardPanel;
    public Transform leaderboardContainer;
    public GameObject leaderboardEntryPrefab;
    [Header("Card Visuals")]
    public List<CardTextureMapping> cardTextureMappings; // ⬅️ TAMBAHKAN INI


    [Header("Button References")]
    public Button bot2Button;
    public Button bot3Button;
    public Button bot4Button;
    public GameObject resetSemesterButton;
    public GameObject skipButton;
    [Header("Ticket Sprites")]
    public Sprite defaultTicketSprite; // Texture A
    public List<Sprite> ticketNumberSprites;




    public static GameManager Instance;
    private List<PlayerProfile> bots = new List<PlayerProfile>();
    private List<GameObject> playerEntries = new List<GameObject>();
    private List<Card> deck = new List<Card>();
    public List<PlayerProfile> turnOrder = new List<PlayerProfile>();
    private List<GameObject> cardObjects = new List<GameObject>();
    private HashSet<GameObject> takenCards = new HashSet<GameObject>();
    private List<GameObject> ticketButtons = new List<GameObject>();
    private bool ticketChosen = false;

    private int totalCards = 2;
    GameObject currentlySelectedCard = null;
    GameObject activateBtnInstance = null;
    GameObject saveBtnInstance = null;
    Vector3 originalScale = Vector3.one;


    public int currentCardIndex = 0;
    private int currentTurnIndex = 0;
    public int skipCount = 0;
    public int resetCount = 0;
    public int maxResetCount = 3;

    private Coroutine autoSelectCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetSemesterButton()
    {


        if (resetCount >= maxResetCount)
        {
            ShowLeaderboard();
            Debug.Log("Semester sudah berakhir");
        }
        else
        {
            ResetSemester();
        }

        Debug.Log("Semeter telah selesai");
    }
    // Fungsi yang dipanggil saat semester direset
    public void ResetSemester()
    {
        Debug.Log("🔁 Resetting Semester...");
        resetCount++;

        // Reset data internal pemain
        foreach (var p in turnOrder)
        {
            p.ticketNumber = 0;
            // Jika mau hapus kartu juga, bisa panggil: p.ClearCards(); (jika tersedia)
        }

        rumorPhaseManager.InitializeRumorDeck();

        currentCardIndex = 0;
        currentTurnIndex = 0;
        skipCount = 0;
        ticketChosen = false;
        takenCards.Clear();
        turnOrder.Clear();

        // Bersihkan kartu dari UI
        ClearHiddenCards();
        ClearAllCardsInHolder();

        // Bersihkan UI pemain
        ClearPlayerListUI();

        // Tampilkan kembali pilihan tiket
        ShowTicketChoices();
        resetSemesterButton.SetActive(false);

    }


    private void Start()
    {
        player = new PlayerProfile("You");
        if (resetSemesterButton != null)
            resetSemesterButton.SetActive(false); // ganti dengan nama canvas kamu
        skipButton.SetActive(false);



        bot2Button.onClick.AddListener(() => SetBotCount(2));
        bot3Button.onClick.AddListener(() => SetBotCount(3));
        bot4Button.onClick.AddListener(() => SetBotCount(4));


    }


    private void SetBotCount(int count)
    {
        bots.Clear();
        for (int i = 0; i < count; i++)
        {
            bots.Add(new PlayerProfile("Bot " + (i + 1)));
        }

        ShowTicketChoices();

    }
    public int GetPlayerCount()
    {
        int totalPlayers = bots.Count + 1;
        return totalPlayers;
    }
    private void ShowTicketChoices()
    {
        ClearTicketButtons();

        ticketChosen = false;

        int totalPlayers = bots.Count + 1;
        // 1 player + bots
        ticketManager.InitializeTickets(totalPlayers); // Isi tiket 1..n
        List<int> availableTickets = new List<int>();
        for (int i = 1; i <= totalPlayers; i++)
        {
            availableTickets.Add(i);
        }

        // ⬇️ Acak posisi ticket sebelum buat button
        TicketManager.ShuffleList(availableTickets);

        foreach (int ticketNumber in availableTickets)
        {
            GameObject btnObj = Instantiate(ticketButtonPrefab, ticketListContainer);
            ticketButtons.Add(btnObj);

            // Set sprite awal (belum dipilih)
            Image img = btnObj.GetComponent<Image>();
            if (img != null && defaultTicketSprite != null)
            {
                img.sprite = defaultTicketSprite;
            }


            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                int chosenTicket = ticketNumber;
                btn.onClick.AddListener(() =>
                {
                    OnTicketSelected(chosenTicket, btnObj);
                });
            }
        }

        // Jalankan timer auto-pilih jika player tidak klik


    }

    private void OnTicketSelected(int chosenTicket, GameObject clickedButton)
    {
        if (ticketChosen) return;
        ticketChosen = true;

        // Stop auto-select
        if (autoSelectCoroutine != null)
        {
            StopCoroutine(autoSelectCoroutine);
            autoSelectCoroutine = null;
        }

        player.ticketNumber = ticketManager.PickTicketForPlayer(chosenTicket);


        // 🟡 Ganti sprite tombol yang diklik
        Image img = clickedButton.GetComponent<Image>();
        if (img != null && ticketNumberSprites.Count >= chosenTicket)
        {
            img.sprite = ticketNumberSprites[chosenTicket - 1]; // karena index mulai dari 0
        }

        // ⏳ Mulai delay 3 detik buat bot
        StartCoroutine(AssignTicketsToBotsAfterDelay());
    }


    private IEnumerator AssignTicketsToBotsAfterDelay()
    {
        yield return new WaitForSeconds(3f); // ⏳ Tunggu 3 detik

        foreach (var bot in bots)
        {
            bot.ticketNumber = ticketManager.GetRandomTicketForBot();
        }

        ClearTicketButtons();
        yield return StartCoroutine(resolutionPhaseManager.RevealNextTokenForAllColors());
        yield return new WaitForSeconds(1.5f);
        ResetAll();
    }


    private void ClearTicketButtons()
    {
        foreach (var btn in ticketButtons)
        {
            Destroy(btn);
        }
        ticketButtons.Clear();
    }
    private void AssignTickets()
    {
        List<int> availableTickets = new List<int>();
        int totalPlayers = bots.Count + 1; // 1 player + bots

        for (int i = 1; i <= totalPlayers; i++)
        {
            availableTickets.Add(i);
        }

        // Acak tiket
        for (int i = 0; i < availableTickets.Count; i++)
        {
            int randIndex = Random.Range(i, availableTickets.Count);
            int temp = availableTickets[i];
            availableTickets[i] = availableTickets[randIndex];
            availableTickets[randIndex] = temp;
        }

        player.ticketNumber = availableTickets[0];
        for (int i = 0; i < bots.Count; i++)
        {
            bots[i].ticketNumber = availableTickets[i + 1];
        }
    }



    private void ResetAll()
    {
        ClearPlayerListUI();
        AddPlayerEntry(player.playerName, player.ticketNumber, player.cardCount, player.finpoint);

        foreach (var bot in bots)
        {
            AddPlayerEntry(bot.playerName, bot.ticketNumber, bot.cardCount, bot.finpoint);
        }

        List<PlayerProfile> allPlayers = new List<PlayerProfile> { player };
        allPlayers.AddRange(bots);
        allPlayers.Sort((a, b) => a.ticketNumber.CompareTo(b.ticketNumber)); // 🎟️ Urut berdasarkan tiket kecil ke besar

        turnOrder = new List<PlayerProfile>(allPlayers);
        UpdatePlayerUI();

        DrawCardsInOrder();
    }



    private void InitializeDeck()
    {
        deck.Clear();


        List<string> colors = new List<string> { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };


        deck.Add(new Card("TradeFree", "Deal 5 damage", 1, GetRandomColor(colors)));
        deck.Add(new Card("TenderOffer", "Recover 3 HP", 0, GetRandomColor(colors)));
        deck.Add(new Card("StockSplit", "Block next attack", 1, GetRandomColor(colors)));
        deck.Add(new Card("InsiderTrade", "Take 1 card", 2, GetRandomColor(colors)));
        deck.Add(new Card("Flashbuy", "Take 2 more cards", 3, GetRandomColor(colors)));

        ShuffleDeck();

        int totalCardsToGive = totalCards * (bots.Count + 1);

        if (deck.Count < totalCardsToGive)
        {
            int cardsNeeded = totalCardsToGive - deck.Count;
            for (int i = 0; i < cardsNeeded; i++)
            {
                Card randomCard = deck[Random.Range(0, deck.Count)];
                deck.Add(new Card(randomCard.cardName, randomCard.description, randomCard.value, GetRandomColor(colors)));
            }
        }
        else if (deck.Count > totalCardsToGive)
        {
            deck = new List<Card>(deck.GetRange(0, totalCardsToGive));
        }


        // Update nilai kartu setelah deck dibuat
        UpdateDeckCardValuesWithIPO();
    }

    public void UpdateDeckCardValuesWithIPO()
    {
        foreach (Card card in deck)
        {
            int ipoValue = sellingManager.GetFullCardPrice(card.color);
            card.value = card.baseValue + ipoValue;
        }
        Debug.Log("Update harga");
        for (int i = 0; i < cardObjects.Count; i++)
        {
            GameObject cardObj = cardObjects[i];
            Card card = deck[i];

            Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
            if (cardValueText != null)
            {
                cardValueText.text = card.value.ToString();
            }
        }
    }


    private int GetCardValue(GameObject cardObj)
    {
        Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
        int cardValue = 0;
        if (cardValueText != null) int.TryParse(cardValueText.text, out cardValue);
        return cardValue;
    }
    private Sprite GetCardSprite(string cardName, string color)
    {
        var mapping = cardTextureMappings.FirstOrDefault(m => m.cardName == cardName && m.color == color);
        return mapping != null ? mapping.cardSprite : null;
    }


    private string GetRandomColor(List<string> colorOptions)
    {
        return colorOptions[Random.Range(0, colorOptions.Count)];
    }


    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int rand = Random.Range(i, deck.Count);
            deck[i] = deck[rand];
            deck[rand] = temp;
        }
    }

    private void DrawCardsInOrder()
    {
        InitializeDeck();
        ClearAllCardsInHolder();
        cardObjects.Clear();
        int totalCardsToGive = totalCards * (bots.Count + 1);

        for (int i = 0; i < totalCardsToGive && i < deck.Count; i++)
        {
            Card card = deck[i];
            GameObject cardObj = Instantiate(cardPrefab, cardHolderParent);
            Sprite sprite = GetCardSprite(card.cardName, card.color);
            Image cardImage = cardObj.transform.Find("CardImage")?.GetComponent<Image>();
            if (cardImage != null && sprite != null)
            {
                cardImage.sprite = sprite;
            }


            // Ambil Text untuk nama kartu
            Text cardText = cardObj.transform.Find("CardText")?.GetComponent<Text>();
            if (cardText != null) cardText.text = card.cardName;

            // Ambil Text untuk warna kartu
            Text cardColorText = cardObj.transform.Find("CardColor")?.GetComponent<Text>();
            if (cardColorText != null) cardColorText.text = card.color;


            // Ambil Text untuk nilai kartu
            Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
            if (cardValueText != null) cardValueText.text = card.value.ToString();

            cardObjects.Add(cardObj);
        }


        currentCardIndex = 0;
        currentTurnIndex = 0;
        StartCoroutine(NextTurn());
    }
    public void RefreshCardValuesUI()
    {
        for (int i = 0; i < cardObjects.Count; i++)
        {
            GameObject cardObj = cardObjects[i];
            Card card = deck[i];

            Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
            if (cardValueText != null)
            {
                cardValueText.text = card.value.ToString();
            }
        }
    }


    private IEnumerator NextTurn()
    {
        sellingManager.InitializePlayers(turnOrder);
        int totalCardsToGive = totalCards * (bots.Count + 1);
        if (skipCount >= turnOrder.Count)
        {

            Debug.Log("🚫 Semua pemain skip. Menghapus kartu yang tersisa.");
            ClearHiddenCards();
            currentCardIndex = totalCardsToGive; // anggap sudah selesai
            skipCount = 0;

            yield return new WaitForSeconds(2f);

            Debug.Log("Memulai fase penjualan...");
            helpCardPhaseManager.StartHelpCardPhase(turnOrder, resetCount);


            yield break;

        }
        if (currentCardIndex >= totalCardsToGive || currentCardIndex >= cardObjects.Count)
        {
            Debug.Log("✅ Semua kartu sudah dibagikan.");

            yield return new WaitForSeconds(1f); // Delay sedikit biar visual terlihat
            ClearHiddenCards(); // 🔥 Hapus semua kartu dari UI

            Debug.Log("Memulai fase penjualan...");
            helpCardPhaseManager.StartHelpCardPhase(turnOrder, resetCount);


            yield break;
        }

        PlayerProfile currentPlayer = turnOrder[currentTurnIndex];

        if (currentPlayer.playerName.Contains("You"))
        {
            bool cardTaken = false;
            List<Button> clickableButtons = new List<Button>();

            if (skipButton != null && currentCardIndex < Mathf.Min(totalCardsToGive, cardObjects.Count))
            {
                skipButton.SetActive(true);
                skipButton.GetComponent<Button>().onClick.RemoveAllListeners();
                skipButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    skipButton.SetActive(false);
                    ResetCardSelection();
                    skipCount++;
                    currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
                    StartCoroutine(NextTurn());
                });
            }

            for (int i = 0; i < cardObjects.Count; i++)
            {
                GameObject obj = cardObjects[i];
                if (obj == null || takenCards.Contains(obj)) continue;

                Button cardBtn = obj.GetComponent<Button>();
                if (cardBtn != null)
                {
                    cardBtn.onClick.RemoveAllListeners();
                    int index = i;

                    cardBtn.onClick.AddListener(() =>
                    {
                        if (cardTaken) return;

                        // Jika sudah ada kartu lain yang dipilih, reset dulu
                        if (currentlySelectedCard != null && currentlySelectedCard != obj)
                        {
                            ResetCardSelection();
                        }

                        if (currentlySelectedCard == obj) return; // Sudah aktif

                        currentlySelectedCard = obj;
                        originalScale = obj.transform.localScale;
                        obj.transform.localScale = originalScale * 1.1f;

                        // Ganti canvasTransform dengan reference ke Canvas utama

                        activateBtnInstance = Instantiate(activateButtonPrefab, ActiveSaveContainer);
                        saveBtnInstance = Instantiate(saveButtonPrefab, ActiveSaveContainer);
                        // Set posisi tetap di layar (misalnya di tengah bawah laya
                        activateBtnInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(-100, -150);
                        saveBtnInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, -150);


                        activateBtnInstance.GetComponent<Button>().onClick.AddListener(() =>
                        {

                            Text cardNameText = obj.transform.Find("CardText")?.GetComponent<Text>();
                            Text cardColorText = obj.transform.Find("CardColor")?.GetComponent<Text>();
                            string cardName = cardNameText != null ? cardNameText.text : "";
                            string cardColor = cardColorText != null ? cardColorText.text : "";

                            // 1. Cek dulu apakah syarat aktivasi efek terpenuhi
                            if (!CanActivateEffect(cardName, cardColor, currentPlayer))
                            {
                                // Jika tidak, tampilkan pesan error spesifik dan hentikan aksi
                                Debug.LogWarning(GetActivationErrorMessage(cardName));
                                return; // Hentikan eksekusi
                            }

                            // 2. Jika syarat efek terpenuhi, baru cek finpoint
                            int cardValue = GetCardValue(obj);
                            if (!currentPlayer.CanAfford(cardValue))
                            {
                                Debug.LogWarning($"{currentPlayer.playerName} tidak punya finpoint cukup ({cardValue} FP) untuk mengaktifkan kartu ini.");
                                return; // Hentikan eksekusi
                            }


                            cardTaken = true;
                            StartCoroutine(ActivateCardAndProceed(obj, currentPlayer));

                            // --- MODIFIKASI SELESAI ---
                        });


                        saveBtnInstance.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            int cardValue = GetCardValue(obj);
                            if (currentPlayer.finpoint < cardValue)
                            {
                                Debug.LogWarning($"{currentPlayer.playerName} tidak punya finpoint cukup untuk menyimpan kartu ini.");
                                return;
                            }
                            cardTaken = true;
                            ResetCardSelection();
                            if (skipButton != null) skipButton.SetActive(false);

                            // 2. Memanggil TakeCard (ini adalah metode void, jadi langsung dijalankan).
                            TakeCard(obj, player);

                            // 3. Setelah kartu diambil, perbarui status giliran.
                            Debug.Log("✅ TakeCard selesai. Melanjutkan giliran.");
                            skipCount = 0;
                            currentCardIndex++;
                            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

                            // 4. Mulai giliran berikutnya.
                            StartCoroutine(NextTurn());

                        });
                    });
                }
            }


        }

        else
        {
            yield return new WaitForSeconds(1f);

            // Cari kartu yang masih tersedia
            List<GameObject> availableCards = cardObjects.FindAll(c => c != null && !takenCards.Contains(c));

            // Periksa apakah bot akan skip
            bool botSkips = Random.value < 0.3f; // 30% kemungkinan skip
            if (botSkips)
            {
                skipCount++;
                Debug.Log($"{currentPlayer.playerName} skipped their turn.");

                // Ganti giliran ke pemain berikutnya
                currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
                StartCoroutine(NextTurn());
                yield break; // keluar dari coroutine untuk mencegah aksi lanjutan
            }

            // Jika tidak skip, lanjut ke ambil/aktifkan kartu
            bool botActivates = Random.value < 0.7f; // 70% kemungkinan bot menyimpan kartu
                                                     // Filter kartu yang bisa diambil oleh bot berdasarkan finpoint
            List<GameObject> affordableCards = availableCards.FindAll(card =>
            {
                int val = GetCardValue(card);
                return currentPlayer.finpoint >= val;
            });

            if (affordableCards.Count == 0)
            {
                Debug.Log($"{currentPlayer.playerName} tidak mampu mengambil kartu manapun.");
                skipCount++;
                currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
                StartCoroutine(NextTurn());
                yield break;
            }

            // ... di dalam blok 'else' untuk giliran Bot

            GameObject randomCard = affordableCards[Random.Range(0, affordableCards.Count)];

            // --- MODIFIKASI DIMULAI ---
            // Ambil nama dan warna kartu untuk pengecekan
            Text cardNameTextBot = randomCard.transform.Find("CardText")?.GetComponent<Text>();
            Text cardColorTextBot = randomCard.transform.Find("CardColor")?.GetComponent<Text>();
            string cardNameBot = cardNameTextBot != null ? cardNameTextBot.text : "";
            string cardColorBot = cardColorTextBot != null ? cardColorTextBot.text : "";

            // Cek apakah bot boleh mengaktifkan kartu ini
            bool canBotActivate = CanActivateEffect(cardNameBot, cardColorBot, currentPlayer);

            // Bot akan mencoba mengaktifkan HANYA JIKA syarat terpenuhi DAN ia memutuskan untuk aktif
            if (canBotActivate && botActivates)
            {
                Debug.Log($"[BOT-LOGIC] {currentPlayer.playerName} memenuhi syarat dan memilih untuk MENGAKTIFKAN '{cardNameBot}'.");
                yield return StartCoroutine(ActivateCard(randomCard, currentPlayer));
            }
            else
            {
                // Jika syarat tidak terpenuhi ATAU bot memilih untuk tidak aktif, ia akan MENYIMPAN kartu.
                if (!canBotActivate)
                {
                    Debug.Log($"[BOT-LOGIC] {currentPlayer.playerName} TIDAK memenuhi syarat untuk '{cardNameBot}', jadi MENYIMPAN kartu.");
                }
                else
                {
                    Debug.Log($"[BOT-LOGIC] {currentPlayer.playerName} memenuhi syarat, tapi memilih untuk MENYIMPAN kartu.");
                }
                TakeCard(randomCard, currentPlayer);
            }
            // --- MODIFIKASI SELESAI ---

            // Reset skip counter karena aksi diambil
            currentCardIndex++;
            skipCount = 0;
            // ... (sisa kodenya tetap sama)

            // Lanjut ke giliran berikutnya
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            StartCoroutine(NextTurn());

        }


    }
    public bool CanActivateEffect(string cardName, string cardColor, PlayerProfile activator)
    {
        switch (cardName)
        {
            case "TradeFree":
            case "StockSplit":
                // Syarat: Pengaktif harus punya minimal 1 kartu tersimpan dengan warna yang sama.
                return activator.GetCardColorCounts()[cardColor] >= 1;

            case "TenderOffer":
                // Syarat: Harus ada target yang jumlah kartunya di warna itu lebih sedikit dari si pengaktif.
                int activatorColorCount = activator.GetCardColorCounts()[cardColor];
                if (activatorColorCount < 1) return false; // Pengaktif harus punya minimal 1 untuk perbandingan.

                // Cari di semua pemain lain (turnOrder berisi semua pemain)
                foreach (var target in turnOrder)
                {
                    if (target == activator) continue; // Jangan bandingkan dengan diri sendiri

                    int targetColorCount = target.GetCardColorCounts()[cardColor];
                    if (targetColorCount < activatorColorCount)
                    {
                        return true; // Ditemukan target yang valid!
                    }
                }
                return false; // Tidak ada target yang memenuhi syarat.

            case "Flashbuy":
                int availableCardsCount = cardObjects.Count(c => c != null && c.activeSelf && !takenCards.Contains(c));
                return availableCardsCount > 1;

            default:
                // Kartu lain tidak punya syarat khusus.
                return true;
        }
    }
    private IEnumerator ActivateCardAndProceed(GameObject cardObj, PlayerProfile player)
    {
        // 1. Membersihkan UI (tombol, dll.)
        ResetCardSelection();
        if (skipButton != null) skipButton.SetActive(false);

        // --- MODIFIKASI DIMULAI ---
        // Ambil nama kartu terlebih dahulu
        Text cardNameText = cardObj.transform.Find("CardText")?.GetComponent<Text>();
        string cardName = cardNameText != null ? cardNameText.text : "";

        // Sembunyikan holder kartu HANYA JIKA kartu yang diaktifkan BUKAN "Flashbuy"
        if (cardName != "Flashbuy")
        {
            if (cardHolderParent != null) cardHolderParent.gameObject.SetActive(false);
            yield return new WaitForSeconds(1f);
        }



        // 2. Memanggil ActivateCard dan MENUNGGU sampai selesai.
        // Ini akan menjalankan HandleFlashbuySelection jika kartunya adalah Flashbuy.
        yield return StartCoroutine(ActivateCard(cardObj, player));

        // 3. Setelah efek selesai, perbarui status giliran.
        yield return new WaitForSeconds(1.5f);
        if (cardHolderParent != null) cardHolderParent.gameObject.SetActive(true);
        Debug.Log("✅ Efek ActivateCard selesai. Melanjutkan giliran.");
        skipCount = 0;
        currentCardIndex++;
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

        // 4. Mulai giliran berikutnya.
        StartCoroutine(NextTurn());
    }
    public IEnumerator HandleFlashbuySelection(PlayerProfile currentPlayer)
{
    // Menggunakan pola yang sama persis dengan NextTurn
    if (currentPlayer.playerName.Contains("You"))
    {
        // --- LOGIKA UNTUK PEMAIN MANUSIA ---
        if (cardHolderParent != null) cardHolderParent.gameObject.SetActive(true);

        List<GameObject> selectedCards = new List<GameObject>();
        Dictionary<Button, UnityEngine.Events.UnityAction> originalListeners = new Dictionary<Button, UnityEngine.Events.UnityAction>();

        GameObject takeButtonObj = Instantiate(saveButtonPrefab, ActiveSaveContainer);
        takeButtonObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
        Button takeButton = takeButtonObj.GetComponent<Button>();
        Text takeButtonText = takeButton.GetComponentInChildren<Text>();
        
        // --- MODIFIKASI 1 ---
        // Teks awal diubah menjadi "Lewati" karena 0 kartu dipilih
        takeButtonText.text = "Lewati"; 
        // Tombol langsung bisa ditekan untuk kasus 0 kartu
        takeButton.interactable = true; 

        System.Action cleanupUI = () =>
        {
            foreach (var card in selectedCards)
            {
                if (card != null) card.transform.localScale = Vector3.one;
            }
            foreach (var pair in originalListeners)
            {
                if (pair.Key != null)
                {
                    pair.Key.onClick.RemoveAllListeners();
                    if (pair.Value != null) pair.Key.onClick.AddListener(pair.Value);
                }
            }
            if (takeButtonObj != null) Destroy(takeButtonObj);
        };

        List<GameObject> availableCards = cardObjects.FindAll(c => c != null && c.activeSelf && !takenCards.Contains(c));
        foreach (var cardObj in availableCards)
        {
            Button cardBtn = cardObj.GetComponent<Button>();
            if (cardBtn == null) continue;

            var registeredListeners = new UnityEngine.Events.UnityAction(() => cardBtn.onClick.Invoke());
            originalListeners[cardBtn] = cardBtn.onClick.GetPersistentEventCount() > 0 ? registeredListeners : null;

            cardBtn.onClick.RemoveAllListeners();
            cardBtn.onClick.AddListener(() =>
            {
                if (selectedCards.Contains(cardObj))
                {
                    selectedCards.Remove(cardObj);
                    cardObj.transform.localScale = Vector3.one;
                }
                else
                {
                    if (selectedCards.Count < 2)
                    {
                        selectedCards.Add(cardObj);
                        cardObj.transform.localScale = Vector3.one * 1.1f;
                    }
                    else
                    {
                        Debug.LogWarning("[Flashbuy] Maksimal 2 kartu yang bisa dipilih.");
                    }
                }

                // --- MODIFIKASI 2 ---
                // Logika untuk tombol konfirmasi diubah di sini
                int totalCost = 0;
                foreach (var selectedCard in selectedCards) totalCost += GetCardValue(selectedCard);

                // Jika tidak ada kartu dipilih, tampilkan teks "Lewati"
                if (selectedCards.Count == 0)
                {
                    takeButtonText.text = "Lewati";
                }
                else
                {
                    // Jika ada kartu, tampilkan detailnya
                    takeButtonText.text = $"Ambil ({selectedCards.Count}) - {totalCost} FP";
                }

                // Tombol bisa ditekan HANYA berdasarkan apakah finpoint cukup
                // Untuk 0 kartu, totalCost adalah 0, jadi akan selalu bisa.
                takeButton.interactable = currentPlayer.CanAfford(totalCost);
            });
        }

        bool purchaseAttempted = false;
        takeButton.onClick.AddListener(() =>
        {
            purchaseAttempted = true;
        });

        yield return new WaitUntil(() => purchaseAttempted);

        int finalCost = 0;
        foreach (var card in selectedCards) finalCost += GetCardValue(card);

        if (currentPlayer.CanAfford(finalCost))
        {
            // Jika tidak ada kartu dipilih (finalCost == 0), pesan ini tetap valid
            Debug.Log($"[Flashbuy] {currentPlayer.playerName} mencoba membeli {selectedCards.Count} kartu seharga {finalCost} FP.");
            
            // Jika selectedCards kosong, loop ini tidak akan berjalan, yang mana sudah benar
            List<GameObject> cardsToProcess = new List<GameObject>(selectedCards);
            foreach (var cardToTake in cardsToProcess)
            {
                TakeCard(cardToTake, currentPlayer);
                currentCardIndex++;
            }
        }
        else
        {
            // Pesan ini hanya akan muncul jika pemain mencoba membeli kartu yang tidak mampu mereka bayar
            Debug.LogWarning($"[Flashbuy] Pembelian dibatalkan, Finpoint tidak cukup.");
        }

        cleanupUI();
        UpdatePlayerUI();
        yield return new WaitForSeconds(1f);
    }
        else // Ini adalah giliran Bot
        {
            // --- LOGIKA BARU DAN HANDAL UNTUK BOT ---
            Debug.Log($"[Flashbuy] {currentPlayer.playerName} (Bot) sedang memilih kartu...");
            yield return new WaitForSeconds(1.5f);

            int cardsBought = 0;
            for (int i = 0; i < 2; i++)
            {
                var affordableCards = cardObjects
                    .Where(c => c != null && c.activeSelf && !takenCards.Contains(c) && currentPlayer.CanAfford(GetCardValue(c)))
                    .OrderByDescending(c => GetCardValue(c))
                    .ToList();

                if (affordableCards.Count > 0)
                {
                    GameObject cardToTake = affordableCards.First();
                    Debug.Log($"[Flashbuy] {currentPlayer.playerName} (Bot) membeli {cardToTake.name}.");
                    TakeCard(cardToTake, currentPlayer);
                    currentCardIndex++;
                    cardsBought++;
                }
                else
                {
                    break;
                }
            }

            if (cardsBought == 0)
            {
                Debug.Log($"[Flashbuy] {currentPlayer.playerName} (Bot) tidak membeli kartu tambahan.");
            }
        }

        // --- PENGECEKAN KONDISI AKHIR FASE ---
        int totalCardsToGive = totalCards * (bots.Count + 1);
        if (takenCards.Count >= totalCardsToGive || takenCards.Count >= cardObjects.Count)
        {
            Debug.Log("✅ Semua kartu sudah dibagikan setelah Flashbuy.");

            yield return new WaitForSeconds(1f);
            ClearHiddenCards();

            Debug.Log("Memulai fase penjualan...");
            helpCardPhaseManager.StartHelpCardPhase(turnOrder, resetCount);
        }
    }
    private IEnumerator ActivateCard(GameObject cardObj, PlayerProfile currentPlayer)
    {
        Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
        int cardValue = 0;
        if (cardValueText != null) int.TryParse(cardValueText.text, out cardValue);

        // Kurangi finpoint sesuai nilai kartu
        currentPlayer.finpoint -= cardValue;
        if (cardObj == null || takenCards.Contains(cardObj)) yield break;


        Text cardNameText = cardObj.transform.Find("CardText")?.GetComponent<Text>();
        string cardName = cardNameText != null ? cardNameText.text : "";

        // Ambil warna kartu
        Text cardColorText = cardObj.transform.Find("CardColor")?.GetComponent<Text>();
        string cardColor = cardColorText != null ? cardColorText.text : "Konsumer";

        // Tandai kartu sudah diambil
        takenCards.Add(cardObj);

        // Nonaktifkan klik dan buat buram
        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
        cg.alpha = 0.3f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Sembunyikan semua anak dari kartu
        foreach (Transform child in cardObj.transform)
        {
            child.gameObject.SetActive(false);
        }

        // Perbarui UI
        UpdatePlayerUI();
        if (!string.IsNullOrEmpty(cardName))
        {
            // Kirim nama, pemain, dan warna ke efek
            Debug.Log($"🎴 Kartu '{cardName}' ({cardColor}) diaktifkan untuk {currentPlayer.playerName}");
            yield return StartCoroutine(CardEffectManager.ApplyEffect(cardName, currentPlayer, cardColor));

        }
        else
        {
            Debug.LogWarning("⚠️ Nama kartu tidak ditemukan. Efek tidak dijalankan.");
            yield break;
        }
        UpdatePlayerUI();

    }

    void ResetCardSelection()
    {
        if (currentlySelectedCard != null)
        {
            currentlySelectedCard.transform.localScale = originalScale;
        }

        if (activateBtnInstance != null) Destroy(activateBtnInstance);
        if (saveBtnInstance != null) Destroy(saveBtnInstance);

        currentlySelectedCard = null;
        activateBtnInstance = null;
        saveBtnInstance = null;
    }



    private void ClearHiddenCards()
    {
        foreach (var cardObj in cardObjects)
        {
            if (cardObj != null)
            {
                Destroy(cardObj);
            }
        }
        cardObjects.Clear();
    }




    private void OnCardClicked(GameObject cardObj, int cardIndex)
    {
        if (currentCardIndex != cardIndex) return;

        PlayerProfile currentPlayer = turnOrder[currentTurnIndex];
        if (!currentPlayer.playerName.Contains("You")) return;

        TakeCard(cardObj, currentPlayer);
        currentCardIndex++;
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

        StartCoroutine(AutoBotTurn());
    }

    private IEnumerator AutoBotTurn()
    {
        int totalCardsToGive = totalCards * (bots.Count + 1);
        while (currentCardIndex < totalCardsToGive)
        {
            PlayerProfile currentPlayer = turnOrder[currentTurnIndex];

            if (!currentPlayer.playerName.Contains("You"))
            {
                yield return new WaitForSeconds(1f);

                // 🔄 Cari semua kartu yang masih aktif
                List<GameObject> availableCards = new List<GameObject>();
                foreach (GameObject card in cardObjects)
                {
                    if (card != null) availableCards.Add(card);
                }

                if (availableCards.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableCards.Count);
                    GameObject selectedCard = availableCards[randomIndex];

                    TakeCard(selectedCard, currentPlayer);
                    currentCardIndex++;
                }
            }

            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            yield return null;
        }

        Debug.Log("✅ Semua kartu sudah diambil oleh player & bot.");
    }



    private void TakeCard(GameObject cardObj, PlayerProfile currentPlayer)
    {
        if (cardObj == null || takenCards.Contains(cardObj)) return;

        // Ambil nama kartu
        Text textComp = cardObj.GetComponentInChildren<Text>();
        string cardName = textComp != null ? textComp.text : "Unknown";

        // Ambil nilai kartu
        Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
        int cardValue = 0;
        if (cardValueText != null) int.TryParse(cardValueText.text, out cardValue);

        // Kurangi finpoint sesuai nilai kartu
        currentPlayer.finpoint -= cardValue;

        // Buat kartu dan tambahkan
        // Ambil nilai warna dari UI
        Text cardColorText = cardObj.transform.Find("CardColor")?.GetComponent<Text>();
        string cardColor = cardColorText != null ? cardColorText.text : "Red"; // default Red jika null

        // Buat kartu dan tambahkan
        Card card = new Card(cardName, "", cardValue, cardColor);

        currentPlayer.AddCard(card);

        // Tandai kartu sudah diambil
        takenCards.Add(cardObj);

        // Nonaktifkan klik dan buat buram
        CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
        cg.alpha = 0.3f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        // Sembunyikan semua anak dari kartu
        foreach (Transform child in cardObj.transform)
        {
            child.gameObject.SetActive(false);
        }

        // Memperbarui UI dengan jumlah kartu berdasarkan warna
        UpdatePlayerUI();
    }



    public void UpdatePlayerUI()
    {
        ClearPlayerListUI();
        for (int i = 0; i < turnOrder.Count; i++)
        {
            var p = turnOrder[i];
            AddPlayerEntry($"{i + 1}. {p.playerName}", p.ticketNumber, p.cardCount, p.finpoint);
        }
    }


    private void AddPlayerEntry(string name, int ticket, int cardCount, int finpoint)
    {
        GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
        Text[] texts = entry.GetComponentsInChildren<Text>();
        foreach (Text t in texts)
        {
            if (t.name == "NameText") t.text = name;
            else if (t.name == "ScoreText") t.text = $"Tiket {ticket}";
            else if (t.name == "CardText") t.text = $"{cardCount} kartu";
            else if (t.name == "Finpoint") t.text = $"FP {finpoint}";
        }

        // Ambil jumlah kartu berdasarkan warna dari player yang sedang diproses (bisa player atau bot)
        var colorCounts = turnOrder.Find(p => $"{turnOrder.IndexOf(p) + 1}. {p.playerName}" == name)?.GetCardColorCounts() ?? new Dictionary<string, int>();

        Text redCardText = entry.transform.Find("RedCardText")?.GetComponent<Text>();
        Text blueCardText = entry.transform.Find("BlueCardText")?.GetComponent<Text>();
        Text greenCardText = entry.transform.Find("GreenCardText")?.GetComponent<Text>();
        Text orangeCardText = entry.transform.Find("OrangeCardText")?.GetComponent<Text>();

        if (redCardText != null)
            redCardText.text = $"K: {(colorCounts.ContainsKey("Konsumer") ? colorCounts["Konsumer"] : 0)}";
        if (blueCardText != null)
            blueCardText.text = $"I: {(colorCounts.ContainsKey("Infrastruktur") ? colorCounts["Infrastruktur"] : 0)}";
        if (greenCardText != null)
            greenCardText.text = $"U: {(colorCounts.ContainsKey("Keuangan") ? colorCounts["Keuangan"] : 0)}";
        if (orangeCardText != null)
            orangeCardText.text = $"T: {(colorCounts.ContainsKey("Tambang") ? colorCounts["Tambang"] : 0)}";


        playerEntries.Add(entry);
    }



    private void ClearPlayerListUI()
    {
        foreach (var entry in playerEntries)
        {
            Destroy(entry);
        }
        playerEntries.Clear();
    }

    private void ClearAllCardsInHolder()
    {
        foreach (Transform child in cardHolderParent)
        {
            Destroy(child.gameObject);
        }
    }
    
    public void ShowLeaderboard()
    {
        List<PlayerProfile> allPlayers = new List<PlayerProfile> { player };
        allPlayers.AddRange(bots);
        sellingManager.ForceSellAllCards(allPlayers);
        leaderboardPanel.SetActive(true);


        // Bersihkan entri sebelumnya
        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }

        // Gabungkan player dan bot

        // Urutkan berdasarkan finpoint secara menurun
        var rankedPlayers = allPlayers.OrderByDescending(p => p.finpoint).ToList();

        // Buat entri leaderboard
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            PlayerProfile p = rankedPlayers[i];

            GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContainer);

            // Ambil semua komponen Text anak
            Text[] texts = entry.GetComponentsInChildren<Text>();

            if (texts.Length >= 2)
            {
                texts[0].text = $"{i + 1}. {p.playerName}";
                texts[1].text = $"{p.finpoint} FP";
            }
            else
            {
                Debug.LogWarning("Leaderboard entry prefab tidak memiliki cukup komponen Text!");
            }
        }
    }
    private string GetActivationErrorMessage(string cardName)
    {
        switch (cardName)
        {
            case "TradeFree":
            case "StockSplit":
                return $"Aktivasi {cardName} gagal: Anda harus memiliki minimal 1 kartu tersimpan dengan warna yang sama.";

            case "TenderOffer":
                return "Aktivasi TenderOffer gagal: Tidak ada target yang valid (pemain lain dengan kartu warna sama yang lebih sedikit).";

            case "Flashbuy":
                return "Aktivasi Flashbuy gagal: Harus ada lebih dari 1 kartu yang tersedia di meja.";

            default:
                return "Aktivasi gagal: Syarat kartu tidak terpenuhi.";
        }
    }

}
