using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    public Slider progressBar;
    public float logoDisplayTime = 2f;
    public float fillSpeed = 0.01f; // Semakin kecil, semakin lambat

    void Start()
    {
        if (progressBar != null)
            progressBar.value = 0f;

        StartCoroutine(LoadMainScene());
    }

    IEnumerator LoadMainScene()
    {
        // Tampilkan logo dulu
        yield return new WaitForSeconds(logoDisplayTime);

        AsyncOperation operation = SceneManager.LoadSceneAsync("MainMenu");
        operation.allowSceneActivation = false;

        float fakeProgress = 0f;

        while (!operation.isDone)
        {
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Update fake progress sedikit demi sedikit menuju real progress
            if (fakeProgress < realProgress)
            {
                fakeProgress += fillSpeed;
                progressBar.value = fakeProgress;
            }

            // Jika real progress sudah selesai dan bar sudah penuh
            if (realProgress >= 1f && fakeProgress >= 1f)
            {
                operation.allowSceneActivation = true;
            }

            yield return new WaitForSeconds(0.02f); // jeda antar peningkatan (tweakable)
        }
    }
}
