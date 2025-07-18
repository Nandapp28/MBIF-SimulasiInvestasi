using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class BorderShopManager : MonoBehaviour
{
    [Header("Definisi Item Shop")]
    [Tooltip("Isi daftar ini secara manual di Inspector. 'Border Asset Name' harus sama persis dengan nama file di folder Resources.")]
    public List<BorderShopItem> itemDefinitions; // PERUBAHAN: Menggunakan BorderShopItem

    [Header("Referensi UI")]
    public TextMeshProUGUI playerFinpoinText;
    public Transform shopItemParent;
    public GameObject shopItemPrefab; // Gunakan prefab khusus untuk border

    [Header("Panel Preview")]
    public Image previewBorderImage; // PERUBAHAN: Dari previewAvatarImage
    public TextMeshProUGUI previewPriceText;
    public Button buyButton;

    [Header("Panel Feedback")]
    public GameObject successPopup;
    public GameObject failedPopup;

    private DatabaseReference dbRef;
    private string currentUserUID;
    private long playerFinpoin;
    private List<string> ownedBorders = new List<string>(); // PERUBAHAN: dari ownedAvatars
    private BorderShopItem selectedItem; // PERUBAHAN: Menggunakan BorderShopItem

    async void Start()
    {
        // Selalu tunggu Firebase siap
        while (FirebaseInitializer.Instance == null || !FirebaseInitializer.Instance.IsFirebaseReady)
        {
            await Task.Delay(100);
        }
        
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        var auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser == null) {
            Debug.LogError("User belum login.");
            return; 
        }
        currentUserUID = auth.CurrentUser.UserId;

        buyButton.onClick.AddListener(OnBuyButtonClicked);
        SetupInitialState();
        await LoadPlayerDataAndPopulateShop();
        Debug.Log("Border Shop siap digunakan.");
    }
    
    void SetupInitialState()
    {
        previewPriceText.text = "Price";
        buyButton.gameObject.SetActive(false);
        if (successPopup != null) successPopup.SetActive(false);
        if (failedPopup != null) failedPopup.SetActive(false);
    }

    async Task LoadPlayerDataAndPopulateShop()
    {
        var userSnapshot = await dbRef.Child("users").Child(currentUserUID).GetValueAsync();
        if (!userSnapshot.Exists) return;

        playerFinpoin = userSnapshot.Child("finPoin").Exists ? (long)userSnapshot.Child("finPoin").Value : 0;
        playerFinpoinText.text = "$ " + playerFinpoin.ToString("N0");

        ownedBorders.Clear();
        // PERUBAHAN: Membaca dari "owned_border"
        if (userSnapshot.Child("owned_border").Exists)
        {
            foreach (var child in userSnapshot.Child("owned_border").Children)
            {
                ownedBorders.Add(child.Value.ToString());
            }
        }
        
        PopulateShop();
    }

    void PopulateShop()
    {
        foreach (Transform child in shopItemParent)
        {
            Destroy(child.gameObject);
        }
        
        // PERUBAHAN: Memuat dari folder "Borders"
        Sprite[] allBorders = Resources.LoadAll<Sprite>("Borders");

        foreach (Sprite borderSprite in allBorders)
        {
            // PERUBAHAN: Mencocokkan dengan borderAssetName
            BorderShopItem itemData = itemDefinitions.FirstOrDefault(item => item.borderAssetName == borderSprite.name);
            
            if (itemData == null) {
                Debug.LogWarning($"Tidak ditemukan definisi harga untuk border: {borderSprite.name}.");
                continue;
            }

            GameObject newItem = Instantiate(shopItemPrefab, shopItemParent);
            BorderShopItemUI itemUI = newItem.GetComponent<BorderShopItemUI>(); // PERUBAHAN: Menggunakan BorderShopItemUI
            itemUI.Setup(borderSprite, () => OnItemSelected(borderSprite, itemData));
            
            // PERUBAHAN: Mengecek kepemilikan border
            if (ownedBorders.Contains(itemData.borderAssetName))
            {
                itemUI.SetOwnedStatus(true);
            }
            else
            {
                itemUI.SetOwnedStatus(false);
            }
        }
    }

    public void OnItemSelected(Sprite borderSprite, BorderShopItem itemData)
    {
        selectedItem = itemData;
        previewBorderImage.sprite = borderSprite; // PERUBAHAN
        previewPriceText.text = "$ " + itemData.price.ToString("N0");
        buyButton.gameObject.SetActive(true);
        
        // PERUBAHAN
        if (ownedBorders.Contains(itemData.borderAssetName) || playerFinpoin < itemData.price)
        {
            buyButton.interactable = false;
        }
        else
        {
            buyButton.interactable = true;
        }
    }

    async void OnBuyButtonClicked()
    {
        if (selectedItem == null) return;
        if (playerFinpoin < selectedItem.price)
        {
            Debug.Log("Finpoin tidak cukup!");
            StartCoroutine(ShowFeedback(failedPopup));
            return;
        }
        // PERUBAHAN
        if (ownedBorders.Contains(selectedItem.borderAssetName))
        {
            Debug.Log("Sudah memiliki item ini!");
            return;
        }

        buyButton.interactable = false;
        long newFinpoin = playerFinpoin - selectedItem.price;
        await dbRef.Child("users").Child(currentUserUID).Child("finPoin").SetValueAsync(newFinpoin);
        // PERUBAHAN: Menyimpan ke "owned_border"
        await dbRef.Child("users").Child(currentUserUID).Child("owned_border").Push().SetValueAsync(selectedItem.borderAssetName);
        
        playerFinpoin = newFinpoin;
        ownedBorders.Add(selectedItem.borderAssetName); // PERUBAHAN
        playerFinpoinText.text = "$ " + playerFinpoin.ToString("N0");
        
        Debug.Log("Pembelian border berhasil!");
        StartCoroutine(ShowFeedback(successPopup));
        PopulateShop();
    }

    IEnumerator ShowFeedback(GameObject feedbackPanel)
    {
        if (feedbackPanel == null) yield break;
        feedbackPanel.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        feedbackPanel.SetActive(false);
    }
}