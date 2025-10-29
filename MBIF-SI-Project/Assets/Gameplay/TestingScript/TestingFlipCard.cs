using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Penting untuk mengakses komponen Button

public class CardController : MonoBehaviour
{
    // --- Variabel yang bisa diatur dari Inspector ---
    [Tooltip("Masukkan GameObject kartu yang ingin dibalik di sini.")]
    public GameObject cardToFlip;
    

    [Tooltip("Masukkan sound effect untuk flip kartu.")]
    public AudioClip rumourFlipSound;
    
    // Anda bisa menambahkan referensi ke button jika ingin mengaturnya via kode
    // public Button flipButton;

    // void Start()
    // {
    //     // Jika ingin mengatur listener via kode, uncomment bagian ini
    //     // flipButton.onClick.AddListener(OnFlipButtonClick);
    // }

    /// <summary>
    /// Method PUBLIK ini yang akan kita panggil dari OnClick() Button di Inspector.
    /// </summary>
    public void OnFlipButtonClick()
    {
        // Memastikan kartu tidak null sebelum memulai coroutine
        if (cardToFlip != null)
        {
            StartCoroutine(FlipCard(cardToFlip));
        }
        else
        {
            Debug.LogError("Card To Flip belum di-assign di Inspector!");
        }
    }
     public void OnButtonClick()
    {
        // Memastikan kartu tidak null sebelum memulai coroutine
        cardToFlip.SetActive(false);

    }

    /// <summary>
    /// Coroutine untuk animasi membalik kartu.
    /// </summary>
   private IEnumerator FlipCard(GameObject cardObject)
{
    Vector3 originalPosition = cardObject.transform.position;
    Quaternion finalRotation = Quaternion.Euler(0, 180, 0);
    Vector3 flipStartPosition;

    if (cardObject.activeInHierarchy)
    {
        // --- JIKA AKTIF: Jalankan Animasi 1 (Gerakan Melengkung ke Belakang) ---
        
        float moveDuration = 0.6f; // Sedikit lebih lama untuk gerakan melengkung
        float sideOffset = -2.0f;   // Jarak lengkungan ke samping
        float backOffset = 0.05f;  // Seberapa jauh turun ke belakang
        float moveElapsed = 0f;

        Vector3 moveStartPos = originalPosition;
        // Posisi akhir dari animasi ini adalah di tengah, tapi lebih rendah
        Vector3 moveEndPos = originalPosition;
        moveEndPos.y -= backOffset;

        while (moveElapsed < moveDuration)
        {
            float progress = moveElapsed / moveDuration;

            // 1. Posisi turun secara linear dari awal ke akhir
            Vector3 currentPos = Vector3.Lerp(moveStartPos, moveEndPos, progress);

            // 2. Tambahkan gerakan melengkung ke samping menggunakan Sin
            // Mathf.Sin(progress * Mathf.PI) akan menghasilkan kurva 0 -> 1 -> 0
            currentPos.x += Mathf.Sin(progress * Mathf.PI) * sideOffset;
            
            cardObject.transform.position = currentPos;

            moveElapsed += Time.deltaTime;
            yield return null;
        }

        // Pastikan posisi akhir tepat dan tetapkan sebagai titik awal flip
        cardObject.transform.position = moveEndPos;
        flipStartPosition = moveEndPos;

        yield return new WaitForSeconds(0.2f);
    }
    else
    {
        // --- JIKA TIDAK AKTIF: Langsung siapkan untuk Animasi 2 (Tidak ada perubahan di sini) ---
        flipStartPosition = originalPosition;
        flipStartPosition.y -= 0.01f;
        cardObject.SetActive(true);
    }


    // --- Animasi 2 (Membalik kartu kembali ke posisi semula) - TIDAK ADA PERUBAHAN ---

    Vector3 flipEndPos = originalPosition; 
    Quaternion flipStartRot = Quaternion.Euler(0, 180, 180); 
    cardObject.transform.rotation = flipStartRot;
    
    float flipDuration = 0.7f;
    float flipHeight = 0.5f;
    float flipElapsed = 0f;
    
    if (SfxManager.Instance != null && rumourFlipSound != null)
    {
        SfxManager.Instance.PlaySound(rumourFlipSound);
    }
    
    while (flipElapsed < flipDuration)
    {
        float progress = flipElapsed / flipDuration;

        Vector3 currentPos = Vector3.Lerp(flipStartPosition, flipEndPos, progress);
        currentPos.y += Mathf.Sin(progress * Mathf.PI) * flipHeight;
        cardObject.transform.position = currentPos;

        cardObject.transform.rotation = Quaternion.Slerp(flipStartRot, finalRotation, progress);

        flipElapsed += Time.deltaTime;
        yield return null;
    }

    cardObject.transform.position = originalPosition;
    cardObject.transform.rotation = finalRotation;
}
}