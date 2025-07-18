using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarSelector : MonoBehaviour
{
    [Header("Dependencies")]
    
    public Image Image;

    public void SelectPresetAvatar(Image buttonImage)
    {
        // MODIFIKASI: Ganti 'profileManager' dengan 'ProfileManager.Instance'
        if (ProfileManager.Instance != null && buttonImage != null)
        {
            ProfileManager.Instance.SelectProfilePicture(buttonImage.sprite);
        }
        else
        {
            Debug.LogError("ProfileManager.Instance belum siap atau buttonImage null.");
        }
    }

    public void SelectPresetBorder(Image buttonImage)
    {
        // MODIFIKASI: Ganti 'profileManager' dengan 'ProfileManager.Instance'
        if (ProfileManager.Instance != null && buttonImage != null)
        {
            ProfileManager.Instance.SelectBorder(buttonImage.sprite);
        }
        else
        {
            Debug.LogError("ProfileManager.Instance belum siap atau buttonImage null.");
        }
    }

    public void SelectFromGallery()
    {
        // MODIFIKASI: Ganti 'avatarPicker' dengan 'AvatarPicker.Instance'
        if (AvatarPicker.Instance != null)
        {
            AvatarPicker.Instance.PickImageFromGallery();
        }
        else
        {
            Debug.LogError("AvatarPicker.Instance belum siap.");
        }
    }
}