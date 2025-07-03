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
        finpoint = 10;
        ticketNumber = 0;

    }

    public Dictionary<string, int> GetCardColorCounts()
{
    Dictionary<string, int> colorCounts = new Dictionary<string, int>
    {
        { "Red", 0 },
        { "Blue", 0 },
        { "Green", 0 },
        { "Orange", 0 }
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
