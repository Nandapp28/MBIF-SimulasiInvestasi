using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class TutorialUIController : MonoBehaviour
{
    public static TutorialUIController Instance { get; private set; }

    [System.Serializable]
    public class UIPackage
    {
        public string packageName; // Nama unik untuk memanggil paket ini
        public GameObject packageContainer; // Parent object yang berisi UI (Text, Image, Button)
        [Header("UI Elements (Optional)")]
        public List<Text> textElements;
        public List<Button> buttonElements;
        public List<Image> imageElements;
    }

    [Header("Main Settings")]
    public GameObject tutorialCanvas; // Referensi ke Canvas Tutorial Utama
    public Button closeTutorialButton; // Tombol global untuk mematikan tutorial

    [Header("UI Packages")]
    public List<UIPackage> uiPackages = new List<UIPackage>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Pastikan semua paket mati di awal
        DeactivateAllPackages();
        
        if (tutorialCanvas != null)
            tutorialCanvas.SetActive(false);

        // Setup tombol penutup global
        if (closeTutorialButton != null)
        {
            closeTutorialButton.onClick.AddListener(CloseTutorial);
        }
    }

    /// <summary>
    /// Aktifkan paket tutorial berdasarkan nama.
    /// Contoh panggil: TutorialUIController.Instance.ShowPackage("IntroSemester1");
    /// </summary>
    public void ShowPackage(string name)
{
    // 1. Matikan semua paket yang mungkin sedang aktif sebelumnya
    DeactivateAllPackages();

    // 2. Cari paket berdasarkan nama
    UIPackage target = uiPackages.FirstOrDefault(p => p.packageName == name);

    if (target != null)
    {
        // 3. Aktifkan Canvas Utama
        if (tutorialCanvas != null) 
            tutorialCanvas.SetActive(true);
            
        // 4. AKTIFKAN CONTAINER UTAMA PAKET (Ini yang memastikan UI muncul)
        if (target.packageContainer != null) 
        {
            target.packageContainer.SetActive(true);
        }
        
        // 5. Jeda waktu permainan
        Time.timeScale = 0f;
        
        Debug.Log($"[Tutorial] Package '{name}' aktif dan Container di-set ke ON.");
    }
    else
    {
        Debug.LogWarning($"[Tutorial] Package dengan nama '{name}' tidak ditemukan!");
    }
}

    /// <summary>
    /// Mematikan Canvas Tutorial dan semua package di dalamnya.
    /// </summary>
    public void CloseTutorial()
    {
        DeactivateAllPackages();
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
        }
        Time.timeScale = 1f;
        // Memainkan sound effect jika diperlukan (referensi dari sistem SfxManager yang ada)
        if (SfxManager.Instance != null && GameManager.Instance != null && GameManager.Instance.skipSound != null)
        {
            SfxManager.Instance.PlaySound(GameManager.Instance.skipSound);
        }
    }

    private void DeactivateAllPackages()
    {
        foreach (var pkg in uiPackages)
        {
            if (pkg.packageContainer != null)
            {
                pkg.packageContainer.SetActive(false);
            }
        }
    }
}