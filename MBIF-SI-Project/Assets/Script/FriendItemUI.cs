using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI userNameText;
    public TextMeshProUGUI playerIdText;
    public Image avatarImage;

    // Fungsi ini hanya untuk mengisi data, tanpa tombol
    public void Setup(DataToSave friendData)
    {
        this.userNameText.text = friendData.userName;
        this.playerIdText.text = "ID: " + friendData.playerId;

        if (!string.IsNullOrEmpty(friendData.avatarName))
        {
            Sprite avatarSprite = Resources.Load<Sprite>("Avatars/" + friendData.avatarName);
            if (avatarSprite != null)
            {
                this.avatarImage.sprite = avatarSprite;
            }
        }
    }
}