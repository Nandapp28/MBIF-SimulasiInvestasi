using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // Diperlukan untuk menggunakan List
using System.Linq; // Diperlukan untuk pencarian yang mudah


public class UITransitionAnimator : MonoBehaviour
{
    public static UITransitionAnimator Instance { get; private set; }
    [System.Serializable]
public class NamedSprite
{
    public string name;
    public Sprite sprite;
}


    [Header("Komponen UI")]
    [SerializeField] private Image displayImage;

    // ▼▼▼ DAFTAR TEKSTUR YANG BISA DI-ATTACH DI INSPECTOR ▼▼▼
    [Header("Daftar Tekstur")]
    [Tooltip("Isi daftar ini dengan nama panggilan dan gambar yang sesuai.")]
    public List<NamedSprite> phaseTextures;

    [Header("Target Animasi")]
    [SerializeField] private RectTransform targetPosition;

    [Header("Pengaturan Animasi")]
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private float displayDuration = 1.5f;
    [SerializeField] private float scaleMultiplier = 2f;
    [SerializeField] private float morphDuration = 0.25f; // Durasi untuk efek fade in/out
    
    
    [Header("Sound Effects")]
    public AudioClip phaseChangeSound;

    private RectTransform rectTransform;
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private Coroutine runningCoroutine = null;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;
        initialScale = rectTransform.localScale;

       
    }

    public void StartTransition(string textureName)
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }
        runningCoroutine = StartCoroutine(AnimateTransition(textureName));
    }

    private IEnumerator AnimateTransition(string textureName)
{
    // =======================================================
    // FASE 1: Animasi bergerak ke tengah (TIDAK BERUBAH)
    // =======================================================
    Vector3 targetPos = targetPosition.anchoredPosition;
    Vector3 targetScale = initialScale * scaleMultiplier;
    float elapsedTime = 0f;

    while (elapsedTime < animationDuration)
    {
        float t = elapsedTime / animationDuration;
        rectTransform.anchoredPosition = Vector3.Lerp(initialPosition, targetPos, t);
        rectTransform.localScale = Vector3.Lerp(initialScale, targetScale, t);
        elapsedTime += Time.deltaTime;
        yield return null;
    }

    rectTransform.anchoredPosition = targetPos;
    rectTransform.localScale = targetScale;
    yield return new WaitForSeconds(0.5f);

    if (SfxManager.Instance != null && phaseChangeSound != null)
    {
        SfxManager.Instance.PlaySound(phaseChangeSound);
    }

    // =======================================================
    // FASE 2: EFEK "MORPH" UNTUK GANTI GAMBAR (BAGIAN BARU)
    // =======================================================

    Vector3 centerScale = rectTransform.localScale;
    
    // ▼▼▼ MORPH OUT (Squash): Gepeng secara vertikal ▼▼▼
    elapsedTime = 0f;
    while (elapsedTime < morphDuration)
    {
        float t = elapsedTime / morphDuration;
        float smoothT = t * t; // Ease In Quad

        // Sumbu Y mengecil ke 0, Sumbu X sedikit melebar untuk efek 'squash'
        float newY = Mathf.Lerp(centerScale.y, 0.01f, smoothT);
        float newX = Mathf.Lerp(centerScale.x, centerScale.x * 1.2f, smoothT);
        rectTransform.localScale = new Vector3(newX, newY, centerScale.z);
        
        elapsedTime += Time.deltaTime;
        yield return null;
    }

    // ▼▼▼ GANTI SPRITE SAAT GAMBAR SANGAT GEPENG ▼▼▼
    NamedSprite namedSprite = phaseTextures.Find(p => p.name == textureName);
    if (namedSprite != null)
    {
        displayImage.sprite = namedSprite.sprite;
        displayImage.enabled = true;
    }
    else
    {
        Debug.LogWarning($"Texture dengan nama '{textureName}' tidak ditemukan!");
    }

    // ▼▼▼ MORPH IN (Stretch): Kembali ke bentuk semula ▼▼▼
    elapsedTime = 0f;
    while (elapsedTime < morphDuration)
    {
        float t = elapsedTime / morphDuration;
        float smoothT = 1f - (1f - t) * (1f - t); // Ease Out Quad
        
        // Sumbu Y kembali ke ukuran semula, Sumbu X kembali normal
        float newY = Mathf.Lerp(0.01f, centerScale.y, smoothT);
        float newX = Mathf.Lerp(centerScale.x * 1.2f, centerScale.x, smoothT);
        rectTransform.localScale = new Vector3(newX, newY, centerScale.z);

        elapsedTime += Time.deltaTime;
        yield return null;
    }
    
    // Pastikan scale kembali sempurna
    rectTransform.localScale = centerScale;


    // =======================================================
    // FASE 3: Tahan gambar & Kembali ke Awal (TIDAK BERUBAH)
    // =======================================================
    yield return new WaitForSeconds(displayDuration);

    elapsedTime = 0f;
    while (elapsedTime < animationDuration)
    {
        float t = elapsedTime / animationDuration;
        rectTransform.anchoredPosition = Vector3.Lerp(targetPos, initialPosition, t);
        rectTransform.localScale = Vector3.Lerp(targetScale, initialScale, t);
        elapsedTime += Time.deltaTime;
        yield return null;
    }

    rectTransform.anchoredPosition = initialPosition;
    rectTransform.localScale = initialScale;

    runningCoroutine = null;
}
}