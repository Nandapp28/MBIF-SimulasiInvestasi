using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Photon.Pun;

public class SellingPhaseManager : MonoBehaviour
{
    // --- PENAMBAHAN BARU UNTUK MEMPERBAIKI ERROR ---
    public static SellingPhaseManager Instance; // Singleton Instance

    // Game References tidak lagi dibutuhkan di Inspector
    public GameObject resetSemesterButton;

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

    private Dictionary<int, SellInput> playerSellInputs = new Dictionary<int, SellInput>();
    private PlayerProfile currentPlayer;
    private List<PlayerProfile> currentPlayers;
    private int currentResetCount;
    private int currentMaxResetCount;
    
    public Dictionary<string, int[]> ipoPriceMap = new Dictionary<string, int[]>
    {
        { "Red",    new int[] { 1, 2, 3, 5, 6, 7, 8 } },
        { "Blue",   new int[] { 1, 3, 4, 5, 6, 7, 9 } },
        { "Green",  new int[] { 0, 2, 4, 5, 7, 9, 0 } },
        { "Orange", new int[] { 1, 2, 4, 5, 6, 8, 9 } }
    };
    
    [System.Serializable]
    public class IPOData
    {
        public string color;
        public int ipoIndex = 0;
        public GameObject colorObject;
    }

    public class SellInput
    {
        public Dictionary<string, int> colorSellCounts = new Dictionary<string, int>
        {
            { "Red", 0 }, { "Blue", 0 }, { "Green", 0 }, { "Orange", 0 }
        };
    }
    
    // --- FUNGSI BARU UNTUK MEMPERBAIKI ERROR ---
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
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
    }

    public void StartSellingPhase(List<PlayerProfile> players, int resetCount, int maxResetCount, GameObject resetButton)
    {
        currentPlayers = players;
        currentResetCount = resetCount;
        currentMaxResetCount = maxResetCount;
        resetSemesterButton = resetButton;
        playerSellInputs.Clear();
        
        // Cari pemain lokal berdasarkan ActorNumber
        currentPlayer = players.FirstOrDefault(p => !p.isBot && p.actorNumber == PhotonNetwork.LocalPlayer.ActorNumber);

        UpdateIPOVisuals();

        if (currentPlayer != null)
        {
            SetupSellingUI(currentPlayer);
        }
        else
        {
            // Jika ini bukan pemain lokal atau pemain lokal adalah bot, maka MasterClient akan menunggu input dari pemain lain melalui RPC
            if (PhotonNetwork.IsMasterClient)
            {
                // Proses bot secara otomatis jika ada
                ProcessSellingPhase();
            }
        }
    }

    private void SetupSellingUI(PlayerProfile player)
    {
        sellingUI.SetActive(true);
        confirmSellButton.onClick.RemoveAllListeners();
        foreach (Transform child in colorSellPanelContainer) Destroy(child.gameObject);

        Dictionary<string, int> currentValues = new Dictionary<string, int>();
        Dictionary<string, int> maxValues = player.GetCardColorCounts();

        foreach (var color in ipoPriceMap.Keys)
        {
            GameObject row = Instantiate(colorSellRowPrefab, colorSellPanelContainer);
            row.transform.Find("ColorLabel").GetComponent<Text>().text = color;

            Text valueText = row.transform.Find("ValueText").GetComponent<Text>();
            Button plusButton = row.transform.Find("PlusButton").GetComponent<Button>();
            Button minusButton = row.transform.Find("MinusButton").GetComponent<Button>();

            int currentValue = 0;
            int maxValue = maxValues.ContainsKey(color) ? maxValues[color] : 0;
            currentValues[color] = currentValue;
            valueText.text = currentValue.ToString();

            plusButton.onClick.AddListener(() => {
                if (currentValues[color] < maxValue)
                {
                    currentValues[color]++;
                    valueText.text = currentValues[color].ToString();
                }
            });
            minusButton.onClick.AddListener(() => {
                if (currentValues[color] > 0)
                {
                    currentValues[color]--;
                    valueText.text = currentValues[color].ToString();
                }
            });
        }

        confirmSellButton.onClick.AddListener(() => {
            SellInput input = new SellInput();
            foreach (var color in ipoPriceMap.Keys)
            {
                input.colorSellCounts[color] = currentValues[color];
            }
            
            // Di multiplayer, ini harus mengirim RPC ke MasterClient dengan input penjualan
            // Contoh: photonView.RPC("Cmd_PlayerSells", RpcTarget.MasterClient, player.actorNumber, serializedInput);

            playerSellInputs[player.actorNumber] = input;
            sellingUI.SetActive(false);
        });
    }

    private void ProcessSellingPhase()
    {
        // Hanya MasterClient yang boleh memproses penjualan dan mengubah state game
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom) return;
        
        foreach (var player in currentPlayers)
        {
            // Logika untuk bot atau pemain yang tidak memberikan input
            if (!playerSellInputs.ContainsKey(player.actorNumber))
            {
                // Logika AI Bot Anda untuk menjual kartu
            }
            
            // ... (logika penjualan Anda) ...

            CallUpdatePlayerUI();
            Debug.Log($"{player.playerName} finpoint sekarang: {player.finpoint}");
        }

        if (RumorPhaseManager.Instance != null)
        {
            RumorPhaseManager.Instance.StartRumorPhase(currentPlayers);
        }
        
        Debug.Log("Fase penjualan selesai.");
    }
    
    public void HandleCrashMultiplier(IPOData data, PlayerProfile affectedPlayer)
    {
        // ... (logika crash multiplier Anda) ...
        CallUpdatePlayerUI();
    }

    private void UpdateIPOVisuals()
    {
        // ... (logika visual IPO Anda) ...
    }

    private int GetCurrentColorValue(string color)
    {
        IPOData data = ipoDataList.FirstOrDefault(d => d.color == color);
        if (data != null && ipoPriceMap.ContainsKey(color))
        {
            int index = data.ipoIndex;
            if (color == "Green") index = Mathf.Clamp(index, -2, 2);
            else index = Mathf.Clamp(index, -3, 3);
            return ipoPriceMap[color][index + 3];
        }
        return 0;
    }
    
    private void CallUpdatePlayerUI()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerUI();
        }
        else if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.UpdatePlayerUI();
        }
    }
}
