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

    [System.Serializable]
    public class IPOIndicatorMapping
    {
        public string color;
        public List<Transform> positionSlots; // Daftar semua kemungkinan posisi (misal: 10 slot)
        public GameObject indicatorObject; // Objek indikator untuk warna ini
        public Transform risePositionSlot;    // Slot untuk posisi saat kondisi Rise
        public List<GameObject> riseBonusPrefabs; // Akan berisi prefab +1, +2, +3, ...
    }

    [Header("IPO Visuals")]
    public List<IPOIndicatorMapping> ipoIndicatorMappings; // Ganti 'ipoIndicators' dengan ini
    public float ipoIndicatorOffset = 0.5f;
    private Dictionary<string, Vector3> initialIpoPositions = new Dictionary<string, Vector3>();

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
        { "Infrastruktur", new int[] { 1, 2, 4, 5, 6, 8, 9 } },
        { "Keuangan", new int[] { 1, 3, 4, 5, 6, 7, 9 } },
        { "Tambang",  new int[] { 2, 4, 5, 7, 9 } }, // Hanya 5 nilai
    };

    private const string IPO_INDEX_PREFIX = "ipo_index_";
    private const string IPO_BONUS_PREFIX = "ipo_bonus_";
    private Dictionary<string, GameObject> instantiatedBonusObjects = new Dictionary<string, GameObject>();
    private List<Player> playersToWaitFor;
    private Dictionary<int, Hashtable> allPlayerSellDecisions = new Dictionary<int, Hashtable>();
    private Dictionary<string, int> localSellInputs = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
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
            string ipoKey = IPO_INDEX_PREFIX + mapping.color;
            string bonusKey = IPO_BONUS_PREFIX + mapping.color;

            // --- BAGIAN PEMBERSIHAN ---
            // Sebelum melakukan apa pun, hancurkan objek bonus lama jika ada
            if (instantiatedBonusObjects.ContainsKey(mapping.color))
            {
                if (instantiatedBonusObjects[mapping.color] != null)
                {
                    Destroy(instantiatedBonusObjects[mapping.color]);
                }
                instantiatedBonusObjects.Remove(mapping.color);
            }

            if (roomProps.ContainsKey(ipoKey))
            {
                int ipoIndex = (int)roomProps[ipoKey];
                int ipoBonus = roomProps.ContainsKey(bonusKey) ? (int)roomProps[bonusKey] : 0;

                if (ipoBonus > 0) // Kondisi RISE
                {
                    // 1. Pindahkan indikator utama ke posisi Rise
                    if (mapping.indicatorObject != null && mapping.risePositionSlot != null)
                    {
                        mapping.indicatorObject.transform.position = mapping.risePositionSlot.position;
                        mapping.indicatorObject.SetActive(true);
                    }

                    // 2. Buat (Instantiate) prefab bonus yang sesuai dari Project
                    int bonusPrefabIndex = ipoBonus - 1;
                    if (bonusPrefabIndex >= 0 && bonusPrefabIndex < mapping.riseBonusPrefabs.Count)
                    {
                        GameObject prefabToInstantiate = mapping.riseBonusPrefabs[bonusPrefabIndex];
                        if (prefabToInstantiate != null && mapping.risePositionSlot != null)
                        {
                            // Buat prefab di posisi Rise dan simpan referensinya
                            GameObject newBonusObject = Instantiate(prefabToInstantiate, mapping.risePositionSlot.position, mapping.risePositionSlot.rotation);
                            instantiatedBonusObjects[mapping.color] = newBonusObject;
                        }
                    }
                }
                else // Kondisi NORMAL
                {
                    // Logika lama untuk memindahkan indikator di jalur normal (tidak berubah)
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
    }
    #endregion

    public void StartSellingPhase(List<Player> turnOrder)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient memulai Fase Penjualan dan mengatur IPO awal...");
            playersToWaitFor = new List<Player>(turnOrder);
            allPlayerSellDecisions.Clear();
            // Kirim RPC ke semua pemain untuk memulai fase
            photonView.RPC("Rpc_ShowSellingUI", RpcTarget.All);
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
            
            plusButton.onClick.AddListener(() => {
                if (localSellInputs[colorName] < ownedCards)
                {
                    localSellInputs[colorName]++;
                    valueText.text = localSellInputs[colorName].ToString();
                }
            });
            minusButton.onClick.AddListener(() => {
                if (localSellInputs[colorName] > 0)
                {
                    localSellInputs[colorName]--;
                    valueText.text = localSellInputs[colorName].ToString();
                }
            });
        }

        confirmSellButton.onClick.RemoveAllListeners();
        confirmSellButton.onClick.AddListener(OnConfirmSellButtonClicked);
        sellingPanel.SetActive(true);
    }

    public void OnConfirmSellButtonClicked()
    {
        Hashtable sellDecision = new Hashtable();
        foreach (var entry in localSellInputs)
        {
            if (entry.Value > 0) sellDecision.Add(entry.Key, entry.Value);
        }
        photonView.RPC("SubmitSellDecision", RpcTarget.MasterClient, sellDecision);
        sellingPanel.SetActive(false);
    }

    [PunRPC]
    private void SubmitSellDecision(Hashtable decision, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        Player sender = info.Sender;
        allPlayerSellDecisions[sender.ActorNumber] = decision;

        if (playersToWaitFor.Contains(sender))
        {
            playersToWaitFor.Remove(sender);
        }

        if (playersToWaitFor.Count == 0)
        {
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

        if (MultiplayerManager.Instance != null)
        {
            yield return StartCoroutine(MultiplayerManager.Instance.FadeTransition(
                MultiplayerManager.Instance.rumorTransitionCG, 0.5f, 1.5f, 0.5f
            ));
        }

        // PANGGIL FASE BERIKUTNYA SETELAH SEMUA PEMAIN DIPROSES
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
}