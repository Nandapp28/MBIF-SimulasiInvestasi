using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PlayerProfile
{
    public string playerName;
    public int ticketNumber; 
    public int finpoint; 
    public int lastRoll;
    public List<Card> cards = new List<Card>();
    public bool isBot; 
    public int actorNumber; 

    // --- PERBAIKAN: TAMBAHKAN BARIS INI ---
    public int bonusActions = 0;

    public int cardCount
    {
        get { return cards.Count; }
    }

    // Constructor untuk Single Player / Bot
    public PlayerProfile(string name)
    {
        playerName = name;
        finpoint = 100;
        isBot = true; 
        actorNumber = -1; // ID default untuk non-pemain online
    }

    // Constructor untuk Multiplayer
    public PlayerProfile(string name, int actorNum)
    {
        playerName = name;
        finpoint = 100;
        isBot = false;
        actorNumber = actorNum; // Simpan ID unik pemain
    }

    public Dictionary<string, int> GetCardColorCounts()
    {
        Dictionary<string, int> colorCounts = new Dictionary<string, int> { { "Red", 0 }, { "Blue", 0 }, { "Green", 0 }, { "Orange", 0 } };
        foreach (var card in cards)
        {
            if (colorCounts.ContainsKey(card.color))
                colorCounts[card.color]++;
        }
        return colorCounts;
    }

    public void SetLastRoll(int roll) { lastRoll = roll; }
    public void AddCard(Card card) { cards.Add(card); }
    public bool CanAfford(int cost) { return finpoint >= cost; }
    public void DeductFinpoint(int amount) { finpoint -= amount; }
}
