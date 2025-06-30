// File: HelpCard.cs

using UnityEngine;

// Enum untuk mendefinisikan jenis-jenis efek kartu bantuan
public enum HelpCardEffect
{
    ExtraFinpoints,
    BoostRandomIPO,      // Menaikkan IPO salah satu warna secara acak
    AdiministrativePenalties,
    NegativeEquity,   // Menurunkan IPO salah satu warna secara acak
    FreeCardPurchase,
    TaxEvasion,
    MarketPrediction,
    EyeOfTruth,
    MarketStabilization,
    CardSwap,
    ForcedPurchase 
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