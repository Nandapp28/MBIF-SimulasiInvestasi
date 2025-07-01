using UnityEngine;
using UnityEngine.UI;
using Photon.Pun; // Diperlukan untuk mengakses informasi multiplayer

[RequireComponent(typeof(GridLayoutGroup))]
public class DynamicGridConstraint : MonoBehaviour
{
    private GridLayoutGroup gridLayout;

    void Start()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        int playerCount = 0;

        // Cek dulu apakah kita berada di scene single-player
        if (GameManager.Instance != null)
        {
            // Jika ya, ambil jumlah pemain dari GameManager
            playerCount = GameManager.Instance.GetPlayerCount();
        }
        // Jika tidak, cek apakah kita berada di scene multiplayer
        else if (MultiplayerManager.Instance != null)
        {
            // Jika ya, ambil jumlah pemain dari jaringan Photon
            playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        }
        else
        {
            // Jika kedua manajer tidak ditemukan, beri pesan error yang jelas
            Debug.LogError("Error: Tidak dapat menemukan GameManager atau MultiplayerManager. Pastikan salah satu ada di scene.");
            return; // Hentikan eksekusi jika tidak ada manajer
        }

        // Atur tata letak grid berdasarkan jumlah pemain
        if (playerCount > 0)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = playerCount;
            Debug.Log($"Grid constraint diatur ke: {playerCount}");
        }
    }
}
