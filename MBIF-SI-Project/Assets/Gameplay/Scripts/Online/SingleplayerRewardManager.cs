// File: Scripts/SingleplayerRewardManager.cs (Revisi)

using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using System.Collections.Generic; // Diperlukan untuk Dictionary

public class SingleplayerRewardManager : MonoBehaviour
{
    public static SingleplayerRewardManager Instance { get; private set; }

    private DatabaseReference dbReference;
    private FirebaseAuth auth;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    async void Start()
    {
        // Tetap menunggu Firebase siap untuk digunakan
        while (FirebaseInitializer.Instance == null || !FirebaseInitializer.Instance.IsFirebaseReady)
        {
            await Task.Delay(100); // Tunggu 100 milidetik lalu cek lagi
        }

        // Inisialisasi Firebase Auth dan Database Reference
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    /// <summary>
    /// Fungsi publik untuk memberikan hadiah Finpoint berdasarkan peringkat.
    /// </summary>
    /// <param name="rank">Peringkat pemain (1, 2, 3, dst.)</param>
    public void AwardFinpointsForRank(int rank)
    {
        // Pengecekan utama: Apakah ada user yang sedang login?
        if (auth.CurrentUser == null)
        {
            Debug.Log("Pemain tidak login (offline/guest). Hadiah Finpoint tidak diberikan.");
            return;
        }

        int rewardAmount = GetRewardForRank(rank);

        if (rewardAmount > 0)
        {
            string userId = auth.CurrentUser.UserId;
            DatabaseReference finPoinRef = dbReference.Child("users").Child(userId).Child("finPoin");

            // Gunakan Transaction untuk update data secara aman
            finPoinRef.RunTransaction(mutableData => {
                long currentPoin = 0;
                if (mutableData.Value != null)
                {
                    long.TryParse(mutableData.Value.ToString(), out currentPoin);
                }

                mutableData.Value = currentPoin + rewardAmount;
                return TransactionResult.Success(mutableData);
            }).ContinueWith(task => {
                if (task.Exception != null)
                {
                    Debug.LogWarning($"Gagal memberikan {rewardAmount} Finpoint: {task.Exception}");
                }
                else
                {
                    Debug.Log($"Berhasil! {rewardAmount} Finpoint ditambahkan ke akun Anda.");
                    // Tampilkan notifikasi ke pemain jika NotificationManager ada
                    if (NotificationManager.Instance != null)
                    {
                        NotificationManager.Instance.ShowNotification($"Selamat! Anda mendapatkan {rewardAmount} Finpoint.", 4f);
                    }
                }
            });
        }
    }

    /// <summary>
    /// Menghitung jumlah hadiah berdasarkan peringkat.
    /// </summary>
    /// <param name="rank">Peringkat pemain.</param>
    /// <returns>Jumlah Finpoint.</returns>
    private int GetRewardForRank(int rank)
    {
        switch (rank)
        {
            case 1: return 50;
            case 2: return 40;
            case 3: return 30;
            case 4: return 20;
            case 5: return 10;
            default: return 0;
        }
    }
}