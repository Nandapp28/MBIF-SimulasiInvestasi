using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BorderSelector : MonoBehaviour
{
    [Header("Dependencies")]
    public ProfileManager profileManager;
    public Image borderImage; // Gambar border yang ingin dipilih

    // (Opsional) Preview sebelum klik save
    public Image previewBorderImage;

    public void OnClickSelectBorder()
    {
        if (profileManager != null && borderImage != null)
        {
            profileManager.SelectBorder(borderImage.sprite);

            // Preview langsung saat memilih
            if (previewBorderImage != null)
            {
                previewBorderImage.sprite = borderImage.sprite;
            }
        }
        else
        {
            Debug.LogWarning("ProfileManager atau BorderImage belum di-assign di inspector.");
        }
    }
}
