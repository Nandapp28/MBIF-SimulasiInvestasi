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
    public Transform playerListContainer;
    public GameObject playerInfoPrefab;

    private GameManager gameManager;
    private bool isPanelActive = false;

    void Start()
    {
        gameManager = GameManager.Instance;
        playerTabPanel.SetActive(false);

        if (togglePlayerTabButton != null)
        {
            togglePlayerTabButton.onClick.AddListener(TogglePlayerTab);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(TogglePlayerTab);
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

    public void UpdatePlayerList()
    {
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        if (gameManager == null || gameManager.turnOrder == null)
        {
            Debug.LogError("[PlayerTab] GameManager atau TurnOrder masih NULL!");
            return;
        }

        // --- PERUBAHAN DI SINI ---
        // Urutkan daftar pemain berdasarkan playerName secara alfabetis sebelum ditampilkan.
        var sortedPlayers = gameManager.turnOrder.OrderBy(player => player.playerName).ToList();

        // Loop melalui daftar yang SUDAH diurutkan (sortedPlayers)
        foreach (PlayerProfile player in sortedPlayers)
        {
            GameObject entryObj = Instantiate(playerInfoPrefab, playerListContainer);

            if (!SetTextComponent(entryObj, "NameText", player.playerName)) continue;
            if (!SetTextComponent(entryObj, "ScoreText", $"{player.ticketNumber}")) continue;
            if (!SetTextComponent(entryObj, "Finpoint", player.finpoint.ToString())) continue;
            if (!SetTextComponent(entryObj, "HelpCardCountText", $"{player.helpCards.Count}")) continue;

            var colorCounts = player.GetCardColorCounts();
            if (!SetTextComponent(entryObj, "RedCardText", (colorCounts.ContainsKey("Konsumer") ? colorCounts["Konsumer"] : 0).ToString())) continue;
            if (!SetTextComponent(entryObj, "BlueCardText", (colorCounts.ContainsKey("Infrastruktur") ? colorCounts["Infrastruktur"] : 0).ToString())) continue;
            if (!SetTextComponent(entryObj, "GreenCardText", (colorCounts.ContainsKey("Keuangan") ? colorCounts["Keuangan"] : 0).ToString())) continue;
            if (!SetTextComponent(entryObj, "OrangeCardText", (colorCounts.ContainsKey("Tambang") ? colorCounts["Tambang"] : 0).ToString())) continue;
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