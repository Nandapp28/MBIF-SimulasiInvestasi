// File: TicketButtonMultiplayer.cs (Versi Bersih)

using UnityEngine;
using UnityEngine.UI;

public class TicketButtonMultiplayer : MonoBehaviour
{
    public Button button;
    public Image buttonImage; // Hanya butuh referensi ke Button dan Image

    private int ticketNumber;
    private TicketManagerMultiplayer ticketManager;

    public void Setup(int number, TicketManagerMultiplayer manager, Sprite defaultSprite)
    {
        this.ticketNumber = number;
        this.ticketManager = manager;
        
        if (buttonImage != null)
        {
            buttonImage.sprite = defaultSprite;
        }

        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (ticketManager != null)
        {
            ticketManager.OnTicketButtonClicked(this.ticketNumber);
        }
    }

    public void RevealTicket(Sprite numberSprite)
    {
        if (buttonImage != null)
        {
            buttonImage.sprite = numberSprite;
        }
        button.interactable = false;
    }
}