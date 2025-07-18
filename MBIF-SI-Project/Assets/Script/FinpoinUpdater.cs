using UnityEngine;
using TMPro; // Diperlukan untuk TextMeshProUGUI
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;

public class FinpoinUpdater : MonoBehaviour
{
    [Header("Referensi UI")]
    [Tooltip("Drag & drop komponen TextMeshProUGUI untuk Finpoin ke sini.")]
    public TextMeshProUGUI finpoinText;

    private DatabaseReference finpoinRef;
    private FirebaseAuth auth;

    // Menggunakan async void Start untuk alur yang aman
    async void Start()
    {
        // untuk menunggu FirebaseInitializer siap di dalam sebuah metode async.
        while (FirebaseInitializer.Instance == null || !FirebaseInitializer.Instance.IsFirebaseReady)
        {
            await Task.Delay(100); // Tunggu 100ms lalu cek lagi
        }
        
        // Sisa kode di bawah ini sudah benar dan tidak perlu diubah
        auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("FinpoinUpdater: Pengguna belum login.");
            UpdateFinpoinUI(0); // Tampilkan 0 jika belum login
            return;
        }

        string currentUserUID = auth.CurrentUser.UserId;
        // Buat referensi langsung ke node "finPoin" milik user saat ini
        finpoinRef = FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(currentUserUID).Child("finPoin");

        // Pasang "pendengar" ke referensi tersebut.
        finpoinRef.ValueChanged += OnFinpoinValueChanged;
    }

    // Fungsi ini adalah callback yang dieksekusi saat data finPoin berubah
    private void OnFinpoinValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        long currentFinpoin = 0;
        if (args.Snapshot != null && args.Snapshot.Exists)
        {
            // Ambil nilai dari snapshot dan konversi ke long
            currentFinpoin = (long)args.Snapshot.Value;
        }

        // Panggil fungsi untuk update UI
        UpdateFinpoinUI(currentFinpoin);
    }

    // Fungsi khusus untuk memperbarui teks di UI
    private void UpdateFinpoinUI(long amount)
    {
        if (finpoinText != null)
        {
            // Format "N0" akan menambahkan pemisah ribuan (contoh: 10,000)
            finpoinText.text = "$ " + amount.ToString("N0");
        }
    }

    // PENTING: Hapus "pendengar" saat objek dihancurkan untuk mencegah memory leak
    private void OnDestroy()
    {
        if (finpoinRef != null)
        {
            finpoinRef.ValueChanged -= OnFinpoinValueChanged;
        }
    }
}