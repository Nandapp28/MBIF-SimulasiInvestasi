using System.Collections;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;

public class PlayerStatusManager : MonoBehaviour
{
    [Serializable]
    private class DataToSave
    {
        public string playerId;
        public string userName;
        public int finPoin;
        public string avatarName; // Nama aset avatar
        public string borderName; // Nama aset border
    }

    [Header("UI References")]
    public TextMeshProUGUI playerIdText;
    public TextMeshProUGUI userName;

    [Header("Profile Display")]
    // public Image profileBorder;
    public Image profilePicture;

    [Header("Add Friend")]
    public GameObject addFriendPopUp;

    [Header("Data")]
    private DataToSave dts = new DataToSave();

    private const string GuestUserIdKey = "GuestUserId";
    private DatabaseReference dbRef;


    void Awake()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    void Start()
    {
        LoadData(); // Muat data pengguna saat script aktif
    }

    private string GetStoredUserId()
    {
        return PlayerPrefs.HasKey(GuestUserIdKey) ? PlayerPrefs.GetString(GuestUserIdKey) : null;
    }

    public void LoadData()
    {
        string userId = GetStoredUserId();

        // Ambil User ID dari Firebase Auth jika PlayerPrefs kosong
        if (FirebaseAuth.DefaultInstance.CurrentUser != null && string.IsNullOrEmpty(userId))
            userId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("User ID kosong. Gagal memuat data.");
            return;
        }

        StartCoroutine(LoadDataEnum(userId)); // Mulai Coroutine memuat data
    }

    private IEnumerator LoadDataEnum(string userId)
    {
        var serverData = dbRef.Child("users").Child(userId).GetValueAsync();
        yield return new WaitUntil(() => serverData.IsCompleted); // Tunggu data selesai dimuat

        if (serverData.Exception != null)
        {
            Debug.LogError("Gagal memuat data dari Firebase: " + serverData.Exception);
            UpdateUI();
            yield break;
        }

        DataSnapshot snapshot = serverData.Result;
        string jsonData = snapshot.GetRawJsonValue();

        if (!string.IsNullOrEmpty(jsonData))
        {
            dts = JsonUtility.FromJson<DataToSave>(jsonData); // Deserialisasi data lengkap
            Debug.Log("Data berhasil dimuat: " + jsonData);
        }
        else
        {
            Debug.Log("Tidak ada data ditemukan. Inisialisasi default.");
            StartCoroutine(SaveDataToFirebase(userId)); // Simpan data default ke Firebase
        }
        UpdateUI(); // Perbarui UI setelah data dts terisi
    }

    private void UpdateUI()
    {
        // Perbarui teks UI
        playerIdText.text = "ID : " + dts.playerId;
        userName.text = dts.userName;

        userName.text = dts.userName.ToString();

        // Perbarui gambar profil dari data yang dimuat
        LoadProfileImage(dts.avatarName, profilePicture, "Avatars");
        // LoadProfileImage(dts.borderName, profileBorder, "Borders");
    }

    // Fungsi bantu untuk memuat dan menampilkan gambar dari Resources
    private void LoadProfileImage(string assetName, Image targetImage, string folderName)
    {
        if (string.IsNullOrEmpty(assetName)) return; // Jangan muat jika nama aset kosong

        Sprite loadedSprite = Resources.Load<Sprite>(folderName + "/" + assetName);
        if (loadedSprite != null)
        {
            targetImage.sprite = loadedSprite;
            Debug.Log($"Gambar '{assetName}' dimuat dari Resources/{folderName}.");
        }
        else
        {
            Debug.LogWarning($"Aset '{assetName}' tidak ditemukan di Resources/{folderName}.");
        }
    }

    // Fungsi bantu untuk menyimpan seluruh data ke Firebase
    private IEnumerator SaveDataToFirebase(string userId)
    {
        string json = JsonUtility.ToJson(dts);
        var saveTask = dbRef.Child("users").Child(userId).SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.Exception == null)
        {
            Debug.Log("Data user berhasil disimpan ke Firebase.");
        }
        else
        {
            Debug.LogError("Gagal menyimpan data user: " + saveTask.Exception);
        }
    }

    public void AddFriendPopUP()
    {
        addFriendPopUp.SetActive(true);
    }
}
