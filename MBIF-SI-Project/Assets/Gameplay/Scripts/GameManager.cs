using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public Score scoreCalculator;

    [Header("UI References")]
    public GameObject playerEntryPrefab;
    public Transform playerListContainer;
    public GameObject cardPrefab;
    public Transform cardHolderParent;

    public Button bot2Button;
    public Button bot3Button;
    public Button bot4Button;

    private PlayerProfile player;
    private List<PlayerProfile> bots = new List<PlayerProfile>();
    private List<GameObject> playerEntries = new List<GameObject>();
    private List<Card> deck = new List<Card>();
    private List<PlayerProfile> turnOrder = new List<PlayerProfile>();
    private List<GameObject> cardObjects = new List<GameObject>();
private HashSet<GameObject> takenCards = new HashSet<GameObject>();

    
    private int totalCardsToGive = 10;

    private int currentCardIndex = 0;
    private int currentTurnIndex = 0;

    private void Start()
    {
        player = new PlayerProfile("You");

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

        ResetAllScores();
        ResetDicePositions();
        Invoke(nameof(UpdateScores), 3f);
    }

    private void ResetDicePositions()
    {
        if (scoreCalculator.dice1 != null) scoreCalculator.dice1.ResetPosition();
        if (scoreCalculator.dice2 != null) scoreCalculator.dice2.ResetPosition();
    }

    private void ResetAllScores()
    {
        player.SetScore(0);
        foreach (var bot in bots)
            bot.SetScore(0);

        ClearPlayerListUI();
        AddPlayerEntry(player.playerName, 0, 0);
        foreach (var bot in bots)
            AddPlayerEntry(bot.playerName, 0, 0);
    }

    private void UpdateScores()
    {
        ClearPlayerListUI();

        player.SetLastRoll(scoreCalculator.GetDiceTotal());
        player.SetScore(player.lastRoll);

        foreach (var bot in bots)
        {
            int roll = Random.Range(1, 13);
            bot.SetLastRoll(roll);
            bot.SetScore(roll);
        }

        List<PlayerProfile> allPlayers = new List<PlayerProfile> { player };
        allPlayers.AddRange(bots);
        allPlayers.Sort((a, b) => b.lastRoll.CompareTo(a.lastRoll));

        for (int i = 0; i < allPlayers.Count; i++)
        {
            var p = allPlayers[i];
            AddPlayerEntry($"{i + 1}. {p.playerName}", p.lastRoll, p.cardCount);
        }

        turnOrder = new List<PlayerProfile>(allPlayers);
        DrawCardsInOrder();
    }

    private void InitializeDeck()
{
    deck.Clear();

    // Tambahkan kartu default
    deck.Add(new Card("Trade Offer", "Deal 5 damage", 4));
    deck.Add(new Card("Heal", "Recover 3 HP", 2));
    deck.Add(new Card("Shield", "Block next attack", 3));
    deck.Add(new Card("Steal", "Take 1 card", 5));
    deck.Add(new Card("Flashbuy", "Take 2 more cards", 8));

    ShuffleDeck();

    // Tambahkan/kurangi agar total jadi 10 kartu
    if (deck.Count < totalCardsToGive)
    {
        int cardsNeeded = totalCardsToGive - deck.Count;
        for (int i = 0; i < cardsNeeded; i++)
        {
            // Duplikasikan kartu secara acak dari deck
            Card randomCard = deck[Random.Range(0, deck.Count)];
            Card duplicate = new Card(randomCard.cardName, randomCard.description, randomCard.value);
            deck.Add(duplicate);
        }
    }
    else if (deck.Count > totalCardsToGive)
    {
        // Ambil 10 kartu acak dari deck yang sudah di-shuffle
        deck = new List<Card>(deck.GetRange(0, totalCardsToGive));
    }
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

    for (int i = 0; i < totalCardsToGive && i < deck.Count; i++)
{
    Card card = deck[i];
    GameObject cardObj = Instantiate(cardPrefab, cardHolderParent);

    // Ambil Text untuk nama kartu
    Text cardText = cardObj.transform.Find("CardText")?.GetComponent<Text>();
    if (cardText != null) cardText.text = card.cardName;

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
    if (currentCardIndex >= totalCardsToGive || currentCardIndex >= cardObjects.Count)
    {
        Debug.Log("✅ Semua kartu sudah dibagikan.");
        yield break;
    }

    PlayerProfile currentPlayer = turnOrder[currentTurnIndex];

    if (currentPlayer.playerName.Contains("You"))
    {
        bool cardTaken = false;
        List<Button> clickableButtons = new List<Button>();

        for (int i = 0; i < cardObjects.Count; i++)
        {
            GameObject obj = cardObjects[i];
            if (obj == null) continue;

            Button btn = obj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                int index = i;

                btn.onClick.AddListener(() =>
                {
                    if (!cardTaken)
                    {
                        TakeCard(cardObjects[index], currentPlayer);
                        cardTaken = true;

                        currentCardIndex++;
                        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

                        StartCoroutine(NextTurn()); // Lanjut ke turn berikutnya
                    }
                });

                clickableButtons.Add(btn);
            }
        }

        // Timer 10 detik
        float timer = 0f;
        while (!cardTaken && timer < 10f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!cardTaken)
        {
            // Ambil acak jika tidak sempat pilih
            List<GameObject> available = cardObjects.FindAll(c => c != null && !takenCards.Contains(c));

            if (available.Count > 0)
            {
                GameObject randomCard = available[Random.Range(0, available.Count)];
                TakeCard(randomCard, currentPlayer);
                cardTaken = true;

                currentCardIndex++;
                currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
                StartCoroutine(NextTurn()); // Lanjut
            }
        }

        foreach (var btn in clickableButtons)
            btn.onClick.RemoveAllListeners();
    }
    else
    {
        yield return new WaitForSeconds(1f);

        List<GameObject> availableCards = cardObjects.FindAll(c => c != null && !takenCards.Contains(c));

        if (availableCards.Count > 0)
        {
            GameObject randomCard = availableCards[Random.Range(0, availableCards.Count)];
            TakeCard(randomCard, currentPlayer);

            currentCardIndex++;
        }

        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        StartCoroutine(NextTurn());
    }
    if (currentCardIndex >= totalCardsToGive || currentCardIndex >= cardObjects.Count)
{
    Debug.Log("✅ Semua kartu sudah dibagikan.");

    yield return new WaitForSeconds(1f); // Delay sedikit biar visual terlihat
    ClearHiddenCards(); // 🔥 Hapus semua kartu dari UI

    yield break;
}

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

    // 🔧 Ambil nama kartu
    Text textComp = cardObj.GetComponentInChildren<Text>();
    string cardName = textComp != null ? textComp.text : "Unknown";

    Card card = new Card(cardName, "");
    currentPlayer.AddCard(card);

    // Tandai sebagai sudah diambil
    takenCards.Add(cardObj);

    // Nonaktifkan klik dan buat buram
    CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
    if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();
    cg.alpha = 0.3f;
    cg.interactable = false;
    cg.blocksRaycasts = false;

    // 🔒 Sembunyikan semua anak dari kartu
    foreach (Transform child in cardObj.transform)
    {
        child.gameObject.SetActive(false);
    }

    UpdatePlayerUI();
}



    private void UpdatePlayerUI()
    {
        ClearPlayerListUI();
        for (int i = 0; i < turnOrder.Count; i++)
        {
            var p = turnOrder[i];
            AddPlayerEntry($"{i + 1}. {p.playerName}", p.lastRoll, p.cardCount);
        }
    }

    private void AddPlayerEntry(string name, int score, int cardCount)
    {
        GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
        Text[] texts = entry.GetComponentsInChildren<Text>();
        foreach (Text t in texts)
        {
            if (t.name == "NameText") t.text = name;
            else if (t.name == "ScoreText") t.text = $"{score} pts";
            else if (t.name == "CardText") t.text = $"{cardCount} kartu";
        }
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

    [System.Serializable]
    public class Card
    {
        public string cardName;
        public string description;
        public int value;


        public Card(string name, string desc, int val = 0)
        {
            cardName = name;
            description = desc;
            value = val;
        }
    }
}
