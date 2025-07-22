using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerProfile
{
    public string playerName;
    public int ticketNumber; 
    public int finpoint; 
    public int lastRoll;
    public int cardCount => cards.Count;
    public List<Card> cards = new List<Card>();
    public bool isBot;
    public List<HelpCard> helpCards = new List<HelpCard>();
    public Dictionary<string, MarketPredictionType> marketPredictions = new Dictionary<string, MarketPredictionType>();



    public PlayerProfile(string name)
    {
        playerName = name;
        finpoint = 100;
        ticketNumber = 0;

    }

    public Dictionary<string, int> GetCardColorCounts()
{
    Dictionary<string, int> colorCounts = new Dictionary<string, int>
    {
        { "Konsumer", 0 },
        { "Infrastruktur", 0 },
        { "Keuangan", 0 },
        { "Tambang", 0 }
    };

    foreach (var card in cards)
    {
        if (colorCounts.ContainsKey(card.color))
            colorCounts[card.color]++;
    }

    return colorCounts;
}


    public void SetLastRoll(int roll)
    {
        lastRoll = roll;
    }

    public void AddCard(Card card)
    {
        cards.Add(card);
    }
     public bool CanAfford(int cost)
    {
        return finpoint >= cost;
    }

    public void DeductFinpoint(int amount)
    {
        finpoint -= amount;
    }
}
