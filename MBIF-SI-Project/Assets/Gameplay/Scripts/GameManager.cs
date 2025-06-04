using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
public class GameManager : MonoBehaviour
{
    public TicketManager ticketManager;
    [SerializeField] private SellingPhaseManager sellingManager;

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


    [Header("Button References")]
    public Button bot2Button;
    public Button bot3Button;
    public Button bot4Button;
    public GameObject resetSemesterButton;
    public GameObject skipButton;





    public static GameManager Instance;
    private List<PlayerProfile> bots = new List<PlayerProfile>();
    private List<GameObject> playerEntries = new List<GameObject>();
    private List<Card> deck = new List<Card>();
    private List<PlayerProfile> turnOrder = new List<PlayerProfile>();
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
    private int maxResetCount = 1;

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

    public void ResetButton() {
        if (resetSemesterButton != null)
        {
            resetSemesterButton.SetActive(resetCount < maxResetCount);
            if (resetCount >= maxResetCount)
            {
                ShowLeaderboard();
                Debug.Log("Semester sudah berakhir");
            }
        }

        Debug.Log("Fase penjualan selesai.");
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
        int totalPlayers = bots.Count +1;
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

            Text btnText = btnObj.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = "Choose";

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

        // Ganti tulisan tombol yang diklik saja
        Text btnText = clickedButton.GetComponentInChildren<Text>();
        if (btnText != null)
        {
            btnText.text = $"{chosenTicket}"; // 🛠️ Update text yang diklik saja
        }

        // Mulai delay 3 detik buat bot
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

        List<string> colors = new List<string> { "Red", "Blue", "Green", "Orange" };

        // Tambahkan kartu default dengan warna acak
        deck.Add(new Card("Trade Offer", "Deal 5 damage", 4, GetRandomColor(colors)));
        deck.Add(new Card("Heal", "Recover 3 HP", 2, GetRandomColor(colors)));
        deck.Add(new Card("Stock Split", "Block next attack", 3, GetRandomColor(colors)));
        deck.Add(new Card("Steal", "Take 1 card", 5, GetRandomColor(colors)));
        deck.Add(new Card("Flashbuy", "Take 2 more cards", 8, GetRandomColor(colors)));

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
    }
    private int GetCardValue(GameObject cardObj)
    {
        Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
        int cardValue = 0;
        if (cardValueText != null) int.TryParse(cardValueText.text, out cardValue);
        return cardValue;
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

    private IEnumerator NextTurn()
    {
        int totalCardsToGive = totalCards * (bots.Count + 1);
        if (skipCount >= turnOrder.Count)
        {

            Debug.Log("🚫 Semua pemain skip. Menghapus kartu yang tersisa.");
            ClearHiddenCards();
            currentCardIndex = totalCardsToGive; // anggap sudah selesai
            skipCount = 0;

            yield return new WaitForSeconds(2f);

            Debug.Log("Memulai fase penjualan...");
    sellingManager.StartSellingPhase(turnOrder, resetCount, maxResetCount, resetSemesterButton);


    yield break;

        }
        if (currentCardIndex >= totalCardsToGive || currentCardIndex >= cardObjects.Count)
        {
            Debug.Log("✅ Semua kartu sudah dibagikan.");

            yield return new WaitForSeconds(1f); // Delay sedikit biar visual terlihat
            ClearHiddenCards(); // 🔥 Hapus semua kartu dari UI

            Debug.Log("Memulai fase penjualan...");
    sellingManager.StartSellingPhase(turnOrder, resetCount, maxResetCount, resetSemesterButton);


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
                            int cardValue = GetCardValue(obj);
                            if (currentPlayer.finpoint < cardValue)
                            {
                                Debug.LogWarning($"{currentPlayer.playerName} tidak punya finpoint cukup untuk mengaktifkan kartu ini.");
                                return;
                            }
                            cardTaken = true;
                            ActivateCard(obj, currentPlayer);
                            ResetCardSelection();
                            skipCount = 0;
                            currentCardIndex++;
                            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
                            if (skipButton != null) skipButton.SetActive(false);
                            StartCoroutine(NextTurn());
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
                            TakeCard(obj, currentPlayer);
                            ResetCardSelection();
                            skipCount = 0;
                            currentCardIndex++;
                            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
                            if (skipButton != null) skipButton.SetActive(false);
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

            GameObject randomCard = affordableCards[Random.Range(0, affordableCards.Count)];


            if (botActivates)
            {
                TakeCard(randomCard, currentPlayer);
                Debug.Log($"{currentPlayer.playerName} took a card.");
            }
            else
            {
                ActivateCard(randomCard, currentPlayer);
            }

            // Reset skip counter karena aksi diambil
            currentCardIndex++;
            skipCount = 0;

            // Lanjut ke giliran berikutnya
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
            StartCoroutine(NextTurn());

        }


    }
    private void ActivateCard(GameObject cardObj, PlayerProfile currentPlayer)
    {
        if (cardObj == null || takenCards.Contains(cardObj)) return;

        // Ambil nama kartu dari UI
        // Ambil nama kartu
Text cardNameText = cardObj.transform.Find("CardText")?.GetComponent<Text>();
string cardName = cardNameText != null ? cardNameText.text : "";

// Ambil warna kartu
Text cardColorText = cardObj.transform.Find("CardColor")?.GetComponent<Text>();
string cardColor = cardColorText != null ? cardColorText.text : "Red";

if (!string.IsNullOrEmpty(cardName))
{
    // Kirim nama, pemain, dan warna ke efek
    CardEffectManager.ApplyEffect(cardName, currentPlayer, cardColor);
    Debug.Log($"🎴 Kartu '{cardName}' ({cardColor}) diaktifkan untuk {currentPlayer.playerName}");
}
else
{
    Debug.LogWarning("⚠️ Nama kartu tidak ditemukan. Efek tidak dijalankan.");
    return;
}


        // Ambil nilai kartu
        Text cardValueText = cardObj.transform.Find("CardValue")?.GetComponent<Text>();
        int cardValue = 0;
        if (cardValueText != null) int.TryParse(cardValueText.text, out cardValue);

        // Kurangi finpoint sesuai nilai kartu
        currentPlayer.finpoint -= cardValue;
        if (currentPlayer.finpoint < 0) currentPlayer.finpoint = 0;

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
        if (currentPlayer.finpoint < 0) currentPlayer.finpoint = 0;

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
            redCardText.text = $"M: {(colorCounts.ContainsKey("Red") ? colorCounts["Red"] : 0)}";
        if (blueCardText != null)
            blueCardText.text = $"B: {(colorCounts.ContainsKey("Blue") ? colorCounts["Blue"] : 0)}";
        if (greenCardText != null)
            greenCardText.text = $"H: {(colorCounts.ContainsKey("Green") ? colorCounts["Green"] : 0)}";
        if (orangeCardText != null)
            orangeCardText.text = $"O: {(colorCounts.ContainsKey("Orange") ? colorCounts["Orange"] : 0)}";

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
    leaderboardPanel.SetActive(true);

    // Bersihkan entri sebelumnya
    foreach (Transform child in leaderboardContainer)
    {
        Destroy(child.gameObject);
    }

    // Gabungkan player dan bot
    List<PlayerProfile> allPlayers = new List<PlayerProfile> { player };
    allPlayers.AddRange(bots);

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


}
