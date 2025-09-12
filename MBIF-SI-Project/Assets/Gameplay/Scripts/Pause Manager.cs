using UnityEngine;

public class PauseManager : MonoBehaviour
{
    // Tarik (drag) GameObject Panel UI Pause Anda ke slot ini di Inspector Unity
    public GameObject pauseMenuUI;

    private bool isPaused = false;

    void Start()
    {
        // Pastikan menu pause tidak aktif saat game dimulai
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
    }

    void Update()
    {
        // Cek jika tombol 'Escape' ditekan
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    // Fungsi untuk melanjutkan game
    public void Resume()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f; // Mengembalikan waktu ke normal
        isPaused = false;
        Debug.Log("Game Resumed!");
    }

    // Fungsi untuk mem-pause game
    public void Pause()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
        Time.timeScale = 0f; // Menghentikan waktu
        isPaused = true;
        Debug.Log("Game Paused!");
    }
}