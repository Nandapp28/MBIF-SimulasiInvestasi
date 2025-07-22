using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    public GameObject tabPlayerPanelPrefab; 
    public Transform panelParent; 

    private GameObject spawnedPanel;

    // Fungsi dipanggil saat diklik
    public void OnClickPlayerUI()
    {
        if (spawnedPanel == null)
        {
            spawnedPanel = Instantiate(tabPlayerPanelPrefab, panelParent);
        }
        else
        {
            bool isActive = spawnedPanel.activeSelf;
            spawnedPanel.SetActive(!isActive); // toggle
        }
    }
}
