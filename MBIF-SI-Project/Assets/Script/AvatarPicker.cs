using UnityEngine;
using UnityEngine.UI;

public class AvatarPicker : MonoBehaviour
{
    public static AvatarPicker Instance { get; private set; }

    public RawImage avatarImage;
    public int avatarSize = 256;
    public ProfileManager profileManager;


    // Metode Awake untuk inisialisasi singleton
    void Awake()
    {
        // Logikanya sama persis, hanya nama kelasnya yang berbeda
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void PickImageFromGallery()
    {
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path == null)
                return;

            Texture2D texture = NativeGallery.LoadImageAtPath(path, avatarSize);
            if (texture == null)
            {
                Debug.Log("Gagal memuat gambar");
                return;
            }

            Texture2D readableTexture = MakeTextureReadable(texture);
            Texture2D cropped = CropToSquare(readableTexture);

            avatarImage.texture = cropped;
            avatarImage.rectTransform.sizeDelta = new Vector2(avatarSize, avatarSize);

            Sprite avatarSprite = Sprite.Create(cropped, new Rect(0, 0, cropped.width, cropped.height), new Vector2(0.5f, 0.5f));
            if (profileManager != null)
            {
                profileManager.SelectProfilePicture(avatarSprite);
            }
        }, "Pilih gambar avatar", "image/*");
    }

    private Texture2D MakeTextureReadable(Texture2D texture)
    {
        RenderTexture tmp = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(texture, tmp);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = tmp;

        Texture2D readableTexture = new Texture2D(texture.width, texture.height);
        readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);

        return readableTexture;
    }

    private Texture2D CropToSquare(Texture2D original)
    {
        int size = Mathf.Min(original.width, original.height);
        int x = (original.width - size) / 2;
        int y = (original.height - size) / 2;

        Color[] pixels = original.GetPixels(x, y, size, size);
        Texture2D cropped = new Texture2D(size, size);
        cropped.SetPixels(pixels);
        cropped.Apply();
        return cropped;
    }
}
