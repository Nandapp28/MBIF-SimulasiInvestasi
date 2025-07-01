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
    public RumorPhaseManager rumorPhaseManager;// Diperlukan untuk efek IPO


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
    [Header("Player Selection UI")]
    public GameObject playerSelectionPanel;
    public Transform playerButtonContainer;
    public GameObject playerButtonPrefab;

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

        // Kita tidak perlu lagi menebak tipe data di sini
        string colorToSabotage = null;
        string targetDescription = "";

        switch (card.effectType)
        {
            case HelpCardEffect.AdiministrativePenalties:
                // Tambahkan kurung kurawal buka di sini untuk menciptakan scope baru
                {
                    if (player.playerName.Contains("You"))
                    {
                        yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorToSabotage = selectedColor; }));
                        Debug.Log($"{player.playerName} memilih untuk menyabotase IPO {colorToSabotage}.");
                        targetDescription = $"Target: \n{colorToSabotage}";
                    }
                    else // Logika untuk Bot
                    {
                        Dictionary<string, int> colorCounts = player.GetCardColorCounts();
                        int minCount = colorCounts.Values.Min();
                        List<string> colorsWithMinCount = colorCounts
                            .Where(pair => pair.Value == minCount)
                            .Select(pair => pair.Key)
                            .ToList();
                        int randomIndex = UnityEngine.Random.Range(0, colorsWithMinCount.Count);
                        colorToSabotage = colorsWithMinCount[randomIndex];
                        Debug.Log($"{player.playerName} memilih untuk menyabotase IPO {colorToSabotage}.");
                        targetDescription = $"Target: \n{colorToSabotage}";
                    }

                    // 'var' aman digunakan di dalam scope baru ini
                    var targetIPO = sellingManager.ipoDataList.FirstOrDefault(i => i.color == colorToSabotage);
                    if (targetIPO != null)
                    {
                        targetIPO.ipoIndex -= 2;
                        sellingManager.UpdateIPOVisuals();
                    }
                } // Tambahkan kurung kurawal tutup di sini
                break;

            case HelpCardEffect.NegativeEquity:
                // Tambahkan kurung kurawal buka di sini juga
                {
                    if (player.playerName.Contains("You"))
                    {
                        yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorToSabotage = selectedColor; }));
                        Debug.Log($"{player.playerName} memilih untuk menyabotase IPO {colorToSabotage}.");
                        targetDescription = $"Target: \n{colorToSabotage}";
                    }
                    else // Logika untuk Bot
                    {
                        Dictionary<string, int> colorCounts = player.GetCardColorCounts();
                        int minCount = colorCounts.Values.Min();
                        List<string> colorsWithMinCount = colorCounts
                            .Where(pair => pair.Value == minCount)
                            .Select(pair => pair.Key)
                            .ToList();
                        int randomIndex = UnityEngine.Random.Range(0, colorsWithMinCount.Count);
                        colorToSabotage = colorsWithMinCount[randomIndex];
                        Debug.Log($"{player.playerName} memilih untuk menyabotase IPO {colorToSabotage}.");
                        targetDescription = $"Target: \n{colorToSabotage}";
                    }


                    // 'var' juga aman digunakan di sini karena scope-nya terpisah dari case sebelumnya
                    var targetIPO = sellingManager.ipoDataList.FirstOrDefault(i => i.color == colorToSabotage);
                    if (targetIPO != null)
                    {
                        targetIPO.ipoIndex -= 3;
                        sellingManager.UpdateIPOVisuals();
                    }
                } // Tambahkan kurung kurawal tutup di sini
                break;


            case HelpCardEffect.TaxEvasion:
                Debug.Log($"{player.playerName} mengaktifkan Penghindaran Pajak. Semua pemain harus membayar pajak berdasarkan jumlah kartu!");
                foreach (var p in turnOrder)
                {
                    int cardCount = p.cards.Count;
                    int cost = cardCount * 2;
                    p.DeductFinpoint(cost);
                    Debug.Log($"{p.playerName} membayar {cost} Finpoint untuk {cardCount} kartu. Sisa: {p.finpoint}");
                    targetDescription = $"Target: Semua Pemain(kecuali pemakai)";
                }
                break;
            case HelpCardEffect.MarketPrediction:
                {
                    string chosenColor = null;

                    // ... (Bagian 1: Logika pemilihan warna tidak berubah) ...
                    if (player.playerName.Contains("You"))
                    {
                        yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { chosenColor = selectedColor; }));
                    }
                    else // Logika untuk Bot
                    {
                        int randomIndex = UnityEngine.Random.Range(0, sellingManager.ipoDataList.Count);
                        chosenColor = sellingManager.ipoDataList[randomIndex].color;
                    }
                    Debug.Log($"{player.playerName} mencoba memprediksi pasar untuk warna {chosenColor}.");
                    targetDescription = $"Target: \n{chosenColor}";

                    // ... (Bagian 2 & 3: Logika mencari rumor dan menyimpan prediksi tidak berubah) ...
                    RumorPhaseManager.RumorEffect futureRumor = rumorPhaseManager.shuffledRumorDeck.FirstOrDefault(r => r.color == chosenColor && r.effectType == RumorPhaseManager.RumorEffect.EffectType.ModifyIPO);

                    // ...
                    if (futureRumor != null)
                    {
                        // --- BAGIAN LOG YANG DIPERBAIKI ---
                        if (futureRumor.value > 0)
                        {
                            player.marketPredictions[chosenColor] = MarketPredictionType.Rise;
                            // Pesan ini sekarang jelas hanya untuk pemain yang bersangkutan
                            Debug.Log($"[Prediksi UNTUK {player.playerName}] Pasar {chosenColor} diprediksi akan NAIK.");
                        }
                        else if (futureRumor.value < 0)
                        {
                            player.marketPredictions[chosenColor] = MarketPredictionType.Fall;
                            // Pesan ini sekarang jelas hanya untuk pemain yang bersangkutan
                            Debug.Log($"[Prediksi UNTUK {player.playerName}] Pasar {chosenColor} diprediksi akan TURUN.");
                        }
                        // --- AKHIR BAGIAN YANG DIPERBAIKI ---

                        // Tampilkan kartu di tengah layar menggunakan metode baru yang sudah kita buat
                        Debug.Log($"Menampilkan bocoran kartu rumor untuk {player.playerName}: {futureRumor.cardName}");

                        // Panggil coroutine baru dan tunggu hingga animasinya selesai
                        yield return rumorPhaseManager.ShowPredictionCardAtCenter(futureRumor);

                        // Beri jeda tambahan agar pemain bisa mencerna informasi
                        yield return new WaitForSeconds(2f);

                        // Sembunyikan kembali kartu tersebut
                        rumorPhaseManager.HideAllCardObjects();
                    }
                    // ...
                    else
                    {
                        Debug.Log($"Tidak ada prediksi pergerakan IPO signifikan untuk {chosenColor}.");
                    }
                    break;
                }
            case HelpCardEffect.EyeOfTruth:
                {
                    string chosenColor = null;


                    // ... (Bagian 1: Logika pemilihan warna tidak berubah) ...
                    if (player.playerName.Contains("You"))
                    {
                        yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { chosenColor = selectedColor; }));
                    }
                    else // Logika untuk Bot
                    {
                        int randomIndex = UnityEngine.Random.Range(0, sellingManager.ipoDataList.Count);
                        chosenColor = sellingManager.ipoDataList[randomIndex].color;
                    }
                    Debug.Log($"{player.playerName} mencoba memprediksi pasar untuk warna {chosenColor}.");
                    targetDescription = $"Target: \n{chosenColor}";

                    // ... (Bagian 2 & 3: Logika mencari rumor dan menyimpan prediksi tidak berubah) ...
                    RumorPhaseManager.RumorEffect futureRumor = rumorPhaseManager.shuffledRumorDeck.FirstOrDefault(r => r.color == chosenColor && r.effectType == RumorPhaseManager.RumorEffect.EffectType.ModifyIPO);

                    // ...
                    if (futureRumor != null)
                    {
                        // --- BAGIAN LOG YANG DIPERBAIKI ---
                        if (futureRumor.value > 0)
                        {
                            player.marketPredictions[chosenColor] = MarketPredictionType.Rise;
                            // Pesan ini sekarang jelas hanya untuk pemain yang bersangkutan
                            Debug.Log($"[Prediksi UNTUK {player.playerName}] Pasar {chosenColor} diprediksi akan NAIK.");
                        }
                        else if (futureRumor.value < 0)
                        {
                            player.marketPredictions[chosenColor] = MarketPredictionType.Fall;
                            // Pesan ini sekarang jelas hanya untuk pemain yang bersangkutan
                            Debug.Log($"[Prediksi UNTUK {player.playerName}] Pasar {chosenColor} diprediksi akan TURUN.");
                        }
                        // --- AKHIR BAGIAN YANG DIPERBAIKI ---

                        // Tampilkan kartu di tengah layar menggunakan metode baru yang sudah kita buat
                        Debug.Log($"Menampilkan bocoran kartu rumor untuk {player.playerName}: {futureRumor.cardName}");

                        // Panggil coroutine baru dan tunggu hingga animasinya selesai
                        yield return rumorPhaseManager.ShowPredictionCardAtCenter(futureRumor);

                        // Beri jeda tambahan agar pemain bisa mencerna informasi
                        yield return new WaitForSeconds(2f);

                        // Sembunyikan kembali kartu tersebut
                        rumorPhaseManager.HideAllCardObjects();
                    }
                    // ...
                    else
                    {
                        Debug.Log($"Tidak ada prediksi pergerakan IPO signifikan untuk {chosenColor}.");
                    }
                    break;

                }
            case HelpCardEffect.MarketStabilization:
                {
                    Debug.Log($"{player.playerName} menggunakan kartu 'Stabilisasi Pasar'. Mereset semua nilai IPO!");
                    targetDescription = "Target: Semua Sektor";

                    // Panggil fungsi reset yang ada di SellingPhaseManager
                    rumorPhaseManager.ResetAllIPOIndexes();
                    sellingManager.UpdateIPOVisuals();
                    break;
                }
            case HelpCardEffect.CardSwap:
                {
                    // Cek kondisi aktivasi
                    if (player.cards.Count == 0)
                    {
                        Debug.LogWarning($"[CardSwap] {player.playerName} tidak punya kartu, efek gagal.");
                        player.helpCards.Add(card);
                        yield break; // Keluar dari coroutine
                    }

                    List<PlayerProfile> validTargets = turnOrder.Where(p => p != player && p.cards.Count > 0).ToList();
                    if (validTargets.Count == 0)
                    {
                        Debug.LogWarning($"[CardSwap] Tidak ada target yang valid, efek gagal.");
                        player.helpCards.Add(card);
                        yield break;
                    }

                    string colorFromPlayer = null;
                    PlayerProfile targetPlayer = null;
                    string colorFromTarget = null;

                    if (player.playerName.Contains("You"))
                    {
                        // 1. Player memilih warna dari kartu miliknya
                        yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorFromPlayer = selectedColor; }, player.cards.Select(c => c.color).Distinct().ToList()));

                        // 2. Player memilih pemain target
                        yield return StartCoroutine(ShowPlayerSelectionUI(validTargets, selectedPlayer => { targetPlayer = selectedPlayer; }));

                        // 3. Player memilih warna dari kartu target
                        yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorFromTarget = selectedColor; }, targetPlayer.cards.Select(c => c.color).Distinct().ToList()));
                    }
                    else // Logika untuk Bot
                    {
                        // 1. Bot memilih warna secara acak dari kartunya
                        colorFromPlayer = player.cards[UnityEngine.Random.Range(0, player.cards.Count)].color;

                        // 2. Bot memilih target secara acak
                        targetPlayer = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];

                        // 3. Bot memilih warna secara acak dari kartu target
                        colorFromTarget = targetPlayer.cards[UnityEngine.Random.Range(0, targetPlayer.cards.Count)].color;
                    }

                    // Lakukan pertukaran kartu
                    Card cardFromPlayer = player.cards.FirstOrDefault(c => c.color == colorFromPlayer);
                    Card cardFromTarget = targetPlayer.cards.FirstOrDefault(c => c.color == colorFromTarget);

                    if (cardFromPlayer != null && cardFromTarget != null)
                    {
                        player.cards.Remove(cardFromPlayer);
                        targetPlayer.cards.Remove(cardFromTarget);

                        player.AddCard(cardFromTarget);
                        targetPlayer.AddCard(cardFromPlayer);

                        Debug.Log($"[CardSwap] {player.playerName} menukar kartu {colorFromPlayer} miliknya dengan kartu {colorFromTarget} milik {targetPlayer.playerName}.");
                        targetDescription = $"Menukar {colorFromPlayer} dengan \n {colorFromTarget} milik {targetPlayer.playerName}";
                        gameManager.UpdatePlayerUI();
                    }
                    else
                    {
                        Debug.LogError("[CardSwap] Gagal menemukan kartu untuk ditukar.");
                    }
                    break;
                }
            case HelpCardEffect.ForcedPurchase:
                {
                    // Cek kondisi aktivasi: Apakah ada target yang valid?
                    List<PlayerProfile> validTargets = turnOrder.Where(p => p != player && p.cards.Count > 0).ToList();
                    if (validTargets.Count == 0)
                    {
                        Debug.LogWarning($"[ForcedPurchase] Tidak ada target yang bisa dipilih, efek gagal diaktifkan.");
                        player.helpCards.Add(card);
                        yield break; // Keluar dari coroutine
                    }

                    PlayerProfile targetPlayer = null;
                    string colorToPurchase = null;

                    if (player.playerName.Contains("You"))
                    {
                        // 1. Pemain memilih target
                        yield return StartCoroutine(ShowPlayerSelectionUI(validTargets, selectedPlayer => { targetPlayer = selectedPlayer; }));

                        // 2. Pemain memilih warna dari kartu target
                        List<string> availableColors = targetPlayer.cards.Select(c => c.color).Distinct().ToList();
                        yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorToPurchase = selectedColor; }, availableColors));
                    }
                    else // Logika untuk Bot
                    {
                        // 1. Bot memilih target secara acak
                        targetPlayer = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];

                        // 2. Bot memilih warna secara acak dari kartu target
                        colorToPurchase = targetPlayer.cards[UnityEngine.Random.Range(0, targetPlayer.cards.Count)].color;
                    }

                    // 3. Hitung harga dan lakukan transaksi
                    int fullPrice = sellingManager.GetCurrentColorValue(colorToPurchase);
                    int purchasePrice = Mathf.CeilToInt(fullPrice / 2.0f); // Setengah harga, dibulatkan ke atas

                    Debug.Log($"[ForcedPurchase] Harga asli kartu {colorToPurchase} adalah {fullPrice}. Harga beli paksa: {purchasePrice}.");
                    targetDescription = $"Membeli paksa \n {colorToPurchase} milik {targetPlayer.playerName}";

                    if (player.CanAfford(purchasePrice))
                    {
                        Card cardToMove = targetPlayer.cards.FirstOrDefault(c => c.color == colorToPurchase);
                        if (cardToMove != null)
                        {
                            // Lakukan transaksi
                            player.DeductFinpoint(purchasePrice);
                            targetPlayer.cards.Remove(cardToMove);
                            player.AddCard(cardToMove);

                            Debug.Log($"[ForcedPurchase] {player.playerName} berhasil membeli kartu {colorToPurchase} dari {targetPlayer.playerName} seharga {purchasePrice} Finpoint.");
                            gameManager.UpdatePlayerUI();
                        }
                        else
                        {
                            Debug.LogError($"[ForcedPurchase] Gagal menemukan kartu {colorToPurchase} milik {targetPlayer.playerName} meskipun seharusnya ada.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[ForcedPurchase] {player.playerName} tidak memiliki cukup Finpoint untuk membeli kartu (butuh {purchasePrice}). Efek dibatalkan.");
                    }

                    break;
                }
        }

        gameManager.UpdatePlayerUI();
         yield return StartCoroutine(ShowEffectResult(player, card, targetDescription));
    }
    private IEnumerator ShowIPOSelectionUI(Action<string> onColorSelected, List<string> availableColors = null)
    {
        ipoSelectionPanel.SetActive(true);
        bool selectionMade = false;

        // Jika tidak ada warna spesifik, tampilkan semua
        if (availableColors == null)
        {
            availableColors = new List<string> { "Red", "Blue", "Green", "Orange" };
        }

        // Aktifkan/nonaktifkan tombol berdasarkan warna yang tersedia
        redButton.gameObject.SetActive(availableColors.Contains("Red"));
        blueButton.gameObject.SetActive(availableColors.Contains("Blue"));
        greenButton.gameObject.SetActive(availableColors.Contains("Green"));
        orangeButton.gameObject.SetActive(availableColors.Contains("Orange"));

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

        redButton.onClick.AddListener(() => SelectColor("Red"));
        blueButton.onClick.AddListener(() => SelectColor("Blue"));
        greenButton.onClick.AddListener(() => SelectColor("Green"));
        orangeButton.onClick.AddListener(() => SelectColor("Orange"));

        yield return new WaitUntil(() => selectionMade);
    }

    // Fungsi baru untuk menampilkan UI pemilihan pemain
    private IEnumerator ShowPlayerSelectionUI(List<PlayerProfile> players, Action<PlayerProfile> onPlayerSelected)
    {
        playerSelectionPanel.SetActive(true);
        foreach (Transform child in playerButtonContainer)
        {
            Destroy(child.gameObject);
        }

        bool selectionMade = false;

        foreach (var player in players)
        {
            GameObject btnObj = Instantiate(playerButtonPrefab, playerButtonContainer);
            btnObj.GetComponentInChildren<UnityEngine.UI.Text>().text = player.playerName;
            btnObj.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                onPlayerSelected?.Invoke(player);
                selectionMade = true;
                playerSelectionPanel.SetActive(false);
            });
        }

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
            randomEffect = HelpCardEffect.AdiministrativePenalties; // Atur efek yang ingin dites
        }
        else
        {
            int effectCount = System.Enum.GetNames(typeof(HelpCardEffect)).Length;
            randomEffect = (HelpCardEffect)UnityEngine.Random.Range(0, effectCount);
        }
        Sprite effectSprite = cardArtDictionary.ContainsKey(randomEffect) ? cardArtDictionary[randomEffect] : null;
        switch (randomEffect)
        {

            case HelpCardEffect.AdiministrativePenalties:
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