using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // Diperlukan untuk Action

public class FriendReqItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI userNameText;
    public TextMeshProUGUI playerIdText;
    public Image avatarImage;
    public Button acceptButton;
    public Button ignoreButton;

    // Fungsi untuk mengisi data dan menetapkan aksi tombol
    public void Setup(DataToSave userData, Action onAccept, Action onIgnore)
    {
        // Isi data UI
        this.userNameText.text = userData.userName;
        this.playerIdText.text = "ID: " + userData.playerId;

        // Muat avatar dari Resources
        if (!string.IsNullOrEmpty(userData.avatarName))
        {
            Sprite avatarSprite = Resources.Load<Sprite>("Avatars/" + userData.avatarName);
            if (avatarSprite != null)
            {
                this.avatarImage.sprite = avatarSprite;
            }
            else {
                Debug.LogWarning($"Avatar '{userData.avatarName}' tidak ditemukan.");
            }
        }

        // Atur listener untuk tombol
        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(() => onAccept());

        ignoreButton.onClick.RemoveAllListeners();
        ignoreButton.onClick.AddListener(() => onIgnore());
    }
}