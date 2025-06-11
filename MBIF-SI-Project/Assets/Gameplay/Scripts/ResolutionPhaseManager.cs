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

        public GameObject indicatorObject; // Referensi ke indikator visual (bisa berupa text, arrow, UI element, dll)
    }



    public List<DividendData> dividendDataList = new List<DividendData>();
    public float dividendSpacing = 1.0f; // Atur sesuai jarak antar level dividen
    private Dictionary<string, Vector3> dividendInitialPositions = new Dictionary<string, Vector3>();


    // Mapping dari dividendIndex ke jumlah finpoint per kartu
    private Dictionary<int, int> dividendRewards = new Dictionary<int, int>()
    {
        { -3, 0 },
        { -2, 1 },
        { -1, 1 },
        { 0, 1 },
        { 1, 2 },
        { 2, 2 },
        { 3, 3 }
    };
    private void Start()
    {
        CacheDividendInitialPositions();
    }
    private void Update()
    {
        UpdateDividendVisuals();
    }

    private void CacheDividendInitialPositions()
    {
        dividendInitialPositions.Clear();
        foreach (var data in dividendDataList)
        {
            if (data.indicatorObject != null && !dividendInitialPositions.ContainsKey(data.color))
            {
                dividendInitialPositions[data.color] = data.indicatorObject.transform.position;
            }
        }
    }

    public void StartResolutionPhase(List<PlayerProfile> players)
    {
        UpdateDividendVisuals(); // Update posisi indikator

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
    private void UpdateDividendVisuals()
    {
        foreach (var data in dividendDataList)
        {
            if (data.indicatorObject != null && dividendInitialPositions.ContainsKey(data.color))
            {
                int clampedIndex = Mathf.Clamp(data.dividendIndex, -3, 3);
                Vector3 basePos = dividendInitialPositions[data.color];
                Vector3 offset = new Vector3(clampedIndex * dividendSpacing, 0, 0); // Ubah ke Y/Z jika diperlukan

                data.indicatorObject.transform.position = basePos + offset;

                Debug.Log($"[Dividend - {data.color}] Posisi awal: {basePos}, Index: {clampedIndex}, Posisi baru: {basePos + offset}");
            }
        }
    }

}
