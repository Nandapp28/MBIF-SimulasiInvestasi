using UnityEngine;
using TMPro;

public class SmoothTextBlink : MonoBehaviour
{
    public TextMeshProUGUI pressToPlayText;
    public float fadeSpeed = 1.5f; // Semakin kecil, semakin lambat kedipnya

    private Color originalColor;

    void Start()
    {
        originalColor = pressToPlayText.color;
    }

    void Update()
    {
        float alpha = Mathf.PingPong(Time.time * fadeSpeed, 1f); // Nilai antara 0 dan 1
        Color newColor = originalColor;
        newColor.a = alpha;
        pressToPlayText.color = newColor;
    }
}
