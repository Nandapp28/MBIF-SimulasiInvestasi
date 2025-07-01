using UnityEngine;
using UnityEngine.UI;
using System.Collections; // <-- INI YANG HILANG DAN SEKARANG DITAMBAHKAN
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

public class RumorPhaseManager : MonoBehaviour
{
    public static RumorPhaseManager Instance;

    [System.Serializable]
    public class RumorEffect
    {
        public string color;
        public string description;
        public enum EffectType { ModifyIPO, BonusFinpoint, PenaltyFinpoint }
        public EffectType effectType;
        public int value;
        public bool affectAllPlayers = true;
    }

    public List<RumorEffect> rumorEffects = new List<RumorEffect>();
    private bool rumorRunning = false;
    private List<PlayerProfile> players;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void StartRumorPhase(List<PlayerProfile> currentPlayers)
    {
        if (rumorRunning) return;
        rumorRunning = true;
        players = currentPlayers;
        Debug.Log("Memulai fase rumor...");

        if (PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom)
        {
            rumorEffects = new List<RumorEffect>
            {
                new RumorEffect { color = "Red", effectType = RumorEffect.EffectType.ModifyIPO, value = -1, description = "Red market sedikit turun" },
                new RumorEffect { color = "Red", effectType = RumorEffect.EffectType.ModifyIPO, value = 1, description = "Red market sedikit naik" },
                new RumorEffect { color = "Blue", effectType = RumorEffect.EffectType.BonusFinpoint, value = 10, description = "Investor Blue bagi-bagi bonus +10" },
                new RumorEffect { color = "Green", effectType = RumorEffect.EffectType.ModifyIPO, value = 2, description = "Green market booming!" },
                new RumorEffect { color = "Orange", effectType = RumorEffect.EffectType.PenaltyFinpoint, value = 5, description = "Skandal Orange! -5 finpoint" }
            };
            StartCoroutine(RunRumorSequence());
        }
    }

    private IEnumerator RunRumorSequence()
    {
        List<string> colors = new List<string> { "Red", "Blue", "Green", "Orange" };
        foreach (string color in colors)
        {
            var effectsForColor = rumorEffects.Where(e => e.color == color).ToList();
            if (effectsForColor.Count == 0) continue;

            RumorEffect selected = effectsForColor[Random.Range(0, effectsForColor.Count)];
            Debug.Log($"[Rumor] Warna {color}: {selected.description}");

            // Di multiplayer, MasterClient harus mengirim 'selected' via RPC ke semua pemain
            // Untuk sekarang, kita jalankan secara lokal.
            ApplyRumorEffect(selected);

            CallUpdatePlayerUI();
            yield return new WaitForSeconds(2f); // Kurangi delay agar tidak terlalu lama
        }
        rumorRunning = false;
        CallResetButton();
    }
    
    private void ApplyRumorEffect(RumorEffect effect)
    {
        if (effect.effectType == RumorEffect.EffectType.ModifyIPO)
        {
            ModifyIPOIndex(effect.color, effect.value);
            return;
        }

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
                    }
                    break;
                case RumorEffect.EffectType.PenaltyFinpoint:
                    if (playerHasColor)
                    {
                        player.finpoint = Mathf.Max(0, player.finpoint - effect.value);
                    }
                    break;
            }
        }
    }
    
    private void ModifyIPOIndex(string color, int delta)
    {
        if (SellingPhaseManager.Instance != null)
        {
            var data = SellingPhaseManager.Instance.ipoDataList.FirstOrDefault(i => i.color == color);
            if (data != null)
            {
                data.ipoIndex += delta;
            }
        }
    }

    private void CallUpdatePlayerUI()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerUI();
        }
        else if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.UpdatePlayerUI();
        }
    }

    private void CallResetButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetButton();
        }
    }
}
