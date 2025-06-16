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
        public int dividendIndex;
        public List<int> ramalanTokens = new List<int>(); // Nilai token: -2, -1, +1, +2
        public int revealedTokenCount = 0; // Berapa token yang sudah dibalik // Rentang: -3 hingga 3

        public GameObject indicatorObject; // Referensi ke indikator visual (bisa berupa text, arrow, UI element, dll)
        public List<GameObject> tokenObjects;
    }
    public Material tokenMinus2Material;
public Material tokenMinus1Material;
public Material tokenPlus1Material;
public Material tokenPlus2Material;




    public List<DividendData> dividendDataList = new List<DividendData>();

    public float dividendSpacing = 0.5f; // Atur sesuai jarak antar level dividen
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
        InitializeRamalanTokens();
    }
    private void InitializeRamalanTokens()
{
    int[] possibleTokens = { -2, -1, 1, 2 };

    foreach (var data in dividendDataList)
    {
        data.ramalanTokens = new List<int>();
        data.revealedTokenCount = 0;

        // Random token values
        for (int i = 0; i < 4; i++)
        {
            int token = possibleTokens[Random.Range(0, possibleTokens.Length)];
            data.ramalanTokens.Add(token);
        }

        Debug.Log($"[Init Ramalan] {data.color} tokens: {string.Join(", ", data.ramalanTokens)}");

        // Set material for each token object
        for (int i = 0; i < data.tokenObjects.Count && i < data.ramalanTokens.Count; i++)
        {
            int tokenValue = data.ramalanTokens[i];
            Renderer rend = data.tokenObjects[i].GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = GetTokenMaterial(tokenValue);
                data.tokenObjects[i].SetActive(true); // Hide before reveal
            }
        }
    }
}


    private void Update()
    {
    }
    private Material GetTokenMaterial(int value)
    {
        switch (value)
        {
            case -2: return tokenMinus2Material;
            case -1: return tokenMinus1Material;
            case 1: return tokenPlus1Material;
            case 2: return tokenPlus2Material;
            default: return null;
        }
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
    Debug.Log("[ResolutionPhase] Memulai fase resolusi...");

    foreach (var data in dividendDataList)
    {
        ApplyRamalanEffect(data); // Balik 1 token (secara logika & visual)
    }

    UpdateDividendVisuals(); // Update indikator

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
    gameManager.ResetSemesterButton();
}

    private void ApplyRamalanEffect(DividendData data)
{
    if (data.revealedTokenCount < data.ramalanTokens.Count)
    {
        int index = data.revealedTokenCount;
        int tokenEffect = data.ramalanTokens[index];
        data.dividendIndex += tokenEffect;
        data.dividendIndex = Mathf.Clamp(data.dividendIndex, -3, 3);

        Debug.Log($"[Ramalan - {data.color}] Token #{index + 1}: {tokenEffect}, Index baru: {data.dividendIndex}");

        // Tampilkan token yang dibalik
        if (index < data.tokenObjects.Count)
        {
            GameObject tokenObj = data.tokenObjects[index];
            tokenObj.SetActive(true);

            // Opsional: animasi flip
            StartCoroutine(AnimateTokenFlip(tokenObj));
        }

        data.revealedTokenCount++;
    }
    else
    {
        Debug.LogWarning($"[Ramalan - {data.color}] Semua token telah dibalik.");
    }
}
    
private IEnumerator<WaitForSeconds> AnimateTokenFlip(GameObject token)
{
    float duration = 0.3f;
    float time = 0f;
    Quaternion startRot = token.transform.rotation;
    Quaternion endRot = startRot * Quaternion.Euler(0, 0, 180f);

    while (time < duration)
    {
        time += Time.deltaTime;
        float t = time / duration;
        token.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
        yield return null;
    }

    token.transform.rotation = endRot;
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
