using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ResolutionPhaseManager : MonoBehaviour
{
    public GameManager gameManager;
    public SellingPhaseManager sellingPhaseManager;

    // Dividend Index disimpan terpisah dari IPO
    [System.Serializable]
    public class DividendData
    {
        public string color;
        public int dividendIndex; // Rentang: -3 hingga 3
    }

    public List<DividendData> dividendDataList = new List<DividendData>();

    // Mapping dari dividendIndex ke jumlah finpoint per kartu
    private Dictionary<int, int> dividendRewards = new Dictionary<int, int>()
    {
        { -3, 0 },
        { -2, 0 },
        { -1, 0 },
        { 0, 1 },
        { 1, 1 },
        { 2, 2 },
        { 3, 3 }
    };

    public void StartResolutionPhase(List<PlayerProfile> players)
    {
        Debug.Log("[ResolutionPhase] Memulai fase resolusi...");

        foreach (var data in dividendDataList)
        {
            string color = data.color;
            int dividendIndex = Mathf.Clamp(data.dividendIndex, -3, 3);
            int reward = dividendRewards[dividendIndex];

            if (reward == 0)
            {
                Debug.Log($"[ResolutionPhase] Tidak ada dividen untuk {color} (dividend index: {dividendIndex})");
                continue;
            }

            foreach (var player in players)
            {
                int cardCount = player.cards.Count(c => c.color == color);
                if (cardCount > 0)
                {
                    int totalReward = cardCount * reward;
                    player.finpoint += totalReward;
                    Debug.Log($"{player.playerName} mendapat {totalReward} finpoint dari {cardCount} kartu {color} (dividend index {dividendIndex})");
                }
            }
        }

        gameManager.UpdatePlayerUI();
        gameManager.ResetSemesterButton(); // Tampilkan tombol lanjut
    }
}
