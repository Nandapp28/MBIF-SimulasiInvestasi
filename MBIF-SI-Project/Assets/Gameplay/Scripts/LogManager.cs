// File: Scripts/LogManager.cs (Versi Final dengan Scroll)

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LogManager : MonoBehaviour
{
    public static LogManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Panel utama yang menjadi latar belakang rekap log.")]
    [SerializeField] private GameObject logPanel;

    [Tooltip("Komponen Text untuk menampilkan seluruh rekap log.")]
    [SerializeField] private Text logText;

    [Tooltip("Tombol untuk menampilkan/menyembunyikan panel log.")]
    [SerializeField] private Button toggleLogButton;

    [Tooltip("Tombol untuk menutup panel dari dalam panel itu sendiri.")]
    [SerializeField] private Button closeButton;

    // List untuk menyimpan semua pesan yang akan direkap
    private List<string> logMessages = new List<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (logPanel != null)
        {
            logPanel.SetActive(false);
        }

        if (toggleLogButton != null)
        {
            toggleLogButton.onClick.AddListener(ToggleLogPanel);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ToggleLogPanel);
        }

        UpdateLogDisplay();
    }

    public void ToggleLogPanel()
    {
        if (logPanel != null)
        {
            bool isActive = logPanel.activeSelf;
            logPanel.SetActive(!isActive);
        }
    }

    /// <summary>
    /// Menambahkan pesan baru ke dalam rekap dan memperbarui tampilan.
    /// </summary>
    public void AddLog(string message)
    {
        // Ganti strip (-) dengan simbol bulat (•)
        logMessages.Add($"• {message}");
        UpdateLogDisplay();
    }

    private void UpdateLogDisplay()
    {
        if (logText != null)
        {
            // Tambahkan dua baris baru (\n\n) untuk memberikan jarak antar pesan.
            logText.text = string.Join("\n\n", logMessages);
        }
    }
}