using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
public enum IPOState { Normal, Ascend, Advanced }
public class SellingPhaseManager : MonoBehaviour
{
    [Header("Game References")]
    public GameManager gameManager;
    public RumorPhaseManager rumorPhaseManager;
    public GameObject resetSemesterButton;

    [Header("UI Elements")]
    public GameObject sellingUI;
    public Button confirmSellButton;
    public Transform colorSellPanelContainer;
    public GameObject colorSellRowPrefab;
    [Header("System References")] // <-- TAMBAHKAN HEADER BARU
    public CameraController cameraController;
    [Header("Sound Effects")] // <-- TAMBAHKAN HEADER & VARIABEL INI
    public AudioClip ipoMoveSound;
    public AudioClip sellSound, ipoStateDown, ipoStateUp;

    [Header("IPO Settings")]
    public List<IPOData> ipoDataList = new List<IPOData>();
    public float ipoSpacing = 0.5f;
    private Dictionary<string, Vector3> initialPositions = new Dictionary<string, Vector3>();



    private Dictionary<string, SellInput> playerSellInputs = new Dictionary<string, SellInput>();
    private PlayerProfile currentPlayer;
    private List<PlayerProfile> currentPlayers;
    private int currentResetCount;
    private int currentMaxResetCount;


    public Dictionary<string, int[]> ipoPriceMap = new Dictionary<string, int[]>
    {
        { "Konsumer", new int[] { 1, 2, 3, 5, 6, 7, 8 } },
        { "Infrastruktur", new int[] { 1, 2, 4, 5, 6, 7, 9 } },
        { "Keuangan", new int[] { 1, 3, 4, 5, 6, 7, 9 } },
        { "Tambang",  new int[] { 0, 2, 4, 5, 7, 9, 0 } },
    };

    [System.Serializable]
    public class IPOData
    {
        public string color;
        public int _ipoIndex = 0; // Range: -3 to 3
        public GameObject colorObject;
        [System.NonSerialized] public SellingPhaseManager manager;
        public IPOState currentState = IPOState.Normal;
        public int salesBonus = 0;
        [Header("Visual Indicators")]
        public GameObject ascendVisualIndicator; // Visual untuk state Ascend
        public GameObject advancedVisualIndicator; // Visual untuk state Advanced

        public int ipoIndex
        {
            get => _ipoIndex;
            set
            {
                _ipoIndex = value;
                if (manager != null)
                    manager.UpdateIPOState(this);
            }
        }
    }
    public int GetCurrentColorValue(string color)
    {
        IPOData data = ipoDataList.FirstOrDefault(d => d.color == color);
        if (data != null && ipoPriceMap.ContainsKey(color))
        {
            int index = data.ipoIndex;

            // Clamp khusus Orange
            if (color == "Tambang")
                index = Mathf.Clamp(index, -2, 2);
            else
                index = Mathf.Clamp(index, -3, 3);

            int clampedIndex = index + 3; // convert -3..3 → 0..6
            return ipoPriceMap[color][clampedIndex];
        }
        return 0;
    }
    public int GetFullCardPrice(string color)
    {
        int basePrice = GetCurrentColorValue(color);
        IPOData data = ipoDataList.FirstOrDefault(d => d.color == color);
        if (data != null)
        {
            return basePrice + data.salesBonus; // Harga dasar + bonus dari status
        }
        return basePrice;
    }


    public class SellInput
    {
        public Dictionary<string, int> colorSellCounts = new Dictionary<string, int>
        {
            { "Konsumer", 0 },
            { "Infrastruktur", 0 },
            { "Keuangan", 0 },
            { "Tambang", 0 }
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
            data.manager = this; // INJEKSI Referensi ke manager
        }
        UpdateIPOVisuals();

    }

    private void Update()
    {



    }
    public void InitializePlayers(List<PlayerProfile> players)
    {
        currentPlayers = players;
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
            int pricePerCard = GetFullCardPrice(color);
            Text priceLabel = row.transform.Find("PriceLabel").GetComponent<Text>();
            priceLabel.text = $"{pricePerCard}";

            Text valueText = row.transform.Find("ValueText").GetComponent<Text>();
            Button plusButton = row.transform.Find("PlusButton").GetComponent<Button>();
            Button minusButton = row.transform.Find("MinusButton").GetComponent<Button>();

            int currentValue = 0;
            int maxValue = maxValues.ContainsKey(color) ? maxValues[color] : 0;
            currentValues[color] = currentValue;
            valueText.text = currentValue.ToString();

            if (maxValue == 0)
            {
                plusButton.interactable = false;
                minusButton.interactable = false;
            }
            else
            {
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
        }

        confirmSellButton.onClick.AddListener(() =>
        {
            SellInput input = new SellInput();
            foreach (var color in ipoPriceMap.Keys)
            {
                input.colorSellCounts[color] = currentValues[color];
            }
            if (SfxManager.Instance != null && sellSound != null)
            {
                SfxManager.Instance.PlaySound(sellSound);
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
            else // Logika untuk Bot
            {
                foreach (var color in ipoPriceMap.Keys)
                {
                    int countToSell = 0;
                    if (cardsByColor.ContainsKey(color))
                    {
                        List<Card> ownedCards = cardsByColor[color];

                        // <-- LOGIKA BARU DIMULAI DI SINI -->
                        bool hasPrediction = player.marketPredictions.TryGetValue(color, out MarketPredictionType prediction);

                        if (hasPrediction)
                        {
                            if (prediction == MarketPredictionType.Rise)
                            {
                                // Pasar akan NAIK, jangan jual!
                                countToSell = 0;
                                Debug.Log($"[Prediksi Bot] {player.playerName} tidak menjual {color} karena pasar akan naik.");
                            }
                            else // prediction == MarketPredictionType.Fall
                            {
                                // Pasar akan TURUN, 90% jual semua!
                                if (Random.value < 0.9f)
                                {
                                    countToSell = ownedCards.Count;
                                    Debug.Log($"[Prediksi Bot] {player.playerName} menjual semua ({countToSell}) {color} karena pasar akan turun.");
                                }
                            }
                        }
                        else
                        {
                            // <-- LOGIKA LAMA (JIKA TIDAK ADA PREDIKSI) -->
                            float sellChance = color switch
                            {
                                "Konsumer" => 0.5f,
                                "Infrastruktur" => 0.5f,
                                "Keuangan" => 0.5f,
                                "Tambang" => 0.5f,
                                _ => 0.5f
                            };

                            foreach (var card in ownedCards)
                            {
                                if (Random.value < sellChance)
                                    countToSell++;
                            }
                        }
                        // <-- LOGIKA BARU BERAKHIR DI SINI -->
                    }
                    sellCounts[color] = countToSell;
                }
            }

            foreach (var color in sellCounts.Keys)
            {
                int toSell = sellCounts[color];
                if (cardsByColor.ContainsKey(color))
                {
                    IPOData data = ipoDataList.FirstOrDefault(d => d.color == color); // Ambil data IPO
                    if (data == null) continue;
                    var availableCards = cardsByColor[color];
                    int actualSell = Mathf.Min(toSell, availableCards.Count);
                    int price = GetCurrentColorValue(color);
                    price += data.salesBonus;

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
            player.marketPredictions.Clear();
        }

        rumorPhaseManager.StartRumorPhase(currentPlayers);

        Debug.Log("Fase penjualan selesai.");
    }
    public void ForceSellAllCards(List<PlayerProfile> players)
    {
        Debug.Log("💰 Menjual semua sisa kartu pemain untuk skor akhir...");

        foreach (var player in players)
        {
            int earnedFinpoints = 0;

            // Buat salinan daftar kartu untuk diiterasi, karena kita akan memodifikasi daftar aslinya
            List<Card> cardsToSell = new List<Card>(player.cards);

            foreach (var card in cardsToSell)
            {
                // Dapatkan harga penuh (harga dasar + bonus state) untuk warna kartu
                int price = GetFullCardPrice(card.color);
                earnedFinpoints += price;
            }

            if (earnedFinpoints > 0)
            {
                Debug.Log($"[Penjualan Akhir] {player.playerName} mendapatkan {earnedFinpoints} Finpoint dari {cardsToSell.Count} kartu.");
                player.finpoint += earnedFinpoints;
            }

            // Hapus semua kartu dari profil pemain
            player.cards.Clear();
        }

        // Perbarui UI pemain untuk terakhir kalinya jika diperlukan
        gameManager.UpdatePlayerUI();
    }
    public void UpdateIPOState(IPOData data)
    {
        bool stateHasChanged;
        do
        {
            stateHasChanged = false; // Asumsikan tidak ada perubahan di awal loop
            int currentIndex = data._ipoIndex;
            bool isOrange = data.color == "Tambang";
            int minThreshold = isOrange ? -2 : -3;
            int maxThreshold = isOrange ? 2 : 3;

            switch (data.currentState)
            {
                case IPOState.Normal:
                    if (currentIndex > maxThreshold)
                    {
                        // Hitung kelebihan nilai
                        int excess = currentIndex - (maxThreshold + 1);
                         NotificationManager.Instance.ShowNotification($"[ASCEND] Harga Saham dari Sektor {data.color} Meningkat!!!", 3f);
                        Debug.Log($"[STATE CHANGE] {data.color}: Normal ➡ Ascend. Menyimpan kelebihan nilai: {excess}");

                        // Ubah status dan bonus
                        data.currentState = IPOState.Ascend;
                        data.salesBonus = 5;
                        if (SfxManager.Instance != null && ipoStateUp != null)
                        {
                            SfxManager.Instance.PlaySound(ipoStateUp);
                        }

                        // Atur ulang index ke 0 dan tambahkan kelebihannya
                        data._ipoIndex = 0 + excess;
                        stateHasChanged = true; // Tandai bahwa perubahan terjadi untuk loop selanjutnya
                    }
                    else if (currentIndex < minThreshold)
                    {
                        // Logika Crash tetap sama, tidak perlu loop
                        NotificationManager.Instance.ShowNotification($"[CRASH] Semua Saham dari Sektor {data.color} dikembalikan ke Bank!!!", 3f);
                        Debug.LogWarning($"[CRASH] {data.color} market crash! Saham dikembalikan ke bank.");
                        data._ipoIndex = 0;
                        data.salesBonus = 0;
                        if (SfxManager.Instance != null && ipoStateDown != null)
                        {
                            SfxManager.Instance.PlaySound(ipoStateDown);
                        }

                        foreach (var player in currentPlayers)
                        {
                            var cardsToSell = player.cards.Where(card => card.color == data.color).ToList();
                            if (cardsToSell.Count > 0)
                            {
                                foreach (var c in cardsToSell) player.cards.Remove(c);
                                Debug.Log($"[CRASH] {player.playerName} kehilangan {cardsToSell.Count} saham {data.color}.");
                            }
                        }
                        gameManager.UpdatePlayerUI();
                    }
                    break;

                case IPOState.Ascend:
                    if (currentIndex > 0) // Ambang batas atas untuk Ascend adalah 0
                    {
                        int excess = currentIndex - 1;
                        NotificationManager.Instance.ShowNotification($"[ADVANCED] Harga Saham dari Sektor {data.color} Meningkat!!!", 3f);
                        Debug.Log($"[STATE CHANGE] {data.color}: Ascend ➡ Advanced. Menyimpan kelebihan nilai: {excess}");

                        data.currentState = IPOState.Advanced;
                        data.salesBonus = 10;
                        if (SfxManager.Instance != null && ipoStateUp != null)
                        {
                            SfxManager.Instance.PlaySound(ipoStateUp);
                        }

                        // Atur ulang index ke nilai MINIMUM dari state baru dan tambahkan kelebihannya
                        data._ipoIndex = minThreshold + excess;
                        stateHasChanged = true;
                    }
                    else if (currentIndex < 0) // Ambang batas bawah untuk Ascend adalah 0
                    {
                        int excess = currentIndex + 1; // Seluruh nilai adalah kelebihan negatif
                        NotificationManager.Instance.ShowNotification($"[DESCEND] Harga Saham dari Sektor {data.color} Menurun", 3f);
                        Debug.Log($"[STATE CHANGE] {data.color}: Ascend ➡ Normal. Menyimpan kelebihan nilai: {excess}");

                        data.currentState = IPOState.Normal;
                        data.salesBonus = 0;
                        if (SfxManager.Instance != null && ipoStateDown != null)
                        {
                            SfxManager.Instance.PlaySound(ipoStateDown);
                        }

                        // Atur ulang index ke 0 dan tambahkan kelebihannya (yang bernilai negatif)
                        data._ipoIndex = maxThreshold + excess;
                        stateHasChanged = true;
                    }
                    break;

                case IPOState.Advanced:
                    if (currentIndex < minThreshold)
                    {
                        int excess = currentIndex - (minThreshold - 1);
                        NotificationManager.Instance.ShowNotification($"[DESCEND] Harga Saham dari Sektor {data.color} Kembali ke Normal", 3f);
                        Debug.Log($"[STATE CHANGE] {data.color}: Advanced ➡ Ascend. Menyimpan kelebihan nilai: {excess}");

                        data.currentState = IPOState.Ascend;
                        data.salesBonus = 5;
                        if (SfxManager.Instance != null && ipoStateDown != null)
                        {
                            SfxManager.Instance.PlaySound(ipoStateDown);
                        }

                        // Atur ulang index ke 0 dan tambahkan kelebihannya
                        data._ipoIndex = 0 + excess;
                        stateHasChanged = true;
                    }
                    break;
            }

            // Jika state berubah, loop akan berjalan lagi untuk memeriksa apakah
            // index yang baru (setelah ditambah `excess`) menyebabkan perubahan state lagi.
        } while (stateHasChanged);
        UpdateVisualsForState(data);
    }
    // Tambahkan method ini di dalam kelas SellingPhaseManager
    public IEnumerator ShowMultiColorSellUI(PlayerProfile player, System.Action<Dictionary<string, int>> onConfirm)
    {
        sellingUI.SetActive(true);
        confirmSellButton.onClick.RemoveAllListeners();
        foreach (Transform child in colorSellPanelContainer) Destroy(child.gameObject);

        Dictionary<string, int> currentValues = new Dictionary<string, int>();
        var cardsByColor = player.cards.GroupBy(c => c.color).ToDictionary(g => g.Key, g => g.Count());

        // --- PERUBAHAN DIMULAI DI SINI ---
        // Selalu iterasi melalui semua 4 warna utama dari ipoPriceMap
        foreach (var color in ipoPriceMap.Keys)
        {
            int maxValue = cardsByColor.ContainsKey(color) ? cardsByColor[color] : 0;

            // Buat baris UI untuk setiap warna
            GameObject row = Instantiate(colorSellRowPrefab, colorSellPanelContainer);
            row.transform.Find("ColorLabel").GetComponent<Text>().text = color;
            int pricePerCard = GetFullCardPrice(color);
            row.transform.Find("PriceLabel").GetComponent<Text>().text = $"{pricePerCard}";

            Text valueText = row.transform.Find("ValueText").GetComponent<Text>();
            Button plusButton = row.transform.Find("PlusButton").GetComponent<Button>();
            Button minusButton = row.transform.Find("MinusButton").GetComponent<Button>();

            currentValues[color] = 0; // Mulai dari 0 untuk semua warna
            valueText.text = "0";

            // Nonaktifkan tombol jika pemain tidak punya kartu warna ini
            if (maxValue == 0)
            {
                plusButton.interactable = false;
                minusButton.interactable = false;
            }
            else
            {
                // Gunakan variabel lokal 'currentColor' untuk menghindari masalah closure
                string currentColor = color;

                plusButton.onClick.AddListener(() =>
                {
                    if (currentValues[currentColor] < maxValue)
                    {
                        currentValues[currentColor]++;
                        valueText.text = currentValues[currentColor].ToString();
                    }
                });

                minusButton.onClick.AddListener(() =>
                {
                    if (currentValues[currentColor] > 0)
                    {
                        currentValues[currentColor]--;
                        valueText.text = currentValues[currentColor].ToString();
                    }
                });
            }
        }
        // --- PERUBAHAN SELESAI ---

        confirmSellButton.onClick.AddListener(() =>
        {
            if (SfxManager.Instance != null && sellSound != null)
            {
                SfxManager.Instance.PlaySound(sellSound);
            }

            sellingUI.SetActive(false);
            onConfirm?.Invoke(currentValues);
        });

        // Tunggu sampai UI ditutup
        while (sellingUI.activeSelf)
        {
            yield return null;
        }
    }


    public void UpdateIPOVisuals()
    {
        foreach (var data in ipoDataList)
        {
            if (data.colorObject != null && initialPositions.ContainsKey(data.color))
            {
                int clampedIndex = data.ipoIndex;

                // Clamp khusus untuk Orange
                if (data.color == "Tambang")
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

    public void UpdateVisualsForState(IPOData data)
    {
        // Pastikan kedua visual indicator telah di-assign di Inspector
        if (data.ascendVisualIndicator == null || data.advancedVisualIndicator == null)
        {
            // Anda bisa menambahkan Debug.LogWarning di sini jika mau
            return;
        }

        // Atur visibilitas berdasarkan state saat ini
        switch (data.currentState)
        {
            case IPOState.Ascend:
                data.ascendVisualIndicator.SetActive(true);
                data.advancedVisualIndicator.SetActive(false);
                break;

            case IPOState.Advanced:
                data.ascendVisualIndicator.SetActive(false);
                data.advancedVisualIndicator.SetActive(true);
                break;

            // Untuk state Normal atau state lainnya, nonaktifkan keduanya
            case IPOState.Normal:
            default:
                data.ascendVisualIndicator.SetActive(false);
                data.advancedVisualIndicator.SetActive(false);
                break;
        }
    }
    public IEnumerator ModifyIPOIndexWithCamera(string color, int delta)
    {
        if (cameraController != null)
        {
            // 1. Tentukan target posisi kamera
            CameraController.CameraPosition targetPos = CameraController.CameraPosition.Normal;
            switch (color)
            {
                case "Konsumer": targetPos = CameraController.CameraPosition.Konsumer; break;
                case "Infrastruktur": targetPos = CameraController.CameraPosition.Infrastruktur; break;
                case "Keuangan": targetPos = CameraController.CameraPosition.Keuangan; break;
                case "Tambang": targetPos = CameraController.CameraPosition.Tambang; break;
            }

            // 2. Gerakkan kamera dan tunggu
            if (cameraController.CurrentPosition != targetPos)
            {
                yield return cameraController.MoveTo(targetPos);
            }
            yield return new WaitForSeconds(0.5f);

        }

        // 3. Logika untuk mengubah IPO (seperti sebelumnya)
        var data = ipoDataList.FirstOrDefault(i => i.color == color);
        if (data != null)
        {
            data.ipoIndex += delta;
            if (SfxManager.Instance != null && ipoMoveSound != null)
            {
                SfxManager.Instance.PlaySound(ipoMoveSound);
            }
            UpdateIPOVisuals();
            Debug.Log($"[Modify IPO] {color} IPO index diubah sebesar {delta}, menjadi {data.ipoIndex}");
        }
        else
        {
            Debug.LogWarning($"[Modify IPO] Tidak ditemukan IPOData untuk warna: {color}");
        }

        yield return new WaitForSeconds(1.5f); // Jeda untuk melihat perubahan

        // 4. Kembalikan kamera
        if (cameraController != null)
        {
            yield return cameraController.MoveTo(CameraController.CameraPosition.Normal);
        }
    }

    public IEnumerator ResetAllIPOIndexesWithCamera()
    {
        if (cameraController != null)
        {
            // 1. Gerakkan kamera ke tengah dan tunggu
            yield return cameraController.MoveTo(CameraController.CameraPosition.Center);
            yield return new WaitForSeconds(0.5f);
        }

        // 2. Logika reset (seperti di RumorPhaseManager)
        foreach (var data in ipoDataList)
        {
            if (SfxManager.Instance != null && ipoMoveSound != null)
            {
                SfxManager.Instance.PlaySound(ipoMoveSound);
            }
            data.ipoIndex = 0;
            data.currentState = IPOState.Normal;
            data.salesBonus = 0;
            Debug.Log($"[IPO] IPO {data.color} di-reset ke 0");
            UpdateIPOState(data);
        }
        UpdateIPOVisuals();

        yield return new WaitForSeconds(2.0f); // Jeda untuk melihat perubahan

        // 3. Kembalikan kamera
        if (cameraController != null)
        {
            yield return cameraController.MoveTo(CameraController.CameraPosition.Normal);
        }
    }

}
