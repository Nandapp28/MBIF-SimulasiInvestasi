using System.Collections;
using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine.SceneManagement;
using System;

public class ProfileSceneManager : MonoBehaviour
{
    [Serializable]
    public class DataToSave
    {
        public string playerId;
        public string userName;
        public int finPoin;
    }

    [Header("UI References")]
    public TextMeshProUGUI playerIdText;
    public TMP_InputField userNameInput;     // <- Ganti dari userNameText ke userNameInput
    public TextMeshProUGUI finPoinText;
    public TextMeshProUGUI displayUsernameText;
    public GameObject confirmLogoutPopUp;

    [Header("Data")]
    private DataToSave dts = new DataToSave();

    private const string GuestUserIdKey = "GuestUserId";
    private DatabaseReference dbRef;

    private void Awake()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private void Start()
    {
        LoadData();
    }

    private string GetStoredUserId()
    {
        if (PlayerPrefs.HasKey(GuestUserIdKey))
            return PlayerPrefs.GetString(GuestUserIdKey);

        Debug.LogWarning("User ID tidak ditemukan di PlayerPrefs.");
        return null;
    }

    public void LoadData()
    {
        string userId = GetStoredUserId();

        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("Gagal memuat data: User ID kosong.");
            return;
        }

        StartCoroutine(LoadDataEnum(userId));
    }

    private IEnumerator LoadDataEnum(string userId)
    {
        var serverData = dbRef.Child("users").Child(userId).GetValueAsync();
        yield return new WaitUntil(() => serverData.IsCompleted);

        if (serverData.Exception != null)
        {
            Debug.LogError("Gagal memuat data dari Firebase: " + serverData.Exception);
            yield break;
        }

        DataSnapshot snapshot = serverData.Result;
        string jsonData = snapshot.GetRawJsonValue();

        if (!string.IsNullOrEmpty(jsonData))
        {
            dts = JsonUtility.FromJson<DataToSave>(jsonData);
            Debug.Log("Data berhasil dimuat: " + jsonData);
            UpdateUI();
        }
        else
        {
            Debug.Log("Tidak ada data ditemukan untuk user ini.");
        }
    }

    private void UpdateUI()
    {
        playerIdText.text = dts.playerId;

        // Kosongkan input, isi placeholder
        userNameInput.text = "";
        if (userNameInput.placeholder is TextMeshProUGUI placeholder)
        {
            placeholder.text = dts.userName;
            userNameInput.text = dts.userName;
        }

        finPoinText.text = dts.finPoin.ToString();

        // Tampilkan di DisplayUsername
        displayUsernameText.text = dts.userName;
    }

    public void SaveUserNameOnly()
    {
        string userId = GetStoredUserId();
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogWarning("Tidak bisa menyimpan nama: User ID kosong.");
            return;
        }

        dts.userName = userNameInput.text; // Ambil input dari UI
        string json = JsonUtility.ToJson(dts);

        dbRef.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
                Debug.Log("Username berhasil disimpan ke Firebase.");
            else
                Debug.LogError("Gagal menyimpan username: " + task.Exception);
        });
    }

    public void Logout()
    {
        confirmLogoutPopUp.SetActive(true); // Tampilkan pop-up konfirmasi
    }
    
    public void ConfirmLogoutYes()
    {
        if (PlayerPrefs.HasKey(GuestUserIdKey))
        {
            PlayerPrefs.DeleteKey(GuestUserIdKey);
            PlayerPrefs.Save();
            Debug.Log("Guest ID dihapus dari PlayerPrefs.");
        }

        FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("Logout dari Firebase berhasil.");

        SceneManager.LoadScene("Intro");
    }

    public void ConfirmLogoutNo()
    {
        confirmLogoutPopUp.SetActive(false); // Sembunyikan pop-up
    }
}
