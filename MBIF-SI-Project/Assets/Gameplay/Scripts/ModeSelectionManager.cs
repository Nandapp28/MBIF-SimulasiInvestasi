using UnityEngine;
using UnityEngine.UI;

public class ModeSelectionManager : MonoBehaviour
{
    [Header("Dependencies")]
    public LoadingManager loadingManager; // Drag LoadingManager di Inspector

    [Header("Scene Name")]
    public string gameplaySceneName = "Gameplay"; // Sesuaikan dengan nama scene gameplay Anda

    // Hubungkan fungsi ini ke Button "Normal Mode" di Inspector
    public void SelectNormalMode()
    {
        // Set finpoin ke 100
        GameSettings.StartingFinpoints = 100;
        
        // Pindah scene
        LoadGameplay();
    }

    // Hubungkan fungsi ini ke Button "Hard Mode" di Inspector
    public void SelectHardMode()
    {
        // Set finpoin ke 30
        GameSettings.StartingFinpoints = 30;
        
        // Pindah scene
        LoadGameplay();
    }

    private void LoadGameplay()
    {
        if (loadingManager != null)
        {
            loadingManager.LoadLevel(gameplaySceneName);
        }
        else
        {
            // Fallback jika LoadingManager lupa di-assign
            Debug.LogWarning("LoadingManager belum di-assign, menggunakan SceneManager standar.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
        }
    }
}