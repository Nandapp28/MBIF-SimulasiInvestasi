// File: CardMultiplayer.cs
using UnityEngine;

[System.Serializable]
public class CardMultiplayer
{
    public string cardName;
    public string description;
    public Sektor color; // Kita gunakan enum Sektor dari CardPoolEntry
    public int baseValue;
    public int value;
    public Sprite cardSprite;

    // Konstruktor untuk membuat kartu saat game berjalan
    public CardMultiplayer(string name, string desc, int baseVal, Sektor cardColor, Sprite sprite)
    {
        this.cardName = name;
        this.description = desc;
        this.baseValue = baseVal;
        this.color = cardColor;
        this.cardSprite = sprite;
        this.value = baseVal; // Nilai awal sama dengan nilai dasar
    }
}