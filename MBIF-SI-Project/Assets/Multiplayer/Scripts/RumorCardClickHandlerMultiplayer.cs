// File: RumorCardClickHandlerMultiplayer.cs
using UnityEngine;

/// <summary>
/// Diletakkan pada setiap objek kartu rumor 3D.
/// Bertugas untuk menangani input klik dan menampilkan visual 2D kartu tersebut.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RumorCardClickHandlerMultiplayer : MonoBehaviour
{
    private Renderer cardRenderer;

    void Awake()
    {
        // Ambil komponen Renderer saat objek pertama kali aktif
        cardRenderer = GetComponent<Renderer>();
        if (cardRenderer == null)
        {
            Debug.LogError("Komponen Renderer tidak ditemukan pada objek kartu ini!", this);
        }
    }

    /// <summary>
    /// Fungsi publik ini akan dipanggil oleh InputManager saat kartu ini diklik
    /// dan tidak terhalang oleh UI.
    /// </summary>
    public void TriggerCardView()
    {
        // Lakukan serangkaian pengecekan untuk memastikan semuanya siap
        if (!gameObject.activeInHierarchy || RumorCardViewerUI.Instance == null || RumorPhaseManagerMultiplayer.Instance == null)
        {
            Debug.LogWarning("Salah satu komponen penting (UI, Manager) tidak tersedia.");
            return;
        }

        // 1. Dapatkan tekstur 3D yang sedang ditampilkan di kartu
        Texture texture3D = cardRenderer.material.mainTexture;
        if (texture3D == null) return;

        // 2. Minta RumorPhaseManagerMultiplayer untuk mencari nama kartu dari tekstur ini
        string cardName = RumorPhaseManagerMultiplayer.Instance.GetCardNameFromTexture(texture3D);
        if (string.IsNullOrEmpty(cardName))
        {
            Debug.LogWarning($"Nama kartu tidak ditemukan untuk tekstur: {texture3D.name}");
            return;
        }

        // 3. Setelah nama didapat, minta lagi untuk dicarikan Sprite 2D yang sesuai
        Sprite sprite2D = RumorPhaseManagerMultiplayer.Instance.GetCardSprite2D(cardName);
        if (sprite2D == null)
        {
            Debug.LogWarning($"Sprite 2D tidak ditemukan untuk nama kartu: {cardName}");
            return;
        }

        // 4. Perintahkan UI Viewer untuk menampilkan Sprite tersebut
        RumorCardViewerUI.Instance.ShowCard(sprite2D);
    }
}