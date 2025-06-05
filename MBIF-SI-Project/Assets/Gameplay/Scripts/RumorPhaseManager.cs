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
            PenaltyFinpoint
            // Tambahkan efek lain jika perlu
        }

        public EffectType effectType;
        public int value;
        public bool affectAllPlayers = true;
    }

    public List<RumorEffect> rumorEffects = new List<RumorEffect>();
    private bool rumorRunning = false;

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


    public void StartRumorPhase(List<PlayerProfile> currentPlayers)
    {
        if (rumorRunning) return; // Jangan mulai dua kali
        rumorRunning = true;

        players = currentPlayers;
        Debug.Log("Memulai fase rumor...");

        rumorEffects = new List<RumorEffect>
    {
        new RumorEffect { color = "Red",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Red",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Red",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Red",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.BonusFinpoint, value = 10, description = "Investor Blue bagi-bagi bonus +10" },
        new RumorEffect { color = "Red",cardName = "Stimulus_Ekonomi", effectType = RumorEffect.EffectType.BonusFinpoint, value = 5, description = "Investor Blue bagi-bagi bonus +10" },

        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -3, description = "Red market sedikit turun" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 3, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = -2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.BonusFinpoint, value = 10, description = "Investor Blue bagi-bagi bonus +10" },
        new RumorEffect { color = "Blue",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Red market sedikit naik" },

        new RumorEffect { color = "Green",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Green market booming!" },
        new RumorEffect { color = "Orange",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 5, description = "Skandal Orange! -5 finpoint" }
    };

        StartCoroutine(RunRumorSequence());
    }
    private IEnumerator RunRumorSequence()
{
    List<string> colors = new List<string> { "Red", "Blue", "Green", "Orange" };

    foreach (string color in colors)
    {
        var effectsForColor = rumorEffects.Where(e => e.color == color).ToList();
        if (effectsForColor.Count == 0) continue;

        RumorEffect selected = effectsForColor[Random.Range(0, effectsForColor.Count)];

        // Tampilkan kartu sesuai warna dan cardName
        ShowCardByColorAndName(selected.color, selected.cardName);

        Debug.Log($"[Rumor] Warna {color}: {selected.description}");

        yield return new WaitForSeconds(1.5f); // waktu tampil sebelum efek

        ApplyRumorEffect(selected);

        gameManager.UpdatePlayerUI();

        yield return new WaitForSeconds(2.5f); // waktu tampil setelah efek

        // Sembunyikan kartu
        HideAllCardObjects();

        yield return new WaitForSeconds(1f); // delay sebelum lanjut warna berikutnya
    }

    rumorRunning = false;
    gameManager.ResetButton(); // Lanjut ke fase berikutnya
}

private void ShowCardByColorAndName(string color, string cardName)
{
    // Sembunyikan semua dulu
    HideAllCardObjects();

    Texture selectedTexture = cardVisuals.FirstOrDefault(v => v.cardName == cardName)?.texture;

    if (selectedTexture == null)
    {
        Debug.LogWarning($"[RumorPhase] Texture untuk cardName '{cardName}' tidak ditemukan!");
        return;
    }

    switch (color)
    {
        case "Red":
            if (cardRed && rendererRed)
            {
                rendererRed.material.mainTexture = selectedTexture;
                cardRed.SetActive(true);
            }
            break;

        case "Blue":
            if (cardBlue && rendererBlue)
            {
                rendererBlue.material.mainTexture = selectedTexture;
                cardBlue.SetActive(true);
            }
            break;

        case "Green":
            if (cardGreen && rendererGreen)
            {
                rendererGreen.material.mainTexture = selectedTexture;
                cardGreen.SetActive(true);
            }
            break;

        case "Orange":
            if (cardOrange && rendererOrange)
            {
                rendererOrange.material.mainTexture = selectedTexture;
                cardOrange.SetActive(true);
            }
            break;
    }
}

private void HideAllCardObjects()
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
                if (playerHasColor)
                {
                    player.finpoint = Mathf.Max(0, player.finpoint - effect.value);
                    Debug.Log($"{player.playerName} kehilangan {effect.value} finpoint karena rumor buruk di {effect.color}");
                }
                break;
        }
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
