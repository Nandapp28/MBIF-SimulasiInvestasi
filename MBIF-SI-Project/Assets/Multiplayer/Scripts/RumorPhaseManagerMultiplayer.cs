// File: RumorPhaseManagerMultiplayer.cs (Lengkap dan Diperbaiki)

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RumorPhaseManagerMultiplayer : MonoBehaviour
{
    [Header("Kartu Rumor per Warna")]
    public GameObject cardRed;
    public GameObject cardBlue;
    public GameObject cardGreen;
    public GameObject cardOrange;

    [System.Serializable]
    public class CardVisual
    {
        public string cardName;
        public Texture texture;
    }

    public List<CardVisual> cardVisuals = new List<CardVisual>();

    [Header("Renderers")]
    public Renderer rendererRed;
    public Renderer rendererBlue;
    public Renderer rendererGreen;
    public Renderer rendererOrange;

    // Fungsi ini akan dipanggil oleh RPC dari MultiplayerManager
    public void ShowCardByColorAndName(string color, string cardName)
    {
        HideAllCardObjects(); // Sembunyikan dulu kartu lain

        Texture frontTexture = cardVisuals.FirstOrDefault(v => v.cardName == cardName)?.texture;
        if (frontTexture == null)
        {
            Debug.LogWarning($"[RumorPhase] Texture untuk '{cardName}' tidak ditemukan!");
            return;
        }

        GameObject cardToFlip = null;
        Renderer rendererToSet = null;

        switch (color)
        {
            case "Red": cardToFlip = cardRed; rendererToSet = rendererRed; break;
            case "Blue": cardToFlip = cardBlue; rendererToSet = rendererBlue; break;
            case "Green": cardToFlip = cardGreen; rendererToSet = rendererGreen; break;
            case "Orange": cardToFlip = cardOrange; rendererToSet = rendererOrange; break;
        }

        if (cardToFlip != null && rendererToSet != null)
        {
            rendererToSet.material.mainTexture = frontTexture;
            StartCoroutine(FlipCardAnimation(cardToFlip));
        }
    }

    private IEnumerator FlipCardAnimation(GameObject cardObject)
    {
        cardObject.SetActive(true);
        cardObject.transform.rotation = Quaternion.Euler(0, -180, 180);

        float duration = 0.5f;
        float elapsed = 0f;
        Quaternion startRot = cardObject.transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, -180, 0);

        yield return new WaitForSeconds(0.5f);

        while (elapsed < duration)
        {
            cardObject.transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cardObject.transform.rotation = endRot;
    }

    // FUNGSI YANG HILANG SEBELUMNYA
    public void HideAllCardObjects()
    {
        if (cardRed) cardRed.SetActive(false);
        if (cardBlue) cardBlue.SetActive(false);
        if (cardGreen) cardGreen.SetActive(false);
        if (cardOrange) cardOrange.SetActive(false);
    }
}