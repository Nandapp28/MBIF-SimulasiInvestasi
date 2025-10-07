// File: Scripts/SingleplayerProfileDisplay.cs (Revisi)

using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;

public class SingleplayerProfileDisplay : MonoBehaviour
{
    [Header("Referensi UI di Scene")]
    [Tooltip("Drag & drop komponen Image untuk foto profil pemain di sini.")]
    public Image playerAvatarImage;

    [Tooltip("Drag & drop komponen Image untuk border profil pemain di sini.")]
    public Image playerBorderImage;

    [Header("Pengaturan Default")]
    [Tooltip("Nama file sprite default jika avatar pemain tidak ditemukan di database.")]
    public string defaultAvatarName = "avatar_0";

    private DatabaseReference dbReference;
    private FirebaseAuth auth;

    async void Awake()
    {
        if (playerAvatarImage == null || playerBorderImage == null)
        {
            Debug.LogError("Avatar atau Border Image belum di-assign di Inspector!");
            gameObject.SetActive(false);
            return;
        }


        while (FirebaseInitializer.Instance == null || !FirebaseInitializer.Instance.IsFirebaseReady)
        {
            await Task.Delay(100);
        }

        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (auth.CurrentUser != null)
        {
            await LoadProfileFromDatabase(auth.CurrentUser.UserId);
        }
        else
        {
            await LoadProfileFromDatabase(auth.CurrentUser.UserId);
            Debug.Log("Pemain tidak login. Profil tetap nonaktif.");
            // Tidak perlu memanggil SetDefaultProfile() karena sudah nonaktif dari awal.
        }
    }

    private async Task LoadProfileFromDatabase(string userId)
    {
        var userSnapshot = await dbReference.Child("users").Child(userId).GetValueAsync();

        if (userSnapshot.Exists)
        {
            string avatarName = userSnapshot.Child("avatarName").Exists ? userSnapshot.Child("avatarName").Value.ToString() : defaultAvatarName;
            string borderName = userSnapshot.Child("borderName").Exists ? userSnapshot.Child("borderName").Value.ToString() : null;

            // --- PERUBAHAN 2: Aktifkan avatar dan terapkan sprite ---
            playerAvatarImage.gameObject.SetActive(true);
            ApplySprite(playerAvatarImage, "Avatars/" + avatarName, defaultAvatarName);
            // --- AKHIR PERUBAHAN ---

            if (!string.IsNullOrEmpty(borderName))
            {
                playerBorderImage.gameObject.SetActive(true);
                ApplySprite(playerBorderImage, "Borders/" + borderName, null);
            }
            // Jika borderName kosong, borderImage tetap tidak aktif (sesuai kondisi awal).
        }
        // Jika data user tidak ada di database, tidak ada yang diaktifkan.
    }

    // --- FUNGSI SetDefaultProfile() DIHAPUS KARENA TIDAK DIPERLUKAN LAGI ---

    private void ApplySprite(Image targetImage, string resourcePath, string fallbackResourceName)
    {
        Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);

        if (loadedSprite != null)
        {
            targetImage.sprite = loadedSprite;
        }
        else
        {
            Debug.LogWarning($"Gagal memuat sprite dari '{resourcePath}'.");
            if (!string.IsNullOrEmpty(fallbackResourceName))
            {
                Sprite fallbackSprite = Resources.Load<Sprite>("Avatars/" + fallbackResourceName);
                if (fallbackSprite != null)
                {
                    targetImage.sprite = fallbackSprite;
                }
            }
        }
    }
}