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

            case "Trade Offer":
                TradeOfferEffect(player);
                break;
            case "Stock Split":
                StockSplitEffect(player, color);
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
    int reductionAmount = 1;
    string affectedColor = color;

    // Cari instance SellingPhaseManager di scene
    SellingPhaseManager spm = GameObject.FindObjectOfType<SellingPhaseManager>();
    if (spm == null)
    {
        Debug.LogError("SellingPhaseManager tidak ditemukan di scene!");
        return;
    }

    // Misalnya kita ingin mengurangi ipoIndex warna "Red"
    var ipoData = spm.ipoDataList.FirstOrDefault(d => d.color == affectedColor);
    if (ipoData != null)
    {
        ipoData.ipoIndex -= reductionAmount; // Cegah nilai negatif

        Debug.Log($"📉 Stock Split: IPO index warna {ipoData.color} dikurangi sebanyak -{reductionAmount}. Nilai sekarang: {ipoData.ipoIndex}");
    }
    else
    {
        Debug.LogWarning("IPOData untuk warna 'Red' tidak ditemukan.");
    }
}



}
