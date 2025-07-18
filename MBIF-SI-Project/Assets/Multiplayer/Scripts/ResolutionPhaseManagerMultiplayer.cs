// File: ResolutionPhaseManagerMultiplayer.cs
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ResolutionPhaseManagerMultiplayer : MonoBehaviourPunCallbacks
{
    public static ResolutionPhaseManagerMultiplayer Instance;

    [Header("3D Object References")]
    public List<DividendIndicatorMapping> dividendIndicatorMappings;
    public List<GameObject> tokenObjectsKonsumer;
    public List<GameObject> tokenObjectsInfrastruktur;
    public List<GameObject> tokenObjectsKeuangan;
    public List<GameObject> tokenObjectsTambang;

    [System.Serializable]
    public class TokenMaterial { public int value; public Material material; }
    public List<TokenMaterial> tokenMaterials;
    
    [System.Serializable]
    public class DividendIndicatorMapping
    {
        public string color;
        public GameObject indicatorObject;
        public List<Transform> positionSlots;
    }

    [Header("Game Settings")]
    public float dividendIndicatorOffset = 0.5f;

    // Kunci untuk Room Custom Properties
    private const string RES_TOKENS_PREFIX = "res_tokens_";
    private const string DIVIDEND_INDEX_PREFIX = "div_index_";
    private const string REVEALED_COUNT_PREFIX = "rev_count_";
    private const string X2_BONUS_PREFIX = "x2_bonus_";

    private string[] resolutionOrder = { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };

    private Dictionary<int, int> dividendRewards = new Dictionary<int, int>()
    {
        { -3, 0 }, { -2, 1 }, { -1, 1 }, { 0, 1 },
        { 1, 2 }, { 2, 2 }, { 3, 3 }
    };

    #region Setup
    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // Fungsi Start bisa dibiarkan kosong
    }
    #endregion

    #region Phase Logic
    public void StartResolutionPhase()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient memulai Fase Resolusi...");
            InitializeResolutionData();
        }
    }
    
    private void InitializeResolutionData()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Hashtable roomProps = new Hashtable();
        // Tambahkan 0 untuk mewakili token "x2"
        int[] possibleTokens = { -2, -1, 1, 2, 0 };

        foreach (string color in resolutionOrder)
        {
            roomProps[DIVIDEND_INDEX_PREFIX + color] = 0;
            roomProps[REVEALED_COUNT_PREFIX + color] = 0;
            roomProps[X2_BONUS_PREFIX + color] = false; // <-- TAMBAHKAN INI (set bonus awal ke false)

            List<int> tokens = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                tokens.Add(possibleTokens[Random.Range(0, possibleTokens.Length)]);
            }
            roomProps[RES_TOKENS_PREFIX + color] = tokens.ToArray();
        }
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    private IEnumerator RunResolutionSequence()
    {
        Debug.Log("MasterClient memulai urutan resolusi...");
        yield return new WaitForSeconds(2f);

        foreach (string color in resolutionOrder)
        {
            photonView.RPC("Rpc_RevealNextTokenFor", RpcTarget.All, color);
            yield return new WaitForSeconds(2.5f);
            ApplyTokenEffect(color);
            yield return new WaitForSeconds(1.5f);
            yield return new WaitForSeconds(0.5f);
            photonView.RPC("Rpc_ForceVisualUpdate", RpcTarget.All);
            yield return new WaitForSeconds(1.5f);
        }

        Debug.Log("Semua token telah terungkap. Memproses pembayaran dividen...");
        ProcessDividendPayouts();
    }
    #endregion

    #region Visuals & RPCs
    
    [PunRPC]
    private void Rpc_ForceVisualUpdate()
    {
        UpdateAllDividendVisuals();
    }
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (PhotonNetwork.IsMasterClient && propertiesThatChanged.ContainsKey(RES_TOKENS_PREFIX + "Konsumer"))
        {
            StartCoroutine(RunResolutionSequence());
        }
    }
    
    [PunRPC]
    private void Rpc_RevealNextTokenFor(string color)
    {
        Debug.Log($"[SEMUA PEMAIN] Mengungkap token berikutnya untuk {color}...");
        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;

        int revealedCount = (int)roomProps[REVEALED_COUNT_PREFIX + color];
        int[] tokens = (int[])roomProps[RES_TOKENS_PREFIX + color];
        int tokenValue = tokens[revealedCount];

        List<GameObject> tokenList = null;
        if (color == "Konsumer") tokenList = tokenObjectsKonsumer;
        else if (color == "Infrastruktur") tokenList = tokenObjectsInfrastruktur;
        else if (color == "Keuangan") tokenList = tokenObjectsKeuangan;
        else if (color == "Tambang") tokenList = tokenObjectsTambang;

        if (tokenList != null && revealedCount < tokenList.Count)
        {
            GameObject tokenToFlip = tokenList[revealedCount];
            Material targetMaterial = tokenMaterials.FirstOrDefault(m => m.value == tokenValue)?.material;

            if (tokenToFlip != null && targetMaterial != null)
            {
                StartCoroutine(FlipToken(tokenToFlip, targetMaterial));
            }
        }
    }
    
    private IEnumerator FlipToken(GameObject token, Material frontMaterial)
    {
        token.SetActive(true);
        float duration = 0.5f;
        float elapsed = 0f;
        Quaternion startRot = token.transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, 180);

        while (elapsed < duration)
        {
            token.transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            if (elapsed > duration / 2)
            {
                token.GetComponent<Renderer>().material = frontMaterial;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        token.transform.rotation = endRot;
    }

    private void UpdateAllDividendVisuals()
    {
        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;

        foreach (var mapping in dividendIndicatorMappings)
        {
            string divKey = DIVIDEND_INDEX_PREFIX + mapping.color;

            if (roomProps.ContainsKey(divKey) && mapping.indicatorObject != null)
            {
                int dividendIndex = (int)roomProps[divKey];
                dividendIndex = Mathf.Clamp(dividendIndex, -3, 3);
                int positionIndex = dividendIndex + 3;

                if (positionIndex >= 0 && positionIndex < mapping.positionSlots.Count)
                {
                    Transform targetSlot = mapping.positionSlots[positionIndex];
                    if (targetSlot != null)
                    {
                        mapping.indicatorObject.transform.position = targetSlot.position;
                    }
                }
            }
        }
    }
    #endregion
    
    #region Unchanged Logic
    private void ApplyTokenEffect(string color)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        string divKey = DIVIDEND_INDEX_PREFIX + color;
        string revKey = REVEALED_COUNT_PREFIX + color;
        string tokKey = RES_TOKENS_PREFIX + color;
        string x2Key = X2_BONUS_PREFIX + color; // Kunci untuk bonus x2

        int revealedCount = (int)roomProps[revKey];
        int[] tokens = (int[])roomProps[tokKey];
        int dividendIndex = (int)roomProps[divKey];
        int tokenEffect = tokens[revealedCount];

        Hashtable propsToSet = new Hashtable();

        // --- LOGIKA BARU DI SINI ---
        if (tokenEffect == 0) // Jika token adalah 'x2'
        {
            Debug.Log($"[Token x2] Bonus pengganda untuk {color} diaktifkan!");
            propsToSet[x2Key] = true; // Set status bonus menjadi true
        }
        else // Jika token adalah penambahan/pengurangan biasa
        {
            dividendIndex += tokenEffect;
        }

        // Cek Boom/Crash (logika ini tetap sama)
        if (dividendIndex > 3) 
        {
            ModifyIPOIndex(color, 1);
            dividendIndex = 0;
        }
        else if (dividendIndex < -3)
        {
            ModifyIPOIndex(color, -1);
            dividendIndex = 0;
        }
        
        propsToSet[divKey] = dividendIndex;
        propsToSet[revKey] = revealedCount + 1;
        PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet);
    }

    private void ModifyIPOIndex(string color, int delta)
    {
        string ipoIndexKey = "ipo_index_" + color;
        int currentIndex = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ipoIndexKey) ? (int)PhotonNetwork.CurrentRoom.CustomProperties[ipoIndexKey] : 0;
        Hashtable ipoProp = new Hashtable { { ipoIndexKey, currentIndex + delta } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(ipoProp);
        Debug.Log($"[Modify IPO by Resolution] {color} IPO index diubah sebesar {delta}.");
    }
    
    private void ProcessDividendPayouts()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Debug.Log("MasterClient menghitung pembayaran dividen untuk semua pemain...");
        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            int totalDividendEarnings = 0;
            foreach (string color in resolutionOrder)
            {
                string cardKey = PlayerProfileMultiplayer.GetCardKeyFromColor(color);
                int cardCount = player.CustomProperties.ContainsKey(cardKey) ? (int)player.CustomProperties[cardKey] : 0;
                
                if (cardCount > 0)
                {
                    string divKey = DIVIDEND_INDEX_PREFIX + color;
                    int dividendIndex = roomProps.ContainsKey(divKey) ? (int)roomProps[divKey] : 0;
                    int rewardPerCard = dividendRewards.ContainsKey(dividendIndex) ? dividendRewards[dividendIndex] : 0;
                    
                    int earningsForThisColor = cardCount * rewardPerCard;

                    // --- TERAPKAN PENGGANDA DI SINI ---
                    string x2Key = X2_BONUS_PREFIX + color;
                    bool isX2Active = roomProps.ContainsKey(x2Key) ? (bool)roomProps[x2Key] : false;
                    if (isX2Active)
                    {
                        earningsForThisColor *= 2;
                        Debug.Log($"Bonus x2 diterapkan untuk {player.NickName} di sektor {color}!");
                    }
                    // ---------------------------------

                    totalDividendEarnings += earningsForThisColor;
                }
            }

            if (totalDividendEarnings > 0)
            {
                Hashtable propsToSet = new Hashtable();
                int currentFinpoint = (int)player.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY];
                propsToSet[PlayerProfileMultiplayer.FINPOINT_KEY] = currentFinpoint + totalDividendEarnings;
                player.SetCustomProperties(propsToSet);
                Debug.Log($"[Dividen] {player.NickName} mendapatkan total {totalDividendEarnings} FP.");
            }
        }
        
        Debug.Log("✅ Pembayaran dividen selesai. Fase Resolusi berakhir.");
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.EndGame();
        }
    }
    #endregion
}