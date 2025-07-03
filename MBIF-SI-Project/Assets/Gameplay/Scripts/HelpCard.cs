// File: HelpCard.cs

using UnityEngine;

[System.Serializable]
public class HelpCard
{
    public string cardName;
    public string description;
    public HelpCardEffect effectType;

    public HelpCard(string name, string desc, HelpCardEffect effect)
    {
        cardName = name;
        description = desc;
        effectType = effect;
    }
}