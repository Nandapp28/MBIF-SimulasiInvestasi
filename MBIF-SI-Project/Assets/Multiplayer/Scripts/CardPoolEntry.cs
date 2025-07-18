// File: CardPoolEntry.cs
using UnityEngine;

// [System.Serializable] membuat ini bisa muncul dan di-edit di dalam Inspector
public enum Sektor { Konsumer, Infrastruktur, Keuangan, Tambang, Netral }

[System.Serializable]
public class CardPoolEntry
{
    public string cardName; // Nama efek kartu (Flashbuy, dll.)
    public Sektor color;    // Nama sektor (Konsumer, Keuangan, dll.)
    public Sprite cardSprite;
}