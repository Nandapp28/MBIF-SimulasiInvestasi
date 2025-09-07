// File: HelpCardPhaseManager.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
[System.Serializable]
public class HelpCardArt
{
    public HelpCardEffect effect;
    public Sprite texture;
}

public class HelpCardPhaseManager : MonoBehaviour
{
    [Header("Game References")]
    public GameManager gameManager;
    public SellingPhaseManager sellingManager;
    public RumorPhaseManager rumorPhaseManager;
    // Diperlukan untuk efek IPO


    [Header("UI Elements")]
    public GameObject helpCardActivationPanel; // Panel yang menunjukkan info kartu & tombol
    public UnityEngine.UI.Text cardNameText;
    public UnityEngine.UI.Text cardDescriptionText;
    public UnityEngine.UI.Button activateButton;
    public UnityEngine.UI.Button skipButton;
    [Header("Effect Display UI")]
    public GameObject effectDisplayPanel;
    public UnityEngine.UI.Text effectPlayerNameText;
    public UnityEngine.UI.Image effectCardImage;
    public UnityEngine.UI.Text effectTargetText;
    [Header("Game Assets")] // Header baru untuk aset gambar
    public List<HelpCardArt> cardArtList;
    private Dictionary<HelpCardEffect, Sprite> cardArtDictionary;
    public UnityEngine.UI.Image cardImageUI;
    [Header("IPO Selection UI")]
    public GameObject ipoSelectionPanel;
    public UnityEngine.UI.Button redButton;
    public UnityEngine.UI.Button blueButton;
    public UnityEngine.UI.Button greenButton;
    public UnityEngine.UI.Button orangeButton;
    //[Header("Player Selection UI")]
    //public GameObject playerSelectionPanel;
    //public Transform playerButtonContainer;
    //public GameObject playerButtonPrefab;

    private List<PlayerProfile> turnOrder;
    private void Awake()
    {
        // Ubah List menjadi Dictionary agar pencarian gambar lebih cepat
        cardArtDictionary = new Dictionary<HelpCardEffect, Sprite>();
        foreach (var art in cardArtList)
        {
            if (!cardArtDictionary.ContainsKey(art.effect))
            {
                cardArtDictionary.Add(art.effect, art.texture);
            }
        }
    }


    // Fungsi utama yang dipanggil untuk memulai fase ini
    public void StartHelpCardPhase(List<PlayerProfile> players, int resetCount)
    {
        Debug.Log("--- Memulai Fase Kartu Bantuan ---");
        this.turnOrder = players.OrderBy(p => p.ticketNumber).ToList();


        StartCoroutine(ActivationSequence());
    }

    public void DistributeHelpCards(List<PlayerProfile> playersToDistribute)
    {
        Debug.Log("Membagikan Kartu Bantuan kepada semua pemain...");
        foreach (var player in playersToDistribute)
        {
            var card = GetRandomHelpCard();
            if (player.helpCards == null)
            {
                player.helpCards = new List<HelpCard>();
            }
            player.helpCards.Add(card);
            Debug.Log($"{player.playerName} mendapatkan kartu: '{card.cardName}'");
        }
    }

    private IEnumerator ActivationSequence()
    {
        yield return new WaitForSeconds(1f);

        foreach (var player in turnOrder)
        {
            if (player.helpCards.Count == 0)
            {
                Debug.Log($"{player.playerName} tidak memiliki Kartu Bantuan untuk diaktifkan.");
                continue;
            }

            Debug.Log($"Giliran {player.playerName} untuk mengaktifkan kartu bantuannya.");

            for (int i = player.helpCards.Count - 1; i >= 0; i--)
            {
                HelpCard currentCard = player.helpCards[i];

                if (player.playerName.Contains("You"))
                {
                    // PERUBAHAN KUNCI: Sekarang kita 'yield return' coroutine ini,
                    // artinya ActivationSequence akan berhenti di sini sampai HandlePlayerChoice selesai.
                    yield return HandlePlayerChoice(player, currentCard);
                }
                else
                {
                    // PERUBAHAN KUNCI: Bot juga sekarang menunggu efeknya selesai.
                    yield return HandleBotChoice(player, currentCard);
                }

                yield return new WaitForSeconds(1f);
            }
        }

        Debug.Log("--- Fase Kartu Bantuan Selesai ---");
        UITransitionAnimator.Instance.StartTransition("Selling Phase");
        yield return new WaitForSeconds(4f);
        sellingManager.StartSellingPhase(turnOrder, gameManager.resetCount, gameManager.maxResetCount, gameManager.resetSemesterButton);


    }


    private IEnumerator HandlePlayerChoice(PlayerProfile player, HelpCard card)
    {
        helpCardActivationPanel.SetActive(true);
        cardImageUI.sprite = card.cardImage;

        bool choiceMade = false;
        bool wantsToActivate = false;

        activateButton.onClick.RemoveAllListeners();
        activateButton.onClick.AddListener(() =>
        {
            wantsToActivate = true;
            choiceMade = true;
        });

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(() =>
        {
            wantsToActivate = false;
            choiceMade = true;
        });

        // Tunggu sampai pemain menekan tombol Activate atau Skip
        yield return new WaitUntil(() => choiceMade);

        // Sembunyikan panel setelah pilihan dibuat
        helpCardActivationPanel.SetActive(false);

        if (wantsToActivate)
        {
            // Jika pemain memilih aktivasi, jalankan coroutine ApplyEffect DAN TUNGGU sampai selesai.
            yield return StartCoroutine(ApplyEffect(player, card));
            player.helpCards.Remove(card); // Hapus kartu yang sudah digunakan
        }
        else
        {
            Debug.Log($"{player.playerName} memilih untuk tidak mengaktifkan kartu '{card.cardName}'.");
        }
    }

    private IEnumerator HandleBotChoice(PlayerProfile bot, HelpCard card)
    {
        yield return new WaitForSeconds(1.5f);

        bool activate = UnityEngine.Random.value < 0.6f;

        if (activate)
        {
            yield return StartCoroutine(ApplyEffect(bot, card));
            bot.helpCards.Remove(card);
        }
        else
        {
            Debug.Log($"{bot.playerName} (Bot) memilih untuk tidak mengaktifkan kartu '{card.cardName}'.");
        }
    }

    private IEnumerator ApplyEffect(PlayerProfile player, HelpCard card)
{
    Debug.Log($"{player.playerName} mengaktifkan '{card.cardName}'!");

    string targetDescription = ""; // Dideklarasikan di atas untuk semua case

    switch (card.effectType)
    {
        case HelpCardEffect.AdministrativePenalties:
        case HelpCardEffect.NegativeEquity:
            {
                // --- Langkah 1: Persiapan & Penentuan Target ---
                string colorToSabotage = null;
                if (player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorToSabotage = selectedColor; }));
                }
                else
                {
                    Dictionary<string, int> colorCounts = player.GetCardColorCounts();
                    int minCount = colorCounts.Values.Min();
                    List<string> colorsWithMinCount = colorCounts
                        .Where(pair => pair.Value == minCount)
                        .Select(pair => pair.Key)
                        .ToList();
                    colorToSabotage = colorsWithMinCount[UnityEngine.Random.Range(0, colorsWithMinCount.Count)];
                }
                Debug.Log($"{player.playerName} memilih untuk menyabotase IPO {colorToSabotage}.");

                // --- Langkah 2: Tentukan Deskripsi & Tampilkan Hasil (untuk Bot) ---
                targetDescription = $"Target: \n{colorToSabotage}";
                if (!player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                }

                // --- Langkah 3: Eksekusi Efek ---
                int ipoChange = (card.effectType == HelpCardEffect.AdministrativePenalties) ? -2 : -3;
                yield return StartCoroutine(sellingManager.ModifyIPOIndexWithCamera(colorToSabotage, ipoChange));
            }
            break;

        case HelpCardEffect.TaxEvasion:
            {
                // --- Langkah 1 & 2: Tentukan Deskripsi & Tampilkan Hasil ---
                Debug.Log($"{player.playerName} mengaktifkan Penghindaran Pajak. Semua pemain lain harus membayar pajak!");
                targetDescription = "Target: \n Semua Pemain Lain";
                if (!player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                }

                // --- Langkah 3: Eksekusi Efek ---
                foreach (var p in turnOrder)
                {
                    if (p == player) continue; 

                    int cardCount = p.cards.Count;
                    int cost = cardCount * 2;
                    p.DeductFinpoint(cost);
                    Debug.Log($"{p.playerName} membayar {cost} Finpoint untuk {cardCount} kartu. Sisa: {p.finpoint}");
                }
            }
            break;

        case HelpCardEffect.MarketPrediction:
        case HelpCardEffect.EyeOfTruth:
            {
                // --- Langkah 1: Persiapan & Penentuan Target ---
                string chosenColor = null;
                if (player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { chosenColor = selectedColor; }));
                }
                else
                {
                    int randomIndex = UnityEngine.Random.Range(0, sellingManager.ipoDataList.Count);
                    chosenColor = sellingManager.ipoDataList[randomIndex].color;
                }
                 Debug.Log($"{player.playerName} mencoba memprediksi pasar untuk warna {chosenColor}.");

                // --- Langkah 2: Tentukan Deskripsi & Tampilkan Hasil (untuk Bot) ---
                targetDescription = $"Target: \n{chosenColor}";
                 if (!player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                }

                // --- Langkah 3: Eksekusi Efek ---
                RumorPhaseManager.RumorEffect futureRumor = rumorPhaseManager.shuffledRumorDeck.FirstOrDefault(r => r.color == chosenColor);

                if (futureRumor != null)
                {
                    if (futureRumor.effectType == RumorPhaseManager.RumorEffect.EffectType.ModifyIPO)
                    {
                        if (futureRumor.value > 0)
                        {
                            player.marketPredictions[chosenColor] = MarketPredictionType.Rise;
                            Debug.Log($"[Prediksi UNTUK {player.playerName}] Pasar {chosenColor} diprediksi akan NAIK.");
                        }
                        else if (futureRumor.value < 0)
                        {
                            player.marketPredictions[chosenColor] = MarketPredictionType.Fall;
                            Debug.Log($"[Prediksi UNTUK {player.playerName}] Pasar {chosenColor} diprediksi akan TURUN.");
                        }
                    }

                    if (player.playerName.Contains("You"))
                    {
                        Debug.Log($"Menampilkan bocoran kartu rumor untuk {player.playerName}: {futureRumor.cardName}");
                        yield return rumorPhaseManager.StartCoroutine(rumorPhaseManager.DisplayAndHidePrediction(futureRumor));
                    }
                }
                else
                {
                    Debug.Log($"Tidak ada kartu rumor yang ditemukan untuk {chosenColor} di dek rumor.");
                }
            }
            break;

        case HelpCardEffect.MarketStabilization:
            {
                // --- Langkah 1 & 2: Tentukan Deskripsi & Tampilkan Hasil ---
                Debug.Log($"{player.playerName} menggunakan kartu 'Stabilisasi Pasar'. Mereset semua nilai IPO!");
                targetDescription = "Target: Semua Sektor";
                if (!player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                }
                
                // --- Langkah 3: Eksekusi Efek ---
                yield return StartCoroutine(sellingManager.ResetAllIPOIndexesWithCamera());
            }
            break;

        case HelpCardEffect.CardSwap:
            {
                // --- Langkah 1: Persiapan & Penentuan Target ---
                if (player.cards.Count == 0)
                {
                    Debug.LogWarning($"[CardSwap] {player.playerName} tidak punya kartu, efek gagal.");
                    player.helpCards.Add(card); // Kembalikan kartu
                    yield break;
                }
                List<PlayerProfile> validTargets = turnOrder.Where(p => p != player && p.cards.Count > 0).ToList();
                if (validTargets.Count == 0)
                {
                    Debug.LogWarning($"[CardSwap] Tidak ada target yang valid, efek gagal.");
                    player.helpCards.Add(card); // Kembalikan kartu
                    yield break;
                }

                string colorFromPlayer = null;
                PlayerProfile targetPlayer = null;
                string colorFromTarget = null;

                if (player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorFromPlayer = selectedColor; }, player.cards.Select(c => c.color).Distinct().ToList()));
                    yield return StartCoroutine(ShowPlayerSelectionUI(validTargets, selectedPlayer => { targetPlayer = selectedPlayer; }));
                    yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorFromTarget = selectedColor; }, targetPlayer.cards.Select(c => c.color).Distinct().ToList()));
                }
                else // Logika untuk Bot
                {
                    colorFromPlayer = player.cards[UnityEngine.Random.Range(0, player.cards.Count)].color;
                    targetPlayer = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
                    colorFromTarget = targetPlayer.cards[UnityEngine.Random.Range(0, targetPlayer.cards.Count)].color;
                }

                // --- Langkah 2: Tentukan Deskripsi & Tampilkan Hasil (untuk Bot) ---
                targetDescription = $"{targetPlayer.playerName} \n Menukar sektor {colorFromPlayer} dengan {colorFromTarget} milik target";
                if (!player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                }

                // --- Langkah 3: Eksekusi Efek ---
                Card cardFromPlayer = player.cards.FirstOrDefault(c => c.color == colorFromPlayer);
                Card cardFromTarget = targetPlayer.cards.FirstOrDefault(c => c.color == colorFromTarget);

                if (cardFromPlayer != null && cardFromTarget != null)
                {
                    player.cards.Remove(cardFromPlayer);
                    targetPlayer.cards.Remove(cardFromTarget);
                    player.AddCard(cardFromTarget);
                    targetPlayer.AddCard(cardFromPlayer);
                    Debug.Log($"[CardSwap] {player.playerName} menukar kartu {colorFromPlayer} miliknya dengan kartu {colorFromTarget} milik {targetPlayer.playerName}.");
                    gameManager.UpdatePlayerUI();
                }
                else
                {
                    Debug.LogError("[CardSwap] Gagal menemukan kartu untuk ditukar.");
                }
            }
            break;

        case HelpCardEffect.ForcedPurchase:
            {
                // --- Langkah 1: Persiapan & Penentuan Target ---
                List<PlayerProfile> validTargets = turnOrder.Where(p => p != player && p.cards.Count > 0).ToList();
                if (validTargets.Count == 0)
                {
                    Debug.LogWarning($"[ForcedPurchase] Tidak ada target yang bisa dipilih, efek gagal diaktifkan.");
                    player.helpCards.Add(card);
                    yield break;
                }

                PlayerProfile targetPlayer = null;
                string colorToPurchase = null;

                if (player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowPlayerSelectionUI(validTargets, selectedPlayer => { targetPlayer = selectedPlayer; }));
                    List<string> availableColors = targetPlayer.cards.Select(c => c.color).Distinct().ToList();
                    yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorToPurchase = selectedColor; }, availableColors));
                }
                else // Logika untuk Bot
                {
                    targetPlayer = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
                    colorToPurchase = targetPlayer.cards[UnityEngine.Random.Range(0, targetPlayer.cards.Count)].color;
                }
                
                // --- Langkah 2: Tentukan Deskripsi & Tampilkan Hasil (untuk Bot) ---
                targetDescription = $"{targetPlayer.playerName} \n membeli paksa sektor 1 {colorToPurchase} milik target";
                if (!player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                }
                
                // --- Langkah 3: Eksekusi Efek ---
                int fullPrice = sellingManager.GetFullCardPrice(colorToPurchase);
                int purchasePrice = Mathf.CeilToInt(fullPrice / 2.0f);

                Debug.Log($"[ForcedPurchase] Harga asli kartu {colorToPurchase} adalah {fullPrice}. Harga beli paksa: {purchasePrice}.");

                if (player.CanAfford(purchasePrice))
                {
                    Card cardToMove = targetPlayer.cards.FirstOrDefault(c => c.color == colorToPurchase);
                    if (cardToMove != null)
                    {
                        player.DeductFinpoint(purchasePrice);
                        targetPlayer.cards.Remove(cardToMove);
                        player.AddCard(cardToMove);
                        Debug.Log($"[ForcedPurchase] {player.playerName} berhasil membeli kartu {colorToPurchase} dari {targetPlayer.playerName} seharga {purchasePrice} Finpoint.");
                        gameManager.UpdatePlayerUI();
                    }
                    else
                    {
                        Debug.LogError($"[ForcedPurchase] Gagal menemukan kartu {colorToPurchase} milik {targetPlayer.playerName}.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ForcedPurchase] {player.playerName} tidak memiliki cukup Finpoint (butuh {purchasePrice}). Efek dibatalkan.");
                }
            }
            break;
    }

    gameManager.UpdatePlayerUI(); // Update UI di akhir untuk memastikan semua perubahan tercermin
    // Panggilan ShowEffectResult yang lama di sini sudah dihapus.
}
    private IEnumerator ShowIPOSelectionUI(Action<string> onColorSelected, List<string> availableColors = null)
    {
        ipoSelectionPanel.SetActive(true);
        bool selectionMade = false;

        // Jika tidak ada warna spesifik, tampilkan semua
        if (availableColors == null)
        {
            // --- PERUBAHAN DI SINI ---
            availableColors = new List<string> { "Konsumer", "Infrastruktur", "Keuangan", "Tambang" };
        }

        // Aktifkan/nonaktifkan tombol berdasarkan warna yang tersedia
        // Pastikan nama GameObject tombol sesuai
        redButton.gameObject.SetActive(availableColors.Contains("Konsumer"));
        blueButton.gameObject.SetActive(availableColors.Contains("Infrastruktur"));
        greenButton.gameObject.SetActive(availableColors.Contains("Keuangan"));
        orangeButton.gameObject.SetActive(availableColors.Contains("Tambang"));

        Action<string> SelectColor = (color) =>
        {
            onColorSelected?.Invoke(color);
            selectionMade = true;
            ipoSelectionPanel.SetActive(false);
        };

        redButton.onClick.RemoveAllListeners();
        blueButton.onClick.RemoveAllListeners();
        greenButton.onClick.RemoveAllListeners();
        orangeButton.onClick.RemoveAllListeners();

        // --- PERUBAHAN DI SINI ---
        redButton.onClick.AddListener(() => SelectColor("Konsumer"));
        blueButton.onClick.AddListener(() => SelectColor("Infrastruktur"));
        greenButton.onClick.AddListener(() => SelectColor("Keuangan"));
        orangeButton.onClick.AddListener(() => SelectColor("Tambang"));

        yield return new WaitUntil(() => selectionMade);
    }

    // Fungsi baru untuk menampilkan UI pemilihan pemain
    public IEnumerator ShowPlayerSelectionUI(List<PlayerProfile> players, Action<PlayerProfile> onPlayerSelected)
    {
        // Pastikan panel lama (jika masih ada) tidak aktif
    

        bool selectionMade = false;

        // Panggil fungsi baru di GameManager untuk mengaktifkan tombol target di UI pemain
        gameManager.StartPlayerTargeting(players, selectedPlayer =>
        {
            // Callback ini akan dijalankan oleh GameManager saat target sudah diklik
            onPlayerSelected?.Invoke(selectedPlayer);
            selectionMade = true;
        });

        // Coroutine ini akan berhenti di sini sampai 'selectionMade' menjadi true
        yield return new WaitUntil(() => selectionMade);
    }
    private IEnumerator ShowEffectResult(PlayerProfile player, HelpCard card, string targetInfo)
    {
        // 1. Isi informasi ke dalam panel
        effectPlayerNameText.text = $"{player.playerName}\nmenggunakan:";
        effectCardImage.sprite = card.cardImage; // Gunakan gambar dari kartu
        effectTargetText.text = targetInfo;     // Tampilkan detail target

        // 2. Tampilkan panel
        effectDisplayPanel.SetActive(true);

        // 3. Tunggu selama 3 detik
        yield return new WaitForSeconds(3f);

        // 4. Sembunyikan kembali panelnya
        effectDisplayPanel.SetActive(false);
    }

    public bool isTesting = true;
    private HelpCard GetRandomHelpCard()
    {
        HelpCardEffect randomEffect;
        if (isTesting)
        {
            randomEffect = HelpCardEffect.AdministrativePenalties; // Atur efek yang ingin dites
        }
        else
        {
            int effectCount = System.Enum.GetNames(typeof(HelpCardEffect)).Length;
            randomEffect = (HelpCardEffect)UnityEngine.Random.Range(0, effectCount);
        }
        Sprite effectSprite = cardArtDictionary.ContainsKey(randomEffect) ? cardArtDictionary[randomEffect] : null;
        switch (randomEffect)
        {

            case HelpCardEffect.AdministrativePenalties:
                return new HelpCard("Bad News", "Menurunkan nilai IPO satu warna secara acak.", randomEffect, effectSprite);
            case HelpCardEffect.NegativeEquity:
                return new HelpCard("Bad News", "Menurunkan nilai IPO satu warna secara acak.", randomEffect, effectSprite);
            case HelpCardEffect.TaxEvasion:
                return new HelpCard("Penghindaran Pajak", "Bayar 2 Finpoint untuk setiap kartu yang kamu miliki.", randomEffect, effectSprite);
            case HelpCardEffect.MarketPrediction:
                return new HelpCard("Prediksi Pasar", "Dapatkan bocoran pergerakan pasar untuk satu warna pilihanmu.", randomEffect, effectSprite);
            case HelpCardEffect.EyeOfTruth:
                return new HelpCard("Prediksi Pasar", "Dapatkan bocoran pergerakan pasar untuk satu warna pilihanmu.", randomEffect, effectSprite);
            case HelpCardEffect.MarketStabilization:
                return new HelpCard("Stabilisasi Pasar", "Pemerintah turun tangan! Semua harga saham kembali ke nilai awal.", randomEffect, effectSprite);
            case HelpCardEffect.CardSwap:
                return new HelpCard("Tukar Tambah", "Tukar 1 kartu yang kamu miliki dengan 1 kartu milik pemain lain.", randomEffect, effectSprite);
            case HelpCardEffect.ForcedPurchase:
                return new HelpCard("Beli Paksa", "Beli 1 kartu milik pemain lain dengan setengah harga.", randomEffect, effectSprite);


            default:
                return new HelpCard("Dana Hibah", "Langsung dapat 10 Finpoint.", randomEffect, effectSprite);
        }
    }
}