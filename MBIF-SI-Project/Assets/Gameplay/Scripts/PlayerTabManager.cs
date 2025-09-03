using UnityEngine;
using UnityEngine.UI;
using TMPro; // Menggunakan TextMeshPro
using System.Linq; // <-- PENTING: Diperlukan untuk metode OrderBy
using System.Collections.Generic;

public class PlayerTabManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject playerTabPanel;
    public Button togglePlayerTabButton;
    public Button closeButton;
     public Button sortButton;
    public Transform playerListContainer;
    public GameObject playerInfoPrefab;
    [Header("Game References")] // <-- TAMBAHKAN HEADER INI
    public SellingPhaseManager sellingManager;
    private GameManager gameManager;
    private bool isPanelActive = false;
    private bool isSortedByWorth = false;


    void Start()
    {
        gameManager = GameManager.Instance;
        playerTabPanel.SetActive(false);

        if (togglePlayerTabButton != null)
        {
            togglePlayerTabButton.onClick.AddListener(TogglePlayerTab);
        }
        if (sellingManager == null)
        {
            sellingManager = FindObjectOfType<SellingPhaseManager>();
            if (sellingManager == null)
            {
                Debug.LogError("[PlayerTab] SellingPhaseManager tidak ditemukan di scene!");
            }
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(TogglePlayerTab);
        }
        if (sortButton != null)
        {
            sortButton.onClick.AddListener(ToggleSort);
        }
    }

    public void TogglePlayerTab()
    {
        isPanelActive = !isPanelActive;
        playerTabPanel.SetActive(isPanelActive);

        if (isPanelActive)
        {
            UpdatePlayerList();
        }
    }
private void ToggleSort()
    {
        isSortedByWorth = !isSortedByWorth; // Balikkan status urutan (true -> false, false -> true)
        UpdatePlayerList(); // Perbarui daftar dengan urutan baru
    }
      public void UpdatePlayerList()
    {
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        if (gameManager == null || gameManager.turnOrder == null || sellingManager == null)
        {
            Debug.LogError("[PlayerTab] Salah satu manager (GameManager/SellingPhaseManager) masih NULL!");
            return;
        }

        // 1. Hitung kekayaan setiap pemain
        var playersWithData = gameManager.turnOrder.Select(player => {
            var colorCounts = player.GetCardColorCounts();
            int assetValue = colorCounts.Sum(entry => entry.Value * sellingManager.GetFullCardPrice(entry.Key));
            return new
            {
                Profile = player,
                TotalWorth = player.finpoint + assetValue,
                AssetValue = assetValue
            };
        }).AsEnumerable(); // Gunakan AsEnumerable() agar bisa diurutkan nanti

        // 2. Pilih cara mengurutkan berdasarkan flag 'isSortedByWorth'
        if (isSortedByWorth)
        {
            // Jika true, urutkan berdasarkan kekayaan
            playersWithData = playersWithData.OrderByDescending(p => p.TotalWorth);
        }
        else
        {
            // Jika false (default), urutkan berdasarkan nama
            playersWithData = playersWithData.OrderBy(p => p.Profile.playerName);
        }

        // Loop melalui daftar yang sudah diurutkan
        foreach (var playerData in playersWithData)
        {
            PlayerProfile player = playerData.Profile;
            GameObject entryObj = Instantiate(playerInfoPrefab, playerListContainer);

            // Tampilkan semua data
            SetTextComponent(entryObj, "NameText", player.playerName);
            SetTextComponent(entryObj, "ScoreText", $"{player.ticketNumber}");
            SetTextComponent(entryObj, "Finpoint", player.finpoint.ToString());

            var colorCounts = player.GetCardColorCounts();
            SetTextComponent(entryObj, "RedCardText", (colorCounts.ContainsKey("Konsumer") ? colorCounts["Konsumer"] : 0).ToString());
            SetTextComponent(entryObj, "BlueCardText", (colorCounts.ContainsKey("Infrastruktur") ? colorCounts["Infrastruktur"] : 0).ToString());
            SetTextComponent(entryObj, "GreenCardText", (colorCounts.ContainsKey("Keuangan") ? colorCounts["Keuangan"] : 0).ToString());
            SetTextComponent(entryObj, "OrangeCardText", (colorCounts.ContainsKey("Tambang") ? colorCounts["Tambang"] : 0).ToString());

            SetTextComponent(entryObj, "AssetValueText", playerData.AssetValue.ToString());
            SetTextComponent(entryObj, "TotalWorthText", playerData.TotalWorth.ToString());
        }
    }
    private bool SetTextComponent(GameObject parentObject, string childName, string value)
    {
        Transform childTransform = parentObject.transform.Find(childName);
        if (childTransform == null)
        {
            Debug.LogError($"--> GAGAL menemukan objek anak bernama '{childName}' di prefab '{parentObject.name}'!", parentObject);
            return false;
        }

        TextMeshProUGUI textComponent = childTransform.GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogError($"--> GAGAL menemukan komponen 'TextMeshProUGUI' pada objek anak '{childName}'.", childTransform);
            return false;
        }

        textComponent.text = value;
        return true;
    }
}