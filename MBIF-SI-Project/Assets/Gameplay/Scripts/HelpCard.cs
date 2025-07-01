// File: HelpCard.cs

using UnityEngine;

// Enum untuk mendefinisikan jenis-jenis efek kartu bantuan
public enum HelpCardEffect
{
    ExtraFinpoints,      // Dapat finpoint tambahan
    BoostRandomIPO,      // Menaikkan IPO salah satu warna secara acak
    SabotageRandomIPO,   // Menurunkan IPO salah satu warna secara acak
    FreeCardPurchase     // Menggratiskan pembelian 1 kartu di semester berikutnya (fitur lebih advanced)
}

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