using UnityEngine;

[System.Serializable]
public class PlayerProfile
{
    public string playerName;
    public int score;

    public PlayerProfile(string name)
    {
        playerName = name;
        score = 0;
    }

    public void SetScore(int newScore)
    {
        score = newScore;
    }
}
