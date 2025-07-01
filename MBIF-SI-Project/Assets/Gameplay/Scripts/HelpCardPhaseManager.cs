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

    private List<PlayerProfile> turnOrder;

    // Fungsi utama yang dipanggil untuk memulai fase ini
    public void StartHelpCardPhase(List<PlayerProfile> players, int resetCount)
    {
        Debug.Log("--- Memulai Fase Kartu Bantuan ---");
        this.turnOrder = players.OrderBy(p => p.ticketNumber).ToList();

        // Bagikan kartu bantuan hanya di awal permainan (ketika resetCount == 0)
        if (resetCount == 0)
        {
            DistributeHelpCards();
        }

        // Mulai urutan aktivasi kartu
        StartCoroutine(ActivationSequence());
    }

    private void DistributeHelpCards()
    {
        Debug.Log("Membagikan Kartu Bantuan kepada semua pemain...");
        foreach (var player in turnOrder)
        {
            player.helpCard = GetRandomHelpCard();
            Debug.Log($"{player.playerName} mendapatkan kartu: '{player.helpCard.cardName}'");
        }
    }

    private IEnumerator ActivationSequence()
    {
        yield return new WaitForSeconds(1f);

        foreach (var player in turnOrder)
        {
            if (player.helpCard == null)
            {
                Debug.Log($"{player.playerName} tidak memiliki Kartu Bantuan untuk diaktifkan.");
                continue;
            }

            Debug.Log($"Giliran {player.playerName} untuk mengaktifkan kartu bantuan.");

            if (player.playerName.Contains("You"))
            {
                // Tampilkan UI untuk pemain manusia
                yield return HandlePlayerChoice(player);
            }
            else
            {
                // Logika untuk Bot
                yield return HandleBotChoice(player);
            }

            // Beri jeda antar giliran
            yield return new WaitForSeconds(2f);
        }

        Debug.Log("--- Fase Kartu Bantuan Selesai ---");
        // Panggil fungsi untuk melanjutkan ke siklus berikutnya (misal: reset semester)
        gameManager.ResetSemesterButton();
    }

    private IEnumerator HandlePlayerChoice(PlayerProfile player)
    {
        helpCardActivationPanel.SetActive(true);
        cardNameText.text = player.helpCard.cardName;
        cardDescriptionText.text = player.helpCard.description;

        bool choiceMade = false;

        activateButton.onClick.RemoveAllListeners();
        activateButton.onClick.AddListener(() => {
            ApplyEffect(player);
            player.helpCard = null; // Kartu hangus setelah dipakai
            choiceMade = true;
            helpCardActivationPanel.SetActive(false);
        });

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(() => {
            Debug.Log($"{player.playerName} memilih untuk tidak mengaktifkan kartunya.");
            choiceMade = true;
            helpCardActivationPanel.SetActive(false);
        });

        // Tunggu sampai pemain membuat pilihan
        yield return new WaitUntil(() => choiceMade);
    }

    private IEnumerator HandleBotChoice(PlayerProfile bot)
    {
        yield return new WaitForSeconds(1.5f);

        // 60% kemungkinan bot akan mengaktifkan kartunya
        bool activate = Random.value < 0.6f;

        if (activate)
        {
            ApplyEffect(bot);
            bot.helpCard = null; // Kartu hangus
        }
        else
        {
            Debug.Log($"{bot.playerName} (Bot) memilih untuk tidak mengaktifkan kartunya.");
        }
    }

    private void ApplyEffect(PlayerProfile player)
    {
        Debug.Log($"{player.playerName} mengaktifkan '{player.helpCard.cardName}'!");
        switch (player.helpCard.effectType)
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
                var ipoToSabotage = sellingManager.ipoDataList[Random.Range(0, sellingManager.ipoDataList.Count)];
                ipoToSabotage.ipoIndex--;
                Debug.Log($"IPO {ipoToSabotage.color} menurun!");
                sellingManager.UpdateIPOVisuals();
                break;
        }
        gameManager.UpdatePlayerUI();
    }

    private HelpCard GetRandomHelpCard()
    {
        int effectCount = System.Enum.GetNames(typeof(HelpCardEffect)).Length;
        HelpCardEffect randomEffect = (HelpCardEffect)Random.Range(0, effectCount);

        switch (randomEffect)
        {
            case HelpCardEffect.ExtraFinpoints:
                return new HelpCard("Dana Hibah", "Langsung dapat 10 Finpoint.", randomEffect);
            case HelpCardEffect.BoostRandomIPO:
                return new HelpCard("Good News", "Meningkatkan nilai IPO satu warna secara acak.", randomEffect);
            case HelpCardEffect.SabotageRandomIPO:
                return new HelpCard("Bad News", "Menurunkan nilai IPO satu warna secara acak.", randomEffect);
            default:
                return new HelpCard("Dana Hibah", "Langsung dapat 10 Finpoint.", HelpCardEffect.ExtraFinpoints);
        }
    }
}