using UnityEngine;
using UnityEngine.UI; // Menggunakan namespace untuk Legacy UI
using System.Linq;

// Skrip ini tidak lagi memerlukan "using TMPro;"

public class PlayerInfoPanel : MonoBehaviour
{
    [Header("Referensi Game")]
    public GameManager gameManager;
    public SellingPhaseManager sellingManager;

    [Header("Komponen UI Panel")]
    public GameObject panelObject;
    public Button closeButton;

    // Semua referensi diubah dari TextMeshProUGUI menjadi Text
    public Text nameText;
    public Text scoreText; // Untuk nomor tiket
    public Text finpointText;
    public Text KonsumerCardText;
    public Text InfrastrukturCardText;
    public Text KeuanganCardText;
    public Text TambangCardText;
    public Text assetValueText;
    public Text totalWorthText;

    void Awake()
    {
        // Pastikan panel tersembunyi saat game dimulai
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
        
        // Atur listener untuk tombol close
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }
    }

    /// <summary>
    /// Menampilkan dan mengisi panel dengan data dari pemain yang dipilih.
    /// </summary>
    public void ShowPanelForPlayer(PlayerProfile player)
    {
        if (player == null || sellingManager == null)
        {
            Debug.LogError("Player Profile atau Selling Manager belum di-assign!");
            return;
        }

        // --- Isi data utama ---
        nameText.text = player.playerName;
        scoreText.text = $"TURN: {player.ticketNumber}";
        finpointText.text = player.finpoint.ToString();

        // --- Isi jumlah kartu per warna ---
        var colorCounts = player.GetCardColorCounts();
        KonsumerCardText.text = (colorCounts.ContainsKey("Konsumer") ? colorCounts["Konsumer"] : 0).ToString();
        InfrastrukturCardText.text = (colorCounts.ContainsKey("Infrastruktur") ? colorCounts["Infrastruktur"] : 0).ToString();
        KeuanganCardText.text = (colorCounts.ContainsKey("Keuangan") ? colorCounts["Keuangan"] : 0).ToString();
        TambangCardText.text = (colorCounts.ContainsKey("Tambang") ? colorCounts["Tambang"] : 0).ToString();

        // --- Hitung dan tampilkan nilai aset & total kekayaan ---
        int assetValue = colorCounts.Sum(entry => entry.Value * sellingManager.GetFullCardPrice(entry.Key));
        int totalWorth = player.finpoint + assetValue;

        assetValueText.text = assetValue.ToString();
        totalWorthText.text = totalWorth.ToString();
        
        // Tampilkan panel
        panelObject.SetActive(true);
    }

    /// <summary>
    /// Menyembunyikan panel.
    /// </summary>
    public void HidePanel()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }
    }
}