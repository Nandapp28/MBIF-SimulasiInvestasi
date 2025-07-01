using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

// Memastikan GameObject ini selalu memiliki komponen GridLayoutGroup
[RequireComponent(typeof(GridLayoutGroup))]
public class DynamicLayoutCard : MonoBehaviourPunCallbacks
{
    // Variabel privat untuk menyimpan referensi ke manager yang relevan
    private GameManager gameManager;
    private MultiplayerManager multiplayerManager;

    private GridLayoutGroup grid;
    private int lastPlayerCount = -1;

    // Enum untuk melacak mode mana yang sedang aktif
    private enum Mode { None, SinglePlayer, MultiPlayer }
    private Mode currentMode = Mode.None;

    void Start()
    {
        grid = GetComponent<GridLayoutGroup>();

        // 1. Prioritaskan untuk mencari MultiplayerManager terlebih dahulu
        multiplayerManager = MultiplayerManager.Instance;
        if (multiplayerManager != null)
        {
            currentMode = Mode.MultiPlayer;
            Debug.Log("Mode terdeteksi: Multiplayer");
            return; // Ditemukan, tidak perlu mencari lagi
        }

        // 2. Jika MultiplayerManager tidak ditemukan, cari GameManager
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            currentMode = Mode.SinglePlayer;
            Debug.Log("Mode terdeteksi: Single Player");
            return; // Ditemukan
        }
        
        // 3. Jika keduanya tidak ditemukan, tampilkan error
        if (currentMode == Mode.None)
        {
            Debug.LogError("❌ Tidak dapat menemukan GameManager atau MultiplayerManager di scene ini!", this);
        }
    }

    void Update()
    {
        // Jika tidak ada mode yang aktif, hentikan eksekusi
        if (currentMode == Mode.None) return;

        int currentPlayerCount = 0;

        // Ambil jumlah pemain berdasarkan mode yang aktif
        switch (currentMode)
        {
            case Mode.MultiPlayer:
                currentPlayerCount = multiplayerManager.GetPlayerCount();
                break;
            case Mode.SinglePlayer:
                currentPlayerCount = gameManager.GetPlayerCount(); // Asumsi GameManager punya method ini
                break;
        }

        // Hanya update grid jika jumlah pemain valid dan berubah (untuk efisiensi)
        if (currentPlayerCount > 0 && currentPlayerCount != lastPlayerCount)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = currentPlayerCount;
            lastPlayerCount = currentPlayerCount; // Simpan nilai terakhir

            Debug.Log($"🔄 Grid constraintCount diperbarui menjadi: {currentPlayerCount} (Mode: {currentMode})");
        }
    }
}