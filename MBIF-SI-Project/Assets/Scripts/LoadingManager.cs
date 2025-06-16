using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public GameObject loadingPanel;
    public Slider slider;

    // Ganti parameter dari int ke string
    public void LoadLevel(string sceneName)
    {
        StartCoroutine(LoadWithDelay(sceneName));
    }

    IEnumerator LoadWithDelay(string sceneName)
    {
        loadingPanel.SetActive(true);
        slider.value = 0;

        float totalDuration = 3f; // total waktu loading 3 detik
        float elapsed = 0f;

        // Mulai proses loading di background
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false; // mencegah scene langsung aktif setelah loading

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / totalDuration);
            slider.value = progress;

            yield return null;
        }

        // Pastikan loading scene sudah selesai
        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}