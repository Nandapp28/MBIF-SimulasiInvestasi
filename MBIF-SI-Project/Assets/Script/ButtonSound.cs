using UnityEngine;
using UnityEngine.UI;

// Skrip ini harus dipasang pada GameObject Tombol (Button) itu sendiri.
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    [Tooltip("Audio clip yang akan dimainkan saat tombol ini diklik.")]
    public AudioClip soundToPlay; // Slot untuk memilih suara di Inspector!

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        // Jika tidak ada suara yang dipilih, jangan lakukan apa-apa.
        if (soundToPlay == null) return;

        // Menambahkan listener dengan cara yang sedikit berbeda
        // untuk bisa mengirimkan parameter 'soundToPlay'.
        button.onClick.AddListener(() =>
        {
            if (SfxManager.Instance != null)
            {
                // Panggil fungsi baru dan berikan suara spesifik dari tombol ini.
                SfxManager.Instance.PlaySound(soundToPlay);
            }
        });
    }

    private void OnDestroy()
    {
        // Membersihkan listener saat object dihancurkan.
        if (SfxManager.Instance != null && soundToPlay != null)
        {
            button.onClick.RemoveListener(() => { SfxManager.Instance.PlaySound(soundToPlay); });
        }
    }
}