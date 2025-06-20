using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Pakai ini kalau kamu pakai TextMeshProUGUI
using Firebase.Auth;
using Firebase.Database;

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

    // Firebase references
    private FirebaseAuth auth;
    private DatabaseReference dbRef;

    void Start()
    {
        popupPanel.SetActive(false);
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        StartCoroutine(LoadUserData());
    }

    private IEnumerator LoadUserData()
    {
        if (auth.CurrentUser != null)
        {
            string userId = auth.CurrentUser.UserId;

            var userTask = dbRef.Child("users").Child(userId).GetValueAsync(); // ← disesuaikan

            yield return new WaitUntil(() => userTask.IsCompleted);

            if (userTask.Exception == null)
            {
                DataSnapshot snapshot = userTask.Result;
                if (snapshot.Exists && snapshot.Child("userName") != null)
                {
                    string userName = snapshot.Child("userName").Value.ToString();
                    nicknameText.text = userName;
                }
                else
                {
                    nicknameText.text = "Guest";
                }
            }
            else
            {
                Debug.LogWarning("Gagal mengambil data user dari Firebase: " + userTask.Exception);
                nicknameText.text = "Error";
            }
        }
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
