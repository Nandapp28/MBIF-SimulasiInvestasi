using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class SellingPhaseManager : MonoBehaviour
{
    [Header("Game References")]
    public GameManager gameManager;
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


    private Dictionary<string, SellInput> playerSellInputs = new Dictionary<string, SellInput>();
    private PlayerProfile currentPlayer;
    private List<PlayerProfile> currentPlayers;
    private int currentResetCount;
    private int currentMaxResetCount;

    private Dictionary<string, int[]> ipoPriceMap = new Dictionary<string, int[]>
    {
        { "Red",    new int[] { 1, 3, 4, 5, 6, 8, 9 } },
        { "Blue",   new int[] { 1, 3, 4, 5, 6, 8, 9 } },
        { "Green",  new int[] { 1, 3, 4, 5, 6, 8, 9 } },
        { "Orange", new int[] { 1, 3, 4, 5, 6, 8, 9 } }
    };

    [System.Serializable]
    public class IPOData
    {
        public string color;
        public int ipoIndex = 0; // Range: -3 to 3
        public GameObject colorObject;
    }
    private int GetCurrentColorValue(string color)
    {
        IPOData data = ipoDataList.FirstOrDefault(d => d.color == color);
        if (data != null && ipoPriceMap.ContainsKey(color))
        {
            int index = data.ipoIndex;

            // Clamp khusus Green
            if (color == "Green")
                index = Mathf.Clamp(index, -2, 2);
            else
                index = Mathf.Clamp(index, -3, 3);

            int clampedIndex = index + 3; // convert -3..3 → 0..6
            return ipoPriceMap[color][clampedIndex];
        }
        return 0;
    }


    public class SellInput
    {
        public Dictionary<string, int> colorSellCounts = new Dictionary<string, int>
        {
            { "Red", 0 },
            { "Blue", 0 },
            { "Green", 0 },
            { "Orange", 0 }
        };
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
    private void Update()
    {
        foreach (var data in ipoDataList)
        {
           HandleCrashMultiplier(data);
        }

        UpdateIPOVisuals();
    }



    public void StartSellingPhase(List<PlayerProfile> players, int resetCount, int maxResetCount, GameObject resetButton)
    {
        currentPlayers = players;
        currentResetCount = resetCount;
        currentMaxResetCount = maxResetCount;
        resetSemesterButton = resetButton;

        playerSellInputs.Clear();
        currentPlayer = players.FirstOrDefault(p => p.playerName == "You");

        UpdateIPOVisuals();

        if (currentPlayer != null)
        {
            SetupSellingUI(currentPlayer);
        }
        else
        {
            ProcessSellingPhase();
        }
    }

    private void SetupSellingUI(PlayerProfile player)
    {
        sellingUI.SetActive(true);
        confirmSellButton.onClick.RemoveAllListeners();
        foreach (Transform child in colorSellPanelContainer) Destroy(child.gameObject);

        Dictionary<string, int> currentValues = new Dictionary<string, int>();
        Dictionary<string, int> maxValues = player.cards
            .GroupBy(c => c.color)
            .ToDictionary(g => g.Key, g => g.Count());

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

            plusButton.onClick.AddListener(() =>
            {
                if (currentValues[color] < maxValue)
                {
                    currentValues[color]++;
                    valueText.text = currentValues[color].ToString();
                }
            });

            minusButton.onClick.AddListener(() =>
            {
                if (currentValues[color] > 0)
                {
                    currentValues[color]--;
                    valueText.text = currentValues[color].ToString();
                }
            });
        }

        confirmSellButton.onClick.AddListener(() =>
        {
            SellInput input = new SellInput();
            foreach (var color in ipoPriceMap.Keys)
            {
                input.colorSellCounts[color] = currentValues[color];
            }

            playerSellInputs[currentPlayer.playerName] = input;
            sellingUI.SetActive(false);
            ProcessSellingPhase();
        });
    }

    private void ProcessSellingPhase()
    {


        foreach (var player in currentPlayers)
        {
            int earnedFinpoints = 0;
            List<Card> soldCards = new List<Card>();

            Dictionary<string, List<Card>> cardsByColor = player.cards
                .GroupBy(card => card.color)
                .ToDictionary(g => g.Key, g => g.ToList());

            Dictionary<string, int> sellCounts = new Dictionary<string, int>();

            if (player.playerName == "You")
            {
                if (playerSellInputs.TryGetValue(player.playerName, out SellInput input))
                {
                    sellCounts = input.colorSellCounts;
                }
                else
                {
                    Debug.LogWarning($"Tidak ada input penjualan untuk pemain: {player.playerName}");
                    continue;
                }
            }
            else
            {
                foreach (var color in ipoPriceMap.Keys)
                {
                    int countToSell = 0;
                    if (cardsByColor.ContainsKey(color))
                    {
                        List<Card> ownedCards = cardsByColor[color];

                        float sellChance = color switch
                        {
                            "Red" => 0.5f,
                            "Blue" => 0.5f,
                            "Green" => 0.4f,
                            "Orange" => 1f,
                            _ => 0.5f
                        };

                        foreach (var card in ownedCards)
                        {
                            if (Random.value < sellChance)
                                countToSell++;
                        }
                    }
                    sellCounts[color] = countToSell;
                }
            }

            foreach (var color in sellCounts.Keys)
            {
                int toSell = sellCounts[color];
                if (cardsByColor.ContainsKey(color))
                {
                    var availableCards = cardsByColor[color];
                    int actualSell = Mathf.Min(toSell, availableCards.Count);
                    int price = GetCurrentColorValue(color);
                    if (bonusMultiplierColors.Contains(color))
                    {
                        price *= 2;
                    }

                    earnedFinpoints += actualSell * price;
                    soldCards.AddRange(availableCards.Take(actualSell));
                }
            }

            player.finpoint += earnedFinpoints;
            foreach (var sold in soldCards)
            {
                player.cards.Remove(sold);
            }

            gameManager.UpdatePlayerUI();

            Debug.Log($"{player.playerName} menjual {soldCards.Count} kartu dan mendapatkan {earnedFinpoints} finpoints. Finpoint sekarang: {player.finpoint}");
        }

        if (resetSemesterButton != null)
        {
            resetSemesterButton.SetActive(currentResetCount < currentMaxResetCount);
            if (currentResetCount >= currentMaxResetCount)
            {
                gameManager.ShowLeaderboard();
                Debug.Log("Semester sudah berakhir");
            }
        }

        Debug.Log("Fase penjualan selesai.");
    }
    private void HandleCrashMultiplier(IPOData data)
{
    int index = data.ipoIndex;
    bool isGreen = data.color == "Green";
    int min = isGreen ? -2 : -3;
    int max = isGreen ? 2 : 3;

    if (index < min)
    {
        Debug.LogWarning($"[CRASH] {data.color} index terlalu rendah ({index}) — Market crash, semua kartu dijual otomatis.");
        data.ipoIndex = 0;

        foreach (var player in currentPlayers)
        {
            var cardsToSell = player.cards.Where(card => card.color == data.color).ToList();
            int cardCount = cardsToSell.Count;
            if (cardCount == 0) continue;

            int totalValue = 0;

            player.finpoint += totalValue;
            foreach (var c in cardsToSell)
                player.cards.Remove(c);

            gameManager.UpdatePlayerUI();
            Debug.Log($"[CRASH] {player.playerName} menjual {cardCount} kartu {data.color} & mendapat {totalValue} finpoints.");
        }
    }
    else if (index > max)
    {
        Debug.LogWarning($"[MULTIPLIER] {data.color} index terlalu tinggi ({index}) — index direset ke 0, harga jual {data.color} dikali 2.");
        data.ipoIndex = 0;

        // Flag bonus multiplier saat jual (digunakan di ProcessSellingPhase)
        bonusMultiplierColors.Add(data.color);
    }
}




    private void UpdateIPOVisuals()
    {
        foreach (var data in ipoDataList)
        {
            if (data.colorObject != null && initialPositions.ContainsKey(data.color))
            {
                int clampedIndex = data.ipoIndex;

                // Clamp khusus untuk Green
                if (data.color == "Green")
                    clampedIndex = Mathf.Clamp(clampedIndex, -2, 2);
                else
                    clampedIndex = Mathf.Clamp(clampedIndex, -3, 3);

                Vector3 basePos = initialPositions[data.color];
                Vector3 offset = new Vector3(clampedIndex * ipoSpacing, 0, 0); // Atau ubah ke .z kalau pakai sumbu Z
                data.colorObject.transform.position = basePos + offset;

                Debug.Log($"[{data.color}] Posisi awal: {basePos}, Index: {clampedIndex}, Posisi baru: {basePos + offset}");
            }
        }
    }



}
