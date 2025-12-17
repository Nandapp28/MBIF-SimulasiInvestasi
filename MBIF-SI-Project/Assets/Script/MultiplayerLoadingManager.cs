using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// [RequireComponent(typeof(PhotonView))] // <-- HAPUS BARIS INI, SUDAH TIDAK PERLU RPC
public class MultiplayerLoadingManager : MonoBehaviourPunCallbacks
{
    [Header("Visuals (dari LoadingManager.cs)")]
    public GameObject loadingPanel;
    public Slider slider;
    public TextMeshProUGUI statusText; // Opsional

    [Header("Scene To Load")]
    [Tooltip("Nama scene game multiplayer yang sebenarnya")]
    public string sceneToLoad = "Multiplayer";

    public float minLoadTime = 3.0f;

    // Variabel untuk proses loading
    // private AsyncOperation operation; // <-- HAPUS BARIS INI

    void Start()
    {
        // Pastikan UI terlihat
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (slider != null) slider.value = 0;
        if (statusText != null) statusText.text = "Loading...";

        // Mulai Coroutine untuk memuat scene game
        // StartCoroutine(LoadSceneAsync()); // <-- GANTI NAMA FUNGSI INI
        StartCoroutine(FakeLoadingAndNotifyReady());
    }

    // --- INI FUNGSI BARU (PENGGANTI LoadSceneAsync) ---
    IEnumerator FakeLoadingAndNotifyReady()
    {
        // 1. (SEPERTI LAMA) Update slider berdasarkan progress loading LOKAL
        float elapsed = 0f;
        while (elapsed < minLoadTime)
        {
            elapsed += Time.deltaTime;
            
            // Update slider berdasarkan waktu minimum
            float progress = Mathf.Clamp01(elapsed / minLoadTime);
            if (slider != null) slider.value = progress;

            yield return null;
        }

        // 2. (SEPERTI LAMA) Pastikan slider 100%
        if (slider != null) slider.value = 1f;

        // 3. (SEPERTI LAMA) Ganti teks dan beritahu jaringan
        if (statusText != null) statusText.text = "Menunggu pemain lain...";
        
        // 4. (SEPERTI LAMA) Set properti bahwa KITA sudah siap
        Hashtable props = new Hashtable { { "sceneReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        
        // 5. (PENTING) Jika kita MasterClient, kita harus cek
        //    (Karena bisa jadi kita yang terakhir siap)
        if (PhotonNetwork.IsMasterClient)
        {
            CheckAllPlayersReady();
        }
    }

    // 6. (TETAP SAMA) Callback ini akan dipanggil di SEMUA klien 
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // Kita hanya peduli jika MasterClient (Host) yang mengecek
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        // MasterClient mengecek apakah 'sceneReady' yang berubah
        if (changedProps.ContainsKey("sceneReady"))
        {
            CheckAllPlayersReady();
        }
    }

    // 7. (MODIFIKASI PENTING)
    private void CheckAllPlayersReady()
    {
        if (!PhotonNetwork.IsMasterClient) return; 

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            // TAMBAHAN: Abaikan pemain yang statusnya 'IsInactive' (sedang putus tapi belum timeout)
            if (p.IsInactive) 
            {
                Debug.Log($"Pemain {p.NickName} inaktif, diabaikan.");
                continue;
            }

            // Cek apakah pemain punya properti 'sceneReady' dan nilainya 'true'
            if (!p.CustomProperties.ContainsKey("sceneReady") || !(bool)p.CustomProperties["sceneReady"])
            {
                Debug.Log($"Pemain {p.NickName} belum siap.");
                return;
            }
        }

        Debug.Log("Semua pemain siap! MasterClient akan memuat scene 'Multiplayer'...");
        
        // Hapus properti "sceneReady" agar tidak bentrok di kemudian hari
        Hashtable props = new Hashtable { { "sceneReady", null } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        PhotonNetwork.LoadLevel(sceneToLoad);
    }

    // --- HAPUS FUNGSI RPC DI BAWAH INI KARENA TIDAK DIPERLUKAN LAGI ---
    /*
    [PunRPC]
    public void ActivateScene()
    {
        // ... (FUNGSI INI HAPUS) ...
    }
    */

    // (PENTING) Jika Host keluar saat loading
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // Jika kita menjadi MasterClient baru, kita harus cek ulang
        if (PhotonNetwork.IsMasterClient)
        {
            CheckAllPlayersReady();
        }
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Jika kita adalah MasterClient, kita harus mengecek ulang
        // status kesiapan.
        if (PhotonNetwork.IsMasterClient)
        {
            CheckAllPlayersReady();
        }
    }
}