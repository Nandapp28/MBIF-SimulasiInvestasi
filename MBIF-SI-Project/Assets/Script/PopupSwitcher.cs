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
        easyButton.onClick.AddListener(ShowSelectPlayer);
        normalButton.onClick.AddListener(ShowSelectPlayer);
        closeButton.onClick.AddListener(CloseSelectPlayer);
    }

    void ShowSelectPlayer()
    {
        panelSelectPlayer.SetActive(true);
    }

    void CloseSelectPlayer()
    {
        panelSelectPlayer.SetActive(false);
    }
}
