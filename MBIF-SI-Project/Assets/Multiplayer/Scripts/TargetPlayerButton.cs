// File: TargetPlayerButton.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Jika Anda menggunakan TextMeshPro

public class TargetPlayerButton : MonoBehaviour
{
    public Button button;
    public Text buttonText; // atau public TextMeshProUGUI buttonText;

    public void Setup(string playerName, int actorNumber, string cardColor, ActionPhaseManager manager)
    {
        buttonText.text = playerName;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            // Kirim kembali actorNumber DAN cardColor
            manager.OnTenderOfferTargetSelected(actorNumber, cardColor);
        });
    }
}