using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameModeSelector : MonoBehaviour
{
    // Indeks scene untuk Singleplayer dan Multiplayer (jika ingin pakai index)
    public GameObject multiplayerWarningPopup; 
    public int singleplayerSceneIndex;
    public int multiplayerSceneIndex;

    // Variabel statis untuk menyimpan scene sebelumnya
    private static string previousSceneName;
    

    // Fungsi ini akan dipanggil ketika tombol Singleplayer ditekan
    public void OnSingleplayerButtonPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        SaveCurrentScene();

        // Cek status tutorial HANYA dari penyimpanan lokal (PlayerPrefs)
        // Jika key belum ada, default-nya adalah "no"
        string tutorialStatus = PlayerPrefs.GetString("hasCompletedTutorial", "no");

        if (tutorialStatus == "no")
        {
            // Jika belum menyelesaikan tutorial, arahkan ke scene tutorial
            SceneManager.LoadScene("TutorialScene");
        }
        else
        {
            // Jika sudah, langsung ke gameplay
            SceneManager.LoadScene("Gameplay");
        }
    }

    // Fungsi ini akan dipanggil ketika tombol Multiplayer ditekan
    public void OnMultiplayerButtonPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        string tutorialStatus = PlayerPrefs.GetString("hasCompletedTutorial", "no");
        string warningStatus = PlayerPrefs.GetString("hasWarning", "no");

        // Cek jika tutorial BELUM selesai DAN warning BELUM pernah ditampilkan
        if (tutorialStatus == "no" && warningStatus == "no")
        {
            // Tampilkan popup warning
            if (multiplayerWarningPopup != null)
            {
                multiplayerWarningPopup.SetActive(true);
            }
        }
        else
        {
            // Jika tidak, langsung masuk ke lobi
            SaveCurrentScene();
            SceneManager.LoadScene("Lobby");
        }
    }
     public void ConfirmWarningAndProceedToLobby()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        // 1. Set status warning menjadi "yes" agar tidak muncul lagi
        PlayerPrefs.SetString("hasWarning", "yes");
        PlayerPrefs.Save();


        // 3. Sembunyikan popup
        if (multiplayerWarningPopup != null)
        {
            multiplayerWarningPopup.SetActive(false);
        }

        // 4. Lanjutkan ke lobi
    }
    public void OnMainMenuButtonPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        SaveCurrentScene();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnPlayButtonPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        SaveCurrentScene();
        SceneManager.LoadScene("Play");
    }

    public void OnOptionPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        // Memuat scene berdasarkan indeks
        SceneManager.LoadScene("Options");
    }

    public void OnShopPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        // Memuat scene berdasarkan indeks
        SceneManager.LoadScene("Shop");
    }

    public void OnAvatarShopPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        // Memuat scene berdasarkan indeks
        SceneManager.LoadScene("AvatarShop");
    }

    public void OnBorderShopPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        // Memuat scene berdasarkan indeks
        SceneManager.LoadScene("BorderShop");
    }

    public void OnBackButtonPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        if (SceneTracker.Instance != null && !string.IsNullOrEmpty(SceneTracker.Instance.PreviousSceneName))
        {
            SceneManager.LoadScene(SceneTracker.Instance.PreviousSceneName);
        }
        else
        {
            SceneManager.LoadScene("Play");
        }
    }

    public void OnFriendListButtonPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        SaveCurrentScene();
        SceneManager.LoadScene("FriendList");
    }

    public void OnProfilePress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        SaveCurrentScene();
        SceneManager.LoadScene("Profile");
    }

    // Menyimpan nama scene saat ini
    private void SaveCurrentScene()
    {
        previousSceneName = SceneManager.GetActiveScene().name;
    }
    
    public void OnHelpButtonPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        SaveCurrentScene();
        SceneManager.LoadScene("TutorialScene");
    }
}