using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class WaitingRoom : MonoBehaviourPunCallbacks
{
    public TMP_Text roomNameText;
    public GameObject playerListItemPrefab;     // Prefab yang berisi tombol & teks nama player
    public Transform playerListContainer;       // Parent object (misalnya ScrollView Content)
    public Button backButton;
    public Button playButton;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true; // Sync scene across all players
        // Tampilkan nama room yang sedang diikuti
        if (PhotonNetwork.InRoom)
    {
        string roomName = PhotonNetwork.CurrentRoom.Name;
        int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

        // Coba ambil dari CustomProperties kalau ada
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("maxP"))
        {
            maxPlayers = (int)PhotonNetwork.CurrentRoom.CustomProperties["maxP"];
        }

        roomNameText.text = $"Room: {roomName} ({maxPlayers} Player)";
        UpdatePlayerList();
    }
        // Tambahkan listener tombol
        backButton.onClick.AddListener(OnBackButtonClicked);
        playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    private void UpdatePlayerList()
    {
        // Bersihkan isi lama
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        // Loop semua player yang ada di room sekarang
        foreach (Photon.Realtime.Player player in Photon.Pun.PhotonNetwork.PlayerList)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContainer);

            // Coba ambil komponen teks dari prefab
            Text legacyText = item.GetComponentInChildren<Text>(true); // true untuk include inactive
            if (legacyText != null)
            {
                // Cek apakah player ini adalah MasterClient (host room)
                string role = (player == PhotonNetwork.MasterClient) ? "(Host)" : "(Guest)";
                legacyText.text = $"{player.NickName} {role}";
            }
            else
            {
                Debug.LogWarning("Tidak ditemukan komponen Text di prefab!");
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList(); // Pemain baru masuk
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList(); // Pemain keluar
    }

    public override void OnJoinedRoom()
    {
        UpdatePlayerList(); // Inisialisasi awal
    }

    private void OnBackButtonClicked()
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.PlayButtonClick();

        PhotonNetwork.LeaveRoom(); // Keluar dari room sebelum ke main menu
    }

    private void OnPlayButtonClicked() // Ubah ke public untuk testing
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.PlayButtonClick();

        // Hanya MasterClient yang bisa mulai game
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Multiplayer"); // Pastikan scene Gameplay sudah ada di Build Settings
        }
        else
        {
            Debug.Log("Only MasterClient can start the game.");
        }
    }

    public override void OnLeftRoom()
    {
        // Saat sudah berhasil keluar room, kembali ke scene MainMenu
        SceneManager.LoadScene("MainMenu");
    }
}
