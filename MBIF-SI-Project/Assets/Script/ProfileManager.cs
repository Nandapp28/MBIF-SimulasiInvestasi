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
    private Sprite selectedAvatar;
    private Sprite selectedBorder;

    // Tambahkan variabel untuk menyimpan nama aset yang akan disimpan ke Firebase
    private string selectedAvatarName;
    private string selectedBorderName;

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

            var userTask = dbRef.Child("users").Child(userId).GetValueAsync();

            yield return new WaitUntil(() => userTask.IsCompleted);

            if (userTask.Exception == null)
            {
                DataSnapshot snapshot = userTask.Result;
                if (snapshot.Exists)
                {
                    // === Load Username ===
                    if (snapshot.Child("userName") != null)
                    {
                        string userName = snapshot.Child("userName").Value.ToString();
                        nicknameText.text = userName;
                    }
                    else
                    {
                        nicknameText.text = "Guest";
                    }

                    // === Load Avatar ===
                    if (snapshot.Child("avatarName").Exists)
                    {
                        string savedAvatarName = snapshot.Child("avatarName").Value.ToString();
                        Sprite avatarSprite = Resources.Load<Sprite>("Avatars/" + savedAvatarName); // Pastikan path Resources/Avatars/
                        if (avatarSprite != null)
                        {
                            profilePicture.sprite = avatarSprite;
                            selectedAvatar = avatarSprite; // Set selectedAvatar juga agar bisa di-save ulang jika tidak diubah
                            selectedAvatarName = savedAvatarName; // Simpan nama aset yang sudah dimuat
                            Debug.Log($"ProfileManager: Memuat avatar '{savedAvatarName}'");
                        }
                        else
                        {
                            Debug.LogWarning($"ProfileManager: Avatar asset '{savedAvatarName}' tidak ditemukan di Resources/Avatars/.");
                            // Opsional: set default avatar
                        }
                    }
                    else
                    {
                        Debug.Log("ProfileManager: Tidak ada avatar tersimpan di Firebase.");
                        // Opsional: set default avatar
                    }

                    // === Load Border ===
                    if (snapshot.Child("borderName").Exists)
                    {
                        string savedBorderName = snapshot.Child("borderName").Value.ToString();
                        Sprite borderSprite = Resources.Load<Sprite>("Borders/" + savedBorderName); // Pastikan path Resources/Borders/
                        if (borderSprite != null)
                        {
                            profileBorder.sprite = borderSprite;
                            selectedBorder = borderSprite; // Set selectedBorder juga
                            selectedBorderName = savedBorderName; // Simpan nama aset yang sudah dimuat
                            Debug.Log($"ProfileManager: Memuat border '{savedBorderName}'");
                        }
                        else
                        {
                            Debug.LogWarning($"ProfileManager: Border asset '{savedBorderName}' tidak ditemukan di Resources/Borders/.");
                            // Opsional: set default border
                        }
                    }
                    else
                    {
                        Debug.Log("ProfileManager: Tidak ada border tersimpan di Firebase.");
                        // Opsional: set default border
                    }
                }
                else
                {
                    nicknameText.text = "Guest";
                    Debug.Log("ProfileManager: Tidak ada profil pengguna tersimpan di Firebase.");
                    // Opsional: set default avatar/border
                }
            }
            else
            {
                Debug.LogWarning("Gagal mengambil data user dari Firebase: " + userTask.Exception);
                nicknameText.text = "Error";
            }
        }
        else
        {
            Debug.LogWarning("ProfileManager: Pengguna belum login. Tidak bisa memuat data.");
            nicknameText.text = "Guest";
        }
    }

    public void OnProfileClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        popupPanel.SetActive(true);
    }

    public void OnCancelClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        popupPanel.SetActive(false);
    }

    // === MODIFIKASI OnSaveClicked() ===
    public void OnSaveClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        // Terapkan perubahan ke tampilan profil
        if (selectedAvatar != null)
        {
            profilePicture.sprite = selectedAvatar;
        }
        if (selectedBorder != null)
        {
            profileBorder.sprite = selectedBorder;
        }

        // Simpan perubahan ke Firebase Realtime Database
        StartCoroutine(SaveProfileChangesToFirebase());

        popupPanel.SetActive(false);
    }

    // === Fungsi Coroutine baru untuk menyimpan perubahan ke Firebase ===
    private IEnumerator SaveProfileChangesToFirebase()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("Pengguna belum login. Tidak dapat menyimpan perubahan profil.");
            yield break; // Keluar dari coroutine
        }

        string userId = auth.CurrentUser.UserId;
        DatabaseReference userRef = dbRef.Child("users").Child(userId);

        Dictionary<string, object> updates = new Dictionary<string, object>();

        // Hanya tambahkan ke update jika ada sprite yang dipilih
        if (selectedAvatar != null)
        {
            // Ambil nama dari sprite yang dipilih
            // Penting: Pastikan nama sprite sesuai dengan nama file aset di Resources
            updates["avatarName"] = selectedAvatar.name;
            Debug.Log($"Menyimpan avatar: {selectedAvatar.name}");
        }

        if (selectedBorder != null)
        {
            // Ambil nama dari sprite yang dipilih
            updates["borderName"] = selectedBorder.name;
            Debug.Log($"Menyimpan border: {selectedBorder.name}");
        }

        if (updates.Count > 0)
        {
            var saveTask = userRef.UpdateChildrenAsync(updates);
            yield return new WaitUntil(() => saveTask.IsCompleted);

            if (saveTask.Exception == null)
            {
                Debug.Log("Perubahan avatar dan border berhasil disimpan ke Firebase.");
            }
            else
            {
                Debug.LogError("Gagal menyimpan perubahan avatar dan border ke Firebase: " + saveTask.Exception);
            }
        }
        else
        {
            Debug.Log("Tidak ada perubahan avatar atau border untuk disimpan.");
        }
    }


    // === MODIFIKASI SelectProfilePicture() ===
    public void SelectProfilePicture(Sprite avatar)
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        selectedAvatar = avatar;
        // Simpan juga nama sprite untuk disimpan ke Firebase
        selectedAvatarName = avatar.name; 
        Debug.Log($"Avatar '{avatar.name}' dipilih sementara.");
    }

    // === MODIFIKASI SelectBorder() ===
    public void SelectBorder(Sprite border)
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        selectedBorder = border;
        // Simpan juga nama sprite untuk disimpan ke Firebase
        selectedBorderName = border.name;
        Debug.Log($"Border '{border.name}' dipilih sementara.");
    }
}