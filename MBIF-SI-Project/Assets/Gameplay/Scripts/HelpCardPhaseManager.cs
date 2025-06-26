// File: HelpCardPhaseManager.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class HelpCardPhaseManager : MonoBehaviour
{
    [Header("Game References")]
    public GameManager gameManager;
    public SellingPhaseManager sellingManager; // Diperlukan untuk efek IPO


    [Header("UI Elements")]
    public GameObject helpCardActivationPanel; // Panel yang menunjukkan info kartu & tombol
    public UnityEngine.UI.Text cardNameText;
    public UnityEngine.UI.Text cardDescriptionText;
    public UnityEngine.UI.Button activateButton;
    public UnityEngine.UI.Button skipButton;
    [Header("IPO Selection UI")]
    public GameObject ipoSelectionPanel;
    public UnityEngine.UI.Button redButton;
    public UnityEngine.UI.Button blueButton;
    public UnityEngine.UI.Button greenButton;
    public UnityEngine.UI.Button orangeButton;

    private List<PlayerProfile> turnOrder;

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
        cardNameText.text = card.cardName;
        cardDescriptionText.text = card.description;

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

    switch (card.effectType)
    {
        case HelpCardEffect.ExtraFinpoints:
            player.finpoint += 10;
            Debug.Log($"{player.playerName} mendapatkan 10 Finpoint. Total sekarang: {player.finpoint}");
            break;

        case HelpCardEffect.BoostRandomIPO:
            // Kurung kurawal di sini menciptakan scope lokal, jadi 'ipoToBoost' aman.
            {
                var ipoToBoost = sellingManager.ipoDataList[UnityEngine.Random.Range(0, sellingManager.ipoDataList.Count)];
                ipoToBoost.ipoIndex++;
                Debug.Log($"IPO {ipoToBoost.color} meningkat!");
                sellingManager.UpdateIPOVisuals();
            }
            break;

        case HelpCardEffect.AdiministrativePenalties:
            // Tambahkan kurung kurawal buka di sini untuk menciptakan scope baru
            {
                if (player.playerName.Contains("You"))
                {
                    yield return StartCoroutine(ShowIPOSelectionUI(selectedColor => { colorToSabotage = selectedColor; }));
                    Debug.Log($"{player.playerName} memilih untuk menyabotase IPO {colorToSabotage}.");
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

        case HelpCardEffect.FreeCardPurchase:
            Debug.Log($"{player.playerName} akan mendapatkan kartu gratis di semester berikutnya.");
            break;

        case HelpCardEffect.TaxEvasion:
            Debug.Log($"{player.playerName} mengaktifkan Penghindaran Pajak. Semua pemain harus membayar pajak berdasarkan jumlah kartu!");
            foreach (var p in turnOrder)
            {
                int cardCount = p.cards.Count;
                int cost = cardCount * 2;
                p.DeductFinpoint(cost);
                Debug.Log($"{p.playerName} membayar {cost} Finpoint untuk {cardCount} kartu. Sisa: {p.finpoint}");
            }
            break;
    }

    gameManager.UpdatePlayerUI();
}
    private IEnumerator ShowIPOSelectionUI(Action<string> onColorSelected)
    {
        ipoSelectionPanel.SetActive(true);
        bool selectionMade = false;

        Action<string> SelectColor = (color) =>
        {
            onColorSelected?.Invoke(color); // Panggil callback dengan warna yang dipilih
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

        // Tunggu hingga pemain membuat pilihan
        yield return new WaitUntil(() => selectionMade);
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
        switch (randomEffect)
        {
            case HelpCardEffect.ExtraFinpoints:
                return new HelpCard("Dana Hibah", "Langsung dapat 10 Finpoint.", randomEffect);
            case HelpCardEffect.BoostRandomIPO:
                return new HelpCard("Good News", "Meningkatkan nilai IPO satu warna secara acak.", randomEffect);
            case HelpCardEffect.AdiministrativePenalties:
                return new HelpCard("Bad News", "Menurunkan nilai IPO satu warna secara acak.", randomEffect);
            case HelpCardEffect.NegativeEquity:
                return new HelpCard("Bad News", "Menurunkan nilai IPO satu warna secara acak.", randomEffect);
            case HelpCardEffect.TaxEvasion:
                return new HelpCard("Penghindaran Pajak", "Bayar 2 Finpoint untuk setiap kartu yang kamu miliki.", randomEffect);

            default:
                return new HelpCard("Dana Hibah", "Langsung dapat 10 Finpoint.", HelpCardEffect.ExtraFinpoints);
        }
    }
}