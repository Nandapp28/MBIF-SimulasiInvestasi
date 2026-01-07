using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using Firebase.Auth;

public class IntroManager : MonoBehaviour
{
    public Slider progressBar;
    public float logoDisplayTime = 2f;
    public float fillSpeed = 0.01f; // Semakin kecil, semakin lambat

    private const string GuestUserIdKey = "GuestUserId"; // Gunakan kunci yang sama dengan AnonymousLogin

    void Start()
    {
        if (progressBar != null)
            progressBar.value = 0f;

        StartCoroutine(InitializeGame());
    }

    IEnumerator InitializeGame()
    {
        // Tampilkan logo dulu
        yield return new WaitForSeconds(logoDisplayTime);

        string targetSceneName = "Login";

        // Cek apakah ID guest sudah tersimpan di PlayerPrefs
        if (PlayerPrefs.HasKey(GuestUserIdKey))
        {
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;

            if (auth.CurrentUser != null)
            {
                Debug.Log("ID Guest ditemukan DAN Sesi Firebase Aktif. Ke MainMenu.");
                targetSceneName = "MainMenu"; 
            }
            else
            {
                // Jika ID lokal ada, tapi Firebase belum login (kasus habis install ulang),
                // Kita LEMPAR ke scene 'Login'. 
                // Di sana, script 'AnonymousLogin.cs' akan mendeteksi ID lokal 
                // dan menjalankan 'AutoLoginRecovery' secara otomatis.
                Debug.Log("ID Guest ditemukan tapi Sesi Firebase Mati. Ke Login untuk Recovery.");
                targetSceneName = "Login";
            }
        }
        else
        {
            Debug.Log("ID Guest tidak ditemukan. Memuat scene Login...");
            targetSceneName = "Login"; // Arahkan ke Login jika ID tidak ada
        }

        // Sekarang, mulai operasi pemuatan scene berdasarkan targetSceneName
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
        operation.allowSceneActivation = false; // Jangan langsung aktifkan scene

        float fakeProgress = 0f;

        while (!operation.isDone)
        {
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f);

            // Update fake progress sedikit demi sedikit menuju real progress
            if (fakeProgress < realProgress)
            {
                fakeProgress += fillSpeed;
                if (progressBar != null) // Pastikan progressBar tidak null sebelum mengaksesnya
                    progressBar.value = fakeProgress;
            }

            // Jika real progress sudah selesai dan bar sudah penuh
            if (realProgress >= 1f && fakeProgress >= 1f)
            {
                operation.allowSceneActivation = true; // Aktifkan scene
            }

            yield return new WaitForSeconds(0.02f); // jeda antar peningkatan (tweakable)
        }
    }
}