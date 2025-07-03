// File: ResolutionPhaseManagerMultiplayer.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ResolutionPhaseManagerMultiplayer : MonoBehaviour
{
    [System.Serializable]
    public class DividendData
    {
        public string color;
        public int dividendIndex; // Dikelola oleh MultiplayerManager
        public int revealedTokenCount; // Dikelola oleh MultiplayerManager

        // Referensi Visual
        public GameObject indicatorObject;
        public List<GameObject> tokenObjects;
    }

    public List<DividendData> dividendDataList = new List<DividendData>();
    public float dividendSpacing = 0.5f;
    private Dictionary<string, Vector3> dividendInitialPositions = new Dictionary<string, Vector3>();
    
    // Seret material untuk setiap nilai token di Inspector
    [Header("Token Materials")]
    public Material tokenMinus2Material;
    public Material tokenMinus1Material;
    public Material tokenPlus1Material;
    public Material tokenPlus2Material;


    private void Start()
    {
        CacheDividendInitialPositions();
        // Sembunyikan semua token di awal
        foreach (var data in dividendDataList)
        {
            foreach (var token in data.tokenObjects)
            {
                token.SetActive(false);
            }
        }
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
    
    // Dipanggil oleh RPC dari MultiplayerManager
    public void RevealToken(string color, int tokenValue, int revealedCount)
    {
        var data = dividendDataList.FirstOrDefault(d => d.color == color);
        if (data == null || revealedCount > data.tokenObjects.Count) return;

        int tokenIndex = revealedCount - 1;
        GameObject tokenObj = data.tokenObjects[tokenIndex];
        
        // Atur material berdasarkan nilai, lalu aktifkan
        Renderer rend = tokenObj.GetComponent<Renderer>();
        if (rend != null) rend.material = GetTokenMaterial(tokenValue);
        
        tokenObj.SetActive(true);
    }

    // Dipanggil oleh RPC dari MultiplayerManager
    public void UpdateDividendVisual(string color, int newDividendIndex)
    {
        var data = dividendDataList.FirstOrDefault(d => d.color == color);
        if (data == null || data.indicatorObject == null || !dividendInitialPositions.ContainsKey(color)) return;

        int clampedIndex = Mathf.Clamp(newDividendIndex, -3, 3);
        Vector3 basePos = dividendInitialPositions[color];
        Vector3 offset = new Vector3(clampedIndex * dividendSpacing, 0, 0);

        data.indicatorObject.transform.position = basePos + offset;
    }
    
    // Dipanggil oleh RPC dari MultiplayerManager saat semester baru dimulai
    public void ResetVisuals()
    {
        foreach (var data in dividendDataList)
        {
            UpdateDividendVisual(data.color, 0); // Kembalikan indikator ke tengah
            foreach (var token in data.tokenObjects)
            {
                token.SetActive(false); // Sembunyikan lagi semua token
            }
        }
    }

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