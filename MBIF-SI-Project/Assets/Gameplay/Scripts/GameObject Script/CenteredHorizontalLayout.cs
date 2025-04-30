using UnityEngine;

public class CenteredHorizontalLayout : MonoBehaviour
{
    [Tooltip("Jarak antar elemen (dalam satuan pixel/Unity unit).")]
    public float spacing = 100f;

    [Tooltip("Atur layout otomatis setiap frame (untuk debugging/dinamis).")]
    public bool autoUpdate = false;

    void Update()
    {
        if (autoUpdate)
        {
            ArrangeItems();
        }
    }

    [ContextMenu("Arrange Items")]
    public void ArrangeItems()
    {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        float totalWidth = (childCount - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform rt = transform.GetChild(i).GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(startX + i * spacing, 0f);
            }
        }
    }
}
