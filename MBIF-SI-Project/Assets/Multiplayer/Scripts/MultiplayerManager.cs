// File: MultiplayerManager.cs

using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq; // Diperlukan untuk sorting (OrderBy)
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;

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

    [Header("End Game UI")]
    public GameObject leaderboardPanel;
    public Transform leaderboardContainer;
    public GameObject leaderboardEntryPrefab;
    public Button exitGameButton;

    void Awake()
    {
        if (GameStatusUI.Instance != null)
        {
            gameStatusView = GameStatusUI.Instance.photonView;
        }

        if (Instance != null) { Destroy(gameObject); } else { Instance = this; }

        // Ambil komponen TicketManager yang ada di GameObject yang sama
        ticketManager = FindObjectOfType<TicketManagerMultiplayer>();
    }

    void Start()
    {
        if (playerPrefab == null) return;
        PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity);

        // Setelah membuat prefab, jika kita adalah MasterClient,
        // langsung mulai fase bidding.
        if (PhotonNetwork.IsMasterClient)
        {
            if (ticketManager != null)
            {
                Debug.Log("MasterClient memulai fase bidding dari MultiplayerManager.Start()...");
                ticketManager.InitializeBidding();
            }
            else
            {
                Debug.LogError("Referensi TicketManager tidak ditemukan di MultiplayerManager!");
            }
        }
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
            UpdatePlayerLayoutBasedOnTurnOrder();
        }
    }

    // LOGIKA BARU: Mengatur posisi berdasarkan nomor urut, bukan daftar pemain default.
    private void UpdatePlayerLayoutBasedOnTurnOrder()
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

        // --- TAMBAHKAN LOGIKA INI DI AKHIR FUNGSI ---
        // Setelah semua posisi pemain diatur, panggil fase aksi
        if (PhotonNetwork.IsMasterClient)
        {
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

        // Ambil daftar pemain dan urutkan berdasarkan finpoint
        List<Player> rankedPlayers = PhotonNetwork.PlayerList.OrderByDescending(p =>
            (int)p.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY]
        ).ToList();

        // Buat entri leaderboard untuk setiap pemain
        for (int i = 0; i < rankedPlayers.Count; i++)
        {
            Player p = rankedPlayers[i];
            GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContainer);

            Text[] texts = entry.GetComponentsInChildren<Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = $"{i + 1}. {p.NickName}";
                texts[1].text = $"{(int)p.CustomProperties[PlayerProfileMultiplayer.FINPOINT_KEY]} FP";
            }
        }
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