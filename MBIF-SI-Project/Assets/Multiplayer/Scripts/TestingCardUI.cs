// File: TestingCardUI.cs (Versi Disesuaikan untuk Enum)
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestingCardUI : MonoBehaviour
{
    public Image cardArtwork;
    public TextMeshProUGUI cardNameText;

    public void Setup(TestingCardData cardData)
    {
        if (cardData == null) return;

        if (cardArtwork != null)
        {
            cardArtwork.sprite = cardData.cardSprite;
        }

        if (cardNameText != null)
        {
            // --- PERUBAHAN: Kita ambil nama dari enum dengan .ToString() ---
            // Contoh: jika 'cardType' adalah TestingCardType.Cardtest1,
            // .ToString() akan menghasilkan string "Cardtest1".
            cardNameText.text = cardData.cardType.ToString();
        }
    }
}