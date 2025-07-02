using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    public Image itemImage;
    public Button itemButton;

    private AvatarItemData currentItem;
    private AvatarShopManager shopManager;

    public void Setup(AvatarItemData itemData, AvatarShopManager manager)
    {
        this.currentItem = itemData;
        this.shopManager = manager;

        if (itemData.avatarSprite != null)
        {
            itemImage.sprite = itemData.avatarSprite;
        }

        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(OnItemClicked);
    }

    void OnItemClicked()
    {
        // Beritahu manager bahwa item ini telah diklik
        shopManager.OnItemSelected(currentItem);
    }
}