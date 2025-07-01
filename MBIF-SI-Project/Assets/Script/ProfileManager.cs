using System.Collections;
using System.Collections.Generic; // Untuk Dictionary
using UnityEngine;
using UnityEngine.UI; // Untuk Image
using UnityEngine.SceneManagement; // Untuk SceneManager
using TMPro; // Untuk TextMeshProUGUI
using Firebase.Auth; // Untuk FirebaseAuth
using Firebase.Database; // Untuk DatabaseReference, DataSnapshot

public class ProfileManager : MonoBehaviour
{
    [Header("Profile Display")]
    public Image profileBorder; // Untuk menampilkan border
    public Image profilePicture; // Untuk menampilkan avatar
    public TextMeshProUGUI nicknameText; // Untuk menampilkan nickname

    // Variabel lokal untuk menyimpan pilihan sementara avatar dan border
    [Header("Selections (Temporary)")]
    private Sprite selectedAvatar;
    private Sprite selectedBorder;

    // Nama aset (string) dari pilihan sementara, untuk disimpan ke Firebase nanti
    private string selectedAvatarName;
    private string selectedBorderName;

    // Firebase references
    private FirebaseAuth auth;
    private DatabaseReference dbRef;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        StartCoroutine(LoadUserData()); // Muat data pengguna saat script aktif
    }

    // Coroutine untuk memuat data pengguna dari Firebase
    private IEnumerator LoadUserData()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogWarning("ProfileManager: Pengguna belum login. Tidak bisa memuat data.");
            nicknameText.text = "Guest";

            yield break;
        }

        string userId = auth.CurrentUser.UserId;
        var userTask = dbRef.Child("users").Child(userId).GetValueAsync();

        yield return new WaitUntil(() => userTask.IsCompleted); // Tunggu task selesai

        if (userTask.Exception == null)
        {
            DataSnapshot snapshot = userTask.Result;
            if (snapshot.Exists)
            {
                // Muat Username
                nicknameText.text = snapshot.Child("userName").Exists ? snapshot.Child("userName").Value.ToString() : "Guest";

                // Muat Avatar
                string savedAvatarName = snapshot.Child("avatarName").Exists ? snapshot.Child("avatarName").Value.ToString() : null;
                if (!string.IsNullOrEmpty(savedAvatarName))
                {
                    Sprite avatarSprite = Resources.Load<Sprite>("Avatars/" + savedAvatarName);
                    if (avatarSprite != null)
                    {
                        profilePicture.sprite = avatarSprite; // Tampilkan avatar yang dimuat
                        selectedAvatar = avatarSprite; // Set selectedAvatar untuk OnSaveClicked
                        selectedAvatarName = savedAvatarName; // Simpan nama aset
                        Debug.Log($"ProfileManager: Memuat avatar '{savedAvatarName}'");
                    }
                    else
                    {
                        Debug.LogWarning($"ProfileManager: Aset avatar '{savedAvatarName}' tidak ditemukan di Resources/Avatars/.");
                    }
                }
                else
                {
                    Debug.Log("ProfileManager: Tidak ada avatar tersimpan.");
                }

                // Muat Border
                // string savedBorderName = snapshot.Child("borderName").Exists ? snapshot.Child("borderName").Value.ToString() : null;
                // if (!string.IsNullOrEmpty(savedBorderName))
                // {
                //     Sprite borderSprite = Resources.Load<Sprite>("Borders/" + savedBorderName);
                //     if (borderSprite != null)
                //     {
                //         profileBorder.sprite = borderSprite; // Tampilkan border yang dimuat
                //         selectedBorder = borderSprite; // Set selectedBorder untuk OnSaveClicked
                //         selectedBorderName = savedBorderName; // Simpan nama aset
                //         Debug.Log($"ProfileManager: Memuat border '{savedBorderName}'");
                //     }
                //     else
                //     {
                //         Debug.LogWarning($"ProfileManager: Aset border '{savedBorderName}' tidak ditemukan di Resources/Borders/.");
                //     }
                // }
                // else
                // {
                //     Debug.Log("ProfileManager: Tidak ada border tersimpan.");
                // }
            }
            else
            {
                nicknameText.text = "Guest";
                Debug.Log("ProfileManager: Tidak ada profil pengguna tersimpan.");
            }
        }
        else
        {
            Debug.LogWarning("Gagal mengambil data user dari Firebase: " + userTask.Exception);
            nicknameText.text = "Error";
        }
    }

    // Dipanggil saat tombol "Edit Profile" atau sejenisnya diklik
    public void OnProfileClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        // Asumsi "EditAvatar" adalah scene untuk memilih avatar/border
        SceneManager.LoadScene("Profile");
    }

    // Dipanggil saat tombol "Save" di UI pemilihan avatar/border diklik
    public void OnSaveClicked()
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }
        if (selectedAvatar != null)
        {
            profilePicture.sprite = selectedAvatar;
        }
        if (selectedBorder != null)
        {
            profileBorder.sprite = selectedBorder;
        }

        // Simpan perubahan ke Firebase melalui PlayerDatabase
        StartCoroutine(SaveProfileChangesToFirebase());
    }

    // Coroutine untuk menyimpan perubahan avatar/border ke Firebase
    private IEnumerator SaveProfileChangesToFirebase()
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("Pengguna belum login. Tidak dapat menyimpan perubahan profil.");
            yield break;
        }

        string userId = auth.CurrentUser.UserId;
        DatabaseReference userRef = dbRef.Child("users").Child(userId);

        Dictionary<string, object> updates = new Dictionary<string, object>();

        // Tambahkan avatarName yang dipilih ke updates
        if (!string.IsNullOrEmpty(selectedAvatarName))
        {
            updates["avatarName"] = selectedAvatarName;
            Debug.Log($"Menyimpan avatar: {selectedAvatarName}");
        }
        else
        {
            updates["avatarName"] = profilePicture.sprite != null ? profilePicture.sprite.name : "default_avatar";
            Debug.LogWarning("selectedAvatarName kosong, menyimpan avatar saat ini atau default.");
        }


        // Tambahkan borderName yang dipilih ke updates
        if (!string.IsNullOrEmpty(selectedBorderName))
        {
            updates["borderName"] = selectedBorderName;
            Debug.Log($"Menyimpan border: {selectedBorderName}");
        }
        else
        {
            // Jika tidak ada border baru dipilih, pertahankan yang sudah ada atau set default
            updates["borderName"] = profileBorder.sprite != null ? profileBorder.sprite.name : "default_border";
            Debug.LogWarning("selectedBorderName kosong, menyimpan border saat ini atau default.");
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

    // Dipanggil ketika pemain memilih gambar avatar
    public void SelectProfilePicture(Sprite avatar)
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        selectedAvatar = avatar;
        selectedAvatarName = avatar.name;

        profilePicture.sprite = selectedAvatar; // Update tampilan profilePicture (Preview Lokal)

        Debug.Log($"Avatar '{avatar.name}' dipilih sementara.");
    }

    // Dipanggil ketika pemain memilih gambar border
    public void SelectBorder(Sprite border)
    {
        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlayButtonClick();
        }

        selectedBorder = border;
        selectedBorderName = border.name;

        // === BARU: Tampilkan preview border secara langsung ===
        profileBorder.sprite = selectedBorder; // Update tampilan profileBorder secara instan
        // =======================================================

        Debug.Log($"Border '{border.name}' dipilih sementara.");
    }
}