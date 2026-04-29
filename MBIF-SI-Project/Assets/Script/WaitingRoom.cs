using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.SceneManagement;

public class WaitingRoom : MonoBehaviourPunCallbacks
{
    public TMP_Text roomNameText;
    public GameObject playerListItemPrefab;
    public Transform playerListContainer;
    public Button backButton;
    public Button playButton;

    private const byte KICK_EVENT_CODE = 199;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        if (PhotonNetwork.InRoom)
        {
            string roomName = PhotonNetwork.CurrentRoom.Name;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("maxP"))
            {
                maxPlayers = (int)PhotonNetwork.CurrentRoom.CustomProperties["maxP"];
            }

            roomNameText.text = $"Room: {roomName} ({maxPlayers} Player)";
            UpdatePlayerList();
        }

        backButton.onClick.AddListener(OnBackButtonClicked);
        playButton.onClick.AddListener(OnPlayButtonClicked);

        // Panggil pengecekan awal saat masuk scene
        UpdatePlayButtonState();
    }
    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
    }

    // --- FUNGSI PENERIMA EVENT (Dieksekusi oleh pemain yang di-kick) ---
    private void OnEvent(EventData photonEvent)
    {
        // Jika event yang diterima adalah kode KICK dari Host
        if (photonEvent.Code == KICK_EVENT_CODE)
        {
            Debug.Log("Anda telah di-kick oleh Host. Keluar dari Room...");
            
            // Perintah ini HANYA mengeluarkan pemain dari room, TIDAK memutuskan koneksi server
            PhotonNetwork.LeaveRoom(); 
        }
    }

    private void UpdatePlayerList()
    {
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (Photon.Realtime.Player player in Photon.Pun.PhotonNetwork.PlayerList)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContainer);
            Text legacyText = item.GetComponentInChildren<Text>(true);
            if (legacyText != null)
            {
                string role = (player.IsMasterClient) ? "(Host)" : "(Guest)";
                legacyText.text = $"{player.NickName}\n{role}";
            }
            else
            {
                Debug.LogWarning("Tidak ditemukan komponen Text di prefab!");
            }
            Transform kickButtonTransform = FindChildRecursively(item.transform, "KickButton");
            
            if (kickButtonTransform != null)
            {
                Button kickButton = kickButtonTransform.GetComponent<Button>();
                
                // Tampilkan tombol KICK HANYA jika yang melihat adalah HOST dan bukan dirinya sendiri
                if (PhotonNetwork.IsMasterClient && !player.IsLocal)
                {
                    kickButton.gameObject.SetActive(true);
                    kickButton.onClick.RemoveAllListeners();
                    kickButton.onClick.AddListener(() => OnKickButtonClicked(player));
                }
                else
                {
                    kickButton.gameObject.SetActive(false);
                }
            }
        }
        
        // Setiap kali daftar pemain diperbarui, cek kembali kondisi tombol Play
        UpdatePlayButtonState();
    }
    // Fungsi pembantu untuk mencari objek tombol KickButton di dalam Prefab
    private Transform FindChildRecursively(Transform parent, string exactName)
    {
        if (parent.name == exactName) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursively(child, exactName);
            if (found != null) return found;
        }
        return null;
    }

    // --- FUNGSI KICK (Dieksekusi oleh HOST) ---
    private void OnKickButtonClicked(Player targetPlayer)
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.PlayButtonClick();

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log($"Mengirim perintah LeaveRoom ke pemain: {targetPlayer.NickName}");
            
            // Konfigurasi agar pesan (Event) HANYA dikirimkan ke pemain target
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { TargetActors = new int[] { targetPlayer.ActorNumber } };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            
            // Kirim pesan KICK
            PhotonNetwork.RaiseEvent(KICK_EVENT_CODE, null, raiseEventOptions, sendOptions);
        }
    }

    // --- FUNGSI BARU UNTUK MENGONTROL TOMBOL PLAY ---
    private void UpdatePlayButtonState()
    {
        // Tombol Play hanya bisa diinteraksi oleh MasterClient
        if (PhotonNetwork.IsMasterClient)
        {
            // Cek apakah jumlah pemain saat ini sudah minimal 2
            bool hasEnoughPlayers = PhotonNetwork.CurrentRoom.PlayerCount >= 2;
            
            // Aktifkan tombol HANYA jika pemain sudah cukup (minimal 2)
            playButton.interactable = hasEnoughPlayers;
        }
        else
        {
            // Jika bukan MasterClient, tombol Play selalu non-aktif
            playButton.interactable = false;
        }
    }
    // ------------------------------------------------

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    // --- TAMBAHAN: PENTING UNTUK HOST MIGRATION ---
    // Dipanggil saat MasterClient lama keluar dan MasterClient baru terpilih
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // Update state tombol untuk semua orang, terutama untuk host yang baru
        UpdatePlayButtonState();
    }
    // ---------------------------------------------

    public override void OnJoinedRoom()
    {
        UpdatePlayerList();
    }

    private void OnBackButtonClicked()
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.PlayButtonClick();

        PhotonNetwork.LeaveRoom();
    }

    private void OnPlayButtonClicked()
    {
        // Pengecekan ini tetap ada sebagai lapisan keamanan kedua
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            // --- TAMBAHAN PENTING ---
            PhotonNetwork.CurrentRoom.PlayerTtl = -1;
            Debug.Log("PlayerTTL diubah menjadi -1 (In-Game).");
            // 1. Siapkan properti yang ingin diubah
            var customProps = new ExitGames.Client.Photon.Hashtable();
            customProps["started"] = true;
            
            // 2. Set properti room
            PhotonNetwork.CurrentRoom.SetCustomProperties(customProps);

            // 3. (Direkomendasikan) Tutup room agar tidak terlihat/bisa dijoini dari lobi
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = true;
            // -------------------------

            PhotonNetwork.LoadLevel("LoadingSceneMP"); // 4. Baru pindah scene
        }
        else
        {
            Debug.Log("Game hanya bisa dimulai oleh Host jika minimal ada 2 pemain.");
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Play");
    }
}