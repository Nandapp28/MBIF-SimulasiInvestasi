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
        public string packageName;
        // Diubah menjadi List agar satu package bisa punya banyak panel/halaman
        public List<GameObject> packageContainers = new List<GameObject>(); 
        
        [Header("UI Elements (Optional)")]
        public List<Text> textElements;
        public List<Button> buttonElements;
        public List<Image> imageElements;

        [HideInInspector] public int currentStepIndex = 0; // Melacak halaman aktif
    }

    [Header("Main Settings")]
    public GameObject tutorialCanvas;
    // Tombol ini sekarang berfungsi sebagai Next atau Close
    public Button actionButton; 
    public Text actionButtonText; 

    [Header("UI Packages")]
    public List<UIPackage> uiPackages = new List<UIPackage>();

    private UIPackage activePackage;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        DeactivateAllPackages();
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);

        if (actionButton != null)
        {
            actionButton.onClick.AddListener(HandleNextOrClose);
        }
    }

    public void ShowPackage(string name)
    {
        DeactivateAllPackages();
        activePackage = uiPackages.FirstOrDefault(p => p.packageName == name);

        if (activePackage != null && activePackage.packageContainers.Count > 0)
        {
            if (tutorialCanvas != null) tutorialCanvas.SetActive(true);
            
            activePackage.currentStepIndex = 0;
            ShowCurrentStep();
            
            Time.timeScale = 0f;
            Debug.Log($"[Tutorial] Package '{name}' dimulai.");
        }
    }

    private void ShowCurrentStep()
    {
        // Matikan semua container dalam package ini dulu
        foreach (var container in activePackage.packageContainers)
        {
            container.SetActive(false);
        }

        // Aktifkan container sesuai step saat ini
        GameObject currentPanel = activePackage.packageContainers[activePackage.currentStepIndex];
        currentPanel.SetActive(true);

        // Update teks tombol: Jika masih ada halaman berikut, tampilkan "Next", jika terakhir "Close"
        if (actionButtonText != null)
        {
            actionButtonText.text = (activePackage.currentStepIndex < activePackage.packageContainers.Count - 1) 
                ? "Next" 
                : "Close";
        }
    }

    public void HandleNextOrClose()
    {
        if (activePackage == null) return;

        if (activePackage.currentStepIndex < activePackage.packageContainers.Count - 1)
        {
            // Pindah ke panel berikutnya
            activePackage.currentStepIndex++;
            ShowCurrentStep();
        }
        else
        {
            // Jika sudah di panel terakhir, tutup tutorial
            CloseTutorial();
        }
    }

    public void CloseTutorial()
    {
        DeactivateAllPackages();
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);
        
        Time.timeScale = 1f;
        activePackage = null;

        if (SfxManager.Instance != null && GameManager.Instance != null && GameManager.Instance.skipSound != null)
        {
            SfxManager.Instance.PlaySound(GameManager.Instance.skipSound);
        }
    }

    private void DeactivateAllPackages()
    {
        foreach (var pkg in uiPackages)
        {
            foreach (var container in pkg.packageContainers)
            {
                if (container != null) container.SetActive(false);
            }
        }
    }
}