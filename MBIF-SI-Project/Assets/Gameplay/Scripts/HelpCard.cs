using UnityEngine;

[System.Serializable]
public class HelpCard
{
    public string cardName;
    public string description;
    public HelpCardEffect effectType;
    public Sprite cardImage;

    public HelpCard(string name, string desc, HelpCardEffect effect, Sprite image)
    {
        cardName = name;
        description = desc;
        effectType = effect;
        cardImage = image;
    }
}