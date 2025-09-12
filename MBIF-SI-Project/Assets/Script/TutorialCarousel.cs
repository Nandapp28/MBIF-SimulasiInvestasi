using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Wajib untuk berpindah scene
using TMPro; // Wajib untuk mengubah teks tombol
using System.Collections.Generic;
using System.Linq;

public class TutorialCarousel : MonoBehaviour
{
    [Header("UI Elements")]
    public Image tutorialImage;
    public Button nextButton;
    public TextMeshProUGUI nextButtonText; // Referensi ke teks tombol Next
    public Button previousButton;

    [Header("Configuration")]
    [Tooltip("Nama folder di dalam folder 'Resources' tempat menyimpan gambar.")]
    public string resourceFolderPath = "TutorialImages";
    [Tooltip("Nama scene Main Menu yang akan dituju.")]
    public string mainMenuSceneName = "Play";

    private List<Sprite> tutorialSprites;
    private int currentIndex = 0;

    void Awake()
    {
        tutorialSprites = Resources.LoadAll<Sprite>(resourceFolderPath).ToList();
        if (tutorialSprites.Count == 0)
        {
            Debug.LogError($"Tidak ada sprite yang ditemukan di folder 'Resources/{resourceFolderPath}'.");
            gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (tutorialSprites.Count > 0)
        {
            UpdateTutorialUI();
        }
    }

    public void NextImage()
    {
        // JIKA sudah di gambar terakhir, klik tombol akan ke Main Menu
        if (currentIndex >= tutorialSprites.Count - 1)
    {
        // 1. Set status "yes" di device (PlayerPrefs)
        PlayerPrefs.SetString("hasCompletedTutorial", "yes");
        PlayerPrefs.Save();

        
        
        // Pindah ke scene selanjutnya
        SceneManager.LoadScene("Play");
    }
        else // JIKA BELUM, lanjut ke gambar berikutnya
        {
            currentIndex++;
            UpdateTutorialUI();
        }
    }

    public void PreviousImage()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateTutorialUI();
        }
    }

    private void UpdateTutorialUI()
    {
        tutorialImage.sprite = tutorialSprites[currentIndex];
        previousButton.interactable = (currentIndex > 0);

        // Cek jika ini adalah gambar terakhir
        if (currentIndex >= tutorialSprites.Count - 1)
        {
            nextButtonText.text = "Tutorial Selesai"; // Ganti teks tombol
        }
        else
        {
            nextButtonText.text = "Next"; // Kembalikan teks tombol ke "Next"
        }
    }
}