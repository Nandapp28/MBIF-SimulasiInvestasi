// File: RumorEffectData.cs
using UnityEngine;

// Enum ini kita definisikan di sini agar mudah diakses
public enum RumorType 
{ 
    ModifyIPO, BonusFinpoint, PenaltyFinpoint, 
    ResetAllIPO, TaxByTurnOrder, StockDilution 
}

[System.Serializable]
public class RumorEffectData
{
    public string cardName;
    public string description;
    public Sektor color; // Kita pakai lagi enum Sektor dari CardPoolEntry.cs
    public RumorType effectType;
    public int value;
    public Sprite artwork; // Anda bisa tambahkan ini jika punya gambar untuk setiap rumor
}