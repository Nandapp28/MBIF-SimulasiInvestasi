using UnityEngine;
using Photon.Pun;

public class PauseManager : MonoBehaviourPun
{
    public GameObject pauseMenuUI;
    public GameObject playerController;

    private bool isPaused = false;

    private void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false; // Disable script ini jika bukan milik player lokal
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenuUI.SetActive(isPaused);

        if (playerController != null)
        {
            playerController.SetActive(!isPaused);
        }

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuUI.SetActive(false);

        if (playerController != null)
        {
            playerController.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitToMenu()
    {
        PhotonNetwork.LeaveRoom(); // keluar dari room Photon
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
