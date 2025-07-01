using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine.SceneManagement;
using System.Linq;

public class AnonymousLogin : MonoBehaviour
{
    public GameObject loginBtn;

    private const string GuestUserIdKey = "GuestUserId";      // Firebase UID
    private const string GuestPlayerIdKey = "GuestPlayerId";  // Short custom ID
    private const string GuestUserNameKey = "GuestUserName";  // Default guest name

    private DatabaseReference dbRef;

    void Start()
    {
        // Inisialisasi Firebase dan periksa dependency
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var status = task.Result;
            if (status == Firebase.DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;

                // Jika sebelumnya sudah login, skip tombol login
                if (PlayerPrefs.HasKey(GuestUserIdKey))
                {
                    string storedUserId = PlayerPrefs.GetString(GuestUserIdKey);
                    Debug.Log("Ditemukan ID Guest: " + storedUserId + ". Melewatkan login anonim.");
                    GuestLoginSuccess(storedUserId);
                }
                else
                {
                    Debug.Log("Belum ada ID Guest. Tampilkan tombol login.");
                    loginBtn.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("Firebase tidak tersedia: " + status);
            }
        });
    }

    public async void Login()
    {
        if (!PlayerPrefs.HasKey(GuestUserIdKey))
        {
            await AnonymousLoginBtn();
        }
        else
        {
            Debug.Log("Sudah login sebagai guest.");
            GuestLoginSuccess(PlayerPrefs.GetString(GuestUserIdKey));
        }
    }

    async Task AnonymousLoginBtn()
    {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        await auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Gagal login anonim: " + task.Exception?.Message);
                return;
            }

            AuthResult result = task.Result;
            string firebaseUid = result.User.UserId;
            Debug.Log("Firebase UID: " + firebaseUid);

            // Buat ID player pendek & username default
            string playerId = GeneratePlayerID();
            string username = "Guest" + Random.Range(1000, 9999);

            // Simpan data ke PlayerPrefs
            PlayerPrefs.SetString(GuestUserIdKey, firebaseUid);
            PlayerPrefs.SetString(GuestPlayerIdKey, playerId);
            PlayerPrefs.SetString(GuestUserNameKey, username);
            PlayerPrefs.Save();

            // Simpan data pengguna ke Firebase Realtime Database
            SaveToDatabase(firebaseUid, playerId, username);

            GuestLoginSuccess(firebaseUid);
        });
    }

    void SaveToDatabase(string firebaseUid, string playerId, string username)
    {
        var data = new Dictionary<string, object>
        {
            { "playerId", playerId },
            { "userName", username },
            { "finPoin", 0 }, // Nilai default awal
            { "avatarName", "avatar_0" },
            { "borderName", "avatar_0" }
        };

        Debug.Log($"Menyimpan ke Firebase: UID={firebaseUid}, playerId={playerId}, userName={username}");

        dbRef.Child("users").Child(firebaseUid).SetValueAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
                Debug.Log("Data berhasil disimpan ke Realtime Database.");
            else
                Debug.LogError("Gagal menyimpan data ke Firebase: " + task.Exception);
        });
    }

    string GeneratePlayerID()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[Random.Range(0, s.Length)]).ToArray());
    }

    void GuestLoginSuccess(string id)
    {
        loginBtn.SetActive(false); // Sembunyikan tombol login
        SceneManager.LoadScene("MainMenu"); // Pindah ke scene Main Menu
    }
}
