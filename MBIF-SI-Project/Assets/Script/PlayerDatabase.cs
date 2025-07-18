using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;

public class PlayerDatabase : MonoBehaviour
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
    public TMP_InputField userNameInput;
    public TextMeshProUGUI finPoinText;
    public GameObject confirmLogoutPopUp;

    // Mengaktifkan srollview antara avatar dan border
    [Header("Tab System")]
    public Button avatarTabButton;
    public Button borderTabButton;
    public GameObject avatarPanel; // Panel yang berisi scrollview avatar
    public GameObject borderPanel; // Panel yang berisi scrollview border
    public GameObject avatarIndicator;
    public GameObject borderIndicator;

    [Header("Owned Items Display")]
    public Transform ownedAvatarParent; // Referensi ke 'Content' di dalam ScrollView Avatar
    public GameObject ownedAvatarPrefab; 
    public Transform ownedBorderParent; // Referensi ke 'Content' di dalam ScrollView Border
    public GameObject ownedBorderPrefab;

    [Header("Profile Display")]
    public Image profileBorder;
    public Image profilePicture;

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

        avatarTabButton.onClick.AddListener(ShowAvatarTab);
        borderTabButton.onClick.AddListener(ShowBorderTab);

        // Atur kondisi awal, tampilkan tab avatar secara default
        ShowAvatarTab();
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

            // Panggil fungsi untuk mengisi daftar avatar di sini
            PopulateOwnedAvatars(snapshot);
            // BARU: Panggil juga fungsi untuk mengisi daftar border
            PopulateOwnedBorders(snapshot);
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
        playerIdText.text = dts.playerId;
        userNameInput.text = dts.userName;
        if (userNameInput.placeholder is TextMeshProUGUI placeholder) placeholder.text = dts.userName;
        finPoinText.text = dts.finPoin.ToString();

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

    public void SaveUserNameOnly()
    {
        string userId = GetStoredUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("User ID kosong. Gagal menyimpan.");
            return;
        }

        string newUserName = userNameInput.text;

        // 1. Buat Dictionary untuk menampung HANYA data yang ingin diubah.
        var updates = new Dictionary<string, object>();
        updates["/userName"] = newUserName; // Hanya menargetkan field "userName"

        // 2. Gunakan UpdateChildrenAsync untuk update "bedah", bukan SetRawJsonValueAsync
        dbRef.Child("users").Child(userId).UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log("Username berhasil diupdate.");
                // Update juga data lokal di skrip agar tetap sinkron
                dts.userName = newUserName; 
            }
            else
            {
                Debug.LogError("Gagal mengupdate username: " + task.Exception);
            }
        });
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

    // Fungsi publik yang akan dipanggil oleh tombol Avatar
    public void ShowAvatarTab()
    {
        SetActiveTab("avatar");
    }

    // Fungsi publik yang akan dipanggil oleh tombol Border
    public void ShowBorderTab()
    {
        SetActiveTab("border");
    }

    // Fungsi privat untuk mengatur tampilan
    private void SetActiveTab(string tabName)
    {
        // Gunakan boolean untuk menentukan tab mana yang aktif
        bool isAvatarActive = (tabName == "avatar");

        // Atur aktif/non-aktif panel sesuai dengan boolean
        avatarPanel.SetActive(isAvatarActive);
        borderPanel.SetActive(!isAvatarActive);

        // Atur aktif/non-aktif indikator sesuai dengan boolean
        avatarIndicator.SetActive(isAvatarActive);
        borderIndicator.SetActive(!isAvatarActive);

        sfxEnable();
    }

    // Fungsi untuk mengisi ScrollView dengan avatar yang dimiliki
    private void PopulateOwnedAvatars(DataSnapshot userSnapshot)
    {
        // Cek apakah pemain punya node "owned_avatar"
        if (userSnapshot.Child("owned_avatar").Exists)
        {
            Debug.Log("Ditemukan data owned_avatar, memuat koleksi...");

            // Ulangi untuk setiap avatar yang dimiliki
            foreach (var avatarData in userSnapshot.Child("owned_avatar").Children)
            {
                string avatarAssetName = avatarData.Value.ToString();
                Sprite avatarSprite = Resources.Load<Sprite>("Avatars/" + avatarAssetName);

                if (avatarSprite != null)
                {
                    GameObject newItem = Instantiate(ownedAvatarPrefab, ownedAvatarParent);
                    
                    //    Ini akan menunjuk ke GameObject "Image" 
                    Transform childImageObject = newItem.transform.GetChild(0);

                    Image itemImage = childImageObject.GetComponent<Image>();

                    if (itemImage != null)
                    {
                        // Pasang sprite yang sudah dimuat ke komponen Image anak.
                        itemImage.sprite = avatarSprite;
                    }
                    else
                    {
                        Debug.LogError("Komponen Image tidak ditemukan pada objek anak pertama dari prefab: " + ownedAvatarPrefab.name);
                    }
                }
                else
                {
                    Debug.LogWarning($"Gagal memuat sprite: {avatarAssetName} dari folder Avatars.");
                }
            }
        }
        else
        {
            Debug.Log("Node owned_avatar tidak ditemukan untuk user ini.");
        }
    }
    
    // Fungsi untuk mengisi ScrollView dengan border yang dimiliki
    private void PopulateOwnedBorders(DataSnapshot userSnapshot)
    {
        // PERUBAHAN: Cek apakah pemain punya node "owned_border"
        if (userSnapshot.Child("owned_border").Exists)
        {
            Debug.Log("Ditemukan data owned_border, memuat koleksi...");

            // Ulangi untuk setiap border yang dimiliki
            foreach (var borderData in userSnapshot.Child("owned_border").Children)
            {
                string borderAssetName = borderData.Value.ToString();

                // PERUBAHAN: Muat sprite dari folder Resources/Borders
                Sprite borderSprite = Resources.Load<Sprite>("Borders/" + borderAssetName);

                if (borderSprite != null)
                {
                    // PERUBAHAN: Menggunakan prefab dan parent untuk border
                    GameObject newItem = Instantiate(ownedBorderPrefab, ownedBorderParent);

                    Transform childImageObject = newItem.transform.GetChild(0);
                    Image itemImage = childImageObject.GetComponent<Image>();

                    if (itemImage != null)
                    {
                        // Pasang sprite border yang sudah dimuat
                        itemImage.sprite = borderSprite;
                    }
                    else
                    {
                        Debug.LogError("Komponen Image tidak ditemukan pada objek anak pertama dari prefab: " + ownedBorderPrefab.name);
                    }
                }
                else
                {
                    Debug.LogWarning($"Gagal memuat sprite: {borderAssetName} dari folder Borders.");
                }
            }
        }
        else
        {
            Debug.Log("Node owned_border tidak ditemukan untuk user ini.");
        }
    }

    public void Logout()
    {
        sfxEnable();

        confirmLogoutPopUp.SetActive(true); // Tampilkan pop-up konfirmasi logout
    }

    public void ConfirmLogoutYes()
    {   
        PlayerPrefs.DeleteKey(GuestUserIdKey); // Hapus Guest ID lokal
        PlayerPrefs.Save();
        FirebaseAuth.DefaultInstance.SignOut(); // Logout dari Firebase Auth
        SceneManager.LoadScene("Intro"); // Kembali ke scene Intro
    }

    public void ConfirmLogoutNo()
    {
        sfxEnable();

        confirmLogoutPopUp.SetActive(false); // Sembunyikan pop-up logout
    }

    public void sfxEnable()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
    }
}