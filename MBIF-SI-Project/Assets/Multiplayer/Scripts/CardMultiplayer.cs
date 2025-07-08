[System.Serializable]
public class CardMultiplayer
{
    public string cardName;
    public string description;
    public int baseValue;
    public int value; // Nilai setelah ditambah IPO
    public string color;

    public CardMultiplayer(string name, string desc, int baseVal = 0, string color = "Konsumer")
    {
        cardName = name;
        description = desc;
        baseValue = baseVal;
        this.color = color;
        value = baseValue;
    }
}