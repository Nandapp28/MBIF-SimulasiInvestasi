using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameModeSelector : MonoBehaviour
{
    // Indeks scene untuk Singleplayer dan Multiplayer
    public int singleplayerSceneIndex = 1;
    public int multiplayerSceneIndex = 2;

    // Fungsi ini dipanggil ketika tombol Singleplayer ditekan
    public void OnSingleplayerButtonPress()
    {
        PlayClickSfx();
        SceneManager.LoadScene(1);
    }

    // Fungsi ini dipanggil ketika tombol Multiplayer ditekan
    public void OnMultiplayerButtonPress()
    {
        PlayClickSfx();
        SceneManager.LoadScene(2);
    }

    // Fungsi ini dipanggil ketika tombol Back ditekan
    public void OnBackButtonPress()
    {
        PlayClickSfx();
        SceneManager.LoadScene(0);
    }

    // Fungsi tambahan untuk memainkan SFX tombol
    private void PlayClickSfx()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
    }
}
