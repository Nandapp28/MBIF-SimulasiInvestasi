// File: ActionCardUI.cs (Versi Final dengan Artwork dan Harga Dinamis)
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

        // --- PERUBAHAN LOGIKA HARGA DIMULAI DI SINI ---
        if (costText != null)
        {
            int sectorPrice = 0;
            // 1. Dapatkan harga sektor (IPO) saat ini dari SellingPhaseManagerMultiplayer
            if (SellingPhaseManagerMultiplayer.Instance != null)
            {
                sectorPrice = SellingPhaseManagerMultiplayer.Instance.GetFullCardPrice(cardData.color.ToString());
            }
            else
            {
                Debug.LogWarning("[ActionCardUI] Tidak dapat menemukan SellingPhaseManagerMultiplayer.Instance untuk mengambil harga sektor.");
            }

            // 2. Dapatkan harga efek (baseValue) dari data kartu
            int effectPrice = cardData.baseValue; //

            // 3. Terapkan logika format string
            if (effectPrice > 0)
            {
                // Format: 5(+1)
                costText.text = $"{sectorPrice}(+{effectPrice})";
            }
            else
            {
                // Format: 5
                costText.text = sectorPrice.ToString();
            }
        }
        // --- PERUBAHAN LOGIKA HARGA SELESAI ---
        
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