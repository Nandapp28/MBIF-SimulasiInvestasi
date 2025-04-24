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
        deck.Add(new Card("Fireball", "Deal 5 damage"));
        deck.Add(new Card("Heal", "Recover 3 HP"));
        deck.Add(new Card("Shield", "Block next attack"));
        deck.Add(new Card("Steal", "Take 1 card"));
        deck.Add(new Card("Freeze", "Skip opponent turn"));
        deck.Add(new Card("Burn", "Damage over time"));
        deck.Add(new Card("Boost", "Increase attack"));
        deck.Add(new Card("Swap", "Switch a card"));
        deck.Add(new Card("Guard", "Reduce damage"));
        deck.Add(new Card("Draw 2", "Take 2 more cards"));

        ShuffleDeck();
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

        Text cardText = cardObj.GetComponentInChildren<Text>();
        if (cardText != null) cardText.text = card.cardName;

        cardObjects.Add(cardObj); // simpan
    }

    currentCardIndex = 0;
    currentTurnIndex = 0;
    StartCoroutine(HandleTurns());
}

private IEnumerator HandleTurns()
{
    int cardsGiven = 0;

    while (cardsGiven < totalCardsToGive && cardsGiven < cardObjects.Count)
    {
        PlayerProfile currentPlayer = turnOrder[currentTurnIndex];
        bool cardTaken = false;

        if (currentPlayer.playerName.Contains("You"))
        {
            List<Button> clickableButtons = new List<Button>();
            cardTaken = false;

            // Tambahkan listener ke semua kartu yang masih ada
            for (int i = cardsGiven; i < cardObjects.Count; i++)
            {
                GameObject obj = cardObjects[i];
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

            if (!cardTaken && cardsGiven < cardObjects.Count)
            {
                TakeCard(cardObjects[cardsGiven], currentPlayer);
                cardTaken = true;
            }

            foreach (var btn in clickableButtons)
                btn.onClick.RemoveAllListeners();
        }
        else
{
    yield return new WaitForSeconds(1f);

    // Cari kartu pertama yang belum diambil
    GameObject cardToTake = null;
    foreach (var cardObj in cardObjects)
    {
        if (cardObj != null) // masih eksis
        {
            cardToTake = cardObj;
            break;
        }
    }

    if (cardToTake != null)
    {
        TakeCard(cardToTake, currentPlayer);
        cardTaken = true;
    }
}

        if (cardTaken)
        {
            cardsGiven++;
            currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        }

        yield return new WaitForSeconds(0.2f);
    }

    Debug.Log("✅ Semua kartu sudah dibagikan.");
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
    while (currentCardIndex < totalCardsToGive && cardHolderParent.childCount > 0)
    {
        PlayerProfile currentPlayer = turnOrder[currentTurnIndex];

        if (!currentPlayer.playerName.Contains("You"))
        {
            yield return new WaitForSeconds(1f); // jeda biar ga instan

            if (currentCardIndex < cardHolderParent.childCount)
            {
                GameObject cardObj = cardHolderParent.GetChild(currentCardIndex).gameObject;
                TakeCard(cardObj, currentPlayer);

                currentCardIndex++;
            }
        }

        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

        // Lanjut terus sampai semua kartu habis
        yield return null;
    }

    Debug.Log("✅ Semua kartu sudah diambil oleh player & bot.");
}



    private void TakeCard(GameObject cardObj, PlayerProfile currentPlayer)
{
    if (cardObj == null) return; // ✅ Cek null biar aman

    // 🔧 Ambil nama kartu sebelum di-destroy
    Text textComp = cardObj.GetComponentInChildren<Text>();
    string cardName = textComp != null ? textComp.text : "Unknown";

    Card card = new Card(cardName, "");
    currentPlayer.AddCard(card);

    Destroy(cardObj);
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

        public Card(string name, string desc)
        {
            cardName = name;
            description = desc;
        }
    }
}
