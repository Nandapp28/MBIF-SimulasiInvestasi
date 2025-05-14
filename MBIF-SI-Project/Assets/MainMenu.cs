using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void OnPlayPress()
    {
        // Mainkan SFX jika ada SfxManager
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        SceneManager.LoadScene(3);
    }

    public void OnOptionPress()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        SceneManager.LoadScene(4);
    }
}
