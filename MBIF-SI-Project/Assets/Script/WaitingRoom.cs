using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class WaitingRoom : MonoBehaviourPunCallbacks
{
    public TMP_Text roomNameText;
    public GameObject playerListItemPrefab;
    public Transform playerListContainer;
    public Button backButton;
    public Button playButton;

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
                legacyText.text = $"{player.NickName} {role}";
            }
            else
            {
                Debug.LogWarning("Tidak ditemukan komponen Text di prefab!");
            }
        }
        
        // Setiap kali daftar pemain diperbarui, cek kembali kondisi tombol Play
        UpdatePlayButtonState();
    }

    // --- FUNGSI BARU UNTUK MENGONTROL TOMBOL PLAY ---
    private void UpdatePlayButtonState()
    {
        // Tombol Play hanya bisa diinteraksi oleh MasterClient
        if (PhotonNetwork.IsMasterClient)
        {
            // Cek apakah jumlah pemain saat ini sudah sama dengan kapasitas maksimal room
            bool isRoomFull = PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers;
            
            // Aktifkan tombol HANYA jika room sudah penuh
            playButton.interactable = isRoomFull;
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
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            PhotonNetwork.LoadLevel("Multiplayer");
        }
        else
        {
            Debug.Log("Game hanya bisa dimulai oleh Host saat room sudah penuh.");
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("MainMenu");
    }
}