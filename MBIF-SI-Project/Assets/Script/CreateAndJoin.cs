using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;

public class CreateAndJoin : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    public TMP_InputField roomNameInput;
    public GameObject roomListContainer;
    public GameObject roomListItemPrefab;

    [Header("Popups")]
    public GameObject createRoomPopup;
    public TextMeshProUGUI popupUsername; // Username untuk create room
    public TMP_InputField popupRoomNameInput;
    public TextMeshProUGUI joinUsername;  // Username untuk join room
    public TMP_InputField popupRoomPasswordInput;
    public GameObject enterPasswordPopup;
    public TMP_InputField passwordInput;

    [Header("Alerts")]
    public float invalidPopupDuration = 5f;
    public GameObject invalidPassAlert;

    [Header("Player Count Selection")]
    public Color defaultColor = Color.white;
    public Color selectedColor = Color.green;
    public Button[] playerCountButtons;

    private string selectedRoomName;
    // Cache room list agar update lebih efisien dan tidak berkedip
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    private int selectedMaxPlayers = 5; // Default
    private int selectedCount = 5; // default
    private Button selectedButton = null;

    private bool isReconnecting = false;

    // Firebase references
    private FirebaseAuth auth;
    private DatabaseReference dbRef;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(LoadUserData());

        // Cek status saat scene baru dimuat
        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (PhotonNetwork.InLobby)
            {
                // Jika sudah di lobby tapi list kosong (bug scene change), paksa refresh
                PhotonNetwork.LeaveLobby(); 
            }
            else
            {
                PhotonNetwork.JoinLobby();
            }
        }
        else if (!PhotonNetwork.IsConnected)
        {
            // Jika belum connect sama sekali, connect sekarang
            PhotonNetwork.ConnectUsingSettings();
        }
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("Tersambung ke Master Server. Sedang masuk ke Lobby...");
        isReconnecting = false;
        // Kunci utama: Setiap kali tersambung ke Master (baik awal atau setelah putus),
        // LANGSUNG masuk ke Lobby agar room list muncul.
        PhotonNetwork.JoinLobby();
    }
    private IEnumerator RetryConnection()
    {
        isReconnecting = true;
        Debug.Log("Mencoba menyambung kembali dalam 3 detik...");
        yield return new WaitForSeconds(3f); // Tunggu sebentar agar jaringan stabil

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Melakukan Reconnect...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }
    public override void OnJoinedLobby()
    {
        Debug.Log("Berhasil masuk Lobby.");
        cachedRoomList.Clear();
        ClearRoomListUI();
    }

    public override void OnLeftLobby()
    {
        Debug.Log("Keluar dari Lobby (Reset).");
        cachedRoomList.Clear();
        ClearRoomListUI();

        // FIX: Jika kita keluar lobby karena perintah 'LeaveLobby()' di Start(),
        // kita harus segera masuk lagi agar list room muncul.
        // Pengecekan 'IsConnectedAndReady' memastikan kita tidak masuk lagi jika sedang disconnect.
        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            Debug.Log("Masuk kembali ke Lobby setelah reset...");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Terputus dari server: " + cause);
        cachedRoomList.Clear();
        ClearRoomListUI();
        
        // Coba reconnect jika putus bukan disengaja
        if (cause != DisconnectCause.DisconnectByClientLogic && !isReconnecting)
        {
            StartCoroutine(RetryConnection());
        }
    }

    // --- 2. LOGIKA ROOM LIST (PUN 2 AUTOMATIC) ---

    // Fungsi ini dipanggil OTOMATIS oleh Photon jika ada perubahan list.
    // Kita TIDAK PERLU refresh manual pakai Coroutine lagi.
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log("Daftar Room diperbarui oleh Server.");

        foreach (RoomInfo room in roomList)
        {
            // Hapus dari cache jika room dihapus, tertutup, atau tidak terlihat
            if (room.RemovedFromList || !room.IsOpen || !room.IsVisible)
            {
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    cachedRoomList.Remove(room.Name);
                }
                continue;
            }

            // Update atau Tambah room ke cache
            cachedRoomList[room.Name] = room;
        }

        UpdateRoomListUI();
    }

    private void UpdateRoomListUI()
    {
        // Bersihkan UI lama
        ClearRoomListUI();

        // Render ulang dari data cache yang sudah bersih
        foreach (var kvp in cachedRoomList)
        {
            RoomInfo room = kvp.Value;

            // Logika filter tampilan: Cek apakah game sudah dimulai
            bool hasStarted = room.CustomProperties.ContainsKey("started") && (bool)room.CustomProperties["started"];

            // Buat Item UI
            GameObject item = Instantiate(roomListItemPrefab, roomListContainer.transform);
            Text roomText = item.GetComponentInChildren<Text>();

            int currentPlayers = room.PlayerCount;
            int maxPlayers = room.MaxPlayers;
            string status = "Waiting";
            bool isInteractable = true;

            if (hasStarted)
            {
                status = "In Game";
                isInteractable = false;
            }
            else if (currentPlayers >= maxPlayers)
            {
                status = "Full";
                isInteractable = false;
            }

            if (roomText != null)
            {
                roomText.text = $"{room.Name} ({currentPlayers}/{maxPlayers} - {status})";
            }

            // Atur tombol Join
            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = isInteractable;
                btn.onClick.RemoveAllListeners();

                string roomNameCopy = room.Name; // Copy string untuk closure lambda

                if (isInteractable)
                {
                    btn.onClick.AddListener(() =>
                    {
                        selectedRoomName = roomNameCopy;
                        passwordInput.text = "";
                        enterPasswordPopup.SetActive(true);
                    });
                }
            }
        }
    }

    private void ClearRoomListUI()
    {
        foreach (Transform child in roomListContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    // --- 3. CREATE ROOM LOGIC (DENGAN PROTEKSI) ---

    public void ShowCreateRoomPopup()
    {
        if (SfxManager.Instance != null) SfxManager.Instance.PlayButtonClick();

        // Cek koneksi dulu agar tidak error
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("Tidak terkoneksi ke server, tidak bisa membuat room.");
            return;
        }

        popupRoomNameInput.text = "";
        popupRoomPasswordInput.text = "";
        createRoomPopup.SetActive(true);
    }

    public void CreateRoomFromPopup()
    {
        if (SfxManager.Instance != null) SfxManager.Instance.PlayButtonClick();

        // Validasi koneksi lagi sebelum kirim request
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogError("Terputus dari jaringan. Gagal membuat room.");
            return;
        }

        string username = popupUsername.text;
        string roomName = popupRoomNameInput.text;
        string password = popupRoomPasswordInput.text;

        if (string.IsNullOrEmpty(roomName) || string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Room name and username are required!");
            return;
        }

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = (byte)selectedMaxPlayers;
        options.PlayerTtl = 0; // Jika putus di lobby/room, langsung hapus player
        options.EmptyRoomTtl = 60000; // Room bertahan 60 detik jika kosong

        // Custom Properties untuk Password dan Status Game
        options.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable()
        {
            { "pwd", password },
            { "maxP", selectedMaxPlayers },
            { "started", false }
        };
        options.CustomRoomPropertiesForLobby = new string[] { "pwd", "started", "maxP" };

        PhotonNetwork.NickName = username;
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void CancelCreateRoom()
    {
        if (SfxManager.Instance != null) SfxManager.Instance.PlayButtonClick();
        createRoomPopup.SetActive(false);
    }

    // --- 4. JOIN ROOM LOGIC ---

    public void TryJoinRoomWithPassword()
    {
        if (SfxManager.Instance != null) SfxManager.Instance.PlayButtonClick();

        // Cek apakah room masih ada di cache (validasi anti-ghost room)
        if (!cachedRoomList.ContainsKey(selectedRoomName))
        {
            Debug.LogWarning("Room tidak ditemukan atau sudah bubar!");
            enterPasswordPopup.SetActive(false);
            return;
        }

        RoomInfo targetRoom = cachedRoomList[selectedRoomName];
        string username = joinUsername.text;

        // Validasi Full/Started
        bool isFull = targetRoom.PlayerCount >= targetRoom.MaxPlayers;
        bool isStarted = targetRoom.CustomProperties.ContainsKey("started") && (bool)targetRoom.CustomProperties["started"];

        if (isFull || isStarted)
        {
            Debug.LogWarning("Room penuh atau sudah mulai.");
            enterPasswordPopup.SetActive(false);
            StartCoroutine(ShowInvalidPasswordPopup());
            return;
        }

        // Cek Password
        string correctPassword = targetRoom.CustomProperties.ContainsKey("pwd") ? targetRoom.CustomProperties["pwd"].ToString() : "";

        if (passwordInput.text == correctPassword)
        {
            PhotonNetwork.NickName = username;
            PhotonNetwork.JoinRoom(selectedRoomName);
        }
        else
        {
            Debug.LogWarning("Wrong password!");
            enterPasswordPopup.SetActive(false);
            StartCoroutine(ShowInvalidPasswordPopup());
        }
    }

    public override void OnJoinedRoom()
    {
        if (SfxManager.Instance != null) SfxManager.Instance.PlayButtonClick();
        UnityEngine.SceneManagement.SceneManager.LoadScene("WaitingRoom");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Gagal membuat room: {message} ({returnCode})");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Gagal join room: {message}");
        StartCoroutine(ShowInvalidPasswordPopup());
    }

    // --- 5. UTILITIES (SAMA SEPERTI SEBELUMNYA) ---

    public void SelectPlayerCount(int count)
    {
        selectedCount = count;
        selectedMaxPlayers = count;

        // Reset warna tombol
        foreach (Button btn in playerCountButtons)
        {
            btn.GetComponent<Image>().color = defaultColor;
        }

        // Set warna tombol terpilih
        Button selected = null;
        foreach (Button btn in playerCountButtons)
        {
            if (btn.GetComponentInChildren<TextMeshProUGUI>().text == count.ToString())
            {
                selected = btn;
                break;
            }
        }

        if (selected != null)
        {
            selected.GetComponent<Image>().color = selectedColor;
            selectedButton = selected;
        }
    }

    private IEnumerator LoadUserData()
    {
        if (auth.CurrentUser != null)
        {
            string userId = auth.CurrentUser.UserId;
            var userTask = dbRef.Child("users").Child(userId).GetValueAsync();
            yield return new WaitUntil(() => userTask.IsCompleted);

            if (userTask.Exception == null)
            {
                DataSnapshot snapshot = userTask.Result;
                if (snapshot.Exists && snapshot.Child("userName") != null)
                {
                    string userName = snapshot.Child("userName").Value.ToString();
                    popupUsername.text = userName;
                    joinUsername.text = userName;
                }
                else
                {
                    popupUsername.text = "Guest";
                }
            }
            else
            {
                popupUsername.text = "Error";
            }
        }
    }

    public void CancelPasswordPopup()
    {
        if (SfxManager.Instance != null) SfxManager.Instance.PlayButtonClick();
        enterPasswordPopup.SetActive(false);
    }

    private IEnumerator ShowInvalidPasswordPopup()
    {
        invalidPassAlert.SetActive(true);
        yield return new WaitForSeconds(invalidPopupDuration);
        invalidPassAlert.SetActive(false);
    }

    public void ConfirmInvalidPass()
    {
        invalidPassAlert.SetActive(false);
    }
}