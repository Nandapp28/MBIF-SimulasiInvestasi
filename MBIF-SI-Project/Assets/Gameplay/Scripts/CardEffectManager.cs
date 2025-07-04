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

            case "StockSplit":
                yield return GameManager.Instance.StartCoroutine(StockSplitEffect(player, color));
                break;
            case "InsiderTrade":
                yield return GameManager.Instance.StartCoroutine(InsiderTradeEffect(player, color));
                break;
            case "TenderOffer":
                yield return GameManager.Instance.StartCoroutine(TenderOfferEffect(player, color));
                break;
            case "TradeFree":
                yield return GameManager.Instance.StartCoroutine(TradeFreeEffect(player, color));
                break;
            case "Flashbuy":
                yield return GameManager.Instance.StartCoroutine(FlashbuyEffect(player));
                break;
            // ... (kasus lainnya juga diubah) ...
            default:
                Debug.LogWarning($"Efek belum tersedia untuk kartu: {cardName}");
                yield break; // Tetap harus ada yield
        }
    }


    private static IEnumerator FlashbuyEffect(PlayerProfile player)
    {
        Debug.Log($"⚡️ {player.playerName} mengaktifkan Flashbuy! Bisa membeli hingga 2 kartu tambahan.");
        // Panggil coroutine khusus di GameManager untuk menangani pemilihan kartu
        yield return GameManager.Instance.StartCoroutine(GameManager.Instance.HandleFlashbuySelection(player));
    }
    // Definisikan struct kecil ini di dalam class CardEffectManager, di atas method-methodnya
    // untuk membantu menyimpan data sementara.
    private struct PriceOutcome
{
    public int TotalPrice;
    public IPOState State;
    public int IpoIndex;
    public int SalesBonus;
}

private static IEnumerator StockSplitEffect(PlayerProfile player, string color)
{
    // 1. Dapatkan referensi manager
    SellingPhaseManager spm = GameObject.FindObjectOfType<SellingPhaseManager>();
    GameManager gameManager = GameObject.FindObjectOfType<GameManager>();
    if (spm == null || gameManager == null)
    {
        Debug.LogError("SellingPhaseManager atau GameManager tidak ditemukan!");
        yield break;
    }

    var ipoData = spm.ipoDataList.FirstOrDefault(d => d.color == color);
    if (ipoData == null)
    {
        Debug.LogWarning($"IPOData untuk warna '{color}' tidak ditemukan.");
        yield break;
    }
    int minIndex = (color == "Tambang") ? -2 : -3;

    // Jika state sudah Normal DAN index sudah di posisi paling minimal
    if (ipoData.currentState == IPOState.Normal && ipoData.ipoIndex == minIndex)
    {
        Debug.Log($"📉 [Stock Split] Kondisi Khusus Terpenuhi untuk '{color}'. Index sudah minimal. Menurunkan index sebesar 1 untuk memicu crash.");
        
        // Langsung kurangi index sebesar 1. Ini akan membuat index di bawah ambang batas.
        ipoData.ipoIndex -= 1; 

        // Panggil UpdateIPOState untuk memproses logika CRASH market.
        spm.UpdateIPOState(ipoData); 
        
        // Update visual untuk merefleksikan perubahan (misal: token kembali ke posisi 0 setelah crash).
        spm.UpdateIPOVisuals(); 
        
        // Keluar dari fungsi karena tugas untuk kasus khusus ini sudah selesai.
        yield break; 
    }
    // 2. Hitung harga jual penuh saat ini dan tentukan harga target.
    int currentFullPrice = spm.GetFullCardPrice(color);
    int targetPrice = Mathf.FloorToInt(currentFullPrice / 2f);
    
    Debug.Log($"[Stock Split] Info Awal '{color}': Harga Penuh={currentFullPrice}, State={ipoData.currentState}. Harga Target={targetPrice}");

    // 3. Buat daftar SEMUA kemungkinan harga (kombinasi state + index)
    var allPossibilities = new List<PriceOutcome>();
    int[] priceMap = spm.ipoPriceMap[color];
    int maxIndex = (color == "Tambang") ? 2 : 3;

    // Definisikan setiap state dan bonusnya
    var statesAndBonuses = new[]
    {
        new { State = IPOState.Normal, Bonus = 0 },
        new { State = IPOState.Ascend, Bonus = 5 },
        new { State = IPOState.Advanced, Bonus = 10 }
    };

    // Lakukan iterasi untuk setiap state dan setiap index yang valid
    foreach (var stateInfo in statesAndBonuses)
    {
        for (int i = minIndex; i <= maxIndex; i++)
        {
            int basePrice = priceMap[i + 3]; // Ambil harga dasar dari index
            if (basePrice == 0) continue; // Jangan masukkan harga crash (0) sebagai kemungkinan

            allPossibilities.Add(new PriceOutcome
            {
                TotalPrice = basePrice + stateInfo.Bonus,
                State = stateInfo.State,
                IpoIndex = i,
                SalesBonus = stateInfo.Bonus
            });
        }
    }
    
    // 4. Cari kemungkinan TERBAIK yang harganya paling mendekati harga target.
    if (allPossibilities.Count == 0) {
        Debug.LogError("Tidak ada kemungkinan harga yang valid ditemukan!");
        yield break;
    }

    PriceOutcome bestMatch = allPossibilities
                              .OrderBy(p => Mathf.Abs(p.TotalPrice - targetPrice))
                              .First();
    
    // 5. Terapkan hasil terbaik ke ipoData.
    Debug.Log($"📉 [Stock Split] Hasil Terbaik untuk '{color}': State={bestMatch.State}, Index={bestMatch.IpoIndex}, Harga Total={bestMatch.TotalPrice}");
    
    ipoData.currentState = bestMatch.State;
    ipoData.salesBonus = bestMatch.SalesBonus;
    ipoData.ipoIndex = bestMatch.IpoIndex;

    // 6. Update visual dan data game.
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
        SellingPhaseManager sellingManager = GameObject.FindObjectOfType<SellingPhaseManager>();
        GameManager gameManager = GameManager.Instance;
        HelpCardPhaseManager helpCardManager = GameObject.FindObjectOfType<HelpCardPhaseManager>();

        if (sellingManager == null || helpCardManager == null)
        {
            Debug.LogError("SellingPhaseManager atau HelpCardPhaseManager tidak ditemukan di scene!");
            yield break;
        }

        List<PlayerProfile> validTargets = gameManager.turnOrder
            .Where(p => p != player && p.cards.Any(c => c.color == color))
            .ToList();

        if (validTargets.Count == 0)
        {
            Debug.LogWarning($"[TenderOffer] Tidak ada pemain lain yang memiliki kartu warna '{color}'. Efek dibatalkan.");
            yield break;
        }

        PlayerProfile targetPlayer = null;

        // --- LOGIKA DIPERBAIKI ---
        if (player.playerName.Contains("You")) // Logika untuk Pemain Manusia
        {
            Debug.Log($"[TenderOffer] Menunggu {player.playerName} memilih target...");
            yield return helpCardManager.StartCoroutine(helpCardManager.ShowPlayerSelectionUI(validTargets, selectedPlayer =>
            {
                targetPlayer = selectedPlayer;
            }));
            Debug.Log($"[TenderOffer] {player.playerName} memilih untuk menargetkan {targetPlayer.playerName}.");
        }
        else // Logika untuk Bot
        {
            targetPlayer = validTargets[Random.Range(0, validTargets.Count)];
            Debug.Log($"[TenderOffer] {player.playerName} (Bot) menargetkan {targetPlayer.playerName}.");
        }
        // --- AKHIR PERBAIKAN ---

        int fullPrice = sellingManager.GetFullCardPrice(color);
        int purchasePrice = Mathf.CeilToInt(fullPrice / 2.0f);

        Debug.Log($"[TenderOffer] Harga asli kartu {color} adalah {fullPrice}. Harga beli paksa: {purchasePrice}.");

        if (player.CanAfford(purchasePrice))
        {
            Card cardToMove = targetPlayer.cards.FirstOrDefault(c => c.color == color);
            if (cardToMove != null)
            {
                player.DeductFinpoint(purchasePrice);
                targetPlayer.finpoint += purchasePrice;
                targetPlayer.cards.Remove(cardToMove);
                player.AddCard(cardToMove);

                Debug.Log($"[TenderOffer] {player.playerName} berhasil membeli kartu {color} dari {targetPlayer.playerName} seharga {purchasePrice} Finpoint.");
                gameManager.UpdatePlayerUI();
            }
        }
        else
        {
            Debug.LogWarning($"[TenderOffer] {player.playerName} tidak memiliki cukup Finpoint untuk membeli kartu (butuh {purchasePrice}). Efek dibatalkan.");
        }
    }

    private static IEnumerator TradeFreeEffect(PlayerProfile player, string color)
    {
        SellingPhaseManager sellingManager = GameObject.FindObjectOfType<SellingPhaseManager>();
        if (sellingManager == null)
        {
            Debug.LogError("SellingPhaseManager tidak ditemukan!");
            yield break;
        }

        int cardsOwned = player.cards.Count(c => c.color == color);
        if (cardsOwned == 0)
        {
            Debug.Log($"[TradeFree] {player.playerName} tidak memiliki kartu warna '{color}' untuk dijual.");
            yield break;
        }

        int quantityToSell = 0;

        // --- LOGIKA DIPERBAIKI ---
        if (player.playerName.Contains("You")) // Logika untuk Pemain Manusia
        {
            int sellAmountFromUI = -1;
            Debug.Log($"[TradeFree] Menampilkan UI penjualan untuk {player.playerName}...");
            yield return sellingManager.StartCoroutine(
                sellingManager.ShowSingleColorSellUI(player, color, (confirmedAmount) =>
                {
                    sellAmountFromUI = confirmedAmount;
                })
            );
            
            quantityToSell = sellAmountFromUI;
        }
        else // Logika untuk Bot
        {
            quantityToSell = cardsOwned;
            Debug.Log($"[TradeFree] {player.playerName} (Bot) memutuskan untuk menjual {quantityToSell} kartu '{color}'.");
        }
        // --- AKHIR PERBAIKAN ---

        if (quantityToSell > 0)
        {
            int pricePerCard = sellingManager.GetFullCardPrice(color);
            int totalEarnings = quantityToSell * pricePerCard;

            player.finpoint += totalEarnings;

            List<Card> cardsToRemove = player.cards.Where(c => c.color == color).Take(quantityToSell).ToList();
            foreach (var card in cardsToRemove)
            {
                player.cards.Remove(card);
            }

            Debug.Log($"[TradeFree] Transaksi Berhasil! {player.playerName} menjual {quantityToSell} kartu '{color}' dan mendapatkan {totalEarnings} Finpoint.");

            GameManager.Instance.UpdatePlayerUI();
        }
        else
        {
            Debug.Log($"[TradeFree] {player.playerName} memilih untuk tidak menjual kartu.");
        }
        
        yield break;
    }

    // ... (sisa kode di CardEffectManager.cs tetap sama) ...
}





