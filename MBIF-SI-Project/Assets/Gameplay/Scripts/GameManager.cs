using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
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
    public PlayerInfoPanel playerInfoPanel;


    [Header("UI References")]
    public GameObject playerEntryPrefab;
    public GameObject cardUIPrefab;
    public Transform playerUIPosition;
    public Transform botListContainer;
    public GameObject cardPrefab;
    public Transform cardHolderParent;
    public Button toggleCardsButton;
    public GameObject ticketButtonPrefab;
    public Transform ticketListContainer;
    public GameObject activateButtonPrefab;
    public GameObject saveButtonPrefab;
    public Transform ActiveSaveContainer;
    // ⬅️ BARU: Prefab untuk tombol Detail
    public GameObject cardDetailPanel;    // ⬅️ BARU: Panel untuk menampilkan detail
    public Image cardDetailImage;         // ⬅️ BARU: Image di dalam panel
    public Button closeDetailPanelButton;
    public GameObject leaderboardPanel;
    public Transform leaderboardContainer;
    public GameObject leaderboardEntryPrefab;
    [Header("Card Visuals")]
    public List<CardTextureMapping> cardTextureMappings; // ⬅️ TAMBAHKAN INI
    [Header("Sound Effects")]
    public AudioClip cardSound;
    public AudioClip skipSound, cardTakeSound;

    [Header("Button References")]
    public Button bot2Button;
    public Button bot3Button;
    public Button bot4Button;
    public Button playButton;
    public GameObject botSelectionPanel;
    public GameObject resetSemesterButton;
    public GameObject skipButton;
    [Header("Ticket Sprites")]
    public Sprite defaultTicketSprite; // Texture A
    public List<Sprite> ticketNumberSprites;




    public static GameManager Instance;
    private List<PlayerProfile> bots = new List<PlayerProfile>();
    private List<GameObject> playerEntries = new List<GameObject>();
    private Dictionary<PlayerProfile, GameObject> playerUIEntries = new Dictionary<PlayerProfile, GameObject>();
    private Action<PlayerProfile> onPlayerTargetSelected;
    private List<Card> deck = new List<Card>();
    public List<PlayerProfile> turnOrder = new List<PlayerProfile>();
    private List<GameObject> cardObjects = new List<GameObject>();
    private HashSet<GameObject> takenCards = new HashSet<GameObject>();
    private List<GameObject> ticketButtons = new List<GameObject>();
    private bool ticketChosen = false;
    private bool isBotCountSelected = false;
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
        Time.timeScale = 1f;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {

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

        // --- PERUBAHAN DIMULAI DI SINI ---
        // Reset data internal pemain tanpa menghapus objeknya
        foreach (var p in turnOrder)
        {
            p.ticketNumber = 0;
            // Anda bisa menambahkan reset data lain di sini jika perlu
            // contoh: p.marketPredictions.Clear();
        }

        rumorPhaseManager.InitializeRumorDeck();

        currentCardIndex = 0;
        currentTurnIndex = 0;
        skipCount = 0;
        ticketChosen = false;
        takenCards.Clear();
        // JANGAN hapus turnOrder (turnOrder.Clear();)
        // JANGAN hapus UI pemain (ClearPlayerListUI();)

        // Bersihkan kartu dari UI
        ClearHiddenCards();
        ClearAllCardsInHolder();

        // Perbarui tampilan UI pemain dengan data yang sudah di-reset
        UpdatePlayerUI();
        // --- PERUBAHAN SELESAI ---

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
        if (toggleCardsButton != null)
        {
            toggleCardsButton.gameObject.SetActive(false);
        }
        if (cardDetailPanel != null)
        {
            cardDetailPanel.SetActive(false); // Sembunyikan panel di awal
        }
        if (closeDetailPanelButton != null)
        {
            // Tambahkan listener untuk menyembunyikan panel saat tombol close diklik
            closeDetailPanelButton.onClick.AddListener(HideCardDetailPanel);
        }

        isBotCountSelected = false;

        bot2Button.onClick.AddListener(() => SetBotCount(2));
        bot3Button.onClick.AddListener(() => SetBotCount(3));
        bot4Button.onClick.AddListener(() => SetBotCount(4));

        // Listener play button
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }


    private void OnPlayButtonClicked()
    {
        // --- PERUBAHAN BARU ---

        if (!isBotCountSelected)
        {
            Debug.LogWarning("Harap pilih jumlah bot terlebih dahulu sebelum memulai!");
            return; // Hentikan eksekusi fungsi jika belum ada bot yang dipilih
        }


        if (botSelectionPanel != null)
        {
            botSelectionPanel.SetActive(false); // Sembunyikan panel pemilihan
        }
    }


    private void SetBotCount(int count)
    {
        isBotCountSelected = true;
        bots.Clear();
        for (int i = 0; i < count; i++)
        {
            bots.Add(new PlayerProfile("Bot " + (i + 1)));
        }

        // --- PERUBAHAN DIMULAI DI SINI ---
        // Gabungkan pemain utama dan bot ke dalam turnOrder untuk pertama kalinya
        turnOrder.Clear();
        turnOrder.Add(player);
        turnOrder.AddRange(bots);

        // Langsung tampilkan UI pemain di sini
        UpdatePlayerUI();
        // --- PERUBAHAN SELESAI ---

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


    public IEnumerator AssignTicketsToBotsAfterDelay()
    {
        yield return new WaitForSeconds(3f); // ⏳ Tunggu 3 detik

        foreach (var bot in bots)
        {
            bot.ticketNumber = ticketManager.GetRandomTicketForBot();
            Debug.Log("bagaimana ini");
        }

        ClearTicketButtons();
        yield return StartCoroutine(resolutionPhaseManager.RevealNextTokenForAllColors());
        yield return new WaitForSeconds(1.5f);
        UITransitionAnimator.Instance.StartTransition("Action Phase");
        yield return new WaitForSeconds(4f);
        if (toggleCardsButton != null)
        {
            toggleCardsButton.gameObject.SetActive(true);
        }
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
            int randIndex = UnityEngine.Random.Range(i, availableTickets.Count);
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
        // --- PERUBAHAN DIMULAI DI SINI ---
        // Urutkan kembali 'turnOrder' yang sudah ada berdasarkan nomor tiket baru
        turnOrder.Sort((a, b) => a.ticketNumber.CompareTo(b.ticketNumber));

        // Perbarui UI untuk menampilkan urutan giliran (nomor tiket) yang baru
        UpdatePlayerUI();
        // --- PERUBAHAN SELESAI ---

        DrawCardsInOrder();
    }


    private void InitializeDeck()
    {
        deck.Clear();


        List<string> colors = new List<string> { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };


        deck.Add(new Card("TradeFee", "Deal 5 damage", 1, GetRandomColor(colors)));
        deck.Add(new Card("TenderOffer", "Recover 3 HP", 0, GetRandomColor(colors)));
        deck.Add(new Card("StockSplit", "Block next attack", 0, GetRandomColor(colors)));
        deck.Add(new Card("InsiderTrade", "Take 1 card", 0, GetRandomColor(colors)));
        deck.Add(new Card("Flashbuy", "Take 2 more cards", 0, GetRandomColor(colors)));


        ShuffleDeck();

        int totalCardsToGive = totalCards * (bots.Count + 1);

        if (deck.Count < totalCardsToGive)
        {
            int cardsNeeded = totalCardsToGive - deck.Count;
            for (int i = 0; i < cardsNeeded; i++)
            {
                Card randomCard = deck[UnityEngine.Random.Range(0, deck.Count)];
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
        // 1. Perbarui nilai (data) setiap kartu di dalam deck
        foreach (Card card in deck)
        {
            int ipoValue = sellingManager.GetFullCardPrice(card.color);
            card.value = card.baseValue + ipoValue;
        }
        Debug.Log("Harga kartu (data) telah di-update.");

        // 2. Perbarui tampilan visual (UI) setiap kartu yang ada di meja
        if (cardObjects.Count != deck.Count)
        {
            Debug.LogError("Jumlah UI kartu dan data di deck tidak cocok!");
            return;
        }

        for (int i = 0; i < cardObjects.Count; i++)
        {
            GameObject cardObj = cardObjects[i];
            Card card = deck[i];

            if (cardObj == null) continue;

            // --- Logika pembaruan UI yang lengkap ---

            // Update Text untuk nilai total kartu
            Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
            if (cardValueText != null)
            {
                cardValueText.text = card.value.ToString();
            }

            // Update Text untuk nilai efek (baseValue)
            Text effectValueText = cardObj.transform.Find("EffectValueText")?.GetComponent<Text>();
            if (effectValueText != null)
            {
                if (card.baseValue > 0)
                {
                    effectValueText.text = $"(+{card.baseValue})";
                }
                else
                {
                    effectValueText.text = "";
                }
            }

            // Update Text untuk nilai warna (harga IPO)
            Text colorValueText = cardObj.transform.Find("ColorValueText")?.GetComponent<Text>();
            if (colorValueText != null)
            {
                int colorPrice = card.value - card.baseValue;
                colorValueText.text = colorPrice.ToString();
            }
        }
        Debug.Log("Tampilan harga pada semua kartu di meja telah diperbarui.");
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
        return colorOptions[UnityEngine.Random.Range(0, colorOptions.Count)];
    }


    private void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            Card temp = deck[i];
            int rand = UnityEngine.Random.Range(i, deck.Count);
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
            Text effectValueText = cardObj.transform.Find("EffectValueText")?.GetComponent<Text>();
            if (effectValueText != null)
            {
                // Hanya tampilkan teks jika baseValue > 0
                if (card.baseValue > 0)
                {
                    effectValueText.text = $"(+{card.baseValue})";
                }
                else
                {
                    effectValueText.text = ""; // Kosongkan teks jika nilainya 0
                }
            }
            // --- AKHIR PERUBAHAN ---

            Text colorValueText = cardObj.transform.Find("ColorValueText")?.GetComponent<Text>();
            if (colorValueText != null)
            {
                int colorPrice = card.value - card.baseValue;
                colorValueText.text = colorPrice.ToString();
            }

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
            Text effectValueText = cardObj.transform.Find("EffectValueText")?.GetComponent<Text>();
            if (effectValueText != null)
            {
                // Hanya tampilkan teks jika baseValue > 0
                if (card.baseValue > 0)
                {
                    effectValueText.text = $"(+{card.baseValue})";
                }
                else
                {
                    effectValueText.text = ""; // Kosongkan teks jika nilainya 0
                }
            }
            // --- AKHIR PERUBAHAN ---

            Text colorValueText = cardObj.transform.Find("ColorValueText")?.GetComponent<Text>();
            if (colorValueText != null)
            {
                int colorPrice = card.value - card.baseValue;
                colorValueText.text = colorPrice.ToString();
            }
        }
    }

    public void ToggleCardHolderPanel()
    {
        UpdateDeckCardValuesWithIPO();
        if (cardHolderParent != null)
        {
            // Mengubah status aktif/non-aktif dari GameObject panel
            bool isActive = cardHolderParent.gameObject.activeSelf;
            cardHolderParent.gameObject.SetActive(!isActive);
            Debug.Log($"Panel list kartu di-toggle menjadi {(cardHolderParent.gameObject.activeSelf ? "Aktif" : "Tidak Aktif")}");
            if (isActive)
            {
                // Reset semua status pilihan kartu (menghilangkan highlight dan tombol activate/save)
                ResetCardSelection();
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
            if (toggleCardsButton != null)
            {
                toggleCardsButton.gameObject.SetActive(false);
            }


            yield return new WaitForSeconds(2f);

            Debug.Log("Memulai fase penjualan...");
            helpCardPhaseManager.StartHelpCardPhase(turnOrder, resetCount);


            yield break;

        }
        if (currentCardIndex >= totalCardsToGive || currentCardIndex >= cardObjects.Count)
        {
            Debug.Log("✅ Semua kartu sudah dibagikan.");
            if (toggleCardsButton != null)
            {
                toggleCardsButton.gameObject.SetActive(false);
            }


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
                    if (SfxManager.Instance != null && cardSound != null) // <-- MODIFIKASI DISINI
                    {
                        SfxManager.Instance.PlaySound(cardSound); // <-- MODIFIKASI DISINI
                    }

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
                        if (currentlySelectedCard == obj)
                        {
                            // ...MAKA TAMPILKAN PANEL DETAIL.
                            if (SfxManager.Instance != null && cardSound != null) // <-- MODIFIKASI DISINI
                            {
                                SfxManager.Instance.PlaySound(cardSound); // <-- MODIFIKASI DISINI
                            }
                            ShowCardDetailPanel(obj);
                            return; // Hentikan eksekusi lebih lanjut untuk klik ini.
                        }

                        // Jika sudah ada kartu lain yang dipilih, reset dulu
                        if (currentlySelectedCard != null && currentlySelectedCard != obj)
                        {
                            ResetCardSelection();
                        }
                        if (SfxManager.Instance != null && cardSound != null) // <-- MODIFIKASI DISINI
                        {
                            SfxManager.Instance.PlaySound(cardSound); // <-- MODIFIKASI DISINI
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
                                string pesanError = GetActivationErrorMessage(cardName, cardColor, currentPlayer);

                                // 2. Gunakan variabel tersebut untuk Debug Log
                                Debug.LogWarning(pesanError);

                                // 3. Gunakan variabel yang sama untuk menampilkan notifikasi
                                NotificationManager.Instance.ShowNotification(pesanError, 2f);

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
            bool botSkips = UnityEngine.Random.value < 0.3f; // 30% kemungkinan skip
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
            bool botActivates = UnityEngine.Random.value < 0.7f; // 70% kemungkinan bot menyimpan kartu
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

            GameObject randomCard = affordableCards[UnityEngine.Random.Range(0, affordableCards.Count)];

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
            case "TradeFee":
                // Syarat: Pengaktif harus punya minimal 1 kartu (warna apa saja).
                return activator.cards.Count > 0;
            case "StockSplit":
    // Syarat 1: Pengaktif harus punya minimal 1 kartu tersimpan dengan warna yang sama.
    bool hasMatchingCard = activator.GetCardColorCounts().ContainsKey(cardColor) && activator.GetCardColorCounts()[cardColor] >= 1;
    if (!hasMatchingCard)
    {
        return false;
    }

    // Syarat 2: Harga tidak boleh di titik terendah JIKA state-nya Normal.
    SellingPhaseManager.IPOData ipoData = sellingManager.GetIPOData(cardColor);// Ambil data IPO
    if (ipoData == null) return false; // Keamanan jika data tidak ditemukan

    if (sellingManager.ipoPriceMap.TryGetValue(cardColor, out int[] prices))
    {
        int lowestPrice;
        if (cardColor == "Tambang")
        {
            var relevantPrices = new ArraySegment<int>(prices, 1, 5);
            lowestPrice = relevantPrices.Min();
        }
        else
        {
            lowestPrice = prices.Min();
        }

        int currentPrice = sellingManager.GetCurrentColorValue(cardColor);

        // Aturan baru: Jika harga saat ini lebih rendah/sama dengan harga terendah DAN state-nya Normal, maka GAGAL.
        if (currentPrice <= lowestPrice && ipoData.currentState == IPOState.Normal)
        {
            return false; // Kondisi aktivasi tidak terpenuhi
        }
        
        // Jika lolos dari semua pengecekan di atas, kartu boleh diaktifkan.
        return true;
    }

    return false;

            case "TenderOffer":
                // Syarat baru: Pengaktif harus punya minimal 2 kartu warna ini, agar bisa ada target yang punya lebih sedikit tapi bukan nol.
                int activatorColorCount = activator.GetCardColorCounts()[cardColor];
                if (activatorColorCount < 2) return false;

                // Cari di semua pemain lain
                foreach (var target in turnOrder)
                {
                    if (target == activator) continue; // Jangan bandingkan dengan diri sendiri

                    int targetColorCount = target.GetCardColorCounts()[cardColor];

                    // Syarat baru: target harus punya kartu (> 0) DAN jumlahnya lebih sedikit dari pengaktif.
                    if (targetColorCount > 0 && targetColorCount < activatorColorCount)
                    {
                        return true; // Ditemukan target yang valid!
                    }
                }
                return false;  // Tidak ada target yang memenuhi syarat.

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
                    if (SfxManager.Instance != null && cardSound != null) // <-- MODIFIKASI DISINI
                    {
                        SfxManager.Instance.PlaySound(cardSound); // <-- MODIFIKASI DISINI
                    }
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
        //#currentPlayer.finpoint -= cardValue;
        if (cardObj == null || takenCards.Contains(cardObj)) yield break;
        int cardIndex = cardObjects.IndexOf(cardObj);
        if (cardIndex == -1 || cardIndex >= deck.Count)
        {
            Debug.LogError("Tidak dapat menemukan data kartu yang sesuai untuk diaktifkan!");
            yield break;
        }

        Card cardData = deck[cardIndex];
        int effectCost = cardData.baseValue; // Ambil biaya efek murni (baseValue)

        // 2. Kurangi finpoint pemain hanya sebesar biaya efek
        Debug.Log($"{currentPlayer.playerName} membayar {effectCost} FP untuk efek kartu '{cardData.cardName}'.");
        currentPlayer.finpoint -= effectCost;


        Text cardNameText = cardObj.transform.Find("CardText")?.GetComponent<Text>();
        string cardName = cardNameText != null ? cardNameText.text : "";

        // Ambil warna kartu
        Text cardColorText = cardObj.transform.Find("CardColor")?.GetComponent<Text>();
        string cardColor = cardColorText != null ? cardColorText.text : "Konsumer";

        // Tandai kartu sudah diambil
        takenCards.Add(cardObj);

        // Nonaktifkan klik dan buat buram
        if (SfxManager.Instance != null && skipSound != null) // <-- MODIFIKASI DISINI
        {
            SfxManager.Instance.PlaySound(skipSound); // <-- MODIFIKASI DISINI
        }
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
            NotificationManager.Instance.ShowNotification($"Kartu '{cardName}' disektor ({cardColor}) diaktifkan untuk {currentPlayer.playerName}", 3f, true);
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
                    int randomIndex = UnityEngine.Random.Range(0, availableCards.Count);
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


        // Ambil nilai kartu
        Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
        int cardValue = 0;
        if (cardValueText != null) int.TryParse(cardValueText.text, out cardValue);

        // Kurangi finpoint sesuai nilai kartu
        currentPlayer.finpoint -= cardValue;

        // Buat kartu dan tambahkan
        // Ambil nilai warna dari UI
        Text cardNameText = cardObj.transform.Find("CardText")?.GetComponent<Text>();
        string cardName = cardNameText != null ? cardNameText.text : "StockSplit";
        Text cardColorText = cardObj.transform.Find("CardColor")?.GetComponent<Text>();
        string cardColor = cardColorText != null ? cardColorText.text : "Red"; // default Red jika null

        // Buat kartu dan tambahkan
        Card card = new Card(cardName, "", cardValue, cardColor);

        currentPlayer.AddCard(card);

        // Tandai kartu sudah diambil
        takenCards.Add(cardObj);

        // Nonaktifkan klik dan buat buram
        if (SfxManager.Instance != null && cardTakeSound != null) // <-- MODIFIKASI DISINI
        {
            SfxManager.Instance.PlaySound(cardTakeSound); // <-- MODIFIKASI DISINI
        }
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
        // Clear all existing player UI entries
        ClearPlayerListUI();

        // 1. Display the main player at their fixed position
        if (player != null)
        {
            AddPlayerEntry(player, playerUIPosition);
        }

        // 2. Display all bots in the grid layout container
        // We find the bot's profile in turnOrder to ensure the data is up-to-date
        foreach (var bot in bots)
        {
            PlayerProfile botProfile = turnOrder.Find(p => p.playerName == bot.playerName);
            if (botProfile != null)
            {
                AddPlayerEntry(botProfile, botListContainer);
            }
        }
    }

    private void AddPlayerEntry(PlayerProfile playerProfile, Transform parentContainer)
    {
        // Instantiate the prefab into the correct container (PlayerUIPosition or BotListContainer)
        GameObject entry = Instantiate(playerEntryPrefab, parentContainer);
        playerEntries.Add(entry); // Keep tracking the entry for cleanup
        Button entryButton = entry.GetComponent<Button>();
        if (entryButton == null)
        {
            entryButton = entry.AddComponent<Button>();
        }

        // 2. Bersihkan listener lama dan tambahkan yang baru
        entryButton.onClick.RemoveAllListeners();
        entryButton.onClick.AddListener(() =>
        {
            // Panggil fungsi di panel untuk menampilkan info pemain ini
            if (playerInfoPanel != null)
            {
                playerInfoPanel.ShowPanelForPlayer(playerProfile);
            }
            else
            {
                Debug.LogError("Referensi 'playerInfoPanel' belum di-assign di GameManager!");
            }
        });


        if (!playerUIEntries.ContainsKey(playerProfile))
        {
            playerUIEntries.Add(playerProfile, entry);
        }
        else
        {
            playerUIEntries[playerProfile] = entry;
        }

        // Cari Tombol Target di dalam prefab yang baru dibuat
        Button targetButton = entry.transform.Find("TargetButton")?.GetComponent<Button>();
        if (targetButton != null)
        {
            targetButton.gameObject.SetActive(false); // Sembunyikan tombol secara default
            // Tambahkan listener yang akan memanggil fungsi OnTargetButtonClicked saat diklik
            targetButton.onClick.AddListener(() => OnTargetButtonClicked(playerProfile));
        }
        else
        {
            Debug.LogError($"Tombol 'TargetButton' tidak ditemukan di dalam prefab '{entry.name}'. Pastikan nama objeknya sudah benar.");
        }

        // --- Fill in the UI data ---
        Text[] texts = entry.GetComponentsInChildren<Text>();
        foreach (Text t in texts)
        {
            if (t.name == "NameText") t.text = playerProfile.playerName;
            else if (t.name == "ScoreText") t.text = $"Turn {playerProfile.ticketNumber}";
            else if (t.name == "CardText") t.text = $"";
            else if (t.name == "Finpoint") t.text = $"{playerProfile.finpoint}";
        }
        Transform cardContainer = entry.transform.Find("CardDisplayContainer");

        // Periksa apakah prefab UI kartu dan kontainernya ada
        if (cardUIPrefab != null && cardContainer != null)
        {
            // Loop melalui setiap kartu yang dimiliki pemain
            foreach (Card card in playerProfile.cards)
            {
                // Dapatkan sprite untuk kartu saat ini
                Sprite cardSprite = GetCardSprite(card.cardName, card.color);
                if (cardSprite == null) continue; // Lanjut ke kartu berikutnya jika sprite tidak ditemukan

                // Buat instance prefab UI kartu di dalam kontainer
                GameObject cardImageObj = Instantiate(cardUIPrefab, cardContainer);

                // Atur sprite-nya pada komponen Image
                Image cardImage = cardImageObj.GetComponent<Image>();
                if (cardImage != null)
                {
                    cardImage.sprite = cardSprite;
                    cardImage.preserveAspect = true;
                }
            }
        }
        else
        {
            // Tampilkan peringatan jika ada yang belum di-assign
            if (cardUIPrefab == null)
            {
                Debug.LogWarning("Prefab 'Card UI' belum di-assign di Inspector!");
            }
            if (cardContainer == null)
            {
                Debug.LogWarning("Tidak ditemukan 'CardDisplayContainer' di dalam prefab entri pemain!");
            }
        }
        // --- Fill in the card color counts ---
        var colorCounts = playerProfile.GetCardColorCounts();
        Text redCardText = entry.transform.Find("RedCardText")?.GetComponent<Text>();
        Text orangeCardText = entry.transform.Find("OrangeCardText")?.GetComponent<Text>();
        Text blueCardText = entry.transform.Find("BlueCardText")?.GetComponent<Text>();
        Text greenCardText = entry.transform.Find("GreenCardText")?.GetComponent<Text>();

        if (redCardText != null)
            redCardText.text = $"{(colorCounts.ContainsKey("Konsumer") ? colorCounts["Konsumer"] : 0)}";
        if (orangeCardText != null)
            orangeCardText.text = $"{(colorCounts.ContainsKey("Infrastruktur") ? colorCounts["Infrastruktur"] : 0)}";
        if (blueCardText != null)
            blueCardText.text = $"{(colorCounts.ContainsKey("Keuangan") ? colorCounts["Keuangan"] : 0)}";
        if (greenCardText != null)
            greenCardText.text = $"{(colorCounts.ContainsKey("Tambang") ? colorCounts["Tambang"] : 0)}";

        int totalAssetValue = 0;
        foreach (var colorCount in colorCounts)
        {
            string color = colorCount.Key;
            int count = colorCount.Value;
            if (count > 0)
            {
                // Dapatkan harga penuh saat ini untuk warna tersebut dari SellingPhaseManager
                int pricePerCard = sellingManager.GetFullCardPrice(color);
                totalAssetValue += count * pricePerCard;
            }
        }

        // 2. Temukan komponen Text 'AssetValueText'
        Text assetValueText = entry.transform.Find("AssetValueText")?.GetComponent<Text>();
        if (assetValueText != null)
        {
            // 3. Tampilkan nilainya di UI
            assetValueText.text = $"{totalAssetValue}";
        }
        else
        {
            // Pesan ini akan muncul jika Anda lupa menambahkan Text 'AssetValueText' di prefab
            Debug.LogWarning($"Text 'AssetValueText' tidak ditemukan di prefab player entry. Pastikan nama objeknya sudah benar.");
        }
    }
    public void StartPlayerTargeting(List<PlayerProfile> validTargets, Action<PlayerProfile> onSelected)
    {
        onPlayerTargetSelected = onSelected;

        // Loop melalui setiap UI pemain yang ada di layar
        foreach (var pair in playerUIEntries)
        {
            PlayerProfile profile = pair.Key;
            GameObject uiEntry = pair.Value;

            Button targetButton = uiEntry.transform.Find("TargetButton")?.GetComponent<Button>();
            if (targetButton != null)
            {
                // Aktifkan tombol HANYA jika pemain ini ada di daftar target yang valid
                bool isValidTarget = validTargets.Contains(profile);
                targetButton.gameObject.SetActive(isValidTarget);
            }
        }
    }
    private void OnTargetButtonClicked(PlayerProfile selectedProfile)
    {
        // Jalankan callback (aksi) yang sudah disimpan sebelumnya dengan membawa info pemain yang dipilih
        onPlayerTargetSelected?.Invoke(selectedProfile);

        // Setelah target dipilih, sembunyikan kembali SEMUA tombol target
        foreach (var uiEntry in playerUIEntries.Values)
        {
            Button targetButton = uiEntry.transform.Find("TargetButton")?.GetComponent<Button>();
            if (targetButton != null)
            {
                targetButton.gameObject.SetActive(false);
            }
        }
    }


    private void ClearPlayerListUI()
    {
        foreach (var entry in playerEntries)
        {
            Destroy(entry);
        }
        playerEntries.Clear();
        playerUIEntries.Clear();
    }

    private void ClearAllCardsInHolder()
    {
        foreach (Transform child in cardHolderParent)
        {
            Destroy(child.gameObject);
        }
    }
    // ⬇️ FUNGSI BARU ⬇️
    private void ShowCardDetailPanel(GameObject selectedCard)
    {
        if (cardDetailPanel == null || cardDetailImage == null || selectedCard == null) return;

        // Ambil nama dan warna dari game object kartu yang dipilih
        Text cardNameText = selectedCard.transform.Find("CardText")?.GetComponent<Text>();
        Text cardColorText = selectedCard.transform.Find("CardColor")?.GetComponent<Text>();

        if (cardNameText == null || cardColorText == null) return;

        string cardName = cardNameText.text;
        string cardColor = cardColorText.text;

        // Gunakan fungsi yang sudah ada untuk mendapatkan sprite kartu
        Sprite sprite = GetCardSprite(cardName, cardColor);

        if (sprite != null)
        {
            cardDetailImage.sprite = sprite;
            cardDetailImage.preserveAspect = true;
            cardDetailPanel.SetActive(true); // Tampilkan panel
        }
        else
        {
            Debug.LogWarning($"Sprite untuk {cardName} ({cardColor}) tidak ditemukan.");
        }
    }

    // ⬇️ FUNGSI BARU ⬇️
    private void HideCardDetailPanel()
    {
        if (cardDetailPanel != null)
        {
            cardDetailPanel.SetActive(false); // Sembunyikan panel
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
    private string GetActivationErrorMessage(string cardName, string cardColor, PlayerProfile activator)
{
    switch (cardName)
    {
        case "TradeFee":
            return "Anda tidak punya kartu untuk dijual.";

        case "StockSplit":
            // Cek ulang syarat 1: Kepemilikan kartu
            bool hasMatchingCard = activator.GetCardColorCounts().ContainsKey(cardColor) && activator.GetCardColorCounts()[cardColor] >= 1;
            if (!hasMatchingCard)
            {
                return $"Syarat gagal: Anda harus punya minimal 1 saham di sektor {cardColor}.";
            }

            // Cek ulang syarat 2: Harga terendah pada state Normal
            SellingPhaseManager.IPOData ipoData = sellingManager.GetIPOData(cardColor);
            if (ipoData != null && sellingManager.ipoPriceMap.TryGetValue(cardColor, out int[] prices))
            {
                int lowestPrice;
                if (cardColor == "Tambang")
                {
                    var relevantPrices = new ArraySegment<int>(prices, 1, 5);
                    lowestPrice = relevantPrices.Min();
                }
                else
                {
                    lowestPrice = prices.Min();
                }
                int currentPrice = sellingManager.GetCurrentColorValue(cardColor);

                if (currentPrice <= lowestPrice && ipoData.currentState == IPOState.Normal)
                {
                    return $"Harga Terendah: Saham sektor {cardColor} tidak bisa di-split saat ini.";
                }
            }
            // Pesan fallback jika ada kondisi lain yang tidak terpenuhi
            return "Aktivasi Stock Split gagal: Syarat tidak terpenuhi.";

        case "TenderOffer":
            return "Tidak ada pemain yang dapat ditarget.";

        case "Flashbuy":
            return "Tidak ada kartu yang dapat diambil dari pengaktifan Flashbuy.";

        default:
            return "Aktivasi gagal: Syarat kartu tidak terpenuhi.";
    }
}
}
