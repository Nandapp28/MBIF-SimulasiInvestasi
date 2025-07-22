using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Indeks scene yang ingin dituju
    public int MainMenuIndex;

    // Fungsi ini akan dipanggil ketika tombol ditekan
    public void OnPlayPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        // Memuat scene berdasarkan indeks
        SceneManager.LoadScene("Play");
    }
}