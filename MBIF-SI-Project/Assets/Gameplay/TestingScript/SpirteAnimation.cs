using UnityEngine;
using UnityEngine.UI; // Wajib ditambahkan untuk mengakses komponen UI

[RequireComponent(typeof(Image))]
public class UISpriteAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    public Sprite[] frames;           // Masukkan potongan frame sprite di sini
    public float framesPerSecond = 10f; // Kecepatan animasi
    public bool loop = true;

    private Image uiImage;
    private int currentFrame;
    private float timer;

    void Start()
    {
        // Mengambil komponen Image yang ada di Gameobject ini
        uiImage = GetComponent<Image>();
    }

    void Update()
    {
        // Mencegah error jika array kosong
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;

        // Cek apakah sudah waktunya ganti frame
        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop)
                    currentFrame = 0; // Ulangi dari awal
                else
                    currentFrame = frames.Length - 1; // Berhenti di frame terakhir
            }

            // Ganti gambar pada UI Image
            uiImage.sprite = frames[currentFrame];
        }
    }
}