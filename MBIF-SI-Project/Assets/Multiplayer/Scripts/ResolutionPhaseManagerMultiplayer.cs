// File: ResolutionPhaseManagerMultiplayer.cs (Versi Diperbaiki)

using UnityEngine;
using System.Collections; // Pastikan using ini ada untuk IEnumerator
using System.Collections.Generic;
using System.Linq;

public class ResolutionPhaseManagerMultiplayer : MonoBehaviour
{
    [System.Serializable]
    public class DividendData
    {
        public string color;
        public int dividendIndex;
        public int revealedTokenCount;
        public GameObject indicatorObject;
        public List<GameObject> tokenObjects;
    }

    [Header("Token Materials")]
    public Material faceDownMaterial; // <-- TAMBAHKAN BARIS INI
    public Material tokenMinus2Material;
    public Material tokenMinus1Material;
    public Material tokenPlus1Material;
    public Material tokenPlus2Material;

    public List<DividendData> dividendDataList = new List<DividendData>();
    public float dividendSpacing = 0.5f;
    private Dictionary<string, Vector3> dividendInitialPositions = new Dictionary<string, Vector3>();
    private void Start()
    {
        CacheDividendInitialPositions();
        // Sembunyikan semua token di awal, jika Anda tidak ingin mereka terlihat face-down
        // foreach (var data in dividendDataList)
        // {
        //     foreach (var token in data.tokenObjects)
        //     {
        //         token.SetActive(false);
        //     }
        // }
    }

    private void CacheDividendInitialPositions()
    {
        dividendInitialPositions.Clear();
        foreach (var data in dividendDataList)
        {
            if (data.indicatorObject != null && !dividendInitialPositions.ContainsKey(data.color))
            {
                dividendInitialPositions[data.color] = data.indicatorObject.transform.position;
            }
        }
    }
    
    public void RevealToken(string color, int tokenValue, int revealedCount)
    {
        var data = dividendDataList.FirstOrDefault(d => d.color == color);
        if (data == null || revealedCount > data.tokenObjects.Count) return;

        int tokenIndex = revealedCount - 1;
        GameObject tokenObj = data.tokenObjects[tokenIndex];

        Material targetMaterial = GetTokenMaterial(tokenValue);

        if (tokenObj != null && targetMaterial != null)
        {
            StartCoroutine(FlipTokenAnimation(tokenObj, targetMaterial));
        }
    }

    public void UpdateDividendVisual(string color, int newDividendIndex)
    {
        var data = dividendDataList.FirstOrDefault(d => d.color == color);
        if (data == null || data.indicatorObject == null || !dividendInitialPositions.ContainsKey(color)) return;

        int clampedIndex = Mathf.Clamp(newDividendIndex, -3, 3);
        Vector3 basePos = dividendInitialPositions[color];
        Vector3 offset = new Vector3(clampedIndex * dividendSpacing, 0, 0);

        data.indicatorObject.transform.position = basePos + offset;
    }
    
    public void ResetVisuals()
    {
        foreach (var data in dividendDataList)
        {
            UpdateDividendVisual(data.color, 0);
            foreach (var token in data.tokenObjects)
            {
                token.SetActive(false);
            }
        }
    }

    // Fungsi ini bisa Anda panggil dari RPC di MultiplayerManager jika ingin token terlihat dari awal
    public void ShowFaceDownTokens()
    {
        // Pengecekan untuk memastikan material sudah di-assign
        if (faceDownMaterial == null)
        {
            Debug.LogWarning("Face Down Material belum di-assign di Inspector!");
            return;
        }

        foreach(var data in dividendDataList)
        {
            foreach(var token in data.tokenObjects)
            {
                // Jangan aktifkan token jika referensinya tidak ada
                if (token == null) continue;

                // DAPATKAN RENDERER DAN ATUR MATERIALNYA (TIDAK LAGI DI-KOMENTAR)
                Renderer rend = token.GetComponent<Renderer>();
                if(rend != null)
                {
                    rend.material = faceDownMaterial;
                }
                
                // AKTIFKAN OBJEK SETELAH MATERIAL DIATUR
                token.SetActive(true);
                token.transform.rotation = Quaternion.Euler(0, 180, 0); // Atur ke posisi terbalik
            }
        }
    }

    // INI FUNGSI ANIMASINYA
    private IEnumerator FlipTokenAnimation(GameObject token, Material newMaterial)
    {
        token.SetActive(true);
        Quaternion startRot = Quaternion.Euler(0, 180, 0);
        token.transform.rotation = startRot;

        float duration = 0.25f;
        float elapsed = 0f;
        Quaternion midRot = Quaternion.Euler(0, 90, 0);

        while (elapsed < duration)
        {
            token.transform.rotation = Quaternion.Slerp(startRot, midRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        token.transform.rotation = midRot;

        Renderer rend = token.GetComponent<Renderer>();
        if (rend != null) rend.material = newMaterial;

        elapsed = 0f;
        Quaternion endRot = Quaternion.Euler(0, 0, 0);
        while (elapsed < duration)
        {
            token.transform.rotation = Quaternion.Slerp(midRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        token.transform.rotation = endRot;
    }

    // HANYA ADA SATU FUNGSI INI
    private Material GetTokenMaterial(int value)
    {
        switch (value)
        {
            case -2: return tokenMinus2Material;
            case -1: return tokenMinus1Material;
            case 1: return tokenPlus1Material;
            case 2: return tokenPlus2Material;
            default: return null;
        }
    }
}