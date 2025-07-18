using UnityEngine;
using UnityEngine.UI;
using System; // Diperlukan untuk Action

// Definisikan struktur data item di sini agar bisa diakses oleh manager
[Serializable]
public class AvatarShopItem
{
    public string avatarAssetName; // Nama file di folder Resources
    public int price;
}

public class AvatarShopItemUI : MonoBehaviour
{
    public Image itemImage;
    public Button itemButton;
    public GameObject ownedLabelObject;

    // Fungsi Setup yang baru
    public void Setup(Sprite displaySprite, Action onClickAction)
    {
        if (displaySprite != null)
        {
            itemImage.sprite = displaySprite;
        }

        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(() => onClickAction());
    }

    // Fungsi untuk menampilkan atau menyembunyikan label
    public void SetOwnedStatus(bool isOwned)
    {
        if (ownedLabelObject != null)
        {
            ownedLabelObject.SetActive(isOwned);
        }
    }
}