// File: Scripts/RumorCardViewerUI.cs

using UnityEngine;
using UnityEngine.UI; // Pastikan menggunakan namespace ini

public class RumorCardViewerUI : MonoBehaviour
{
    public static RumorCardViewerUI Instance { get; private set; }

    [Header("Komponen UI")]
    [Tooltip("Objek Panel utama yang berisi semua elemen UI viewer.")]
    public GameObject viewerPanel;

    // --- PERUBAHAN DI SINI ---
    [Tooltip("Komponen Image untuk menampilkan sprite kartu.")]
    public Image cardDisplayImage; // Diubah dari RawImage menjadi Image

    [Tooltip("Tombol untuk menutup panel.")]
    public Button closeButton;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
        }
    }

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
        else
        {
            Debug.LogError("Tombol 'closeButton' belum di-assign di Inspector!");
        }
    }

    /// <summary>
    /// Menampilkan panel dengan sprite kartu yang dipilih.
    /// </summary>
    /// <param name="cardSprite">Sprite dari kartu yang ingin ditampilkan.</param>
    public void ShowCard(Sprite cardSprite) // --- PERUBAHAN DI SINI ---
    {
        if (viewerPanel == null || cardDisplayImage == null)
        {
            Debug.LogError("Panel atau Image display belum di-assign di Inspector!");
            return;
        }

        // Atur sprite dan aktifkan panel
        cardDisplayImage.sprite = cardSprite; // --- PERUBAHAN DI SINI ---
        viewerPanel.SetActive(true);
    }

    public void HidePanel()
    {
        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
        }
    }
}