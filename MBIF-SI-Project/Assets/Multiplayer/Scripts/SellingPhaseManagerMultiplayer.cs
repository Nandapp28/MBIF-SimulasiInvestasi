// File: SellingPhaseManagerMultiplayer.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

public class SellingPhaseManagerMultiplayer : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject sellingUI;
    public Button confirmSellButton;
    public Transform colorSellPanelContainer;
    public GameObject colorSellRowPrefab;

    // Data IPO yang harus disinkronkan
    public List<IPOData> ipoDataList = new List<IPOData>();
    // ... (Logika visual IPO bisa tetap di sini)
    
    private Dictionary<string, int> playerSellChoices = new Dictionary<string, int>();

    public void StartSellingPhase(Dictionary<int, PlayerProfileMultiplayer> players, List<int> turnOrder)
    {
        // Tampilkan UI hanya untuk pemain lokal
        PlayerProfileMultiplayer localPlayer = players[PhotonNetwork.LocalPlayer.ActorNumber];
        SetupSellingUI(localPlayer);
    }
    
    private void SetupSellingUI(PlayerProfileMultiplayer player)
    {
        sellingUI.SetActive(true);
        confirmSellButton.onClick.RemoveAllListeners();
        foreach (Transform child in colorSellPanelContainer) Destroy(child.gameObject);

        Dictionary<string, int> maxValues = player.GetCardColorCounts();
        playerSellChoices.Clear();

        foreach (var color in new string[] {"Red", "Blue", "Green", "Orange"})
        {
            GameObject row = Instantiate(colorSellRowPrefab, colorSellPanelContainer);
            row.transform.Find("ColorLabel").GetComponent<Text>().text = color;

            Text valueText = row.transform.Find("ValueText").GetComponent<Text>();
            Button plusButton = row.transform.Find("PlusButton").GetComponent<Button>();
            Button minusButton = row.transform.Find("MinusButton").GetComponent<Button>();

            playerSellChoices[color] = 0;
            int maxValue = maxValues.ContainsKey(color) ? maxValues[color] : 0;
            valueText.text = "0";

            plusButton.onClick.AddListener(() => {
                if (playerSellChoices[color] < maxValue)
                {
                    playerSellChoices[color]++;
                    valueText.text = playerSellChoices[color].ToString();
                }
            });

            minusButton.onClick.AddListener(() => {
                if (playerSellChoices[color] > 0)
                {
                    playerSellChoices[color]--;
                    valueText.text = playerSellChoices[color].ToString();
                }
            });
        }

        confirmSellButton.onClick.AddListener(ConfirmSell);
    }

    private void ConfirmSell()
    {
        sellingUI.SetActive(false);
        
        // Konversi dictionary ke array untuk dikirim via RPC
        string[] colors = playerSellChoices.Keys.ToArray();
        int[] counts = playerSellChoices.Values.ToArray();
        
        // Kirim data penjualan ke MasterClient melalui MultiplayerManager
        PhotonView pv = MultiplayerManager.Instance.GetComponent<PhotonView>();
        pv.RPC("Cmd_SubmitSellOrder", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, colors, counts);
    }

    // Fungsi untuk mengupdate visual IPO yang dipanggil oleh RPC dari MultiplayerManager
    public void UpdateIPOVisuals(string[] colors, int[] indices)
    {
        // ... Logika untuk menggerakkan objek visual IPO berdasarkan data baru ...
    }

    // Kelas data IPO bisa tetap di sini
    [System.Serializable]
    public class IPOData
    {
        public string color;
        public int ipoIndex;
        public GameObject colorObject;
    }
}