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
        new RumorEffect { color = "Red",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.ResetAllIPO, value = 0, description = "Reformasi ekonomi" },
        new RumorEffect { color = "Red",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 1, description = "Extra Fee" },
        new RumorEffect { color = "Red",cardName = "Krisis_Keuangan", effectType = RumorEffect.EffectType.TaxByTurnOrder, value = 1, description = "Pajak Jalan" },


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

        StartCoroutine(RunRumorSequence());
    }
    private IEnumerator RunRumorSequence()
    {

        List<string> colors = new List<string> { "Red", "Blue", "Green", "Orange" };
        yield return new WaitForSeconds(2f);

        foreach (string color in colors)
        {
            yield return new WaitForSeconds(1f);
            var effectsForColor = rumorEffects.Where(e => e.color == color).ToList();
            if (effectsForColor.Count == 0) continue;

            RumorEffect selected = effectsForColor[Random.Range(0, effectsForColor.Count)];

            // Tampilkan kartu sesuai warna dan cardName
            ShowCardByColorAndName(selected.color, selected.cardName);

            Debug.Log($"[Rumor] Warna {color}: {selected.description}");

            yield return new WaitForSeconds(1.5f); // waktu tampil sebelum efek

            ApplyRumorEffect(selected);
            gameManager.UpdatePlayerUI();

            sellingPhaseManager.UpdateIPOVisuals();


            yield return new WaitForSeconds(2.5f); // waktu tampil setelah efek

            // Sembunyikan kartu
            HideAllCardObjects();

            // delay sebelum lanjut warna berikutnya
        }

        rumorRunning = false;
        resolutionPhaseManager.StartResolutionPhase(players); // Lanjut ke fase berikutnya
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

    private void ResetAllIPOIndexes()
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
