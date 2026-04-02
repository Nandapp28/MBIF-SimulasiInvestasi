using UnityEngine;
using UnityEngine.UI;

public class ModeSelectionManager : MonoBehaviour
{
    [Header("Dependencies")]
    public LoadingManager loadingManager; // Drag LoadingManager di Inspector

    [Header("Scene Name")]
    public string gameplaySceneName = "Gameplay"; // Sesuaikan dengan nama scene gameplay Anda

    // Hubungkan fungsi ini ke Button "Normal Mode" di Inspector
    public void SelectTutorialMode()
    {
        GameSettings.StartingFinpoints = 100;
        GameSettings.IsTutorial = true; // AKTIFKAN MODE TUTORIAL
        
        // Reset Tutorial Manager jika ada
        if(TutorialManager.Instance != null) TutorialManager.Instance.ActivateTutorial();

        LoadGameplay();
    }

    public void SelectNormalMode()
    {
        GameSettings.StartingFinpoints = 100;
        GameSettings.IsTutorial = false;
        LoadGameplay();
    }

    public void SelectHardMode()
    {
        GameSettings.StartingFinpoints = 30;
        GameSettings.IsTutorial = false;
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