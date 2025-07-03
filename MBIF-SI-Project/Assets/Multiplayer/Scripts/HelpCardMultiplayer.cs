// File: HelpCardMultiplayer.cs

[System.Serializable]
public class HelpCardMultiplayer
{
    public string cardName;
    public string description;
    public HelpCardEffect effectType;

    public HelpCardMultiplayer(string name, string desc, HelpCardEffect effect)
    {
        cardName = name;
        description = desc;
        effectType = effect;
    }
}