// File: Scripts/RumorCardClickHandler.cs (Versi Final)

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RumorCardClickHandler : MonoBehaviour
{
    private Renderer cardRenderer;
    private RumorPhaseManager rumorPhaseManager;

    void Awake()
    {
        cardRenderer = GetComponent<Renderer>();
        if (cardRenderer == null)
        {
            Debug.LogError("Tidak ada komponen Renderer yang ditemukan!", this);
        }
    }

    void Start()
    {
        rumorPhaseManager = FindObjectOfType<RumorPhaseManager>();
        if (rumorPhaseManager == null)
        {
            Debug.LogError("RumorPhaseManager tidak ditemukan di scene!");
        }
    }

    // HAPUS FUNGSI OnMouseDown() YANG LAMA

    /// <summary>
    /// Fungsi publik ini akan dipanggil oleh InputManager saat kartu ini diklik
    /// dan tidak terhalang oleh UI.
    /// </summary>
    public void TriggerCardView()
    {
        if (!gameObject.activeSelf || RumorCardViewerUI.Instance == null || rumorPhaseManager == null || cardRenderer == null || cardRenderer.material == null)
        {
            return;
        }

        Texture texture3D = cardRenderer.material.mainTexture;
        if (texture3D == null) return;

        string cardName = rumorPhaseManager.GetCardNameFromTexture(texture3D);
        if (string.IsNullOrEmpty(cardName)) return;

        Sprite sprite2D = rumorPhaseManager.GetCardSprite2D(cardName);
        if (sprite2D == null) return;
        
        RumorCardViewerUI.Instance.ShowCard(sprite2D);
    }
}