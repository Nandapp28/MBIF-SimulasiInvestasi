using System.Collections.Generic;


[System.Serializable]
public class PlayerProfile
{
    public string playerName;
    public int score;
    public int lastRoll;
    public int cardCount => cards.Count;
    public List<GameManager.Card> cards = new List<GameManager.Card>();

    public PlayerProfile(string name)
    {
        playerName = name;
        score = 0;
    }

    public void SetScore(int newScore)
    {
        score = newScore;
    }

    public void SetLastRoll(int roll)
    {
        lastRoll = roll;
    }

    public void AddCard(GameManager.Card card)
    {
        cards.Add(card);
    }
}
