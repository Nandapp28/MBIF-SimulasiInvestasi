using UnityEngine;

public class SettingMenuMultiplayer : MonoBehaviour
{
    // Tarik (drag) GameObject Panel UI Anda ke slot ini di Inspector Unity
    public GameObject menuUI;

    private bool isMenuOpen = false;

    void Start()
    {
        // Pastikan menu tidak aktif saat game dimulai
        if (menuUI != null)
        {
            menuUI.SetActive(false);
        }
    }

    void Update()
    {
        // Cek jika tombol 'Escape' ditekan
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMenuOpen)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    // Fungsi untuk menutup menu
    // Anda bisa memanggil ini dari tombol "Close" atau "Resume" di UI Anda
    public void CloseMenu()
    {
        if (menuUI != null)
        {
            menuUI.SetActive(false);
        }
        isMenuOpen = false;
        Debug.Log("Menu Closed!");
    }

    // Fungsi untuk membuka menu
    public void OpenMenu()
    {
        if (menuUI != null)
        {
            menuUI.SetActive(true);
        }
        isMenuOpen = true;
        Debug.Log("Menu Opened!");
    }
}