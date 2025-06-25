// File: HelpCardPhaseManager.cs

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

            for (int i = 0; i < player.helpCards.Count; i++)
            {
                HelpCard currentCard = player.helpCards[i];

                if (player.playerName.Contains("You"))
                {
                    yield return HandlePlayerChoice(player, currentCard);

                }
                else
                {
                    yield return HandleBotChoice(player, currentCard);
                }

                yield return new WaitForSeconds(1f);
            }

            // Kosongkan kartu setelah fase

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

        activateButton.onClick.RemoveAllListeners();
        activateButton.onClick.AddListener(() =>
        {
            ApplyEffect(player, card);
            player.helpCards.Clear();
            choiceMade = true;
            helpCardActivationPanel.SetActive(false);
        });

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(() =>
        {
            Debug.Log($"{player.playerName} memilih untuk tidak mengaktifkan kartu '{card.cardName}'.");
            choiceMade = true;
            helpCardActivationPanel.SetActive(false);
        });

        yield return new WaitUntil(() => choiceMade);
    }


    private IEnumerator HandleBotChoice(PlayerProfile bot, HelpCard card)
    {
        yield return new WaitForSeconds(1.5f);

        bool activate = Random.value < 0.6f;

        if (activate)
        {
            ApplyEffect(bot, card);
            bot.helpCards.Remove(card);
        }
        else
        {
            Debug.Log($"{bot.playerName} (Bot) memilih untuk tidak mengaktifkan kartu '{card.cardName}'.");
        }
    }

    private void ApplyEffect(PlayerProfile player, HelpCard card)
    {
        Debug.Log($"{player.playerName} mengaktifkan '{card.cardName}'!");

        switch (card.effectType)
        {
            case HelpCardEffect.ExtraFinpoints:
                player.finpoint += 10;
                Debug.Log($"{player.playerName} mendapatkan 10 Finpoint. Total sekarang: {player.finpoint}");
                break;

            case HelpCardEffect.BoostRandomIPO:
                var ipoToBoost = sellingManager.ipoDataList[Random.Range(0, sellingManager.ipoDataList.Count)];
                ipoToBoost.ipoIndex++;
                Debug.Log($"IPO {ipoToBoost.color} meningkat!");
                sellingManager.UpdateIPOVisuals();
                break;

            case HelpCardEffect.SabotageRandomIPO:
                if (player.playerName.Contains("You"))
                {
                    // Tampilkan UI pemilihan warna (dibahas di bawah)
                    StartCoroutine(ShowIPOSelectionUI(player));
                }
                else
                {
                    Dictionary<string, int> colorCounts = player.GetCardColorCounts();
                    string targetColor = colorCounts.OrderBy(kv => kv.Value).First().Key;

                    var targetIPO = sellingManager.ipoDataList.FirstOrDefault(i => i.color == targetColor);
                    if (targetIPO != null)
                    {
                        targetIPO.ipoIndex-=2;
                        Debug.Log($"{player.playerName} (Bot) menurunkan IPO warna {targetColor} karena hanya punya sedikit kartu itu.");
                        sellingManager.UpdateIPOVisuals();
                    }
                }
                break;

            case HelpCardEffect.FreeCardPurchase:
                Debug.Log($"{player.playerName} akan mendapatkan kartu gratis di semester berikutnya.");
                // Tambahkan logika sesuai implementasi kamu nanti
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
    private IEnumerator ShowIPOSelectionUI(PlayerProfile player)
    {
        bool selectionMade = false;

        ipoSelectionPanel.SetActive(true);

        System.Action<string> SelectColor = (color) =>
        {
            var ipo = sellingManager.ipoDataList.FirstOrDefault(i => i.color == color);
            if (ipo != null)
            {
                ipo.ipoIndex-=2;
                Debug.Log($"{player.playerName} menurunkan IPO {color} secara manual.");
                sellingManager.UpdateIPOVisuals();
            }

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


    public bool isTesting = true;
    private HelpCard GetRandomHelpCard()
    {
        HelpCardEffect randomEffect;
        if (isTesting)
        {
            randomEffect = HelpCardEffect.SabotageRandomIPO; // Atur efek yang ingin dites
        }
        else
        {
            int effectCount = System.Enum.GetNames(typeof(HelpCardEffect)).Length;
            randomEffect = (HelpCardEffect)Random.Range(0, effectCount);
        }
        switch (randomEffect)
        {
            case HelpCardEffect.ExtraFinpoints:
                return new HelpCard("Dana Hibah", "Langsung dapat 10 Finpoint.", randomEffect);
            case HelpCardEffect.BoostRandomIPO:
                return new HelpCard("Good News", "Meningkatkan nilai IPO satu warna secara acak.", randomEffect);
            case HelpCardEffect.SabotageRandomIPO:
                return new HelpCard("Bad News", "Menurunkan nilai IPO satu warna secara acak.", randomEffect);
            case HelpCardEffect.TaxEvasion:
                return new HelpCard("Penghindaran Pajak", "Bayar 2 Finpoint untuk setiap kartu yang kamu miliki.", randomEffect);

            default:
                return new HelpCard("Dana Hibah", "Langsung dapat 10 Finpoint.", HelpCardEffect.ExtraFinpoints);
        }
    }
}