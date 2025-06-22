using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Photon.Pun;

// Pastikan GameObject yang memiliki skrip ini juga memiliki komponen "Photon View"
[RequireComponent(typeof(PhotonView))]
public class SellingPhaseManagerMultiplayer : MonoBehaviourPunCallbacks
{
    public static SellingPhaseManagerMultiplayer Instance;

    // Referensi ke GameManager dan RumorPhaseManager dihapus karena tidak lagi diperlukan di multiplayer
    // public GameObject resetSemesterButton; // Ini dikontrol oleh MultiplayerManager

    [Header("UI Elements")]
    public GameObject sellingUI;
    public Button confirmSellButton;
    public Transform colorSellPanelContainer;
    public GameObject colorSellRowPrefab;

    [Header("IPO Settings")]
    public List<IPOData> ipoDataList = new List<IPOData>();
    public float ipoSpacing = 0.5f;
    private Dictionary<string, Vector3> initialPositions = new Dictionary<string, Vector3>();
    private HashSet<string> bonusMultiplierColors = new HashSet<string>();

    public Dictionary<string, int[]> ipoPriceMap = new Dictionary<string, int[]>
    {
        { "Red",    new int[] { 1, 2, 3, 5, 6, 7, 8 } },
        { "Blue",   new int[] { 1, 3, 4, 5, 6, 7, 9 } },
        { "Green",  new int[] { 0, 2, 4, 5, 7, 9, 0 } },
        { "Orange", new int[] { 1, 2, 4, 5, 6, 8, 9 } }
    };

    [System.Serializable]
    public class IPOData { public string color; public int ipoIndex = 0; public GameObject colorObject; }
    public class SellInput { /* ... Definisi SellInput Anda ... */ }

    private void Awake() 
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        foreach (var data in ipoDataList)
        {
            if (data.colorObject != null && !initialPositions.ContainsKey(data.color))
            {
                initialPositions[data.color] = data.colorObject.transform.position;
            }
        }
        sellingUI.SetActive(false);
    }

    public void StartSellingPhase(List<PlayerProfile> players, int resetCount, int maxResetCount, GameObject resetButton)
    {
        sellingUI.SetActive(false); // Sembunyikan dulu untuk semua orang
        
        // Tampilkan UI penjualan hanya untuk pemain lokal
        PlayerProfile localPlayer = players.FirstOrDefault(p => p.actorNumber == PhotonNetwork.LocalPlayer.ActorNumber);
        if (localPlayer != null)
        {
            SetupSellingUI(localPlayer);
        }
    }

    private void SetupSellingUI(PlayerProfile player)
    {
        sellingUI.SetActive(true);
        confirmSellButton.onClick.RemoveAllListeners();
        foreach (Transform child in colorSellPanelContainer) Destroy(child.gameObject);

        Dictionary<string, int> cardsToSell = new Dictionary<string, int>();
        Dictionary<string, int> maxCards = player.GetCardColorCounts();

        foreach (var color in ipoPriceMap.Keys)
        {
            GameObject row = Instantiate(colorSellRowPrefab, colorSellPanelContainer);
            row.transform.Find("ColorLabel").GetComponent<Text>().text = color;
            Text valueText = row.transform.Find("ValueText").GetComponent<Text>();
            Button plusButton = row.transform.Find("PlusButton").GetComponent<Button>();
            Button minusButton = row.transform.Find("MinusButton").GetComponent<Button>();

            cardsToSell[color] = 0;
            int maxAmount = maxCards.ContainsKey(color) ? maxCards[color] : 0;
            valueText.text = "0";

            plusButton.onClick.AddListener(() => {
                if (cardsToSell[color] < maxAmount)
                {
                    cardsToSell[color]++;
                    valueText.text = cardsToSell[color].ToString();
                }
            });
            minusButton.onClick.AddListener(() => {
                if (cardsToSell[color] > 0)
                {
                    cardsToSell[color]--;
                    valueText.text = cardsToSell[color].ToString();
                }
            });
        }

        confirmSellButton.onClick.AddListener(() => {
            List<string> colorsSold = new List<string>();
            List<int> countsSold = new List<int>();
            foreach(var sale in cardsToSell)
            {
                if (sale.Value > 0)
                {
                    colorsSold.Add(sale.Key);
                    countsSold.Add(sale.Value);
                }
            }
            
            // Kirim data penjualan ke MasterClient untuk diproses
            photonView.RPC(nameof(Cmd_ProcessSale), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, colorsSold.ToArray(), countsSold.ToArray());
            
            sellingUI.SetActive(false);
            confirmSellButton.interactable = false; // Mencegah klik ganda
        });
        
        confirmSellButton.interactable = true; // Aktifkan tombol saat UI muncul
    }

    // [Dijalankan di MasterClient] Menerima perintah penjualan
    [PunRPC]
    private void Cmd_ProcessSale(int actorNumber, string[] colors, int[] counts)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int totalGains = 0;
        for (int i = 0; i < colors.Length; i++)
        {
            int price = GetCurrentColorValue(colors[i]);
            totalGains += price * counts[i];
        }

        // Kirim hasilnya kembali ke semua pemain
        photonView.RPC(nameof(RPC_FinalizeSale), RpcTarget.All, actorNumber, totalGains, colors, counts);
    }
    
    // [Dijalankan di SEMUA Pemain] Menerima hasil penjualan dari MasterClient
    [PunRPC]
    private void RPC_FinalizeSale(int actorNumber, int gainedFinpoints, string[] soldColors, int[] soldCounts)
    {
        // Cari profil pemain yang sesuai di setiap komputer
        PlayerProfile player = MultiplayerManager.Instance.GetPlayerProfile(actorNumber);
        if (player != null)
        {
            player.finpoint += gainedFinpoints;
            for (int i = 0; i < soldColors.Length; i++)
            {
                player.RemoveSoldCards(soldColors[i], soldCounts[i]);
            }
            
            Debug.Log($"{player.playerName} menjual kartu dan mendapat {gainedFinpoints} FP. Finpoint sekarang: {player.finpoint}");
            MultiplayerManager.Instance.UpdatePlayerUI();
        }

        if (PhotonNetwork.IsMasterClient)
        {
            MultiplayerManager.Instance.PlayerFinishedSelling(actorNumber);
        }
    }

    private int GetCurrentColorValue(string color)
    {
        IPOData data = ipoDataList.FirstOrDefault(d => d.color == color);
        if (data != null && ipoPriceMap.ContainsKey(color))
        {
            int index = Mathf.Clamp(data.ipoIndex, -3, 3);
            if (color == "Green") index = Mathf.Clamp(data.ipoIndex, -2, 2);
            return ipoPriceMap[color][index + 3];
        }
        return 0;
    }
    
    private void UpdateIPOVisuals()
    {
        foreach (var data in ipoDataList)
        {
            if (data.colorObject != null && initialPositions.ContainsKey(data.color))
            {
                Vector3 basePos = initialPositions[data.color];
                Vector3 offset = new Vector3(data.ipoIndex * ipoSpacing, 0, 0);
                data.colorObject.transform.position = basePos + offset;
            }
        }
    }
    
    public void HandleCrashMultiplier(IPOData data, PlayerProfile affectedPlayer) { /* Implementasi Anda di sini */ }
}
