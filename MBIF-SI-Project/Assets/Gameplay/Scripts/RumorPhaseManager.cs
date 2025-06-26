using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class RumorPhaseManager : MonoBehaviour
{
    [Header("Game References")]
    public GameManager gameManager;
    public SellingPhaseManager sellingPhaseManager;
    public ResolutionPhaseManager resolutionPhaseManager;
    [Header("Posisi Spesial")] // <-- TAMBAHKAN HEADER BARU
    public Transform predictionCardStage; 

    [System.Serializable]
    public class RumorEffect
    {
        public string color;
        public string description;
        public string cardName;

        public enum EffectType
        {
            ModifyIPO,
            BonusFinpoint,
            PenaltyFinpoint,
            ResetAllIPO,
            TaxByTurnOrder,
            StockDilution
            // Tambahkan efek lain jika perlu
        }

        public EffectType effectType;
        public int value;
        public bool affectAllPlayers = true;
    }

    public List<RumorEffect> rumorEffects = new List<RumorEffect>();
    // SOLUSI: Menggunakan backing field
[Header("Debug - Urutan Kartu Rumor Terpilih")]
[SerializeField] // <-- Tambahkan ini agar field private bisa dilihat di Inspector
private List<RumorEffect> _shuffledRumorDeck = new List<RumorEffect>();

// Property publik untuk dibaca oleh skrip lain (misal: HelpCardPhaseManager)
public List<RumorEffect> shuffledRumorDeck => _shuffledRumorDeck;    private bool rumorRunning = false;

    private List<PlayerProfile> players;
    [Header("Kartu Rumor Per Warna")]
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

    public Renderer rendererRed;
    public Renderer rendererBlue;
    public Renderer rendererGreen;
    public Renderer rendererOrange;
    private void Start()
{
    rumorEffects = new List<RumorEffect>
    {
        new RumorEffect { color = "Red",cardName = "Resesi_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Red",cardName = "Revaluasi_Asset", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Buyback", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Tender_Kompetitif", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Audit_Forensik", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Suap_Audit", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Depresiasi_Rupiah", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Rencana_Ekspansi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Ekspansi_Produk", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Investasi_Asing", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Kenaikan_Upah", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Siasat_Pajak", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Red",cardName = "Defisit_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Red",cardName = "Merger", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Reformasi_Ekonomi", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Red",cardName = "Extra_Fee", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Red",cardName = "Pajak_Jalan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },
        new RumorEffect { color = "Red",cardName = "Penerbitan_Saham", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Reformasi ekonomi" },


        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Blue",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.BonusFinpoint, value = 10, description = "Investor Blue bagi-bagi bonus +10" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Skandal Orange! -5 finpoint" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Skandal Orange! -5 finpoint" },

        new RumorEffect { color = "Green",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.StockDilution, value = -1, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Orange",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Skandal Orange! -5 finpoint" },
        new RumorEffect { color = "Orange",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Skandal Orange! -5 finpoint" }
    };
    InitializeRumorDeck(); // Atau panggil dari GameManager saat game dimulai
}


    public void InitializeRumorDeck()
    {
        shuffledRumorDeck.Clear();

        // Ambil satu kartu acak dari tiap warna
        List<string> colors = new List<string> { "Red", "Blue", "Green", "Orange" };

        foreach (string color in colors)
        {
            var possibleRumors = rumorEffects.Where(r => r.color == color).ToList();
            if (possibleRumors.Count > 0)
            {
                RumorEffect chosen = possibleRumors[Random.Range(0, possibleRumors.Count)];
                shuffledRumorDeck.Add(chosen);
            }
        }
        

        Debug.Log("[RumorDeck] Kartu rumor telah diacak dan disiapkan:");
        foreach (var effect in shuffledRumorDeck)
        {
            Debug.Log($"- {effect.color}: {effect.cardName} ({effect.description})");
        }
    }


    public void StartRumorPhase(List<PlayerProfile> currentPlayers)
    {
        if (rumorRunning) return; // Jangan mulai dua kali
        rumorRunning = true;

        players = currentPlayers;
        Debug.Log("Memulai fase rumor...");

        StartCoroutine(RunRumorSequence());
    }
    private IEnumerator RunRumorSequence()
{
    yield return new WaitForSeconds(2f);

    foreach (var selected in shuffledRumorDeck)
    {
        yield return new WaitForSeconds(1f);

        ShowCardByColorAndName(selected.color, selected.cardName);
        Debug.Log($"[Rumor] Warna {selected.color}: {selected.description}");

        yield return new WaitForSeconds(1.5f);

        ApplyRumorEffect(selected);
        gameManager.UpdatePlayerUI();
        sellingPhaseManager.UpdateIPOVisuals();

        yield return new WaitForSeconds(2.5f);
        HideAllCardObjects();
    }

    rumorRunning = false;
    resolutionPhaseManager.StartResolutionPhase(players);
}


    private void ShowCardByColorAndName(string color, string cardName)
    {
        HideAllCardObjects(); // Sembunyikan dulu semua kartu

        Texture frontTexture = cardVisuals.FirstOrDefault(v => v.cardName == cardName)?.texture;
        if (frontTexture == null)
        {
            Debug.LogWarning($"[RumorPhase] Texture untuk cardName '{cardName}' tidak ditemukan!");
            return;
        }

        GameObject card = null;
        Renderer renderer = null;

        switch (color)
        {
            case "Red":
                card = cardRed;
                renderer = rendererRed;
                break;
            case "Blue":
                card = cardBlue;
                renderer = rendererBlue;
                break;
            case "Green":
                card = cardGreen;
                renderer = rendererGreen;
                break;
            case "Orange":
                card = cardOrange;
                renderer = rendererOrange;
                break;
        }

        if (card && renderer)
        {
            renderer.material.mainTexture = frontTexture; // ⬅️ langsung set texture di awal
            StartCoroutine(FlipCard(card));
        }
    }


    private IEnumerator FlipCard(GameObject cardObject)
    {
        cardObject.SetActive(true);

        // Mulai dari kondisi terbalik
        cardObject.transform.rotation = Quaternion.Euler(0, -180, 180);

        float duration = 0.5f;
        float elapsed = 0f;

        Quaternion startRot = cardObject.transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, -180, 0); // Menghadap depan

        yield return new WaitForSeconds(0.5f); // jeda sejenak sebelum animasi

        while (elapsed < duration)
        {
            cardObject.transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cardObject.transform.rotation = endRot;
    }
    // Tambahkan metode ini di dalam kelas RumorPhaseManager.cs

public IEnumerator ShowPredictionCardAtCenter(RumorPhaseManager.RumorEffect rumorToShow)
{
    HideAllCardObjects(); // Pastikan tidak ada kartu lain yang aktif

    // 1. Dapatkan referensi visual kartu berdasarkan warna rumor
    GameObject cardObject = null;
    Texture frontTexture = cardVisuals.FirstOrDefault(v => v.cardName == rumorToShow.cardName)?.texture;

    if (frontTexture == null)
    {
        Debug.LogWarning($"[RumorPhase] Texture untuk cardName '{rumorToShow.cardName}' tidak ditemukan!");
        yield break; // Keluar dari coroutine jika texture tidak ada
    }

    switch (rumorToShow.color)
    {
        case "Red": cardObject = cardRed; break;
        case "Blue": cardObject = cardBlue; break;
        case "Green": cardObject = cardGreen; break;
        case "Orange": cardObject = cardOrange; break;
    }

    if (cardObject != null)
    {
        // 2. Atur posisi dan rotasi kartu ke posisi panggung
        cardObject.transform.position = predictionCardStage.position;
        cardObject.transform.rotation = predictionCardStage.rotation;

        // 3. Set texture dan jalankan animasi flip
        // Kita perlu mengambil renderer yang sesuai
        Renderer cardRenderer = cardObject.GetComponentInChildren<Renderer>(); // Cara mudah mendapatkannya
        if (cardRenderer)
        {
            cardRenderer.material.mainTexture = frontTexture;
        }

        // Jalankan animasi flip dan TUNGGU sampai selesai
        yield return StartCoroutine(FlipCard(cardObject));
    }
}



    public void HideAllCardObjects()
    {
        if (cardRed) cardRed.SetActive(false);
        if (cardBlue) cardBlue.SetActive(false);
        if (cardGreen) cardGreen.SetActive(false);
        if (cardOrange) cardOrange.SetActive(false);
    }



    private void ApplyRumorEffect(RumorEffect effect)
    {
        // Jalankan efek ModifyIPO langsung karena tidak tergantung pemain
        if (effect.effectType == RumorEffect.EffectType.ModifyIPO)
        {
            ModifyIPOIndex(effect.color, effect.value);
            return; // Langsung keluar karena tidak butuh loop
        }
        if (effect.effectType == RumorEffect.EffectType.ResetAllIPO)
        {
            ResetAllIPOIndexes();
            return;
        }
        if (effect.effectType == RumorEffect.EffectType.StockDilution)
{
    // 1. Simpan pemain yang memiliki kartu dengan warna effect.color
    List<PlayerProfile> affectedPlayers = new List<PlayerProfile>();
    foreach (var p in players)
    {
        if (p.cards.Any(c => c.color == effect.color))
        {
            affectedPlayers.Add(p);
        }
    }

    // 2. Modify IPO index
    ModifyIPOIndex(effect.color, effect.value);

    // 3. Tambahkan kartu tambahan ke pemain yang disimpan
    foreach (var p in affectedPlayers)
    {
        var newCard = new Card($"{effect.color}_Extra", $"Kartu tambahan warna {effect.color}", 0, effect.color);
        p.AddCard(newCard);
        Debug.Log($"{p.playerName} menerima 1 kartu tambahan warna {effect.color}");
    }

    return;
}




        // Untuk efek yang tergantung pemain, baru gunakan loop
        foreach (var player in players)
        {
            bool playerHasColor = player.cards.Any(c => c.color == effect.color);

            if (!effect.affectAllPlayers && !player.isBot) continue;

            switch (effect.effectType)
            {
                case RumorEffect.EffectType.BonusFinpoint:
                    if (playerHasColor)
                    {
                        player.finpoint += effect.value;
                        Debug.Log($"{player.playerName} mendapat bonus {effect.value} finpoint karena memegang kartu {effect.color}");
                    }
                    break;

                case RumorEffect.EffectType.PenaltyFinpoint:
                    int cardCount = player.cards.Count(c => c.color == effect.color);
                    if (cardCount > 0)
                    {
                        int penalty = cardCount * effect.value;
                        player.finpoint = Mathf.Max(0, player.finpoint - penalty);
                        Debug.Log($"{player.playerName} membayar {penalty} finpoint karena memiliki {cardCount} kartu {effect.color}");
                    }
                    break;
                case RumorEffect.EffectType.TaxByTurnOrder:
                    {
                        int penalty = player.ticketNumber * effect.value;
                        player.finpoint = Mathf.Max(0, player.finpoint - penalty);
                        Debug.Log($"{player.playerName} membayar pajak jalan sebesar {penalty} finpoint (turnOrder: {player.ticketNumber})");
                    }
                    break;



            }
        }
    }

    public void ResetAllIPOIndexes()
    {
        foreach (var data in sellingPhaseManager.ipoDataList)
        {
            data.ipoIndex = 0;
            Debug.Log($"[IPO] IPO {data.color} di-reset ke 0");
        }
        
    }




    private void ModifyIPOIndex(string color, int delta)
    {
        var data = sellingPhaseManager.ipoDataList.FirstOrDefault(i => i.color == color);
        if (data != null)
        {
            data.ipoIndex += delta;

            string log = delta switch
            {
                >= 2 => $"IPO {color} melonjak +{delta}",
                1 => $"IPO {color} naik +1",
                -1 => $"IPO {color} turun -1",
                <= -2 => $"IPO {color} anjlok {delta}",
                _ => $"IPO {color} tetap"
            };
            Debug.Log($"[IPO] {log}");
        }
    }
}
