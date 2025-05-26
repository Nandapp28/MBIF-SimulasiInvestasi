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
    public GameObject colorSellRowPrefab; // Prefab: satu baris warna, slider, text

    private Dictionary<string, int> colorSellValues = new Dictionary<string, int>
    {
        { "Red", 2 },
        { "Blue", 4 },
        { "Green", 5 },
        { "Orange", 6 }
    };

    private Dictionary<string, SellInput> playerSellInputs = new Dictionary<string, SellInput>();
    private PlayerProfile currentPlayer;
    private List<PlayerProfile> currentPlayers;
    private int currentResetCount;
    private int currentMaxResetCount;

    // Store UI sliders for easy access
    private Dictionary<string, Slider> colorSliders = new Dictionary<string, Slider>();
    private Dictionary<string, Text> sliderValueTexts = new Dictionary<string, Text>();

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

    public void StartSellingPhase(List<PlayerProfile> players, int resetCount, int maxResetCount, GameObject resetButton)
    {
        currentPlayers = players;
        currentResetCount = resetCount;
        currentMaxResetCount = maxResetCount;
        resetSemesterButton = resetButton;

        playerSellInputs.Clear();

        // Ambil pemain manusia pertama (misal game 1p)
        currentPlayer = players.FirstOrDefault(p => p.playerName == "You");



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

        colorSliders.Clear();
        sliderValueTexts.Clear();

        var groupedCards = player.cards.GroupBy(c => c.color).ToDictionary(g => g.Key, g => g.Count());

        foreach (var color in colorSellValues.Keys)
        {
            GameObject row = Instantiate(colorSellRowPrefab, colorSellPanelContainer);
            row.transform.Find("ColorLabel").GetComponent<Text>().text = $"{color}";
            Slider slider = row.transform.Find("SellSlider").GetComponent<Slider>();
            Text valueText = row.transform.Find("ValueText").GetComponent<Text>();

            int maxCards = groupedCards.ContainsKey(color) ? groupedCards[color] : 0;
            slider.maxValue = maxCards;
            slider.value = 0;
            valueText.text = "0";

            colorSliders[color] = slider;
            sliderValueTexts[color] = valueText;

            slider.onValueChanged.AddListener((val) =>
            {
                valueText.text = ((int)val).ToString();
            });
        }

        confirmSellButton.onClick.AddListener(() =>
{
    SellInput input = new SellInput();
    foreach (var color in colorSellValues.Keys)
    {
        input.colorSellCounts[color] = (int)colorSliders[color].value;
    }

    playerSellInputs[currentPlayer.playerName] = input;

    sellingUI.SetActive(false);

    // 🔥 Tambahkan ini

    // 🔥 Baru jalankan proses seluruh pemain
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
                // Bot: jual acak berdasarkan peluang
                foreach (var color in colorSellValues.Keys)
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

                        // Uji peluang untuk setiap kartu yang dimiliki
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
                    earnedFinpoints += actualSell * colorSellValues[color];
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
            if (currentResetCount < currentMaxResetCount)
            {
                resetSemesterButton.SetActive(true);
            }
            else
            {
                resetSemesterButton.SetActive(false);
                gameManager.ShowLeaderboard();
                Debug.Log("Semester sudah berakhir");
            }
        }

        Debug.Log("Fase penjualan selesai.");
    }

}
