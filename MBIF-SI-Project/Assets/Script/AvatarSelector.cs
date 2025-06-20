using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarSelector : MonoBehaviour
{
    [Header("Dependencies")]
    public ProfileManager profileManager;
    public Image avatarImage; // Ini adalah image dari tombol/avatar yang dipilih user
    public AvatarPicker avatarPicker;

    public void SelectPresetAvatar(Image buttonImage)
    {
        if (profileManager != null && buttonImage != null)
        {
            profileManager.SelectProfilePicture(buttonImage.sprite);
        }
    }

    public void SelectFromGallery()
    {
        avatarPicker.PickImageFromGallery();
    }
}