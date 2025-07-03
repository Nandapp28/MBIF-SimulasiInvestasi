// File: HelpCardPhaseManagerMultiplayer.cs

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;

public class HelpCardPhaseManagerMultiplayer : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject helpCardActivationPanel;
    public Text cardNameText;
    public Text cardDescriptionText;
    public Button activateButton;
    public Button skipButton;

    private HelpCardMultiplayer currentCard;

    public void StartHelpCardPhase(Dictionary<int, PlayerProfileMultiplayer> players, List<int> turnOrder)
    {
        // Logika untuk memulai fase ini akan dikontrol oleh MultiplayerManager
        // MultiplayerManager akan menentukan giliran siapa, lalu memanggil RPC
        // untuk menampilkan UI di client yang bersangkutan.
    }
    
    // Ini akan dipanggil oleh RPC dari MultiplayerManager
    public void ShowActivationChoice(HelpCardMultiplayer card, int playerActorNumber)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber != playerActorNumber) return;

        currentCard = card;
        helpCardActivationPanel.SetActive(true);
        cardNameText.text = card.cardName;
        cardDescriptionText.text = card.description;

        activateButton.onClick.RemoveAllListeners();
        activateButton.onClick.AddListener(() => SendChoice(true));

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(() => SendChoice(false));
    }

    private void SendChoice(bool activate)
    {
        helpCardActivationPanel.SetActive(false);
        
        // Kirim pilihan ke MasterClient
        PhotonView pv = MultiplayerManager.Instance.GetComponent<PhotonView>();
        pv.RPC("Cmd_PlayerUsesHelpCard", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber, currentCard.effectType, activate);
    }
}