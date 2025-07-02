using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CardEffectManager
{

    public static void ApplyEffect(string cardName, PlayerProfile player, string color)
    {
        Debug.Log($"🧪 Menjalankan efek untuk kartu: {cardName}");

        switch (cardName)
        {
            case "Heal":
                HealEffect(player);
                break;

            case "Shield":
                ShieldEffect(player);
                break;

            case "TradeOffer":
                TradeOfferEffect(player);
                break;
            case "StockSplit":
                StockSplitEffect(player, color);
                break;
            case "InsiderTrade":
                InsiderTradeEffect(player, color);
                break;

            default:
                Debug.LogWarning($"Efek belum tersedia untuk kartu: {cardName}");
                break;
        }
    }

    private static void HealEffect(PlayerProfile player)
    {
        int healAmount = 5;
        player.finpoint += healAmount;
        Debug.Log($"🔧 Heal: +{healAmount} finpoint");
    }

    private static void ShieldEffect(PlayerProfile player)
    {
        // Contoh: berikan flag "shielded" (kamu bisa buat property di PlayerProfile)
        Debug.Log("🛡️ Player diberi efek shield (placeholder)");
    }

    private static void TradeOfferEffect(PlayerProfile player)
    {
        Debug.Log("📦 Efek trade offer dijalankan (placeholder)");
    }

    private static void StockSplitEffect(PlayerProfile player, string color)
    {
        SellingPhaseManager spm = GameObject.FindObjectOfType<SellingPhaseManager>();
        GameManager gameManager = GameObject.FindObjectOfType<GameManager>();
        if (spm == null)
        {
            Debug.LogError("SellingPhaseManager tidak ditemukan di scene!");
            return;
        }

        var ipoData = spm.ipoDataList.FirstOrDefault(d => d.color == color);
        if (ipoData == null)
        {
            Debug.LogWarning($"IPOData untuk warna '{color}' tidak ditemukan.");
            return;

        }

        int currentIndex = ipoData.ipoIndex;

        // ✅ Jika sudah di -3, kurangi lagi jadi -4 (walau harga tidak ada)
        if (currentIndex == -3)
        {
            ipoData.ipoIndex = -4;
            Debug.LogWarning($"⚠️ IPO index untuk {color} sudah di -3, diturunkan paksa ke -4.");
            spm.UpdateIPOState(ipoData);

            return;
        }

        // 1. Dapatkan harga sekarang
        int clampedIndex = color == "Orange"
            ? Mathf.Clamp(currentIndex, -2, 2)
            : Mathf.Clamp(currentIndex, -3, 3);

        int priceIndex = clampedIndex + 3;
        int currentPrice = spm.ipoPriceMap[color][priceIndex];

        // 2. Hitung harga baru & cari index baru
        int newPrice = Mathf.CeilToInt(currentPrice / 2f);
        int[] priceArray = spm.ipoPriceMap[color];
        int closestPrice = priceArray.OrderBy(p => Mathf.Abs(p - newPrice)).First();
        int newIndexInArray = System.Array.IndexOf(priceArray, closestPrice);
        int newIpoIndex = newIndexInArray - 3;

        ipoData.ipoIndex = newIpoIndex;

        Debug.Log($"📉 Stock Split: IPO {color} turun dari {currentPrice} ke {closestPrice} (Index {ipoData.ipoIndex})");

        // 3. Jalankan pengecekan crash
        spm.UpdateIPOState(ipoData);

        spm.UpdateIPOVisuals();
        gameManager.UpdateDeckCardValuesWithIPO();

    }
private static void InsiderTradeEffect(PlayerProfile player, string color)
    {
        // Cari instance manager yang diperlukan
        RumorPhaseManager rumorPhaseManager = GameObject.FindObjectOfType<RumorPhaseManager>();
        GameManager gameManager = GameObject.FindObjectOfType<GameManager>();

        if (rumorPhaseManager == null || gameManager == null)
        {
            Debug.LogError("RumorPhaseManager atau GameManager tidak ditemukan di scene!");
            return;
        }

        // Cari kartu rumor berikutnya yang sesuai dengan warna dari GameManager
        RumorPhaseManager.RumorEffect futureRumor = rumorPhaseManager.shuffledRumorDeck.FirstOrDefault(r => r.color == color);

        if (futureRumor != null)
        {
            // Set status prediksi untuk pemain (logika bisnis)
            if (futureRumor.effectType == RumorPhaseManager.RumorEffect.EffectType.ModifyIPO)
            {
                if (futureRumor.value > 0)
                {
                    // Anda mungkin perlu menambahkan dictionary 'marketPredictions' di PlayerProfile jika belum ada
                    player.marketPredictions[color] = MarketPredictionType.Rise; 
                    Debug.Log($"[Prediksi UNTUK {player.playerName}] Pasar {color} diprediksi akan NAIK.");
                }
                else if (futureRumor.value < 0)
                {
                    player.marketPredictions[color] = MarketPredictionType.Fall;
                    Debug.Log($"[Prediksi UNTUK {player.playerName}] Pasar {color} diprediksi akan TURUN.");
                }
            }

            // Panggil coroutine yang baru dibuat melalui GameManager untuk menangani visual
            gameManager.StartCoroutine(rumorPhaseManager.DisplayAndHidePrediction(futureRumor));
        }
        else
        {
            Debug.Log($"Tidak ada kartu rumor yang ditemukan untuk {color} di dek rumor.");
        }
    }






}
