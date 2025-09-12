using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UITransitionAnimator : MonoBehaviour
{
    public static UITransitionAnimator Instance { get; private set; }

    [Header("Komponen UI")]
    [SerializeField] private Text displayText;

    [Header("Target Animasi")]
    [Tooltip("Objek kosong (empty object) di Canvas sebagai penanda posisi tujuan.")]
    [SerializeField] private RectTransform targetPosition;

    [Header("Pengaturan Animasi")]
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private float displayDuration = 1.5f;
    [SerializeField] private float scaleMultiplier = 2f;

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

    // DontDestroyOnLoad(gameObject); // <-- BARIS INI YANG HARUS DIHAPUS

    rectTransform = GetComponent<RectTransform>();
    initialPosition = rectTransform.anchoredPosition;
    initialScale = rectTransform.localScale;
}

    public void StartTransition(string newText)
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }
        runningCoroutine = StartCoroutine(AnimateTransition(newText));
    }

    private IEnumerator AnimateTransition(string newText)
    {
        

        Vector3 targetPos = targetPosition.anchoredPosition;
        Vector3 targetScale = initialScale * scaleMultiplier;
        float elapsedTime = 0f;

        // --- FASE 1: Bergerak ke Target dan Membesar ---
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
        if (displayText != null)
        {
            displayText.text = newText;
        }

        // --- FASE 2: Tahan di Posisi Target ---
        yield return new WaitForSeconds(displayDuration);

        // --- FASE 3: Kembali ke Posisi Awal dan Mengecil ---
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