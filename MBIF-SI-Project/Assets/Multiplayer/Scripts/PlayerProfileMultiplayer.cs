// File: PlayerProfileMultiplayer.cs

using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PlayerProfileMultiplayer
{
    public string playerName;
    public int actorNumber; // ID unik dari Photon untuk setiap pemain
    public int ticketNumber;
    public int finpoint;
    public int cardCount => cards.Count;
    public List<CardMultiplayer> cards = new List<CardMultiplayer>();
    public List<HelpCardMultiplayer> helpCards = new List<HelpCardMultiplayer>();
    
    // Prediksi pasar juga perlu disinkronkan jika efeknya ada di multiplayer
    public Dictionary<string, MarketPredictionType> marketPredictions = new Dictionary<string, MarketPredictionType>();

    public PlayerProfileMultiplayer(string name, int actorNum)
    {
        playerName = name;
        actorNumber = actorNum;
        finpoint = 10; // Nilai awal
        ticketNumber = 0;
    }

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