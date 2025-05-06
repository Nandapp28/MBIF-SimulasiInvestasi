using System.Collections.Generic;


[System.Serializable]
public class PlayerProfile
{
    public string playerName;
    public int ticketNumber; 
    public int finpoint; 
    public int lastRoll;
    public int cardCount => cards.Count;
    public List<GameManager.Card> cards = new List<GameManager.Card>();

    public PlayerProfile(string name)
    {
        playerName = name;
        finpoint = 100; 
        
    }

    

    public void SetLastRoll(int roll)
    {
        lastRoll = roll;
    }

    public void AddCard(GameManager.Card card)
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
