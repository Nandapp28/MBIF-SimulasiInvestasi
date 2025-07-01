[System.Serializable]
public class Card
{
    public string cardName;
    public string description;
    public int baseValue;  // nilai dasar kartu
    public int value;      // nilai kartu setelah ditambah IPO price
    public string color;

    public Card(string name, string desc, int baseVal = 0, string color = "Red")
    {
        cardName = name;
        description = desc;
        baseValue = baseVal;
        this.color = color;
        value = baseValue;  // awalnya sama dengan baseValue
    }
}
