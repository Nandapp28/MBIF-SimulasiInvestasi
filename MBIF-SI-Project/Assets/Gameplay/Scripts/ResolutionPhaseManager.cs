using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class ResolutionPhaseManager : MonoBehaviour
{
    public GameManager gameManager;
    public SellingPhaseManager sellingPhaseManager;
    public HelpCardPhaseManager helpCardPhaseManager;
  
    [Header("System References")]
    public CameraController cameraController; // <-- TAMBAHKAN INI



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
    private List<string> resolutionOrder = new List<string> { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };

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
        { 2, 3 },
        { 3, 3 }
    };
    private void Start()
    {
        CacheDividendInitialPositions();
        InitializeRamalanTokens();
        UpdateDividendVisuals();
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
    public IEnumerator RevealNextTokenForAllColors()
    {
        Debug.Log("--- FASE RAMALAN: Membalik token berikutnya untuk semua sektor (Visual Saja). ---");
        yield return new WaitForSeconds(1.5f);

        foreach (string color in resolutionOrder)
        {
            var data = dividendDataList.FirstOrDefault(d => d.color == color);
            yield return StartCoroutine(RevealNextToken(data)); // Memanggil coroutine baru
            yield return new WaitForSeconds(1f); // Jeda antar warna
        }
    }
    public IEnumerator RevealNextToken(DividendData data)
    {
        if (data != null && data.revealedTokenCount < data.tokenObjects.Count)
        {
            int index = data.revealedTokenCount;
            GameObject tokenObj = data.tokenObjects[index];

            Debug.Log($"[Visual] Mengungkap token #{index + 1} untuk {data.color}...");

            // Set material (seharusnya sudah di-set saat Inisialisasi, tapi aman untuk set lagi)
            Renderer rend = tokenObj.GetComponent<Renderer>();
            if (rend != null) rend.material = GetTokenMaterial(data.ramalanTokens[index]);

            // Aktifkan dan animasikan
            tokenObj.SetActive(true);
            yield return StartCoroutine(AnimateTokenFlip(tokenObj));
        }
    }

    public void StartResolutionPhase(List<PlayerProfile> players)
    {
        StartCoroutine(ResolutionSequence(players));
    }
    private IEnumerator ResolutionSequence(List<PlayerProfile> players)
    {
        Debug.Log("[ResolutionPhase] Memulai fase resolusi secara berurutan...");
        yield return new WaitForSeconds(2.0f);

        // Loop utama per sektor
        foreach (string color in resolutionOrder)
        {
            // --- 1. Gerakkan Kamera ke Sektor yang Sesuai ---
            if (cameraController != null)
            {
                // Tentukan posisi kamera berdasarkan nama warna
                CameraController.CameraPosition targetPos = CameraController.CameraPosition.Normal;
                switch (color)
                {
                    case "Konsumer": targetPos = CameraController.CameraPosition.Konsumer; break;
                    case "Infrastruktur": targetPos = CameraController.CameraPosition.Infrastruktur; break;
                    case "Keuangan": targetPos = CameraController.CameraPosition.Keuangan; break;
                    case "Tambang": targetPos = CameraController.CameraPosition.Tambang; break;
                }
                Debug.Log($"[Camera] Bergerak ke view {color}...");
                yield return cameraController.MoveTo(targetPos); // Tunggu kamera sampai
                yield return new WaitForSeconds(0.5f); // Jeda sejenak agar pemain bisa lihat
            }

            var data = dividendDataList.FirstOrDefault(d => d.color == color);
            if (data != null)
            {
                // --- 2. Terapkan Efek Token Ramalan ---
                ApplyRamalanEffect(data);
                UpdateDividendVisuals(); // Perbarui posisi indikator dividen
                yield return new WaitForSeconds(0.5f); // Jeda untuk melihat pergerakan marker

                // Cek apakah ada efek Boom/Crash ke IPO
                sellingPhaseManager.UpdateIPOVisuals();
                yield return new WaitForSeconds(0.5f);

                // --- 3. Hitung dan Bagikan Dividen untuk Sektor Ini (PINDAH KE SINI) ---
                int dividendIndex = Mathf.Clamp(data.dividendIndex, -3, 3);
                int reward = dividendRewards[dividendIndex];

                Debug.Log($"--- Resolusi Dividen untuk {color} (Index: {dividendIndex}, Reward/kartu: {reward} FP) ---");

                if (reward > 0)
                {
                    foreach (var player in players)
                    {
                        int cardCount = player.cards.Count(c => c.color == color);
                        if (cardCount > 0)
                        {
                            int totalReward = cardCount * reward;
                            player.finpoint += totalReward;
                            Debug.Log($"-> {player.playerName} mendapat {totalReward} finpoint dari {cardCount} kartu {color}.");
                            gameManager.UpdatePlayerUI(); // Update UI langsung agar terlihat
                            yield return new WaitForSeconds(0.5f); // Jeda singkat per pemain
                        }
                    }
                }
                else
                {
                    Debug.Log($"-> Tidak ada dividen yang dibagikan untuk {color}.");
                }

                yield return new WaitForSeconds(1f); // Jeda sebelum pindah ke sektor berikutnya
            }
        }

        // --- 4. Proses Akhir Setelah Semua Sektor Selesai ---
        if (cameraController != null)
        {
            Debug.Log("[Camera] Kembali ke view normal...");
            yield return cameraController.MoveTo(CameraController.CameraPosition.Normal); // Kembalikan kamera
        }

        ApplyNegativeFinpointPenalty(players);
        gameManager.UpdatePlayerUI(); // Final UI update

        yield return new WaitForSeconds(2f);

        if (gameManager.resetCount == 0)
        {
            helpCardPhaseManager.DistributeHelpCards(players);
        }

        gameManager.ResetSemesterButton();
    }
    private void ApplyRamalanEffect(DividendData data)
    {
        if (data.revealedTokenCount < data.ramalanTokens.Count)
        {
            int index = data.revealedTokenCount;
            int tokenEffect = data.ramalanTokens[index];
            data.dividendIndex += tokenEffect;

            Debug.Log($"[Ramalan - {data.color}] Efek token #{index + 1} diterapkan: {tokenEffect}. Index dividen sementara: {data.dividendIndex}");

            // Cek overflow/underflow sebelum clamp
            if (data.dividendIndex < -3)
            {
                Debug.LogWarning($"[Dividen Crash] {data.color} terlalu rendah (index: {data.dividendIndex}). Mengurangi IPO index.");
                ModifyIPOIndex(data.color, -1);
                data.dividendIndex = 0; // Reset dividend index
            }
            else if (data.dividendIndex > 3)
            {
                Debug.LogWarning($"[Dividen Boom] {data.color} terlalu tinggi (index: {data.dividendIndex}). Menambah IPO index.");
                ModifyIPOIndex(data.color, 1);
                data.dividendIndex = 0; // Reset dividend index
            }
            else
            {
                // Jika tidak boom/crash, clamp nilainya seperti biasa
                data.dividendIndex = Mathf.Clamp(data.dividendIndex, -3, 3);
            }

            // Naikkan counter HANYA setelah efek diterapkan
            data.revealedTokenCount++;
        }
        else
        {
            Debug.LogWarning($"[Ramalan - {data.color}] Semua token telah diterapkan efeknya.");
        }
    }
    private void ModifyIPOIndex(string color, int delta)
    {
        var ipo = sellingPhaseManager.ipoDataList.FirstOrDefault(i => i.color == color);
        if (ipo != null)
        {
            ipo.ipoIndex += delta;
            Debug.Log($"[Modify IPO] {color} IPO index diubah menjadi {ipo.ipoIndex}");
        }
        else
        {
            Debug.LogWarning($"[Modify IPO] Tidak ditemukan IPOData untuk warna: {color}");
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
    private void ApplyNegativeFinpointPenalty(List<PlayerProfile> players)
{
    Debug.Log("[ResolutionPhase] Mengecek pemain dengan finpoint negatif untuk penalti...");
    foreach (var player in players)
    {
        // Jika finpoint pemain menjadi negatif setelah pembagian dividen
        if (player.finpoint < 0)
        {
            player.finpoint -= 1; // Kurangi 1 finpoint lagi
            Debug.LogWarning($"[PENALTI] {player.playerName} memiliki finpoint negatif. Dikenakan penalti -1 FP. Finpoint baru: {player.finpoint}");
        }
    }
}

}
