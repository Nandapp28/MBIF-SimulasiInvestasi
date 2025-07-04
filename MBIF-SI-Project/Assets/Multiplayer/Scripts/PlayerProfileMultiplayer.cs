using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PlayerProfileMultiplayer
{
    public const int DEFAULT_FINPOINT = 10; // <-- TAMBAHKAN KONSTANTA INI

    public string playerName;
    public int actorNumber;
    public int ticketNumber;
    public int finpoint;
    public int cardCount => cards.Count;
    public List<CardMultiplayer> cards = new List<CardMultiplayer>();
    public List<HelpCardMultiplayer> helpCards = new List<HelpCardMultiplayer>();
    public Dictionary<string, MarketPredictionType> marketPredictions = new Dictionary<string, MarketPredictionType>();

    public PlayerProfileMultiplayer(string name, int actorNum)
    {
        playerName = name;
        actorNumber = actorNum;
        finpoint = DEFAULT_FINPOINT; // Gunakan konstanta di sini
        ticketNumber = 0;
    }

    // <-- TAMBAHKAN FUNGSI BARU INI -->
    public void ResetStats()
    {
        finpoint = DEFAULT_FINPOINT; // Kembalikan finpoint ke nilai awal
        cards.Clear(); // Kosongkan daftar kartu
        helpCards.Clear(); // Kosongkan juga kartu bantuan
        marketPredictions.Clear(); // Hapus prediksi pasar
    }
    // <-- AKHIR FUNGSI BARU -->

    public void AddCard(CardMultiplayer card)
    {
        cards.Add(card);
    }

    public Dictionary<string, int> GetCardColorCounts()
    {
        Dictionary<string, int> colorCounts = new Dictionary<string, int>
        {
            { "Red", 0 }, { "Blue", 0 }, { "Green", 0 }, { "Orange", 0 }
        };
        foreach (var card in cards)
        {
            if (colorCounts.ContainsKey(card.color))
                colorCounts[card.color]++;
        }
        return colorCounts;
    }
}