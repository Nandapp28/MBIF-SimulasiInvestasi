// File: Scripts/NotificationManager.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    // Singleton instance to be accessible from any script
    public static NotificationManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Panel utama yang menjadi latar belakang notifikasi.")]
    [SerializeField] private GameObject notificationPanel;

    [Tooltip("Komponen Text untuk menampilkan pesan notifikasi.")]
    [SerializeField] private Text notificationText;

    private Coroutine notificationCoroutine;

    private void Awake()
    {
        // Setup Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Jangan hancurkan objek ini saat berpindah scene (opsional)
            // DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        // Pastikan panel tidak aktif saat game dimulai
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[NotificationManager] Referensi 'notificationPanel' belum di-assign di Inspector!");
        }
    }

    /// <summary>
    /// Menampilkan notifikasi popup dengan pesan dan durasi tertentu.
    /// </summary>
    /// <param name="message">Pesan yang ingin ditampilkan.</param>
    /// <param name="duration">Berapa lama notifikasi akan muncul (dalam detik).</param>
    public void ShowNotification(string message, float duration = 3f)
    {
        if (notificationPanel == null || notificationText == null)
        {
            Debug.LogError("[NotificationManager] Panel atau Text UI belum di-assign!");
            return;
        }

        // Jika sudah ada notifikasi yang berjalan, hentikan dulu
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        // Mulai coroutine baru untuk menampilkan notifikasi
        notificationCoroutine = StartCoroutine(DisplayNotification(message, duration));
    }

    private IEnumerator DisplayNotification(string message, float duration)
    {
        // 1. Tampilkan pesan dan aktifkan panel
        notificationText.text = message;
        notificationPanel.SetActive(true);

        // 2. Tunggu sesuai durasi yang ditentukan
        yield return new WaitForSeconds(duration);

        // 3. Sembunyikan kembali panelnya
        notificationPanel.SetActive(false);

        // 4. Set coroutine menjadi null untuk menandakan sudah selesai
        notificationCoroutine = null;
    }
}