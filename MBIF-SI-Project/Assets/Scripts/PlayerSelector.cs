using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerButtonSelector : MonoBehaviour
{
    [Header("Assign all player buttons here")]
    public List<Button> playerButtons;

    [Header("Colors")]
    public Color selectedColor = Color.green;
    public Color defaultColor = Color.white;

    private Button selectedButton;

    void Start()
    {
        // Tambahkan listener ke setiap tombol
        foreach (Button btn in playerButtons)
        {
            btn.onClick.AddListener(() => OnPlayerButtonClicked(btn));
        }

        // Inisialisasi: pilih tombol pertama jika ada
        if (playerButtons.Count > 0)
        {
            OnPlayerButtonClicked(playerButtons[0]);
        }
    }

    void OnPlayerButtonClicked(Button clickedButton)
    {
        // Reset warna semua tombol
        foreach (Button btn in playerButtons)
        {
            btn.image.color = defaultColor;
        }

        // Ubah warna tombol yang diklik
        clickedButton.image.color = selectedColor;

        // Simpan tombol yang terpilih
        selectedButton = clickedButton;

        Debug.Log("Selected player: " + clickedButton.name);
    }
}
