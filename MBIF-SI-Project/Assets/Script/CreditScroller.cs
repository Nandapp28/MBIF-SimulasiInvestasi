using UnityEngine;
using UnityEngine.SceneManagement; // Diperlukan untuk mengelola scene
using System.Collections;         // Diperlukan untuk Coroutine (IEnumerator)

/// <summary>
/// Script ini akan menggerakkan RectTransform secara otomatis ke atas
/// dan kembali ke scene yang ditentukan setelah waktu delay tertentu.
/// </summary>
public class CreditScroller : MonoBehaviour
{
    [Header("Pengaturan Scroll")]
    [Tooltip("Drag RectTransform dari panel/objek yang berisi teks kredit Anda ke sini.")]
    public RectTransform creditContent; // Objek yang akan di-scroll
    
    [Tooltip("Kecepatan scroll dalam satuan piksel per detik.")]
    public float scrollSpeed = 50f;     // Kecepatan scroll

    [Header("Pengaturan Pindah Scene")]
    [Tooltip("Waktu dalam detik sebelum otomatis kembali ke scene lain.")]
    public float returnDelay = 20f;     // Waktu tunggu 10 detik

    [Tooltip("Nama scene yang akan dituju setelah delay selesai.")]
    public string returnSceneName = "Options"; // Scene tujuan

    // Start dipanggil pada frame pertama saat script aktif
    void Start()
    {
        // Memulai Coroutine yang akan menangani timer dan pindah scene
        StartCoroutine(ReturnToSceneAfterDelay());
    }

    // Update dipanggil setiap frame
    void Update()
    {
        // Jika creditContent sudah di-assign (tidak null)
        if (creditContent != null)
        {
            // Gerakkan creditContent ke atas (Vector3.up)
            // Dikalikan scrollSpeed dan Time.deltaTime agar gerakan mulus
            // dan tidak tergantung pada frame rate komputer
            creditContent.Translate(Vector3.up * scrollSpeed * Time.deltaTime);
        }
    }

    // Ini adalah Coroutine yang berfungsi sebagai timer
    IEnumerator ReturnToSceneAfterDelay()
    {
        // Perintah ini akan "menjeda" eksekusi fungsi ini
        // selama 'returnDelay' detik (sesuai permintaan Anda, 10 detik).
        yield return new WaitForSeconds(returnDelay);

        // Setelah menunggu, muat scene yang ditentukan (Options)
        // Pastikan Anda memiliki scene bernama "Options" di Build Settings Anda.
        SceneManager.LoadScene(returnSceneName);
    }
}