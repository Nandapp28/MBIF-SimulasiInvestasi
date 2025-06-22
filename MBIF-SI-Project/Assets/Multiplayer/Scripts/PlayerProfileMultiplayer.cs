using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PlayerProfileMultiplayer
{
    public string playerName;
    public int ticketNumber; 
    public int finpoint; 
    public int lastRoll;
    public List<Card> cards = new List<Card>();
    public bool isBot; 
    public int actorNumber; 
    public int bonusActions = 0;

    public int cardCount
    {
        get { return cards.Count; }
    }

    // Constructor untuk Single Player / Bot
    public PlayerProfileMultiplayer(string name)
    {
        playerName = name;
        finpoint = 100;
        isBot = true; 
        actorNumber = -1;
    }

    // Constructor untuk Multiplayer
    public PlayerProfileMultiplayer(string name, int actorNum)
    {
        playerName = name;
        finpoint = 100;
        isBot = false;
        actorNumber = actorNum;
    }

    public Dictionary<string, int> GetCardColorCounts()
    {
        var colorCounts = new Dictionary<string, int> { { "Red", 0 }, { "Blue", 0 }, { "Green", 0 }, { "Orange", 0 } };
        foreach (var card in cards)
        {
            if (colorCounts.ContainsKey(card.color))
                colorCounts[card.color]++;
        }
        return colorCounts;
    }

    // --- FUNGSI BARU UNTUK FASE PENJUALAN ---
    // Fungsi ini akan menghapus kartu yang telah dijual dari tangan pemain.
    public void RemoveSoldCards(string color, int amount)
    {
        int removedCount = 0;
        // Kita menggunakan loop dari belakang agar aman saat menghapus item dari list
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            if (removedCount >= amount) break; // Keluar jika sudah cukup kartu yang dihapus

            if (cards[i].color == color)
            {
                cards.RemoveAt(i);
                removedCount++;
            }
        }
    }

    public void SetLastRoll(int roll) { lastRoll = roll; }
    public void AddCard(Card card) { cards.Add(card); }
    public bool CanAfford(int cost) { return finpoint >= cost; }
    public void DeductFinpoint(int amount) { finpoint -= amount; }
}
