using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CardEffectManager
{
    
    public static void ApplyEffect(string cardName, PlayerProfile player)
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
}
