using System.Collections.Generic;
using UnityEngine;

public class SellingPhaseManager : MonoBehaviour
{
    public GameManager gameManager;

    public void StartSellingPhase(List<PlayerProfile> players, int resetCount, int maxResetCount, GameObject resetSemesterButton)
    {
        Dictionary<string, int> colorSellValues = new Dictionary<string, int>
        {
            { "Red", 30 },
            { "Blue", 20 },
            { "Green", 15 },
            { "Orange", 10 }
        };

        foreach (var player in players)
        {
            int earnedFinpoints = 0;
            List<Card> soldCards = new List<Card>();

            foreach (var card in player.cards)
            {
                if (colorSellValues.TryGetValue(card.color, out int sellValue))
                {
                    earnedFinpoints += sellValue;
                    soldCards.Add(card);
                }
            }

            player.finpoint += earnedFinpoints;

            foreach (var sold in soldCards)
            {
                player.cards.Remove(sold);
            }

            // Panggil fungsi dari GameManager jika dibutuhkan
            gameManager.UpdatePlayerUI();

            if (resetSemesterButton != null)
            {
                if (resetCount < maxResetCount)
                {
                    resetSemesterButton.SetActive(true);
                }
                else
                {
                    resetSemesterButton.SetActive(false);
                    gameManager.ShowLeaderboard();
                    Debug.Log($" Semester Sudah Berakhir");
                }
            }


            Debug.Log($"{player.playerName} menjual {soldCards.Count} kartu dan mendapatkan {earnedFinpoints} finpoints. Finpoint sekarang: {player.finpoint}");
        }

        Debug.Log("Fase penjualan selesai.");
    }
}
