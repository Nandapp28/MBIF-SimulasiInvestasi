using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Untuk Button
using Photon.Pun; // Untuk PhotonNetwork
using Photon.Realtime; // Untuk ClientState, RoomInfo, Player
using TMPro; // SANGAT PENTING untuk TMP_InputField dan TextMeshProUGUI

public class FeaturesTests : IMonoBehaviourTest
{
    private bool m_IsFinished;
    public bool IsTestFinished { get { return m_IsFinished; } set { m_IsFinished = value; } }

    // [UnitySetUp] akan dijalankan sebelum setiap metode pengujian.
    // Memastikan setiap tes dimulai dari kondisi yang bersih dan scene yang benar.
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Debug.Log("--- Test Setup Started ---");
        // Pastikan tidak ada koneksi Photon dari tes sebelumnya
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
        }

        // Muat scene Lobby (tempat CreateAndJoin berada) untuk memulai pengujian dari sana
        SceneManager.LoadScene("Lobby"); // 
        yield return null; // Tunggu satu frame agar scene dimuat

        // Pastikan SfxManagerDummy ada di scene test
        if (SfxManagerDummy.Instance == null)
        {
            GameObject sfxManagerGO = new GameObject("SfxManagerDummy");
            sfxManagerGO.AddComponent<SfxManagerDummy>();
        }
        yield return null; // Beri waktu untuk Start() SfxManagerDummy berjalan

        Debug.Log("--- Test Setup Completed ---");
    }

    // [UnityTearDown] akan dijalankan setelah setiap metode pengujian.
    // Membersihkan kondisi setelah tes.
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Debug.Log("--- Test Teardown Initiated ---");
        // Pastikan terputus dari Photon setelah tes
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            yield return new WaitUntil(() => !PhotonNetwork.IsConnected);
        }
        // Hancurkan semua objek sementara yang mungkin dibuat oleh tes
        GameObject[] testObjects = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject go in testObjects)
        {
            if (go.name.Contains("Test") || go.name.Contains("Dummy"))
            {
                Object.Destroy(go);
            }
        }
        yield return null;
        Debug.Log("--- Test Teardown Completed ---");
    }

    // --- TEST CASE UNTUK FITUR: MEMBUAT LOBBY ---
    // Akan menguji tombol "Create Button" di scene Lobby, mengisi popup, dan menekan tombol "Create Room" di popup
    [UnityTest]
    public IEnumerator TC_001_CreateLobby()
    {
        m_IsFinished = false;

        // 0. Pastikan klien terhubung ke Master Server dan Lobby
        PhotonNetwork.ConnectUsingSettings();
        yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.JoinedLobby);
        // Tambahkan jeda singkat untuk memastikan state stabil setelah JoinedLobby
        yield return new WaitForSeconds(0.5f); // Jeda 0.5 detik
        Assert.IsTrue(PhotonNetwork.IsConnected, "Failed to connect to Photon Master Server/Lobby before attempting to create room.");

        // 1. Temukan tombol "Create Button" di scene Lobby dan klik
        Button createButton = GameObject.Find("Create Button")?.GetComponent<Button>();
        Assert.IsNotNull(createButton, "Tombol 'Create Button' tidak ditemukan di scene Lobby!");
        createButton.onClick.Invoke();
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah klik
        yield return null; // Tunggu 1 frame setelah klik untuk popup muncul

        // 2. Verifikasi popup "Create Room" muncul dan temukan input fields serta tombol di dalamnya
        GameObject createRoomPopupPanel = GameObject.Find("CreateRoomPopup");
        Assert.IsNotNull(createRoomPopupPanel, "Popup 'CreateRoomPopup' tidak ditemukan!");
        Assert.IsTrue(createRoomPopupPanel.activeSelf, "Popup 'CreateRoomPopup' tidak aktif setelah tombol diklik!");

        TMP_InputField roomNameInputField = createRoomPopupPanel.transform.Find("RoomNameInput")?.GetComponent<TMP_InputField>();
        Assert.IsNotNull(roomNameInputField, "Input field 'RoomNameInput' tidak ditemukan di dalam popup!");

        TMP_InputField roomPasswordInputField = createRoomPopupPanel.transform.Find("PasswordInput")?.GetComponent<TMP_InputField>();
        Assert.IsNotNull(roomPasswordInputField, "Input field 'PasswordInput' tidak ditemukan di dalam popup!");

        Button createRoomPopupButton = createRoomPopupPanel.transform.Find("Create Room")?.GetComponent<Button>();
        Assert.IsNotNull(createRoomPopupButton, "Tombol 'Create Room' tidak ditemukan di dalam popup!");

        // 3. Isi input fields
        string lobbyName = "AutoTestLobby";
        string lobbyPassword = "test123";

        roomNameInputField.text = lobbyName;
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah isi nama room
        
        roomPasswordInputField.text = lobbyPassword;
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah isi password room
        yield return null; // Beri waktu 1 frame setelah mengisi teks

        // 4. Klik tombol "Create Room" di dalam popup
        createRoomPopupButton.onClick.Invoke();
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah klik tombol Create Room di popup

        // 5. Verifikasi koneksi Photon dan masuk ke lobby
        yield return new WaitUntil(() => PhotonNetwork.InRoom); // Tunggu sampai masuk room
        Assert.IsTrue(PhotonNetwork.InRoom, "Gagal masuk ke lobby setelah mengisi form dan klik 'Create Room' di popup.");
        Assert.AreEqual(lobbyName, PhotonNetwork.CurrentRoom.Name, "Nama lobby yang dibuat tidak sesuai.");

        Debug.Log($"TC_001_CreateLobby: Alur pembuatan lobby berhasil untuk '{lobbyName}'.");

        m_IsFinished = true;
    }


    // --- TEST CASE UNTUK FITUR: BERGABUNG LOBBY ---
    // Akan menguji tombol "Join Room" di scene Lobby
    [UnityTest]
    public IEnumerator TC_002_JoinRoom()
    {
        m_IsFinished = false;

        // 0. Pastikan klien terhubung ke Master Server dan Lobby
        PhotonNetwork.ConnectUsingSettings();
        yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.JoinedLobby);
        // Tambahkan jeda singkat untuk memastikan state stabil setelah JoinedLobby
        yield return new WaitForSeconds(0.5f); // Jeda 0.5 detik
        Assert.IsTrue(PhotonNetwork.IsConnected, "Failed to connect to Photon Master Server/Lobby before attempting to create room.");

        // 1. Temukan tombol "Room(Clone)" di scene Lobby dan klik
        Button selectRoom = GameObject.Find("Room(Clone)")?.GetComponent<Button>();
        Assert.IsNotNull(selectRoom, "Tombol 'Room(Clone)' tidak ditemukan di scene Lobby!");
        selectRoom.onClick.Invoke();
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah klik
        yield return null; // Tunggu 1 frame setelah klik untuk popup muncul

        // 2. Verifikasi popup "Join Room" muncul dan temukan input fields serta tombol di dalamnya
        GameObject joinRoomPopUpPanel = GameObject.Find("JoinRoomPopUp");
        Assert.IsNotNull(joinRoomPopUpPanel, "Popup 'JoinRoomPopUp' tidak ditemukan!");
        Assert.IsTrue(joinRoomPopUpPanel.activeSelf, "Popup 'JoinRoomPopUp' tidak aktif setelah tombol diklik!");

        TMP_InputField passwordRoomInputField = joinRoomPopUpPanel.transform.Find("PasswordRoomInput")?.GetComponent<TMP_InputField>();
        Assert.IsNotNull(passwordRoomInputField, "Input field 'PasswordRoomInput' tidak ditemukan di dalam popup!");

        Button joinRoomButton = joinRoomPopUpPanel.transform.Find("Join Room")?.GetComponent<Button>();
        Assert.IsNotNull(joinRoomButton, "Tombol 'Join Room' tidak ditemukan di dalam popup!");

        // 3. Isi input fields
        string roomPassword = "123";

        passwordRoomInputField.text = roomPassword;
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah isi password room

        // 4. Klik tombol "Join Room" di dalam popup
        joinRoomButton.onClick.Invoke();
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah klik tombol Join Room di popup

        // 5. Verifikasi koneksi Photon dan masuk ke lobby
        yield return new WaitUntil(() => PhotonNetwork.InRoom); // Tunggu sampai masuk room
        Assert.IsTrue(PhotonNetwork.InRoom, "Gagal masuk ke lobby setelah mengisi form dan klik 'Join Room' di popup.");

        Debug.Log($"TC_002_CreateLobby: Alur pembuatan lobby berhasil.");

        m_IsFinished = true;
    }

    // --- TEST CASE UNTUK FITUR: MEMULAI PERMAINAN ---
    // Akan menguji tombol "Play Button" di scene WaitingRoom
    [UnityTest]
    public IEnumerator TC_003_Play()
    {
        m_IsFinished = false;

        // --- ALUR PEMBUATAN LOBBY (Sama seperti TC_001_CreateLobby) ---

        // 0. Pastikan klien terhubung ke Master Server dan Lobby
        PhotonNetwork.ConnectUsingSettings();
        yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.JoinedLobby);
        yield return new WaitForSeconds(0.5f); // Jeda 0.5 detik untuk memastikan state stabil
        Assert.IsTrue(PhotonNetwork.IsConnected, "Failed to connect to Photon Master Server/Lobby for 'Play' test.");

        // 1. Temukan tombol "Create Button" di scene Lobby dan klik
        Button createButton = GameObject.Find("Create Button")?.GetComponent<Button>();
        Assert.IsNotNull(createButton, "Tombol 'Create Button' tidak ditemukan di scene Lobby untuk 'Play' test!");
        createButton.onClick.Invoke();
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah klik
        yield return null; // Tunggu 1 frame untuk popup muncul

        // 2. Verifikasi popup "Create Room" muncul dan temukan input fields serta tombol di dalamnya
        GameObject createRoomPopupPanel = GameObject.Find("CreateRoomPopup");
        Assert.IsNotNull(createRoomPopupPanel, "Popup 'CreateRoomPopup' tidak ditemukan untuk 'Play' test!");
        Assert.IsTrue(createRoomPopupPanel.activeSelf, "Popup 'CreateRoomPopup' tidak aktif setelah tombol diklik untuk 'Play' test!");

        TMP_InputField roomNameInputField = createRoomPopupPanel.transform.Find("RoomNameInput")?.GetComponent<TMP_InputField>();
        Assert.IsNotNull(roomNameInputField, "Input field 'RoomNameInput' tidak ditemukan di dalam popup untuk 'Play' test!");

        TMP_InputField roomPasswordInputField = createRoomPopupPanel.transform.Find("PasswordInput")?.GetComponent<TMP_InputField>();
        Assert.IsNotNull(roomPasswordInputField, "Input field 'PasswordInput' tidak ditemukan di dalam popup untuk 'Play' test!");

        Button createRoomPopupButton = createRoomPopupPanel.transform.Find("Create Room")?.GetComponent<Button>();
        Assert.IsNotNull(createRoomPopupButton, "Tombol 'Create Room' tidak ditemukan di dalam popup untuk 'Play' test!");

        // 3. Isi input fields
        string lobbyName = "PlayTest"; // Nama unik untuk tes Play
        string lobbyPassword = "123";

        roomNameInputField.text = lobbyName;
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah isi nama room
        
        roomPasswordInputField.text = lobbyPassword;
        yield return new WaitForSeconds(3f); // Delay 3 detik setelah isi password room
        yield return null; // Beri waktu 1 frame setelah mengisi teks

        // 4. Klik tombol "Create Room" di dalam popup
        createRoomPopupButton.onClick.Invoke();
        yield return new WaitForSeconds(20f); // Delay 20 detik setelah klik tombol Create Room di popup

        // 5. Verifikasi koneksi Photon dan masuk ke lobby
        yield return new WaitUntil(() => PhotonNetwork.InRoom); // Tunggu sampai masuk room
        Assert.IsTrue(PhotonNetwork.InRoom, "Gagal masuk ke lobby setelah mengisi form dan klik 'Create Room' di popup untuk 'Play' test.");
        Assert.AreEqual(lobbyName, PhotonNetwork.CurrentRoom.Name, "Nama lobby yang dibuat tidak sesuai untuk 'Play' test.");
        Assert.IsTrue(PhotonNetwork.IsMasterClient, "Klien bukan Master Client setelah membuat lobby untuk 'Play' test.");


        // --- LANJUT KE ALUR MEMULAI PERMAINAN ---

        // 6. Karena OnJoinedRoom di CreateAndJoin.cs akan memuat WaitingRoom scene,
        // kita perlu menunggu scene WaitingRoom dimuat.
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "WaitingRoom");
        Assert.AreEqual("WaitingRoom", SceneManager.GetActiveScene().name, "Gagal memuat scene WaitingRoom untuk 'Play' test.");

        // 7. Temukan tombol "Play Button" di scene WaitingRoom
        Button playButton = GameObject.Find("Play Button")?.GetComponent<Button>();
        Assert.IsNotNull(playButton, "Tombol 'Play Button' tidak ditemukan di scene WaitingRoom!");

        // 8. Simulasikan klik tombol "Play Button"
        playButton.onClick.Invoke();
        yield return new WaitForSeconds(5f); // Delay 5 detik setelah klik tombol Play

        // 9. Verifikasi bahwa scene "Multiplayer" dimuat
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "Multiplayer");
        Assert.AreEqual("Multiplayer", SceneManager.GetActiveScene().name, "Scene 'Multiplayer' gagal dimuat setelah klik tombol 'Play Button'.");

        Debug.Log("TC_003_Play: Tombol 'Play Button' berfungsi dan memuat scene 'Multiplayer'.");

        m_IsFinished = true;
    }
}