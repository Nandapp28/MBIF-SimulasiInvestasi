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
            case "TradeFree":
                yield return GameManager.Instance.StartCoroutine(TradeFreeEffect(player, color));
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
// Tambahkan method coroutine baru ini di dalam kelas CardEffectManager
private static IEnumerator TradeFreeEffect(PlayerProfile player, string color)
{
    // 1. Dapatkan referensi manager
    SellingPhaseManager sellingManager = GameObject.FindObjectOfType<SellingPhaseManager>();
    if (sellingManager == null)
    {
        Debug.LogError("SellingPhaseManager tidak ditemukan!");
        yield break;
    }

    // 2. Cek apakah pemain punya kartu untuk dijual
    int cardsOwned = player.cards.Count(c => c.color == color);
    if (cardsOwned == 0)
    {
        Debug.Log($"[TradeFree] {player.playerName} tidak memiliki kartu warna '{color}' untuk dijual.");
        yield break;
    }

    int quantityToSell = 0;

    // 3. Logika untuk Bot vs Pemain
    if (player.isBot)
    {
        // Bot akan selalu menjual semua kartu yang dimilikinya dari warna tersebut
        quantityToSell = cardsOwned;
        Debug.Log($"[TradeFree] {player.playerName} (Bot) memutuskan untuk menjual {quantityToSell} kartu '{color}'.");
    }
    else // Untuk pemain manusia, tampilkan UI
    {
        int sellAmountFromUI = -1; // Variabel untuk menampung hasil dari UI

        Debug.Log($"[TradeFree] Menampilkan UI penjualan untuk {player.playerName}...");
        yield return sellingManager.StartCoroutine(
            sellingManager.ShowSingleColorSellUI(player, color, (confirmedAmount) =>
            {
                sellAmountFromUI = confirmedAmount;
            })
        );
        
        quantityToSell = sellAmountFromUI;
    }

    // 4. Proses transaksi jika ada kartu yang dijual
    if (quantityToSell > 0)
    {
        // Dapatkan harga jual saat ini
        int pricePerCard = sellingManager.GetFullCardPrice(color);
        int totalEarnings = quantityToSell * pricePerCard;

        // Tambahkan finpoint ke pemain
        player.finpoint += totalEarnings;

        // Hapus kartu dari tangan pemain
        List<Card> cardsToRemove = player.cards.Where(c => c.color == color).Take(quantityToSell).ToList();
        foreach (var card in cardsToRemove)
        {
            player.cards.Remove(card);
        }

        Debug.Log($"[TradeFree] Transaksi Berhasil! {player.playerName} menjual {quantityToSell} kartu '{color}' dan mendapatkan {totalEarnings} Finpoint.");

        // Perbarui UI game
        GameManager.Instance.UpdatePlayerUI();
    }
    else
    {
        Debug.Log($"[TradeFree] {player.playerName} memilih untuk tidak menjual kartu.");
    }
    
    yield break; // Efek selesai
}






}
