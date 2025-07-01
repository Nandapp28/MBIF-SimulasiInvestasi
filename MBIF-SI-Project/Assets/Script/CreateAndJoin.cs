using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro UI
using System.Collections;
using System.Collections.Generic;
using Firebase.Auth;
using Firebase.Database;

public class CreateAndJoin : MonoBehaviourPunCallbacks
{
    public TMP_InputField roomNameInput;
    public GameObject roomListContainer;
    public GameObject roomListItemPrefab;
    public GameObject createRoomPopup;
    public TextMeshProUGUI popupUsername; // Username untuk create room
    public TMP_InputField popupRoomNameInput;
    public TextMeshProUGUI joinUsername;  // Username untuk join room
    public TMP_InputField popupRoomPasswordInput;
    public GameObject enterPasswordPopup;
    public TMP_InputField passwordInput;
    public float invalidPopupDuration = 5f; // durasi tampil (detik)
    public GameObject invalidPassAlert;
    public Color defaultColor = Color.white;
    public Color selectedColor = Color.green;
    public Button[] playerCountButtons; // Tambahkan ini di atas
    private string selectedRoomName;
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();
    private Coroutine autoRefreshCoroutine;
    private int selectedMaxPlayers = 5; // Default
    private int selectedCount = 5; // default
    private Button selectedButton = null;

     // Firebase references
    private FirebaseAuth auth;
    private DatabaseReference dbRef;

    private void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinLobby();
            autoRefreshCoroutine = StartCoroutine(AutoRefreshRoomList());
        }

        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        StartCoroutine(LoadUserData());
    }

    private IEnumerator LoadUserData()
    {
        if (auth.CurrentUser != null)
        {
            string userId = auth.CurrentUser.UserId;

            var userTask = dbRef.Child("users").Child(userId).GetValueAsync(); // ← disesuaikan

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
                Debug.LogWarning("Gagal mengambil data user dari Firebase: " + userTask.Exception);
                popupUsername.text = "Error";
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        StartCoroutine(WaitForLobbyThenStartRefresh());
    }

    private IEnumerator WaitForLobbyThenStartRefresh()
    {
        // Tunggu sampai masuk lobby
        while (PhotonNetwork.NetworkClientState != ClientState.JoinedLobby)
        {
            yield return null;
        }

        autoRefreshCoroutine = StartCoroutine(AutoRefreshRoomList());
    }


    public void SelectPlayerCount(int count)
    {
        selectedCount = count;
        selectedMaxPlayers = count;

        // Reset semua tombol ke default color
        foreach (Button btn in playerCountButtons)
        {
            btn.GetComponent<Image>().color = defaultColor;
        }

        // Temukan tombol sesuai count yang dipilih
        Button selected = null;
        foreach (Button btn in playerCountButtons)
        {
            if (btn.GetComponentInChildren<TextMeshProUGUI>().text == count.ToString())
            {
                selected = btn;
                break;
            }
        }

        // Ubah warna tombol yang dipilih
        if (selected != null)
        {
            selected.GetComponent<Image>().color = selectedColor;
            selectedButton = selected;
        }

        Debug.Log("Selected player count: " + selectedCount);
    }

    public void CreateRoom()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = (byte)selectedMaxPlayers;
        options.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable()
        {
            { "maxP", selectedMaxPlayers }
        };
        options.CustomRoomPropertiesForLobby = new string[] { "maxP" };

        PhotonNetwork.CreateRoom(roomNameInput.text, options);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform child in roomListContainer.transform)
        {
            Destroy(child.gameObject);
        }

        cachedRoomList.Clear();

        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
                continue;

            GameObject item = Instantiate(roomListItemPrefab, roomListContainer.transform);
            Text roomText = item.GetComponentInChildren<Text>();

            int currentPlayers = room.PlayerCount;
            int maxPlayers = room.MaxPlayers;

            string status = "Waiting";
            bool isInteractable = true;

            // Cek apakah room sudah dimulai
            if (room.CustomProperties.ContainsKey("started") && (bool)room.CustomProperties["started"])
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

            // Ambil dan atur button
            Button btn = item.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = isInteractable;
                btn.onClick.RemoveAllListeners(); // Hapus listener sebelumnya

                // Simpan nama room ke variabel lokal agar tidak ter-overwrite di lambda
                string roomNameCopy = room.Name;

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

            cachedRoomList[room.Name] = room;
        }
    }

    public override void OnJoinedRoom()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        if (autoRefreshCoroutine != null)
        {
            StopCoroutine(autoRefreshCoroutine);
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("WaitingRoom");
    }

    public void TryJoinRoomWithPassword()
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.PlayButtonClick();

        string username = joinUsername.text;

        RoomInfo targetRoom = null;

        foreach (var room in cachedRoomList.Values)
        {
            if (room.Name == selectedRoomName)
            {
                targetRoom = room;
                break;
            }
        }

        if (targetRoom != null)
        {
            // Cegah join jika room sudah full
            if (targetRoom.PlayerCount >= targetRoom.MaxPlayers)
            {
                Debug.LogWarning("Room is full!");
                enterPasswordPopup.SetActive(false);
                StartCoroutine(ShowInvalidPasswordPopup());
                return;
            }

            // Cegah join jika room sudah dimulai
            if (targetRoom.CustomProperties.ContainsKey("started") && (bool)targetRoom.CustomProperties["started"])
            {
                Debug.LogWarning("Room has already started!");
                enterPasswordPopup.SetActive(false);
                StartCoroutine(ShowInvalidPasswordPopup());
                return;
            }

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
        else
        {
            Debug.LogWarning("Room not found!");
            enterPasswordPopup.SetActive(false);
        }
    }


    public void CancelPasswordPopup()
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.PlayButtonClick();

        enterPasswordPopup.SetActive(false);
    }

    public void RefreshRoomList()
    {
        // Hindari LeaveLobby jika belum selesai masuk ke lobby
        if (PhotonNetwork.NetworkClientState == ClientState.JoinedLobby)
        {
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.JoinLobby(); // Join lagi untuk memperbarui room list
        }
        else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby && PhotonNetwork.NetworkClientState != ClientState.JoiningLobby)
        {
            PhotonNetwork.JoinLobby(); // Join jika belum di lobby
        }
    }

    private IEnumerator AutoRefreshRoomList()
    {
        while (true)
        {
            RefreshRoomList();
            yield return new WaitForSeconds(1f); // Ulangi setiap 1 detik
        }
    }

    public void ShowCreateRoomPopup()
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.PlayButtonClick();

        popupRoomNameInput.text = "";
        popupRoomPasswordInput.text = "";
        createRoomPopup.SetActive(true);
    }

    // 🔘 Dipanggil dari popup: button "Create Room"
    public void CreateRoomFromPopup()
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.PlayButtonClick();

        string username = popupUsername.text;
        string roomName = popupRoomNameInput.text;
        string password = popupRoomPasswordInput.text;

        if (string.IsNullOrEmpty(roomName) || string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Room name and username are required!");
            // Kamu bisa tambahkan UI warning di sini
            return;
        }

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = (byte)selectedMaxPlayers;

        // Simpan password di custom properties
        options.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable()
        {
            { "pwd", password },
            { "maxP", selectedMaxPlayers },
            { "started", false } // ⬅️ Tambahkan ini
        };
        options.CustomRoomPropertiesForLobby = new string[] { "pwd", "started", "maxP" };

        PhotonNetwork.NickName = username; // Simpan username sementara ke Photon
        PhotonNetwork.CreateRoom(roomName, options);
    }

    // 🔘 Dipanggil dari popup: button "Cancel"
    public void CancelCreateRoom()
    {
        if (SfxManager.Instance != null)
            SfxManager.Instance.PlayButtonClick();

        createRoomPopup.SetActive(false);
    }

    private IEnumerator ShowInvalidPasswordPopup()
    {
        // Tampilkan popup
        invalidPassAlert.SetActive(true);

        // Tunggu selama beberapa detik (durasi diatur oleh variabel invalidPopupDuration)
        yield return new WaitForSeconds(invalidPopupDuration);

        // Sembunyikan popup kembali
        invalidPassAlert.SetActive(false);

        // Kembali ke lobby jika belum berada di dalamnya
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public void ConfirmInvalidPass()
    {
        invalidPassAlert.SetActive(false);
    }

    void Update()
    {
        Debug.Log("Photon State: " + PhotonNetwork.NetworkClientState);
    }
    
    void ResetPlayerCountButtonColors()
    {
        foreach (Button btn in playerCountButtons)
        {
            var colors = btn.colors;
            colors.normalColor = defaultColor;
            btn.colors = colors;
        }
    }
}