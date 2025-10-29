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
            case "TradeFee":
                yield return GameManager.Instance.StartCoroutine(TradeFeeEffect(player));
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
        CameraController cameraController = spm.cameraController; // Ambil referensi kamera

        var ipoData = spm.ipoDataList.FirstOrDefault(d => d.color == color);
        if (ipoData == null)
        {
            Debug.LogWarning($"IPOData untuk warna '{color}' tidak ditemukan.");
            yield break;
        }

        // --- LOGIKA BARU: KAMERA DAN UI ---
        // Sembunyikan card holder
        if (gameManager.cardHolderParent != null)
        {
            gameManager.cardHolderParent.gameObject.SetActive(false);
        }

        // Gerakkan kamera ke target
        if (cameraController != null)
        {
            CameraController.CameraPosition targetPos = CameraController.CameraPosition.Normal;
            switch (color)
            {
                case "Konsumer": targetPos = CameraController.CameraPosition.Konsumer; break;
                case "Infrastruktur": targetPos = CameraController.CameraPosition.Infrastruktur; break;
                case "Keuangan": targetPos = CameraController.CameraPosition.Keuangan; break;
                case "Tambang": targetPos = CameraController.CameraPosition.Tambang; break;
            }

            if (cameraController.CurrentPosition != targetPos)
            {
                yield return cameraController.MoveTo(targetPos);
                yield return new WaitForSeconds(0.5f);
            }
        }
        // --- AKHIR LOGIKA BARU ---


        // [ ... logika penyesuaian harga yang sudah ada tetap di sini ... ]
        #region Existing Price Logic (No Changes)
        int minIndex = (color == "Tambang") ? -2 : -3;
        if (ipoData.currentState == IPOState.Normal && ipoData.ipoIndex == minIndex)
        {
            Debug.Log($"📉 [Stock Split] Kondisi Khusus Terpenuhi untuk '{color}'. Index sudah minimal. Menurunkan index sebesar 1 untuk memicu crash.");
            ipoData.ipoIndex -= 1;
            spm.UpdateIPOState(ipoData);
            spm.UpdateIPOVisuals();
        }
        else
        {
            int currentFullPrice = spm.GetFullCardPrice(color);
            float targetPrice = currentFullPrice / 2f;
            Debug.Log($"[Stock Split] Info Awal '{color}': Harga Penuh={currentFullPrice}, State={ipoData.currentState}. Harga Target={targetPrice}");
            IPOState originalState = ipoData.currentState;
            var allPossibilities = new List<PriceOutcome>();
            int[] priceMap = spm.ipoPriceMap[color];
            int maxIndex = (color == "Tambang") ? 2 : 3;
            var statesAndBonuses = new[]
            {
            new { State = IPOState.Normal, Bonus = 0 },
            new { State = IPOState.Ascend, Bonus = 5 },
            new { State = IPOState.Advanced, Bonus = 10 }
        };

            foreach (var stateInfo in statesAndBonuses)
            {
                for (int i = minIndex; i <= maxIndex; i++)
                {
                    int basePrice = priceMap[i + 3];
                    if (basePrice == 0) continue;
                    allPossibilities.Add(new PriceOutcome
                    {
                        TotalPrice = basePrice + stateInfo.Bonus,
                        State = stateInfo.State,
                        IpoIndex = i,
                        SalesBonus = stateInfo.Bonus
                    });
                }
            }

            if (allPossibilities.Count == 0)
            {
                Debug.LogError("Tidak ada kemungkinan harga yang valid ditemukan!");

                // Kembalikan UI sebelum keluar
                if (gameManager.cardHolderParent != null) gameManager.cardHolderParent.gameObject.SetActive(true);
                yield break;
            }

            PriceOutcome bestMatch = allPossibilities
        .OrderBy(p => Mathf.Abs(p.TotalPrice - targetPrice)) // Kriteria 1: Jarak terdekat
        .ThenBy(p => p.TotalPrice)                           // Kriteria 2: Jika jarak sama, pilih harga terendah
        .First();

            Debug.Log($"📉 [Stock Split] Hasil Terbaik untuk '{color}': State={bestMatch.State}, Index={bestMatch.IpoIndex}, Harga Total={bestMatch.TotalPrice}");
            ipoData.currentState = bestMatch.State;
            ipoData.salesBonus = bestMatch.SalesBonus;
            ipoData.ipoIndex = bestMatch.IpoIndex;
            if (bestMatch.State != originalState && (int)bestMatch.State < (int)originalState)
            {
                // Jika state turun, putar suara 'state down'
                if (SfxManager.Instance != null && spm.ipoMoveSound != null)
                {
                    SfxManager.Instance.PlaySound(spm.ipoMoveSound);
                }
                if (SfxManager.Instance != null && spm.ipoStateDown != null)
                {
                    SfxManager.Instance.PlaySound(spm.ipoStateDown);
                }
            }
            else
            {
                // Jika state tidak turun (tetap atau naik), putar suara gerak biasa
                if (SfxManager.Instance != null && spm.ipoMoveSound != null)
                {
                    SfxManager.Instance.PlaySound(spm.ipoMoveSound);
                }
            }

            spm.UpdateIPOVisuals();
            gameManager.UpdateDeckCardValuesWithIPO();
        }
        #endregion

        yield return new WaitForSeconds(1.5f); // Jeda agar pemain bisa lihat perubahan IPO

        // --- LOGIKA DUPLIKASI KARTU DIMULAI ---
        Debug.Log($"[Stock Split] Menggandakan semua kartu berwarna '{color}' untuk setiap pemain.");

        foreach (var p in gameManager.turnOrder)
        {
            var cardsToDuplicate = p.cards.Where(c => c.color == color).ToList();
            if (cardsToDuplicate.Any())
            {
                Debug.Log($"Pemain {p.playerName} memiliki {cardsToDuplicate.Count} kartu '{color}'. Menggandakan...");
                foreach (var card in cardsToDuplicate)
                {
                    Card newCard = new Card(card.cardName, card.description, card.value, card.color);
                    p.AddCard(newCard);
                    Debug.Log($"    -> Menambahkan duplikat '{newCard.cardName}' ke {p.playerName}.");
                }
            }
        }

        gameManager.UpdatePlayerUI();
        // --- LOGIKA DUPLIKASI KARTU SELESAI ---

        // --- LOGIKA BARU: KEMBALIKAN KAMERA DAN UI ---
        if (cameraController != null && cameraController.CurrentPosition != CameraController.CameraPosition.Normal)
        {
            yield return cameraController.MoveTo(CameraController.CameraPosition.Normal);
        }

        if (gameManager.cardHolderParent != null)
        {
            gameManager.cardHolderParent.gameObject.SetActive(true);
        }
        // --- AKHIR LOGIKA BARU ---

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

    private static IEnumerator TradeFeeEffect(PlayerProfile player)
    {
        SellingPhaseManager sellingManager = GameObject.FindObjectOfType<SellingPhaseManager>();
        if (sellingManager == null)
        {
            Debug.LogError("SellingPhaseManager tidak ditemukan!");
            yield break;
        }

        if (player.cards.Count == 0)
        {
            Debug.Log($"[TradeFee] {player.playerName} tidak memiliki kartu untuk dijual.");
            yield break;
        }

        Dictionary<string, int> quantitiesToSell = new Dictionary<string, int>();

        if (player.playerName.Contains("You"))
        {
            bool hasConfirmed = false;
            Debug.Log($"[TradeFee] Menampilkan UI penjualan multi-warna untuk {player.playerName}...");

            yield return sellingManager.StartCoroutine(
                sellingManager.ShowMultiColorSellUI(player, (confirmedAmounts) =>
                {
                    quantitiesToSell = confirmedAmounts;
                    hasConfirmed = true;
                })
            );

            yield return new WaitUntil(() => hasConfirmed);
        }
        else // Logika untuk Bot
        {
            Debug.Log($"[TradeFee] {player.playerName} (Bot) mengevaluasi kartu untuk dijual...");
            Dictionary<string, List<Card>> cardsByColor = player.cards
                .GroupBy(card => card.color)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var color in sellingManager.ipoPriceMap.Keys)
            {
                int countToSell = 0;
                if (cardsByColor.ContainsKey(color))
                {
                    // Logika peluang yang sama seperti di SellingPhaseManager
                    float sellChance = 0.5f; // Bisa disesuaikan
                    foreach (var card in cardsByColor[color])
                    {
                        if (Random.value < sellChance)
                        {
                            countToSell++;
                        }
                    }
                }
                if (countToSell > 0)
                {
                    quantitiesToSell[color] = countToSell;
                }
            }
        }

        if (quantitiesToSell.Count > 0)
        {
            int totalEarnings = 0;
            string salesSummary = "";

            foreach (var pair in quantitiesToSell)
            {
                string color = pair.Key;
                int amount = pair.Value;

                if (amount == 0) continue;

                int pricePerCard = sellingManager.GetFullCardPrice(color);
                int earningsForColor = amount * pricePerCard;
                totalEarnings += earningsForColor;

                List<Card> cardsToRemove = player.cards.Where(c => c.color == color).Take(amount).ToList();
                foreach (var card in cardsToRemove)
                {
                    player.cards.Remove(card);
                }
                salesSummary += $"{amount} '{color}' ({earningsForColor} FP), ";
            }

            if (totalEarnings > 0)
            {
                player.finpoint += totalEarnings;
                Debug.Log($"[TradeFee] Transaksi Berhasil! {player.playerName} menjual {salesSummary}dan mendapatkan total {totalEarnings} Finpoint.");
                GameManager.Instance.UpdatePlayerUI();
            }
            else
            {
                Debug.Log($"[TradeFee] {player.playerName} memilih untuk tidak menjual kartu.");
            }
        }
        else
        {
            Debug.Log($"[TradeFee] {player.playerName} memilih untuk tidak menjual kartu.");
        }

        yield break;
    }

    // ... (sisa kode di CardEffectManager.cs tetap sama) ...
}





