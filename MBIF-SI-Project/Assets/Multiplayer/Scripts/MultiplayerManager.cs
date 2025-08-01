// File: MultiplayerManager.cs

using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Collections;
using System.Linq; // Diperlukan untuk sorting (OrderBy)
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Database;
using Firebase.Auth;

public class MultiplayerManager : MonoBehaviourPunCallbacks
{
    public static MultiplayerManager Instance;
    private TicketManagerMultiplayer ticketManager;
    private PhotonView gameStatusView;

    [Header("Player Management")]
    public GameObject playerPrefab;
    public Transform playerContainer;

    [Header("Layout Management")]
    public List<Transform> playerPositions;
    public const string POSITION_KEY = "posIndex";

    [Header("Transisi UI")] // <-- TAMBAHKAN HEADER INI
    public CanvasGroup biddingTransitionCG;
    public CanvasGroup actionTransitionCG;
    public CanvasGroup sellingTransitionCG;
    public CanvasGroup rumorTransitionCG;
    public CanvasGroup resolutionTransitionCG;

    [Header("End Game UI")]
    public GameObject leaderboardPanel;
    public Transform leaderboardContainer;
    public GameObject leaderboardEntryPrefab;
    public Button exitGameButton;

    [Header("Firebase Setup")]
    private DatabaseReference dbReference;
    private FirebaseAuth auth;

    [Header("Semester Management")]
    public const int MAX_SEMESTERS = 4;
    public const string SEMESTER_KEY = "currentSemester";

    void Awake()
    {
        if (GameStatusUI.Instance != null)
        {
            gameStatusView = GameStatusUI.Instance.photonView;
        }

        if (Instance != null) { Destroy(gameObject); } else { Instance = this; }

        // Ambil komponen TicketManager yang ada di GameObject yang sama
        ticketManager = FindObjectOfType<TicketManagerMultiplayer>();
        
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    async void Start()
    {
        if (playerPrefab == null) return;

        // 1. Ambil playerId dari Firebase
        string localUserId = auth.CurrentUser.UserId;
        DataSnapshot snapshot = await dbReference.Child("users").Child(localUserId).Child("playerId").GetValueAsync();

        if (snapshot.Exists)
        {
            string playerId = snapshot.Value.ToString();

            // 2. Simpan playerId ke Custom Properties pemain lokal
            Hashtable playerProps = new Hashtable
            {
                { "playerId", playerId },
                { "authId", localUserId }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
            Debug.Log($"PlayerId '{playerId}' berhasil disimpan ke Custom Properties.");
        }
        else
        {
            Debug.LogError($"Tidak dapat menemukan playerId untuk user: {localUserId}");
        }

        if (playerPrefab == null) return;
        PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity);

        // Setelah membuat prefab, jika kita adalah MasterClient,
        // langsung mulai fase bidding.
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable initialProps = new Hashtable { { SEMESTER_KEY, 1 } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(initialProps);

            if (ResolutionPhaseManagerMultiplayer.Instance != null)
            {
                // Panggil fungsi yang sudah ada ini
                ResolutionPhaseManagerMultiplayer.Instance.CreateInitialTokens();

                // Tambahkan blok ini untuk menginisialisasi properti bonus
                Hashtable bonusProps = new Hashtable();
                string[] colors = { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };
                foreach (string color in colors)
                {
                    bonusProps["ipo_bonus_" + color] = 0;
                }
                PhotonNetwork.CurrentRoom.SetCustomProperties(bonusProps);

                StartCoroutine(InitialTokenReveal());
            }

            if (ticketManager != null)
            {
                Debug.Log("MasterClient memulai fase bidding dari MultiplayerManager.Start()...");
            }
            else
            {
                Debug.LogError("Referensi TicketManager tidak ditemukan di MultiplayerManager!");
            }
        }
    }

    private IEnumerator InitialTokenReveal()
    {
        // Beri jeda singkat agar properti pembuatan token tersinkronisasi
        yield return new WaitForSeconds(1.0f);

        // 1. Mulai animasi flip token seperti biasa
        if (ResolutionPhaseManagerMultiplayer.Instance != null)
        {
            // Kirim '1' karena ini adalah semester pertama
            ResolutionPhaseManagerMultiplayer.Instance.RevealTokensForCurrentSemester(1);
        }
        // 2. Beri jeda yang cukup agar animasi selesai (sama seperti di StartNewSemester)
        yield return new WaitForSeconds(2.5f);

        // 3. BARULAH mulai fase bidding setelah jeda
        if (PhotonNetwork.IsMasterClient && ticketManager != null)
        {
            yield return StartCoroutine(FadeTransition(biddingTransitionCG, 0.5f, 1f, 0.5f)); 
            Debug.Log("MasterClient memulai fase bidding SETELAH animasi token awal selesai.");
            ticketManager.InitializeBidding();
        }
    }

    public void StartNewSemester()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int currentSemester = (int)PhotonNetwork.CurrentRoom.CustomProperties[SEMESTER_KEY];

            if (currentSemester < MAX_SEMESTERS)
            {
                Debug.Log($"SEMESTER {currentSemester} SELESAI. Memulai semester berikutnya...");

                // 1. Naikkan penghitung semester
                Hashtable props = new Hashtable { { SEMESTER_KEY, currentSemester + 1 } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                Debug.LogWarning(">>> [SEMESTER BARU] Harga pasar (IPO) TIDAK direset dan akan melanjutkan dari posisi terakhir.");

                if (ResolutionPhaseManagerMultiplayer.Instance != null)
                {
                    // Kirim nilai semester BERIKUTNYA secara langsung
                    ResolutionPhaseManagerMultiplayer.Instance.RevealTokensForCurrentSemester(currentSemester + 1);
                }

                // 2. Mulai lagi dari Fase Bidding
                if (ticketManager != null)
                {
                    // Beri jeda agar animasi flip token selesai
                    StartCoroutine(StartBiddingAfterDelay(2.5f));
                }
            }
            else
            {
                Debug.Log("SEMESTER FINAL SELESAI. Mengakhiri permainan...");
                // Semester maksimum tercapai, panggil EndGame()
                EndGame();
            }
        }
    }

    public IEnumerator FadeTransition(CanvasGroup cg, float fadeInTime, float holdTime, float fadeOutTime)
    {
        // Pastikan objek aktif sebelum memulai
        cg.gameObject.SetActive(true);
        cg.alpha = 0;

        // --- FADE IN ---
        float timer = 0f;
        while (timer < fadeInTime)
        {
            cg.alpha = Mathf.Lerp(0, 1, timer / fadeInTime);
            timer += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 1;

        // --- HOLD ---
        yield return new WaitForSeconds(holdTime);

        // --- FADE OUT ---
        timer = 0f;
        while (timer < fadeOutTime)
        {
            cg.alpha = Mathf.Lerp(1, 0, timer / fadeOutTime);
            timer += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 0;

        // Nonaktifkan kembali objek setelah selesai
        cg.gameObject.SetActive(false);
    }

    private IEnumerator StartBiddingAfterDelay(float delay)
    {
        // Jeda ini memberi waktu bagi semua client untuk melihat animasi flip token
        // sebelum UI bidding muncul.
        yield return new WaitForSeconds(delay);
        ticketManager.InitializeBidding();
    }

    public void UpdatePlayerLayout()
    {
        // ... (kode di dalam sini tetap sama)

        // Setelah selesai mengatur layout, update status
        if (PhotonNetwork.IsMasterClient && gameStatusView != null)
        {
            gameStatusView.RPC("UpdateStatusText", RpcTarget.All, "Semua Pemain Siap! Fase Aksi Dimulai.");
        }

        // Setelah semua siap, mulai Fase Aksi
        ActionPhaseManager.Instance.StartActionPhase();
    }

    public int GetPlayerCount()
    {
        return PhotonNetwork.CurrentRoom.PlayerCount;
    }

    // FUNGSI BARU: Ini akan dipanggil oleh skrip bidding Anda setelah urutan ditentukan.
    public void StartPlayerPlacementPhase()
    {
        // Hanya MasterClient yang boleh menjalankan ini untuk menghindari konflik
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(UpdatePlayerLayoutBasedOnTurnOrder());
        }
    }

    private IEnumerator UpdatePlayerLayoutBasedOnTurnOrder()
    {
        Player[] players = PhotonNetwork.PlayerList;

        List<Player> sortedPlayers = players.OrderBy(p =>
            (p.CustomProperties.ContainsKey(PlayerProfileMultiplayer.TURN_ORDER_KEY)) ?
            (int)p.CustomProperties[PlayerProfileMultiplayer.TURN_ORDER_KEY] :
            int.MaxValue
        ).ToList();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            Hashtable props = new Hashtable { { POSITION_KEY, i } };
            sortedPlayers[i].SetCustomProperties(props);
        }

        // --- MODIFIKASI DI SINI ---
        if (PhotonNetwork.IsMasterClient)
        {
            // BARU: Panggil transisi sebelum fase aksi
            yield return StartCoroutine(FadeTransition(actionTransitionCG, 0.5f, 1.5f, 0.5f));

            if (RumorPhaseManagerMultiplayer.Instance != null)
            {
                RumorPhaseManagerMultiplayer.Instance.PrepareNextRumorDeck();
            }

            Debug.Log("Layout pemain selesai. Memulai Fase Aksi...");
            ActionPhaseManager.Instance.StartActionPhase();
        }
    }

    public void EndGame()
    {
        // Hanya MasterClient yang memicu penjualan akhir
        if (PhotonNetwork.IsMasterClient)
        {
            SellingPhaseManagerMultiplayer.Instance.ForceSellAllCardsForLeaderboard();
        }
    }

    // Fungsi ini akan dipanggil oleh SellingPhaseManager setelah penjualan akhir selesai
    public void ShowLeaderboard()
    {
        // Kirim RPC ke semua pemain untuk menampilkan leaderboard
        photonView.RPC("Rpc_ShowLeaderboard", RpcTarget.All);
    }

    [PunRPC]
    private void Rpc_ShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);

        // Bersihkan entri lama
        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }

        // Ambil daftar pemain dan urutkan berdasarkan investpoint
        List<Player> rankedPlayers = PhotonNetwork.PlayerList.OrderByDescending(p =>
            (int)p.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY]
        ).ToList();

        // Hanya MasterClient yang menjalankan logika penyimpanan dan pembagian poin
        if (PhotonNetwork.IsMasterClient)
        {
            // Panggil fungsi simpan data yang sudah ada
            SaveMatchHistoryToFirebase(rankedPlayers);
            // Panggil fungsi baru untuk membagikan finPoin
            DistributeFinPoinRewards(rankedPlayers);
        }

        // Buat entri leaderboard untuk setiap pemain (logika ini tidak berubah)
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            Player p = rankedPlayers[i];
            GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContainer);

            // Pastikan Anda menggunakan Text atau TextMeshPro sesuai dengan prefab Anda
            Text[] texts = entry.GetComponentsInChildren<Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = $"{i + 1}. {p.NickName}";
                texts[1].text = $"{(int)p.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY]} IP"; // IP singkatan dari InvestPoin
            }
        }
    }

    private void SaveMatchHistoryToFirebase(List<Player> rankedPlayers)
    {
        // 1. Buat ID unik untuk pertandingan ini
        string matchId = dbReference.Child("matchHistories").Push().Key;

        // 2. Siapkan data pertandingan
        long timestamp = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
        List<Dictionary<string, object>> playersData = new List<Dictionary<string, object>>();
        Dictionary<string, object> playerIndexUpdates = new Dictionary<string, object>();

        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            Player p = rankedPlayers[i];
            
            // Ambil playerId dari Custom Properties (asumsi sudah tersimpan di sana)
            string playerId = p.CustomProperties.ContainsKey("playerId") ? p.CustomProperties["playerId"].ToString() : "N/A";
            
            // Siapkan data untuk setiap pemain
            Dictionary<string, object> playerData = new Dictionary<string, object>
            {
                { "rank", i + 1 },
                { "userName", p.NickName },
                { "playerId", playerId },
                // --- PERBAIKAN TYPO ---
                { "investPoin", (int)p.CustomProperties[PlayerProfileMultiplayer.INVESTPOINT_KEY] } 
            };
            playersData.Add(playerData);

            // 3. Siapkan data untuk indeks pemain
            // Ambil 'authId' dari Custom Properties
            if (p.CustomProperties.ContainsKey("authId"))
            {
                string playerAuthId = p.CustomProperties["authId"].ToString();
                playerIndexUpdates[$"/playerMatchHistories/{playerAuthId}/{matchId}"] = true;
            }
        }

        Dictionary<string, object> matchData = new Dictionary<string, object>
        {
            { "timestamp", timestamp },
            { "players", playersData }
        };

        // 4. Simpan data utama pertandingan
        dbReference.Child("matchHistories").Child(matchId).SetValueAsync(matchData).ContinueWith(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("Gagal menyimpan matchHistories: " + task.Exception);
                return;
            }
            Debug.Log("Berhasil menyimpan matchHistories.");
        });

        // 5. Update indeks untuk semua pemain yang terlibat
        dbReference.UpdateChildrenAsync(playerIndexUpdates).ContinueWith(task => {
            if (task.IsFaulted)
            {
                // INI YANG AKAN MENAMPILKAN ERROR YANG SEBENARNYA
                Debug.LogError("Gagal menyimpan playerMatchHistories: " + task.Exception);
                return;
            }
            Debug.Log("Berhasil menyimpan playerMatchHistories.");
        });
    }

    private void DistributeFinPoinRewards(List<Player> rankedPlayers)
    {
        if (rankedPlayers.Count <= 1)
        {
            Debug.Log("Hanya ada 1 pemain, tidak ada finPoin yang dibagikan.");
            return;
        }

        Debug.Log("MasterClient mengirimkan perintah RPC untuk pembagian FinPoin...");

        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            Player targetPlayer = rankedPlayers[i];
            int rank = i + 1;

            int finPoinToAward = 0;
            if (rank == 1) finPoinToAward = 50;
            else if (rank == 2) finPoinToAward = 45;
            else if (rank == 3) finPoinToAward = 40;
            else if (rank == 4) finPoinToAward = 35;
            else if (rank == 5) finPoinToAward = 30;

            if (finPoinToAward > 0)
            {
                // Kirim RPC dengan nilai FinPoin yang akan ditambahkan
                photonView.RPC("Rpc_UpdateFinPoin", targetPlayer, finPoinToAward);
            }
        }
    }

    [PunRPC]
    private void Rpc_UpdateFinPoin(int finPoinToAdd) // Ganti nama parameter juga
    {
        string playerAuthId = auth.CurrentUser.UserId;
        DatabaseReference finPoinRef = dbReference.Child("users").Child(playerAuthId).Child("finPoin");

        finPoinRef.RunTransaction(mutableData =>
        {
            long currentPoin = 0;
            if (mutableData.Value != null)
            {
                long.TryParse(mutableData.Value.ToString(), out currentPoin);
            }

            mutableData.Value = currentPoin + finPoinToAdd; // Gunakan parameter baru
            return TransactionResult.Success(mutableData);
        });

        Debug.Log($"RPC diterima: Menambahkan {finPoinToAdd} FinPoin ke user {playerAuthId}");
    }

    public void OnExitGameButtonClicked()
    {
        Debug.Log("Tombol Exit Game ditekan.");
        // Pastikan kita terhubung ke Photon sebelum mencoba meninggalkan ruangan
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.LeaveRoom(); // Meninggalkan ruangan Photon
        }
        else
        {
            // Jika tidak terhubung ke ruangan, langsung kembali ke scene lobi (atau scene menu utama)
            SceneManager.LoadScene("MainMenu"); // Ganti "MainMenu" dengan nama scene lobi Anda
        }
    }
    
    public override void OnLeftRoom()
    {
        Debug.Log("Berhasil meninggalkan ruangan Photon. Memuat scene lobi...");
        // Setelah berhasil meninggalkan ruangan, kembali ke scene lobi
        SceneManager.LoadScene("MainMenu"); // Ganti "MainMenu" dengan nama scene lobi Anda
    }
}