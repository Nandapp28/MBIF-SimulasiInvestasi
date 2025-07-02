using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

public class CardEffectManager
{

    public static IEnumerator ApplyEffect(string cardName, PlayerProfile player, string color)
    {
        Debug.Log($"🧪 Menjalankan efek untuk kartu: {cardName}");

        // 'yield return' akan memastikan kita menunggu coroutine selesai.
        // Kita butuh instance MonoBehaviour untuk menjalankan coroutine, kita akan pakai GameManager.Instance
        switch (cardName)
        {
            case "TradeOffer":
                yield return GameManager.Instance.StartCoroutine(TradeOfferEffect(player));
                break;
            case "StockSplit":
                yield return GameManager.Instance.StartCoroutine(StockSplitEffect(player, color));
                break;
            case "InsiderTrade":
                yield return GameManager.Instance.StartCoroutine(InsiderTradeEffect(player, color));
                break;
            case "TenderOffer":
            yield return GameManager.Instance.StartCoroutine(TenderOfferEffect(player, color));
            break;
            // ... (kasus lainnya juga diubah) ...
            default:
                Debug.LogWarning($"Efek belum tersedia untuk kartu: {cardName}");
                yield break; // Tetap harus ada yield
        }
    }

    private static IEnumerator HealEffect(PlayerProfile player)
    {
        int healAmount = 5;
        player.finpoint += healAmount;
        Debug.Log($"🔧 Heal: +{healAmount} finpoint");
        yield break;
    }

    private static IEnumerator ShieldEffect(PlayerProfile player)
    {
        // Contoh: berikan flag "shielded" (kamu bisa buat property di PlayerProfile)
        Debug.Log("🛡️ Player diberi efek shield (placeholder)");
        yield break;
    }

    private static IEnumerator TradeOfferEffect(PlayerProfile player)
    {
        Debug.Log("📦 Efek trade offer dijalankan (placeholder)");
        yield break;
    }

    private static IEnumerator StockSplitEffect(PlayerProfile player, string color)
    {
        SellingPhaseManager spm = GameObject.FindObjectOfType<SellingPhaseManager>();
        GameManager gameManager = GameObject.FindObjectOfType<GameManager>();
        if (spm == null)
        {
            Debug.LogError("SellingPhaseManager tidak ditemukan di scene!");
            yield break;
        }

        var ipoData = spm.ipoDataList.FirstOrDefault(d => d.color == color);
        if (ipoData == null)
        {
            Debug.LogWarning($"IPOData untuk warna '{color}' tidak ditemukan.");
            yield break;

        }

        int currentIndex = ipoData.ipoIndex;

        // ✅ Jika sudah di -3, kurangi lagi jadi -4 (walau harga tidak ada)
        if (currentIndex == -3)
        {
            ipoData.ipoIndex = -4;
            Debug.LogWarning($"⚠️ IPO index untuk {color} sudah di -3, diturunkan paksa ke -4.");
            spm.UpdateIPOState(ipoData);

            yield break;
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
        yield break;

    }
    private static IEnumerator InsiderTradeEffect(PlayerProfile player, string color)
    {
        // Cari instance manager yang diperlukan
        RumorPhaseManager rumorPhaseManager = GameObject.FindObjectOfType<RumorPhaseManager>();
        GameManager gameManager = GameObject.FindObjectOfType<GameManager>();

        if (rumorPhaseManager == null || gameManager == null)
        {
            Debug.LogError("RumorPhaseManager atau GameManager tidak ditemukan di scene!");
            yield break;
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
            if (player.playerName.Contains("You"))
            {
                yield return rumorPhaseManager.StartCoroutine(rumorPhaseManager.DisplayAndHidePrediction(futureRumor));
            }
        }
        else
        {
            Debug.Log($"Tidak ada kartu rumor yang ditemukan untuk {color} di dek rumor.");
        }
    }
    private static IEnumerator TenderOfferEffect(PlayerProfile player, string color)
{
    // Dapatkan referensi ke manager yang dibutuhkan
    SellingPhaseManager sellingManager = GameObject.FindObjectOfType<SellingPhaseManager>();
    GameManager gameManager = GameManager.Instance;
    
    // 🔽 CARI INSTANCE HELPCARDPHASEMANAGER DI SCENE 🔽
    HelpCardPhaseManager helpCardManager = GameObject.FindObjectOfType<HelpCardPhaseManager>();

    // Validasi apakah semua manager ditemukan
    if (sellingManager == null || helpCardManager == null)
    {
        Debug.LogError("SellingPhaseManager atau HelpCardPhaseManager tidak ditemukan di scene!");
        yield break;
    }

    // ... (kode untuk mencari validTargets tetap sama) ...
    List<PlayerProfile> validTargets = gameManager.turnOrder
        .Where(p => p != player && p.cards.Any(c => c.color == color))
        .ToList();

    if (validTargets.Count == 0)
    {
        Debug.LogWarning($"[TenderOffer] Tidak ada pemain lain yang memiliki kartu warna '{color}'. Efek dibatalkan.");
        yield break;
    }

    PlayerProfile targetPlayer = null;

    if (player.isBot)
    {
        targetPlayer = validTargets[Random.Range(0, validTargets.Count)];
        Debug.Log($"[TenderOffer] {player.playerName} (Bot) menargetkan {targetPlayer.playerName}.");
    }
    else // Jika yang mengaktifkan adalah Player Manusia
    {
        // 🔽 PANGGIL COROUTINE DARI INSTANCE HELPCARDMANAGER YANG DITEMUKAN 🔽
        Debug.Log($"[TenderOffer] Menunggu {player.playerName} memilih target...");
        // Perhatikan: kita menjalankan coroutine menggunakan variabel 'helpCardManager'
        yield return helpCardManager.StartCoroutine(helpCardManager.ShowPlayerSelectionUI(validTargets, selectedPlayer => 
        {
            targetPlayer = selectedPlayer;
        }));
        Debug.Log($"[TenderOffer] {player.playerName} memilih untuk menargetkan {targetPlayer.playerName}.");
    }

    // 3. Hitung harga beli (setengah harga, dibulatkan ke atas)
    int fullPrice = sellingManager.GetFullCardPrice(color);
    int purchasePrice = Mathf.CeilToInt(fullPrice / 2.0f);

    Debug.Log($"[TenderOffer] Harga asli kartu {color} adalah {fullPrice}. Harga beli paksa: {purchasePrice}.");

    // 4. Lakukan transaksi
    if (player.CanAfford(purchasePrice))
    {
        Card cardToMove = targetPlayer.cards.FirstOrDefault(c => c.color == color);
        if (cardToMove != null)
        {
            // Lakukan transaksi
            player.DeductFinpoint(purchasePrice);
            targetPlayer.finpoint += purchasePrice; // Pemain target menerima uangnya
            targetPlayer.cards.Remove(cardToMove);
            player.AddCard(cardToMove);

            Debug.Log($"[TenderOffer] {player.playerName} berhasil membeli kartu {color} dari {targetPlayer.playerName} seharga {purchasePrice} Finpoint.");
            gameManager.UpdatePlayerUI(); // Update UI semua pemain
        }
    }
    else
    {
        Debug.LogWarning($"[TenderOffer] {player.playerName} tidak memiliki cukup Finpoint untuk membeli kartu (butuh {purchasePrice}). Efek dibatalkan.");
    }
}






}
