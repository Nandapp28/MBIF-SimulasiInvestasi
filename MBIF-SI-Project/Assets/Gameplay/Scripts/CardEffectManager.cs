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
    SellingPhaseManager spm = GameObject.FindObjectOfType<SellingPhaseManager>();
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
        spm.HandleCrashMultiplier(ipoData, player);
        return;
    }

    // 1. Dapatkan harga sekarang
    int clampedIndex = color == "Green"
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
    spm.HandleCrashMultiplier(ipoData, player);
}






}
