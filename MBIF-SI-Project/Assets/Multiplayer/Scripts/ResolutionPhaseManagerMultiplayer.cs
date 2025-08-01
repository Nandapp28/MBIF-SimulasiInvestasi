// File: ResolutionPhaseManagerMultiplayer.cs (Versi Perbaikan Final)
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ResolutionPhaseManagerMultiplayer : MonoBehaviourPunCallbacks
{
    private HashSet<GameObject> flippedTokens = new HashSet<GameObject>();
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

    // Kunci untuk Room Custom Properties
    private const string RES_TOKENS_PREFIX = "res_tokens_";
    private const string DIVIDEND_INDEX_PREFIX = "div_index_";

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
    #endregion

    #region Phase Logic

    // Fungsi ini dipanggil dari MultiplayerManager di awal permainan
    public void CreateInitialTokens()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning(">>> [GAME START] Membuat semua 16 token resolusi untuk 4 semester.");
            Hashtable roomProps = new Hashtable();
            int[] possibleTokens = { -2, -1, 1, 2 };

            foreach (string color in resolutionOrder)
            {
                roomProps[DIVIDEND_INDEX_PREFIX + color] = 0;

                List<int> tokens = new List<int>();
                for (int i = 0; i < 4; i++)
                {
                    tokens.Add(possibleTokens[Random.Range(0, possibleTokens.Length)]);
                }
                roomProps[RES_TOKENS_PREFIX + color] = tokens.ToArray();
            }
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }
    }

    // Fungsi ini dipanggil setiap awal Fase Resolusi
    public void StartResolutionPhase()
    {
        // --- TAMBAHKAN BLOK INI ---
        if (GameStatusUI.Instance != null)
        {
            GameStatusUI.Instance.photonView.RPC("UpdateStatusText", RpcTarget.All, "Fase Resolusi: Efek token dan dividen diproses...");
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient memulai Fase Resolusi...");
            StartCoroutine(ProcessRevealedTokens());
        }
    }

    private IEnumerator ProcessRevealedTokens()
    {
        if (!PhotonNetwork.IsMasterClient) yield break;

        int currentSemester = (int)PhotonNetwork.CurrentRoom.CustomProperties["currentSemester"];
        int tokenIndexToProcess = currentSemester - 1; // Menggunakan nama variabel yg lebih jelas

        Debug.Log($"Memulai urutan proses resolusi untuk SEMESTER {currentSemester}. Memproses token di indeks {tokenIndexToProcess}.");
        yield return new WaitForSeconds(2f);

        foreach (string color in resolutionOrder)
        {
            // =========================================================================
            // --- INTI PERUBAHAN ---
            // HAPUS atau KOMENTARI baris RPC ini. Animasi sudah dilakukan di awal.
            // photonView.RPC("Rpc_RevealSpecificToken", RpcTarget.All, color, tokenIndexToReveal);
            // -------------------------------------------------------------------------

            // Sekarang kita hanya perlu memberi jeda agar pemain fokus pada token yang efeknya akan diaktifkan.
            Debug.Log($"[MasterClient] Memproses efek untuk token {color}...");
            yield return new WaitForSeconds(2.5f); // Jeda tetap penting untuk flow permainan

            // Logika untuk menerapkan efek tetap berjalan seperti biasa.
            ApplyTokenEffect(color, tokenIndexToProcess);
            yield return new WaitForSeconds(1.5f);
        }

        Debug.Log("Semua efek token untuk semester ini telah diterapkan. Memproses pembayaran dividen...");
        ProcessDividendPayouts();
    }

    #endregion

    #region Visuals & RPCs

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

    public void RevealTokensForCurrentSemester(int semesterToReveal)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Gunakan parameter langsung, bukan membaca dari properti yang mungkin usang
        int tokenIndexToReveal = semesterToReveal - 1;

        Debug.Log($"[MasterClient] Memerintahkan semua pemain untuk membuka token semester {semesterToReveal} (index: {tokenIndexToReveal}).");

        photonView.RPC("Rpc_AnimateAllSemesterTokens", RpcTarget.All, tokenIndexToReveal);
    }

    [PunRPC]
    private void Rpc_AnimateAllSemesterTokens(int tokenIndex)
    {
        Debug.Log($"[SEMUA PEMAIN] Memulai animasi flip untuk semua token di indeks {tokenIndex}.");
        // Jalankan coroutine agar token terbuka satu per satu, bukan serentak.
        StartCoroutine(FlipAllTokensSequentially(tokenIndex));
    }

    // --- COROUTINE BARU UNTUK MENGATUR URUTAN ANIMASI ---
    private IEnumerator FlipAllTokensSequentially(int tokenIndex)
    {
        // Jeda awal sebelum animasi pertama
        yield return new WaitForSeconds(0.5f);

        foreach (string color in resolutionOrder)
        {
            // Panggil logika untuk membuka satu token
            RevealSingleTokenVisual(color, tokenIndex);
            // Beri jeda antar token agar animasinya terlihat bagus
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void RevealSingleTokenVisual(string color, int tokenIndex)
    {
        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        int[] tokens = (int[])roomProps[RES_TOKENS_PREFIX + color];
        if (tokenIndex < 0 || tokenIndex >= tokens.Length) return;
        int tokenValue = tokens[tokenIndex];

        List<GameObject> tokenList = null;
        if (color == "Konsumer") tokenList = tokenObjectsKonsumer;
        else if (color == "Infrastruktur") tokenList = tokenObjectsInfrastruktur;
        else if (color == "Keuangan") tokenList = tokenObjectsKeuangan;
        else if (color == "Tambang") tokenList = tokenObjectsTambang;

        if (tokenList != null && tokenIndex < tokenList.Count)
        {
            GameObject tokenToFlip = tokenList[tokenIndex];
            Material targetMaterial = tokenMaterials.FirstOrDefault(m => m.value == tokenValue)?.material;

            if (tokenToFlip != null && targetMaterial != null)
            {
                // 1. Ganti pengecekan nama dengan pengecekan apakah token SUDAH ADA di dalam daftar 'flippedTokens'
                if (!flippedTokens.Contains(tokenToFlip))
                {
                    // 2. Jika belum ada (belum di-flip), jalankan animasi
                    StartCoroutine(FlipToken(tokenToFlip, targetMaterial));

                    // 3. Setelah animasi dimulai, langsung tambahkan token ke daftar agar tidak di-flip lagi
                    flippedTokens.Add(tokenToFlip);
                }
            }
        }
    }

    // Fungsi ini dipanggil dari RPC lama, bisa kita hapus atau biarkan kosong.
    // Sebaiknya kita biarkan agar OnRoomPropertiesUpdate tidak error jika masih ada.
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Cek apakah ada properti dividen yang berubah
        foreach (var prop in propertiesThatChanged)
        {
            if (prop.Key.ToString().StartsWith(DIVIDEND_INDEX_PREFIX))
            {
                // Jika ada, update semua visual indikator
                UpdateAllDividendVisuals();
                break; // Cukup cek sekali saja
            }
        }
    }

    #endregion

    #region Core Logic

    // Perbaikan: Fungsi ini sekarang menerima tokenIndex
    private void ApplyTokenEffect(string color, int tokenIndex)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        string divKey = DIVIDEND_INDEX_PREFIX + color;
        string tokKey = RES_TOKENS_PREFIX + color;

        int[] tokens = (int[])roomProps[tokKey];
        int dividendIndex = (int)roomProps[divKey];
        int tokenEffect = tokens[tokenIndex];

        dividendIndex += tokenEffect;

        // Logika overflow sekarang memanggil fungsi terpusat
        if (dividendIndex > 3)
        {
            // Panggil fungsi terpusat di SellingPhaseManager untuk menaikkan IPO
            SellingPhaseManagerMultiplayer.Instance.ModifyIPOIndex(color, 1);
            dividendIndex = 0; // Reset dividend index setelah overflow
        }
        else if (dividendIndex < -3)
        {
            // Panggil fungsi terpusat di SellingPhaseManager untuk menurunkan IPO (ini bisa memicu CRASH)
            SellingPhaseManagerMultiplayer.Instance.ModifyIPOIndex(color, -1);
            dividendIndex = 0; // Reset dividend index setelah overflow
        }

        // Set properti untuk dividendIndex saja
        Hashtable propsToSet = new Hashtable { { divKey, dividendIndex } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(propsToSet);
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
                int positionIndex = dividendIndex + 3; // Mengubah rentang -3 s/d 3 menjadi 0 s/d 6

                if (positionIndex >= 0 && positionIndex < mapping.positionSlots.Count)
                {
                    Transform targetSlot = mapping.positionSlots[positionIndex];
                    if (targetSlot != null)
                    {
                        // Pindahkan objek indikator ke posisi slot yang benar
                        mapping.indicatorObject.transform.position = targetSlot.position;
                        mapping.indicatorObject.SetActive(true);
                    }
                }
            }
        }
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

                    totalDividendEarnings += earningsForThisColor;
                }
            }

            if (totalDividendEarnings > 0)
            {
                Hashtable propsToSet = new Hashtable();
                int currentInvestpoint = (int)player.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY];
                propsToSet[PlayerProfileMultiplayer.INVESTPOINT_KEY] = currentInvestpoint + totalDividendEarnings;
                player.SetCustomProperties(propsToSet);
                Debug.Log($"[Dividen] {player.NickName} mendapatkan total {totalDividendEarnings} FP.");
            }
        }

        Debug.Log("✅ Pembayaran dividen selesai. Fase Resolusi berakhir.");
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.StartNewSemester();
        }
    }
    #endregion
}