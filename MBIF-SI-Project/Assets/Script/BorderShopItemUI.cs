using UnityEngine;
using UnityEngine.UI;
using System; // Diperlukan untuk Action

// Definisikan struktur data untuk item BORDER
[Serializable]
public class BorderShopItem
{
    // PERUBAHAN: Nama field diubah agar sesuai dengan border
    public string borderAssetName; // Nama file di folder Resources
    public int price;
}

// PERUBAHAN: Nama kelas utama diubah
public class BorderShopItemUI : MonoBehaviour
{
    // Variabel-variabel ini tetap sama karena fungsinya generik
    public Image itemImage;
    public Button itemButton;
    public GameObject ownedLabelObject;

    // Fungsi Setup yang baru (logikanya tetap sama)
    public void Setup(Sprite displaySprite, Action onClickAction)
    {
        if (displaySprite != null)
        {
            itemImage.sprite = displaySprite;
        }

        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(() => onClickAction());
    }

    // Fungsi untuk menampilkan atau menyembunyikan label (logikanya tetap sama)
    public void SetOwnedStatus(bool isOwned)
    {
        if (ownedLabelObject != null)
        {
            ownedLabelObject.SetActive(isOwned);
        }
    }
}