using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;
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
    private AsyncOperation operation;

    void Start()
    {
        // Pastikan UI terlihat
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (slider != null) slider.value = 0;
        if (statusText != null) statusText.text = "Loading...";

        // Mulai Coroutine untuk memuat scene game
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        // 1. Mulai memuat scene game secara asinkron
        operation = SceneManager.LoadSceneAsync(sceneToLoad);
        
        // 2. Tahan scene agar tidak aktif otomatis setelah selesai 90%
        operation.allowSceneActivation = false;

        // 3. Update slider berdasarkan progress loading LOKAL
        float elapsed = 0f;
        while (elapsed < minLoadTime)
        {
            elapsed += Time.deltaTime;
            
            // Update slider berdasarkan waktu minimum
            float progress = Mathf.Clamp01(elapsed / minLoadTime);
            if (slider != null) slider.value = progress;

            yield return null;
        }

        // 3. Pastikan slider 100% setelah waktu minimum selesai
        if (slider != null) slider.value = 1f;

        // 4. (PENTING) Sekarang, tunggu loading *asli* selesai
        //    Ini untuk perangkat lambat yang mungkin butuh > 3 detik
        while (operation.progress < 0.9f)
        {
            // Slider sudah 100%, kita hanya perlu menunggu
            yield return null; 
        }

        // 5. Loading selesai. Ganti teks dan beritahu jaringan
        if (statusText != null) statusText.text = "Menunggu pemain lain...";
        Hashtable props = new Hashtable { { "sceneReady", true } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // 6. Callback ini akan dipanggil di SEMUA klien setiap kali 
    //    ada pemain yang mengubah properties-nya (termasuk kita).
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

    // 7. (HANYA MASTER CLIENT) Mengecek apakah semua sudah siap
    private void CheckAllPlayersReady()
    {
        // Pastikan ini HANYA MasterClient
        if (!PhotonNetwork.IsMasterClient) return; 

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            // Cek apakah pemain punya properti 'sceneReady' dan nilainya 'true'
            if (!p.CustomProperties.ContainsKey("sceneReady") || !(bool)p.CustomProperties["sceneReady"])
            {
                // Jika ada SATU saja yang belum siap, berhenti mengecek.
                Debug.Log($"Pemain {p.NickName} belum siap.");
                return;
            }
        }

        // Jika kita lolos dari loop, berarti SEMUA pemain sudah siap.
        Debug.Log("Semua pemain siap! Mengirim RPC untuk pindah scene.");
        
        // 8. Kirim perintah ke SEMUA pemain untuk mengaktifkan scene
        photonView.RPC("ActivateScene", RpcTarget.All);
    }

    // 9. (DITERIMA SEMUA PEMAIN) Perintah terakhir untuk pindah scene
    [PunRPC]
    public void ActivateScene()
    {
        Debug.Log("Menerima RPC, mengaktifkan scene...");
        if (statusText != null) statusText.text = "Memasuki game...";
        
        // Ini adalah perintah untuk melanjutkan loading dari 90% ke 100%
        // dan pindah scene
        if (operation != null)
        {
            operation.allowSceneActivation = true;
        }
    }

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