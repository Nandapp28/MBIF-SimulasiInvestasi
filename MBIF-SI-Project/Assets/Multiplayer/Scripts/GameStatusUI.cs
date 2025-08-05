// File: GameStatusUI.cs

using UnityEngine;
using TMPro; // Gunakan ini jika Anda pakai TextMeshPro
using Photon.Pun;
using System.Collections;

// Skrip ini memerlukan PhotonView untuk menerima RPC
[RequireComponent(typeof(PhotonView))]
public class GameStatusUI : MonoBehaviour
{
    public static GameStatusUI Instance;

    public TextMeshProUGUI statusText;
    public TextMeshProUGUI temporaryNotificationText;
    
     // Hubungkan komponen teks Anda di Inspector
    [HideInInspector] // Sembunyikan dari Inspector agar tidak perlu di-drag manual
    public PhotonView photonView;
    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // --- TAMBAHKAN BARIS INI ---
            // Saat Awake, ambil komponen PhotonView yang ada di GameObject ini
            photonView = GetComponent<PhotonView>();
            // --- AKHIR PENAMBAHAN ---
        }
    }

    // Fungsi ini akan dipanggil oleh manajer lain melalui RPC
    [PunRPC]
    public void UpdateStatusText(string newStatus)
    {
        if (statusText != null)
        {
            statusText.text = newStatus;
        }
    }
    public void ShowTemporaryNotification(string message, float duration)
    {
        // Hentikan coroutine sebelumnya jika ada untuk menghindari tumpang tindih
        StopAllCoroutines();
        // Mulai coroutine baru untuk menampilkan pesan
        StartCoroutine(NotificationCoroutine(message, duration));
    }
    private IEnumerator NotificationCoroutine(string message, float duration)
    {
        if (temporaryNotificationText == null) yield break;

        // Tampilkan teks notifikasi
        temporaryNotificationText.text = message;
        temporaryNotificationText.gameObject.SetActive(true);

        // Tunggu selama durasi yang ditentukan
        yield return new WaitForSeconds(duration);

        // Sembunyikan kembali teks notifikasi
        temporaryNotificationText.gameObject.SetActive(false);
    }
}