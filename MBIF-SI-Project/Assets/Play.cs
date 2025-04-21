using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // Pastikan untuk mengimpor namespace ini
using UnityEngine.UI; // Untuk menggunakan UI

public class GameModeSelector : MonoBehaviour
{
    // Indeks scene untuk Singleplayer dan Multiplayer
    public int singleplayerSceneIndex;
    public int multiplayerSceneIndex;

    // Fungsi ini akan dipanggil ketika tombol Singleplayer ditekan
    public void OnSingleplayerButtonPress()
    {
        SceneManager.LoadScene(1);
    }

    // Fungsi ini akan dipanggil ketika tombol Multiplayer ditekan
    public void OnMultiplayerButtonPress()
    {
        SceneManager.LoadScene(2);
    }

    // Fungsi ini akan dipanggil ketika tombol Back ditekan
    public void OnBackButtonPress()
    {
        SceneManager.LoadScene(0);
    }
}