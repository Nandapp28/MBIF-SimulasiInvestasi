using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProfileManager : MonoBehaviour
{
    [Header("Profile Display")]
    public Image profileBorder;
    public Image profilePicture;
    public Text nicknameText;

    [Header("Popup References")]
    public GameObject popupPanel;
    public InputField inputNickname;

    private Sprite selectedAvatar;
    private Sprite selectedBorder;

    void Start()
    {
        popupPanel.SetActive(false);
    }

    public void OnProfileClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        popupPanel.SetActive(true);
        inputNickname.text = nicknameText.text;
    }

    public void OnCancelClicked()
    {

        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        popupPanel.SetActive(false);
        selectedAvatar = null;
        selectedBorder = null;
    }

    public void OnSaveClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        nicknameText.text = inputNickname.text;

        if (selectedAvatar != null)
            profilePicture.sprite = selectedAvatar;

        if (selectedBorder != null)
            profileBorder.sprite = selectedBorder;

        popupPanel.SetActive(false);
    }

    public void SelectProfilePicture(Sprite avatar)
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        selectedAvatar = avatar;
    }

    public void SelectBorder(Sprite border)
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        selectedBorder = border;
    }
}
