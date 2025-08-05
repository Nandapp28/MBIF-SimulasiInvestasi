// File: TestingCardUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Gunakan ini jika Anda memakai TextMeshPro

public class TestingCardUI : MonoBehaviour
{
    // Referensi ke komponen UI di dalam prefab kartu
    public Image cardArtwork;
    public TextMeshProUGUI cardNameText; // Ganti ke 'public Text' jika pakai UI Text biasa

    // Fungsi sederhana untuk mengatur tampilan kartu
    // Kita menggunakan CardPoolEntry karena lebih simpel dan sudah ada datanya.
    public void Setup(CardPoolEntry cardData)
    {
        if (cardData == null) return;

        if (cardArtwork != null)
        {
            cardArtwork.sprite = cardData.cardSprite;
        }

        if (cardNameText != null)
        {
            cardNameText.text = cardData.cardName;
        }
    }
}