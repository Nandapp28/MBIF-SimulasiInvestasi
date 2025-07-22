using UnityEngine;
using UnityEngine.UI;

public class PanelManager : MonoBehaviour
{
    public GameObject bgPanel;
    public GameObject panelSelectPlayer;

    public Button easyButton;
    public Button normalButton;
    public Button closeButton;

    void Start()
    {
        // Awal: hanya BgPanel yang terlihat
        bgPanel.SetActive(true);
        panelSelectPlayer.SetActive(false);

        // Tambahkan listener untuk tombol
        closeButton.onClick.AddListener(CloseSelectPlayer);
    }

    void CloseSelectPlayer()
    {
        panelSelectPlayer.SetActive(false);
    }
}
