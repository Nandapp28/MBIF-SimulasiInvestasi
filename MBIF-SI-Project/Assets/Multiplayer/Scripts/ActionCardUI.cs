// File: ActionCardUI.cs (Versi Final dengan Artwork)
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionCardUI : MonoBehaviour
{
    [Header("UI References")]
    // Tambahkan kembali referensi untuk artwork
    public Image artworkImage; 
    public TextMeshProUGUI costText;
    public Button selectButton;

    private int cardId;
    private ActionPhaseManager actionManager;

    public void Setup(CardMultiplayer cardData, int id, ActionPhaseManager manager)
    {
        this.cardId = id;
        this.actionManager = manager;

        if (this.actionManager == null)
        {
            Debug.LogError($"[ActionCardUI] GAGAL: Referensi ke ActionPhaseManager adalah null saat Setup kartu ID #{id}.");
            return;
        }

        // Atur artwork unik untuk kartu ini
        if (artworkImage != null)
        {
            artworkImage.sprite = cardData.cardSprite;
        }

        // Atur teks harga
        if (costText != null)
        {
            costText.text = cardData.value.ToString();
        }
        
        // Atur listener untuk tombol
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnCardSelected);
        }
    }

    private void OnCardSelected()
    {
        if (actionManager == null) return;

        // --- PERUBAHAN: Mengambil CardMultiplayer ---
        CardMultiplayer cardData = actionManager.GetCardFromTable(this.cardId);
        if (cardData != null)
        {
            Debug.Log($"Kartu ID #{this.cardId} ({cardData.cardName}) telah diklik!");
        }
        else
        {
            Debug.Log($"Kartu ID #{this.cardId} telah diklik, data tidak ditemukan.");
        }
        actionManager.OnCardSelected(this.cardId);
    }
}