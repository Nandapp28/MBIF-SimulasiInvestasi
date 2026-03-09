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
    [Header("Player Selection Settings")] // <-- TAMBAHKAN HEADER INI
    public RectTransform selectionUIContainer; // Container list pemain yang akan dianimasikan      
    public float animationDuration = 0.5f;
    private Vector2 originalPosition;
    private Vector3 originalScale;
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
    private bool isPhaseRunning = false;
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
        // --- PERBAIKAN 1: Cek apakah fase sedang berjalan ---
        if (isPhaseRunning) 
        {
            Debug.LogWarning("[HelpCardPhase] Fase sudah berjalan, mengabaikan pemanggilan ganda.");
            return;
        }
        
        isPhaseRunning = true; // Tandai fase dimulai
        Debug.Log("--- Memulai Fase Kartu Bantuan ---");
        
        this.turnOrder = players.OrderBy(p => p.ticketNumber).ToList();

        // --- PERBAIKAN 2: Cek apakah ada pemain yang punya kartu bantuan ---
        bool anyPlayerHasCards = this.turnOrder.Any(p => p.helpCards != null && p.helpCards.Count > 0);

        if (!anyPlayerHasCards)
        {
            Debug.Log("🚫 Tidak ada pemain yang memiliki Kartu Bantuan. Langsung melompat ke Fase Penjualan.");
            StartCoroutine(SkipToSellingPhase());
        }
        else
        {
            StartCoroutine(ActivationSequence());
        }
    }

   public IEnumerator DistributeHelpCards(List<PlayerProfile> playersToDistribute)
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

        // --- LOGIKA TAMPILAN PEMAIN (YOU) ---
        if (player.playerName.Contains("You"))
        {
            if (helpCardActivationPanel != null)
            {
                // 1. Setup Data UI (Gambar & Teks)
                // Pastikan variabel Text di Inspector sudah di-assign
                if (cardNameText != null) cardNameText.text = "ANDA MENDAPATKAN:\n" + card.cardName;
                if (cardDescriptionText != null) cardDescriptionText.text = card.description;
                if (cardImageUI != null) cardImageUI.sprite = card.cardImage;

                // 2. Sembunyikan Tombol agar hanya jadi "Viewer"
                if (activateButton != null) activateButton.gameObject.SetActive(false);
                if (skipButton != null) skipButton.gameObject.SetActive(false);

                // 3. Pastikan ada CanvasGroup untuk efek Fade
                CanvasGroup cg = helpCardActivationPanel.GetComponent<CanvasGroup>();
                if (cg == null) cg = helpCardActivationPanel.AddComponent<CanvasGroup>();

                // 4. Reset kondisi awal & Aktifkan Panel
                cg.alpha = 0f; 
                helpCardActivationPanel.SetActive(true);

                // 5. Animasi FADE IN (0.5 detik)
                yield return StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, 0.5f));

                // 6. Tahan tampilan (2 detik)
                yield return new WaitForSeconds(2f);
                if (GameSettings.IsTutorial && TutorialManager.Instance != null && TutorialManager.Instance.CurrentSemester == 1)
            {
                TutorialUIController.Instance.ShowPackage("Resolution1");
            }

                // 7. Animasi FADE OUT (0.5 detik)
                yield return StartCoroutine(FadeCanvasGroup(cg, 1f, 0f, 0.5f));

                // 8. Matikan panel dan KEMBALIKAN tombol (PENTING!)
                helpCardActivationPanel.SetActive(false);
                
                // Kembalikan tombol agar bisa dipakai di fase aktivasi nanti
                if (activateButton != null) activateButton.gameObject.SetActive(true);
                if (skipButton != null) skipButton.gameObject.SetActive(true);
                
                // Reset alpha ke 1 jaga-jaga jika panel dibuka tanpa animasi nanti
                cg.alpha = 1f; 
            }
        }
    }
}

// --- FUNGSI TAMBAHAN UNTUK ANIMASI HALUS ---
private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
{
    float elapsedTime = 0f;
    cg.alpha = startAlpha;

    while (elapsedTime < duration)
    {
        elapsedTime += Time.deltaTime;
        // Mengubah alpha secara bertahap
        cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
        yield return null;
    }

    cg.alpha = endAlpha;
}
    private IEnumerator SkipToSellingPhase()
    {
        yield return new WaitForSeconds(1f); 
        yield return StartCoroutine(EndHelpCardPhase());
    }   

    private IEnumerator ActivationSequence()
    {
        yield return new WaitForSeconds(1f);

        foreach (var player in turnOrder)
        {
            if (player.helpCards == null || player.helpCards.Count == 0)
            {
                // Debug.Log($"{player.playerName} tidak memiliki Kartu Bantuan untuk diaktifkan.");
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

        yield return StartCoroutine(EndHelpCardPhase());

    }
    private IEnumerator EndHelpCardPhase()
    {
        Debug.Log("--- Fase Kartu Bantuan Selesai ---");
        
        if (UITransitionAnimator.Instance != null)
        {
            UITransitionAnimator.Instance.StartTransition("Selling Phase");
        }
        
        yield return new WaitForSeconds(4f);
        
        isPhaseRunning = false; // Reset flag agar bisa dijalankan lagi di semester berikutnya
        
        if (sellingManager != null)
        {
            sellingManager.StartSellingPhase(turnOrder, gameManager.resetCount, gameManager.maxResetCount, gameManager.resetSemesterButton);
        }
        else
        {
            Debug.LogError("SellingPhaseManager reference is missing!");
        }
    }


    private IEnumerator HandlePlayerChoice(PlayerProfile player, HelpCard card)
    {
        CanvasGroup cg = helpCardActivationPanel.GetComponent<CanvasGroup>();
    if (cg != null) cg.alpha = 1f;

        helpCardActivationPanel.SetActive(true);
        cardImageUI.sprite = card.cardImage;
        if (cardNameText != null) cardNameText.text = card.cardName;
    if (cardDescriptionText != null) cardDescriptionText.text = card.description;

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
        case HelpCardEffect.PositiveEquity:
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
                
                yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                

                // --- Langkah 3: Eksekusi Efek ---
                int ipoChange = (card.effectType == HelpCardEffect.AdministrativePenalties) ? -2 : 2;
                yield return StartCoroutine(sellingManager.ModifyIPOIndexWithCamera(colorToSabotage, ipoChange));
            }
            break;

        case HelpCardEffect.TaxEvasion:
            {
                // --- Langkah 1 & 2: Tentukan Deskripsi & Tampilkan Hasil ---
                Debug.Log($"{player.playerName} mengaktifkan Penghindaran Pajak. Semua pemain lain harus membayar pajak!");
                targetDescription = "Target: \n Semua Pemain Lain";
                
                yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                

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
                
                yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                

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
                
                yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                
                
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
                targetDescription = $"Menukar sektor {colorFromPlayer} dengan {colorFromTarget} milik \n{targetPlayer.playerName}";
                
                yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                

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
                targetDescription = $"membeli paksa sektor 1 {colorToPurchase} milik\n {targetPlayer.playerName}";
                
                yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
                
                
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
        // 1. Simpan kondisi awal UI Container
        if (selectionUIContainer != null)
        {
            originalPosition = selectionUIContainer.anchoredPosition;
            originalScale = selectionUIContainer.localScale;
            Vector2 targetCustomPosition = new Vector2(-40f, 100f);
            
            // Animasi Masuk: Bergerak ke Tengah (0,0) dan Skala 1.5x
            StartCoroutine(AnimateUI(selectionUIContainer, targetCustomPosition, Vector3.one * 0.8f));
        }

        bool selectionMade = false;

        // 3. Panggil fungsi GameManager untuk setup tombol target di list pemain
        gameManager.StartPlayerTargeting(players, selectedPlayer =>
        {
            onPlayerSelected?.Invoke(selectedPlayer);
            selectionMade = true;
        });

        // Tunggu sampai pemain memilih target ATAU menekan skip
        yield return new WaitUntil(() => selectionMade);


        if (selectionUIContainer != null)
        {
            // Animasi Keluar: Kembali ke posisi dan skala awal
            StartCoroutine(AnimateUI(selectionUIContainer, originalPosition, originalScale));
        }
    }

    // --- TAMBAHKAN HELPER COROUTINE INI ---
    private IEnumerator AnimateUI(RectTransform target, Vector2 targetPos, Vector3 targetScale)
    {
        float elapsed = 0f;
        Vector2 startPos = target.anchoredPosition;
        Vector3 startScale = target.localScale;

        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;
            // Menggunakan SmoothStep untuk gerakan yang lebih luwes
            t = t * t * (3f - 2f * t); 

            target.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            target.localScale = Vector3.Lerp(startScale, targetScale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.anchoredPosition = targetPos;
        target.localScale = targetScale;
    }
    private IEnumerator ShowEffectResult(PlayerProfile player, HelpCard card, string targetInfo)
    {
        // 1. Isi informasi ke dalam panel
        effectPlayerNameText.text = $"{player.playerName}\nmenggunakan:";
        effectCardImage.sprite = card.cardImage; // Gunakan gambar dari kartu
        effectTargetText.text = targetInfo;     // Tampilkan detail target
        if (LogManager.Instance != null)
        {
            // Gabungkan nama pemain, nama kartu, dan target menjadi satu kalimat log.
            // Ganti '\n' dengan spasi agar menjadi satu baris.
            string logMessage = $"{player.playerName} menggunakan '{card.cardName}', {targetInfo.Replace("\n", " ")}";
            LogManager.Instance.AddLog(logMessage);
        }

        // 2. Tampilkan panel
        if (!player.playerName.Contains("You"))
        {
            effectDisplayPanel.SetActive(true);
        }

        // 3. Tunggu selama 3 detik
        yield return new WaitForSeconds(3f);

        // 4. Sembunyikan kembali panelnya
        effectDisplayPanel.SetActive(false);
    }

    public bool isTesting = false;
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
                return new HelpCard("Administrative Penalties", "Menurunkan nilai IPO satu warna secara acak.", randomEffect, effectSprite);
            case HelpCardEffect.PositiveEquity:
                return new HelpCard("Positive Equity", "Menurunkan nilai IPO satu warna secara acak.", randomEffect, effectSprite);
            case HelpCardEffect.TaxEvasion:
                return new HelpCard("Tax Evasion", "Bayar 2 Finpoint untuk setiap kartu yang kamu miliki.", randomEffect, effectSprite);
            case HelpCardEffect.MarketPrediction:
                return new HelpCard("Market Prediction", "Dapatkan bocoran pergerakan pasar untuk satu warna pilihanmu.", randomEffect, effectSprite);
            case HelpCardEffect.EyeOfTruth:
                return new HelpCard("Eye of Truth", "Dapatkan bocoran pergerakan pasar untuk satu warna pilihanmu.", randomEffect, effectSprite);
            case HelpCardEffect.MarketStabilization:
                return new HelpCard("Market Stabilization", "Pemerintah turun tangan! Semua harga saham kembali ke nilai awal.", randomEffect, effectSprite);
            case HelpCardEffect.CardSwap:
                return new HelpCard("Card Swap", "Tukar 1 kartu yang kamu miliki dengan 1 kartu milik pemain lain.", randomEffect, effectSprite);
            case HelpCardEffect.ForcedPurchase:
                return new HelpCard("Forced Purchase", "Beli 1 kartu milik pemain lain dengan setengah harga.", randomEffect, effectSprite);


            default:
                return new HelpCard("Dana Hibah", "Langsung dapat 10 Finpoint.", randomEffect, effectSprite);
        }
    }
}