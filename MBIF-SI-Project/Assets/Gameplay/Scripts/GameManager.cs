using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public Score scoreCalculator;

    [Header("UI References")]
    public GameObject playerEntryPrefab;         // Prefab untuk 1 entry (Text/Panel)
    public Transform playerListContainer;        // Parent object (misalnya Vertical Layout Group)

    public Button bot2Button;
    public Button bot3Button;
    public Button bot4Button;

    private PlayerProfile player;
    private List<PlayerProfile> bots = new List<PlayerProfile>();
    private List<GameObject> playerEntries = new List<GameObject>();

    private void Start()
    {
        player = new PlayerProfile("You");

        bot2Button.onClick.AddListener(() => SetBotCount(2));
        bot3Button.onClick.AddListener(() => SetBotCount(3));
        bot4Button.onClick.AddListener(() => SetBotCount(4));
    }

    private void SetBotCount(int count)
    {
        bots.Clear();
        for (int i = 0; i < count; i++)
        {
            bots.Add(new PlayerProfile("Bot " + (i + 1)));
        }

        ResetAllScores(); // 🔄 Reset semua skor saat jumlah pemain berubah
        ResetDicePositions(); // Reset posisi dan rotasi dadu
        Invoke(nameof(UpdateScores), 3f); // Tunggu dadu selesai roll
    }

    private void ResetDicePositions()
    {
        // Mengembalikan posisi dan rotasi dadu ke posisi awal
        if (scoreCalculator.dice1 != null) scoreCalculator.dice1.ResetPosition();
        if (scoreCalculator.dice2 != null) scoreCalculator.dice2.ResetPosition();
    }

    private void ResetAllScores()
    {
        player.SetScore(0);
        foreach (var bot in bots)
        {
            bot.SetScore(0);
        }

        ClearPlayerListUI();
        AddPlayerEntry(player.playerName, 0);
        foreach (var bot in bots)
        {
            AddPlayerEntry(bot.playerName, 0);
        }

        Debug.Log("🔁 Skor direset karena jumlah pemain berubah.");
    }

    private void UpdateScores()
    {
        ClearPlayerListUI();

        // Hitung skor player
        int playerScore = scoreCalculator.GetDiceTotal();
        player.SetScore(playerScore);
        AddPlayerEntry(player.playerName, player.score);

        Debug.Log($"[Player] {player.playerName}: {player.score}");

        // Hitung skor untuk setiap bot
        foreach (var bot in bots)
        {
            int botScore = Random.Range(1, 13);
            bot.SetScore(botScore);
            AddPlayerEntry(bot.playerName, bot.score);

            // Tampilkan di Console
            Debug.Log($"[Bot] {bot.playerName}: {bot.score}");
        }
    }

    private void AddPlayerEntry(string name, int score)
    {
        GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
        Text text = entry.GetComponentInChildren<Text>();
        text.text = $"{name}: {score}";
        playerEntries.Add(entry);
    }

    private void ClearPlayerListUI()
    {
        foreach (var entry in playerEntries)
        {
            Destroy(entry);
        }
        playerEntries.Clear();
    }
}
