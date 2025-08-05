// File: TestingCardData.cs (Versi dengan Dropdown/Enum)
using UnityEngine;

// --- TAMBAHAN: Kita definisikan semua kemungkinan nama kartu di sini ---
// Nama enum ini bisa apa saja, misalnya TestingCardName atau TestingCardType.
public enum TestingCardType 
{
    Cardtest1,
    Cardtest2,
    Cardtest3,
    Cardtest4,
    Cardtest5,
    Cardtest6,
    Cardtest7,
    Cardtest8
}

[System.Serializable]
public class TestingCardData
{
    // --- PERUBAHAN: Mengganti 'string cardName' dengan enum yang baru kita buat ---
    // Di Inspector, ini akan otomatis menjadi sebuah dropdown.
    public TestingCardType cardType;
    public Sprite cardSprite;
}