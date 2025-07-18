// File: GameStatusUI.cs

using UnityEngine;
using TMPro; // Gunakan ini jika Anda pakai TextMeshPro
using Photon.Pun;

// Skrip ini memerlukan PhotonView untuk menerima RPC
[RequireComponent(typeof(PhotonView))]
public class GameStatusUI : MonoBehaviour
{
    public static GameStatusUI Instance;

    public TextMeshProUGUI statusText; // Hubungkan komponen teks Anda di Inspector
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
}