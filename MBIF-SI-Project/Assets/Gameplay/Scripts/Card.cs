[System.Serializable]
public class Card
{
    public string cardName;
    public string description;
    public int value;
    public string color; // 🔴 Tambahkan warna

    public Card(string name, string desc, int val = 0, string color = "Red")
    {
        cardName = name;
        description = desc;
        value = val;
        this.color = color;
    }
}
