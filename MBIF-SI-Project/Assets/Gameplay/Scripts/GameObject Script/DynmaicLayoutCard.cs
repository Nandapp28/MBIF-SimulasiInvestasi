using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class DynamicGridConstraint : MonoBehaviour
{
    public GameManager gameManager;
    private GridLayoutGroup grid;

    private int lastPlayerCount = -1;

    void Start()
    {
        grid = GetComponent<GridLayoutGroup>();

        if (gameManager == null)
        {
            Debug.LogError("❌ GameManager belum di-assign.");
        }
    }

    void Update()
    {
        if (gameManager == null) return;

        int currentPlayerCount = gameManager.GetPlayerCount();

        // Hanya update jika nilai berubah
        if (currentPlayerCount != lastPlayerCount)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = currentPlayerCount;
            lastPlayerCount = currentPlayerCount;

            Debug.Log($"🔄 Grid constraintCount updated to: {currentPlayerCount}");
        }
    }
}
