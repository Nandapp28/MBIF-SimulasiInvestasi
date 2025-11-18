using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ExitGames.Client.Photon;
using TMPro;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class SellingPhaseManagerMultiplayer : MonoBehaviourPunCallbacks
{
    public static SellingPhaseManagerMultiplayer Instance;

    [Header("UI Elements")]
    public GameObject sellingPanel;
    public Button confirmSellButton;
    public Transform colorSellRowContainer;
    public GameObject colorSellRowPrefab;
    public AudioClip buttonClickSellSfx; // <-- BARIS INI ADA
    private AudioSource audioSource;

    [Header("Timer UI (Shared)")]
    public GameObject timerPanel;
    public Image timerBar;
    public TextMeshProUGUI timerText;
    public const float SELLING_TIME = 30.0f; // Waktu dalam detik
    private Coroutine sellingTimerCoroutine;
    private const string SELLING_START_TIME_KEY = "sellingStartTime";
    private bool localPlayerHasConfirmedSell = false;
    private Coroutine botSellingCoroutine;

    [System.Serializable]
    public class IPOIndicatorMapping
    {
        public string color;
        public List<Transform> positionSlots; // Daftar semua kemungkinan posisi (misal: 10 slot)
        public GameObject indicatorObject; // Objek indikator untuk warna ini
        public Transform risePositionSlot;    // Slot untuk posisi saat kondisi Rise
        public List<GameObject> riseBonusPrefabs; // Akan berisi prefab +1, +2, +3, ...
        public GameObject riseIndicatorPrefab;
    }

    [Header("IPO Visuals")]
    public List<IPOIndicatorMapping> ipoIndicatorMappings; // Ganti 'ipoIndicators' dengan ini
    public float ipoIndicatorOffset = 0.5f;
    public Dictionary<string, Vector3> initialIpoPositions = new Dictionary<string, Vector3>();
    private Dictionary<string, int> minIpoIndexMap = new Dictionary<string, int>
    {
        { "Konsumer", -3 },      // Harga terendah 1
        { "Infrastruktur", -3 }, // Harga terendah 1
        { "Keuangan", -3 },      // Harga terendah 1
        { "Tambang",  -2 },      // Harga terendah 2 (sesuai map baru)
    };

    private Dictionary<string, int> resetIpoIndexMap = new Dictionary<string, int>
    {
        { "Konsumer", 0 },      // Harga 5
        { "Infrastruktur", 0 }, // Harga 5
        { "Keuangan", 0 },      // Harga 5
        { "Tambang",  0 },      // Harga 5
    };

    private Dictionary<string, int> maxIpoIndexMap = new Dictionary<string, int>
    {
        { "Konsumer", 3 },      // Harga tertinggi 8
        { "Infrastruktur", 3 }, // Harga tertinggi 9
        { "Keuangan", 3 },      // Harga tertinggi 9
        { "Tambang",  2 },      // Harga tertinggi 9
    };

    private readonly int[] risePrices = { 10, 12, 13, 15 };

    private Dictionary<string, int[]> ipoPriceMap = new Dictionary<string, int[]>
    {
        { "Konsumer", new int[] { 1, 2, 3, 5, 6, 7, 8 } },
        { "Infrastruktur", new int[] { 1, 2, 4, 5, 6, 7, 9 } },
        { "Keuangan", new int[] { 1, 3, 4, 5, 6, 7, 9 } },
        { "Tambang",  new int[] { 2, 4, 5, 7, 9 } }, // Hanya 5 nilai
    };

    private const string IPO_INDEX_PREFIX = "ipo_index_";
    private const string IPO_BONUS_PREFIX = "ipo_bonus_";
    private Dictionary<string, GameObject> instantiatedBonusObjects = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> instantiatedRiseIndicators = new Dictionary<string, GameObject>();
    private List<Player> playersToWaitFor;
    private Dictionary<int, Hashtable> allPlayerSellDecisions = new Dictionary<int, Hashtable>();
    private Dictionary<string, int> localSellInputs = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
        if (PhotonNetwork.IsMasterClient)
        {
            // Di sinilah Anda menentukan nilai awal yang Anda inginkan!
            Dictionary<string, int> startingIpoValues = new Dictionary<string, int>
        {
            { "Konsumer", 0 },
            { "Infrastruktur", 0 },
            { "Keuangan", 0 },
            { "Tambang", 0 } // Ingat, Tambang range-nya -2 sampai 2
        };

            // Panggil fungsi yang ada di SellingPhaseManagerMultiplayer
            if (SellingPhaseManagerMultiplayer.Instance != null)
            {
                SellingPhaseManagerMultiplayer.Instance.InitializeIpoState(startingIpoValues);
            }
            else
            {
                Debug.LogError("Instance SellingPhaseManagerMultiplayer tidak ditemukan!");
            }
        }
    }
    void Start()
    {
        if (timerPanel != null) timerPanel.SetActive(false);
    }
    public void InitializeIpoState(Dictionary<string, int> initialIndices)
{
    // Pastikan hanya MasterClient yang bisa menjalankan ini
    if (!PhotonNetwork.IsMasterClient) return;

    Debug.Log("MASTERCLIENT: Mengatur state awal IPO...");

    // Siapkan Hashtable untuk menampung semua properti baru
    Hashtable initialRoomProps = new Hashtable();

    // Loop melalui setiap warna yang ada di ipoPriceMap
    foreach (var color in ipoPriceMap.Keys)
    {
        string ipoIndexKey = IPO_INDEX_PREFIX + color;
        string ipoBonusKey = IPO_BONUS_PREFIX + color;

        int initialIndex = 0;
        // Cek apakah ada nilai awal yang diberikan untuk warna ini
        if (initialIndices != null && initialIndices.ContainsKey(color))
        {
            initialIndex = initialIndices[color];
        }

        // Tambahkan nilai index dan bonus awal ke Hashtable
        initialRoomProps[ipoIndexKey] = initialIndex;
        initialRoomProps[ipoBonusKey] = 0; // Bonus selalu dimulai dari 0

        Debug.Log($" > Mengatur {color}: Index = {initialIndex}, Bonus = 0");
    }

    // Kirim semua properti yang sudah diatur ke jaringan (untuk semua pemain)
    PhotonNetwork.CurrentRoom.SetCustomProperties(initialRoomProps);
}

    public void ModifyIPOIndex(string color, int delta)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Ambil state saat ini
        string ipoIndexKey = IPO_INDEX_PREFIX + color;
        string ipoBonusKey = IPO_BONUS_PREFIX + color;
        int currentIndex = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ipoIndexKey) ? (int)PhotonNetwork.CurrentRoom.CustomProperties[ipoIndexKey] : 0;
        int currentBonus = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ipoBonusKey) ? (int)PhotonNetwork.CurrentRoom.CustomProperties[ipoBonusKey] : 0;

        int minIndex = minIpoIndexMap[color];
        int maxIndex = maxIpoIndexMap[color];

        // Gabungkan index dan bonus untuk mendapatkan "posisi" efektif, lalu tambahkan perubahan
        int combinedPosition = currentIndex + currentBonus + delta;

        int newIndex;
        int newBonus;

        if (combinedPosition > maxIndex) // Masuk atau tetap di kondisi RISE
        {
            newIndex = maxIndex;
            newBonus = combinedPosition - maxIndex;

            // Tentukan harga maksimum di jalur normal untuk clamping
            int maxNormalPrice = ipoPriceMap[color][ipoPriceMap[color].Length - 1];
            int maxBonus = 15 - maxNormalPrice;

            // Pastikan bonus tidak membuat harga total melebihi 15
            newBonus = Mathf.Min(newBonus, maxBonus);
        }
        else if (combinedPosition < minIndex) // Masuk kondisi CRASH
        {
            newIndex = resetIpoIndexMap[color];
            newBonus = 0; // Reset bonus saat crash
                          // Logika crash lainnya (force sell) tetap berjalan di sini
            Debug.LogWarning($"💥💥💥 [CRASH] IPO Sektor {color} jatuh di bawah batas! Memulai reset...");
            string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(color);
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.CustomProperties.ContainsKey(cardKey) && (int)p.CustomProperties[cardKey] > 0)
                {
                    p.SetCustomProperties(new Hashtable { { cardKey, 0 } });
                }
            }
        }
        else // Kondisi NORMAL
        {
            newIndex = combinedPosition;
            newBonus = 0;
        }

        // Siapkan properti baru untuk dikirim ke jaringan
        Hashtable roomProps = new Hashtable
    {
        { ipoIndexKey, newIndex },
        { ipoBonusKey, newBonus }
    };
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    #region Visuals
    private void UpdateAllIpoVisuals()
    {
        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        foreach (var mapping in ipoIndicatorMappings)
        {
            // --- BAGIAN PEMBERSIHAN (SEKARANG MEMBERSIHKAN KEDUANYA) ---
            // Hancurkan duplikat indikator RISE lama jika ada
            if (instantiatedRiseIndicators.ContainsKey(mapping.color))
            {
                if (instantiatedRiseIndicators[mapping.color] != null)
                {
                    Destroy(instantiatedRiseIndicators[mapping.color]);
                }
                instantiatedRiseIndicators.Remove(mapping.color);
            }
            // Hancurkan objek bonus lama jika ada
            if (instantiatedBonusObjects.ContainsKey(mapping.color))
            {
                if (instantiatedBonusObjects[mapping.color] != null)
                {
                    Destroy(instantiatedBonusObjects[mapping.color]);
                }
                instantiatedBonusObjects.Remove(mapping.color);
            }

            string ipoKey = IPO_INDEX_PREFIX + mapping.color;
            string bonusKey = IPO_BONUS_PREFIX + mapping.color;

            if (roomProps.ContainsKey(ipoKey))
            {
                int ipoIndex = (int)roomProps[ipoKey];
                int ipoBonus = roomProps.ContainsKey(bonusKey) ? (int)roomProps[bonusKey] : 0;

                // --- LOGIKA BARU UNTUK KONDISI RISE ---
                if (ipoBonus > 0)
                {
                    // 1. INDIKATOR UTAMA TETAP DI POSISI MAKSIMUM
                    int maxNormalIndex;
                    if (mapping.color == "Tambang") { maxNormalIndex = Mathf.Clamp(maxIpoIndexMap[mapping.color], -2, 2) + 2; }
                    else { maxNormalIndex = Mathf.Clamp(maxIpoIndexMap[mapping.color], -3, 3) + 3; }
                    
                    if (mapping.indicatorObject != null && maxNormalIndex < mapping.positionSlots.Count)
                    {
                        mapping.indicatorObject.transform.position = mapping.positionSlots[maxNormalIndex].position;
                        mapping.indicatorObject.SetActive(true);
                    }

                    // 2. BUAT DUPLIKAT INDIKATOR DI SLOT RISE
                    if (mapping.riseIndicatorPrefab != null && mapping.risePositionSlot != null)
                    {
                        GameObject newRiseIndicator = Instantiate(mapping.riseIndicatorPrefab, mapping.risePositionSlot.position, mapping.risePositionSlot.rotation);
                        instantiatedRiseIndicators[mapping.color] = newRiseIndicator; // Simpan referensinya
                    }
                    
                    // 3. TAMPILKAN PREFAB BONUS (+1, +2, dst.) DI SLOT RISE
                    int bonusPrefabIndex = ipoBonus - 1;
                    if (bonusPrefabIndex >= 0 && bonusPrefabIndex < mapping.riseBonusPrefabs.Count)
                    {
                        GameObject prefabToInstantiate = mapping.riseBonusPrefabs[bonusPrefabIndex];
                        if (prefabToInstantiate != null && mapping.risePositionSlot != null)
                        {
                            GameObject newBonusObject = Instantiate(prefabToInstantiate, mapping.risePositionSlot.position, mapping.risePositionSlot.rotation);
                            instantiatedBonusObjects[mapping.color] = newBonusObject;
                        }
                    }
                }
                // --- LOGIKA LAMA UNTUK KONDISI NORMAL (TIDAK BERUBAH) ---
                else
                {
                    int positionIndex;
                    if (mapping.color == "Tambang") { positionIndex = Mathf.Clamp(ipoIndex, -2, 2) + 2; }
                    else { positionIndex = Mathf.Clamp(ipoIndex, -3, 3) + 3; }

                    if (mapping.indicatorObject != null && positionIndex < mapping.positionSlots.Count)
                    {
                        mapping.indicatorObject.transform.position = mapping.positionSlots[positionIndex].position;
                        mapping.indicatorObject.SetActive(true);
                    }
                }
            }
        }
    }

    private int GetCurrentColorValue(string color, int ipoIndex)
    {
        // Ambil juga nilai bonus saat ini
        string bonusKey = IPO_BONUS_PREFIX + color;
        int ipoBonus = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(bonusKey) ? (int)PhotonNetwork.CurrentRoom.CustomProperties[bonusKey] : 0;

        int basePrice = 0;
        if (ipoPriceMap.ContainsKey(color))
        {
            int mapIndex;
            if (color == "Tambang") { mapIndex = Mathf.Clamp(ipoIndex, -2, 2) + 2; }
            else { mapIndex = Mathf.Clamp(ipoIndex, -3, 3) + 3; }

            if (mapIndex >= 0 && mapIndex < ipoPriceMap[color].Length)
            {
                basePrice = ipoPriceMap[color][mapIndex];
            }
        }

        // Harga final adalah harga dasar + bonus
        return basePrice + ipoBonus;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        UpdateAllIpoVisuals();
        if (propertiesThatChanged.ContainsKey(SELLING_START_TIME_KEY))
        {
            if (sellingTimerCoroutine != null) StopCoroutine(sellingTimerCoroutine);
            double startTime = (double)propertiesThatChanged[SELLING_START_TIME_KEY];
            sellingTimerCoroutine = StartCoroutine(StartSellingTimer(startTime));
        }
    }

    private IEnumerator StartSellingTimer(double startTime)
    {
        if (timerPanel != null) timerPanel.SetActive(true);
        float timeLeft = SELLING_TIME;

        while (timeLeft > 0)
        {
            double elapsed = PhotonNetwork.Time - startTime;
            timeLeft = SELLING_TIME - (float)elapsed;

            if (timeLeft < 0) timeLeft = 0;

            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(timeLeft).ToString();
            }
            if (timerBar != null)
            {
                timerBar.fillAmount = Mathf.Clamp01(timeLeft / SELLING_TIME);
            }
            
            yield return null;
        }
        
        // Waktu habis, cek apakah pemain ini sudah submit
        if (!localPlayerHasConfirmedSell)
        {
            Debug.Log("Waktu Selling habis! Otomatis submit 0 sales.");
            BotModeManager.SetBotMode(true);
            OnConfirmSellButtonClicked(); // Otomatis submit
        }
        // Panel akan disembunyikan oleh Rpc_StopSellingTimer
    }
    
    //RPC untuk menghentikan timer & UI
    [PunRPC]
    private void Rpc_StopSellingTimer()
    {
        if (sellingTimerCoroutine != null)
        {
            StopCoroutine(sellingTimerCoroutine);
            sellingTimerCoroutine = null;
        }
        if (timerPanel != null) timerPanel.SetActive(false);
        if (sellingPanel != null) sellingPanel.SetActive(false); // Sembunyikan panel utama di sini
    }
    #endregion

    public void StartSellingPhase(List<Player> turnOrder)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient memulai Fase Penjualan dan mengatur IPO awal...");
            playersToWaitFor = new List<Player>(turnOrder);
            allPlayerSellDecisions.Clear();
            foreach (Player p in turnOrder) 
            {
                if (p.IsInactive)
                {
                    StartCoroutine(HandleDisconnectedPlayerSelling(p));
                }
            }
            // Kirim RPC ke semua pemain untuk memulai fase
            photonView.RPC("Rpc_ShowSellingUI", RpcTarget.All);

            Hashtable timerProps = new Hashtable { { SELLING_START_TIME_KEY, PhotonNetwork.Time } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(timerProps);
        }
    }

    [PunRPC]
    private void Rpc_ShowSellingUI()
    {
        localSellInputs.Clear();
        foreach (Transform child in colorSellRowContainer) Destroy(child.gameObject);

        Player localPlayer = PhotonNetwork.LocalPlayer;

        string[] colors = { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };

        for (int i = 0; i < colors.Length; i++)
        {
            string colorName = colors[i];
            string colorKey = PlayerProfileMultiplayer.GetCardKeyFromColor(colorName);
            int ownedCards = localPlayer.CustomProperties.ContainsKey(colorKey) ? (int)localPlayer.CustomProperties[colorKey] : 0;

            localSellInputs[colorName] = 0;
            GameObject row = Instantiate(colorSellRowPrefab, colorSellRowContainer);
            row.transform.Find("ColorLabel").GetComponent<Text>().text = colorName;
            row.transform.Find("PriceLabel").GetComponent<Text>().text = GetFullCardPrice(colorName).ToString();

            Text valueText = row.transform.Find("ValueText").GetComponent<Text>();
            Button plusButton = row.transform.Find("PlusButton").GetComponent<Button>();
            Button minusButton = row.transform.Find("MinusButton").GetComponent<Button>();

            valueText.text = "0";

            plusButton.onClick.AddListener(() =>
            {
                if (localSellInputs[colorName] < ownedCards)
                {
                    localSellInputs[colorName]++;
                    valueText.text = localSellInputs[colorName].ToString();
                }
            });
            minusButton.onClick.AddListener(() =>
            {
                if (localSellInputs[colorName] > 0)
                {
                    localSellInputs[colorName]--;
                    valueText.text = localSellInputs[colorName].ToString();
                }
            });
        }

        localPlayerHasConfirmedSell = false;
        confirmSellButton.gameObject.SetActive(true);
        confirmSellButton.interactable = true;

        confirmSellButton.onClick.RemoveAllListeners();
        confirmSellButton.onClick.AddListener(OnConfirmSellButtonClicked);
        sellingPanel.SetActive(true);

        if (botSellingCoroutine != null) StopCoroutine(botSellingCoroutine); // Hentikan jika ada sisa
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerProfileMultiplayer.IS_BOT_MODE_KEY)
            && (bool)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.IS_BOT_MODE_KEY])
        {
            botSellingCoroutine = StartCoroutine(BotSellingCoroutine());
        }
    }
    private IEnumerator BotSellingCoroutine()
    {
        Debug.Log("[Bot Mode] Selling: Menunggu 5 detik sebelum konfirmasi otomatis...");
        yield return new WaitForSeconds(5.0f);

        // Cek lagi setelah 5 detik
        bool isStillBot = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PlayerProfileMultiplayer.IS_BOT_MODE_KEY) &&
                          (bool)PhotonNetwork.LocalPlayer.CustomProperties[PlayerProfileMultiplayer.IS_BOT_MODE_KEY];

        if (isStillBot && !localPlayerHasConfirmedSell) // Pastikan pemain belum konfirmasi manual
        {
            Debug.Log("[Bot Mode] Selling: Waktu tunggu 5 detik selesai. Masih dalam mode bot. Konfirmasi 0 penjualan.");
            OnConfirmSellButtonClicked(); // Fungsi ini sudah menangani 'localPlayerHasConfirmedSell = true' dan RPC
        }
        else
        {
            Debug.Log("[Bot Mode] Selling: Dibatalkan. Pemain kembali ke mode manual atau sudah konfirmasi.");
        }
    }

    public void OnConfirmSellButtonClicked()
    {
        if (localPlayerHasConfirmedSell) return;
        localPlayerHasConfirmedSell = true;
        // --- TAMBAHKAN BLOK KODE INI ---
        // Panggil SFX melalui instance singleton dari SfxManager
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        // --------------------------------

        Hashtable sellDecision = new Hashtable();
        foreach (var entry in localSellInputs)
        {
            if (entry.Value > 0) sellDecision.Add(entry.Key, entry.Value);
        }
        photonView.RPC("SubmitSellDecision", RpcTarget.MasterClient, sellDecision);
        confirmSellButton.gameObject.SetActive(false);
        foreach (Transform row in colorSellRowContainer)
        {
            row.Find("PlusButton").GetComponent<Button>().interactable = false;
            row.Find("MinusButton").GetComponent<Button>().interactable = false;
        }
    }

    [PunRPC]
    private void SubmitSellDecision(Hashtable decision, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        // Panggil helper baru
        ProcessPlayerSellDecision(decision, info.Sender.ActorNumber);
    }

    // --- FUNGSI HELPER BARU (untuk dipanggil oleh RPC dan Coroutine) ---
    private void ProcessPlayerSellDecision(Hashtable decision, int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Cari player berdasarkan actorNumber
        Player sender = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        if (sender == null)
        {
            Debug.LogWarning($"[SellingPhase] SubmitSellDecision gagal: Player dengan ActorNumber {actorNumber} tidak ditemukan.");
            return;
        }

        allPlayerSellDecisions[sender.ActorNumber] = decision;

        if (playersToWaitFor.Contains(sender))
        {
            playersToWaitFor.Remove(sender);
        }
        else
        {
             // Jika 'sender' tidak ada di 'playersToWaitFor', mungkin karena dia disconnect
             // dan coroutine HandleDisconnectedPlayerSelling memanggil ini.
             // Kita tidak perlu log warning, cukup pastikan dia tidak ada di list.
        }

        if (playersToWaitFor.Count == 0)
        {
            photonView.RPC("Rpc_StopSellingTimer", RpcTarget.All);
            StartCoroutine(ProcessAllSales());
        }
    }

    public int GetFullCardPrice(string color)
    {
        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        int ipoIndex = roomProps.ContainsKey(IPO_INDEX_PREFIX + color) ? (int)roomProps[IPO_INDEX_PREFIX + color] : 0;
        int finalPrice = GetCurrentColorValue(color, ipoIndex);
        return finalPrice;
    }



    private IEnumerator ProcessAllSales()
    {
        if (!PhotonNetwork.IsMasterClient) yield break;
        Debug.Log("Memulai proses kalkulasi penjualan untuk semua pemain...");

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!allPlayerSellDecisions.ContainsKey(player.ActorNumber)) continue;

            Hashtable playerDecision = allPlayerSellDecisions[player.ActorNumber];
            Hashtable playerProps = player.CustomProperties;
            int totalEarnings = 0;
            Hashtable propsToSet = new Hashtable();

            foreach (var decisionEntry in playerDecision)
            {
                string colorName = (string)decisionEntry.Key;
                int quantityToSell = (int)decisionEntry.Value;
                if (quantityToSell <= 0) continue;

                totalEarnings += quantityToSell * GetFullCardPrice(colorName);

                string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(colorName);
                if (!string.IsNullOrEmpty(cardKey))
                {
                    int currentCards = playerProps.ContainsKey(cardKey) ? (int)playerProps[cardKey] : 0;
                    propsToSet[cardKey] = currentCards - quantityToSell;
                }
            }

            int currentInvestpoint = playerProps.ContainsKey(PlayerProfileMultiplayer.INVESTPOINT_KEY) ? (int)playerProps[PlayerProfileMultiplayer.INVESTPOINT_KEY] : 0;
            propsToSet[PlayerProfileMultiplayer.INVESTPOINT_KEY] = currentInvestpoint + totalEarnings;

            player.SetCustomProperties(propsToSet);
            Debug.Log($"[Penjualan] {player.NickName} mendapatkan {totalEarnings} InvestPoint.");
        }

        // 1. Panggil RPC untuk memulai transisi di SEMUA klien
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.photonView.RPC(
                "Rpc_StartFadeTransition",
                RpcTarget.All,
                MultiplayerManager.TransitionType.Rumor
            );
        }

        // 2. Tunggu selama total durasi transisi
        yield return new WaitForSeconds(2.0f);

        Debug.Log("Semua penjualan diproses. Memulai Fase Rumor...");
        if (RumorPhaseManagerMultiplayer.Instance != null)
        {
            RumorPhaseManagerMultiplayer.Instance.StartRumorPhase(PhotonNetwork.PlayerList.ToList());
        }
        else
        {
            Debug.LogError("Tidak dapat memulai Fase Rumor, referensi tidak ditemukan!");
        }
    }
    
    public void ForceSellAllCardsForLeaderboard()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Debug.Log("💰 [GAME END] Menjual semua sisa kartu pemain...");
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int finalEarnings = 0;
            Hashtable playerProps = player.CustomProperties;
            Hashtable propsToSet = new Hashtable();
            string[] colors = { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };
            
            foreach(string color in colors)
            {
                string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(color);
                int cardCount = playerProps.ContainsKey(cardKey) ? (int)playerProps[cardKey] : 0;
                if (cardCount > 0)
                {
                    finalEarnings += cardCount * GetFullCardPrice(color);
                    propsToSet[cardKey] = 0;
                }
            }
            
            if (finalEarnings > 0)
            {
                int currentInvestpoint = (int)playerProps[PlayerProfileMultiplayer.INVESTPOINT_KEY];
                propsToSet[PlayerProfileMultiplayer.INVESTPOINT_KEY] = currentInvestpoint + finalEarnings;
            }
            
            if (propsToSet.Count > 0) player.SetCustomProperties(propsToSet);
        }
        StartCoroutine(ShowLeaderboardAfterDelay());
    }

    private IEnumerator ShowLeaderboardAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        MultiplayerManager.Instance.ShowLeaderboard();
    }
    public void HandlePlayerDisconnect(Player disconnectedPlayer)
    {
        // Hanya MasterClient & hanya jika kita sedang menunggu pemain
        if (!PhotonNetwork.IsMasterClient || playersToWaitFor == null || playersToWaitFor.Count == 0)
        {
            return;
        }

        if (playersToWaitFor.Contains(disconnectedPlayer))
        {
            Debug.Log($"[SellingPhaseManager] {disconnectedPlayer.NickName} disconnect. Mensubmit 0 penjualan untuknya.");
            
            // Ini adalah logika yang sama dari Rpc_SubmitSellDecision
            allPlayerSellDecisions[disconnectedPlayer.ActorNumber] = new Hashtable(); // Submit 0 sales
            playersToWaitFor.Remove(disconnectedPlayer);

            // Cek apakah semua pemain (yang tersisa) sudah selesai
            if (playersToWaitFor.Count == 0)
            {
                photonView.RPC("Rpc_StopSellingTimer", RpcTarget.All);
                StartCoroutine(ProcessAllSales());
            }
        }
    }
    private IEnumerator HandleDisconnectedPlayerSelling(Player disconnectedPlayer)
    {
        Debug.Log($"[SellingPhaseManager] Player {disconnectedPlayer.NickName} sudah disconnect. Menunggu 5 detik (Bot Mode) sebelum submit 0 sales...");
        yield return new WaitForSeconds(5.0f);

        // Cek lagi jika MasterClient masih aktif dan fase masih berjalan
        if (!PhotonNetwork.IsMasterClient || playersToWaitFor == null || !playersToWaitFor.Contains(disconnectedPlayer))
        {
            yield break; // Batalkan jika fase sudah selesai atau pemain sudah diproses
        }

        Debug.Log($"[SellingPhaseManager] Mensubmit 0 penjualan untuk {disconnectedPlayer.NickName} (Disconnect Bot Mode).");
        
        // Panggil fungsi helper yang baru kita buat
        ProcessPlayerSellDecision(new Hashtable(), disconnectedPlayer.ActorNumber);
    }
}