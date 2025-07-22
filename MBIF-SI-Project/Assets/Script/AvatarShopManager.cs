using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // PENTING: Tambahkan ini

public class AvatarShopManager : MonoBehaviour
{
    [Header("Definisi Item Shop")]
    [Tooltip("Isi daftar ini secara manual di Inspector. 'Avatar Asset Name' harus sama persis dengan nama file di folder Resources.")]
    public List<AvatarShopItem> itemDefinitions;

    [Header("Referensi UI")]
    public TextMeshProUGUI playerFinpoinText;
    public Transform shopItemParent;
    public GameObject shopItemPrefab;

    [Header("Panel Preview")]
    public Image previewAvatarImage;
    public TextMeshProUGUI previewPriceText;
    public Button buyButton;

    [Header("Panel Feedback")]
    public GameObject successPopup;
    public GameObject failedPopup;

    private DatabaseReference dbRef;
    private string currentUserUID;
    private long playerFinpoin;
    private List<string> ownedAvatars = new List<string>();
    private AvatarShopItem selectedItem;


    // MODIFIKASI: Mengubah Start menjadi async void untuk alur yang lebih aman
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

        // Tunggu (await) sampai semua data selesai dimuat dan shop di-populate
        await LoadPlayerDataAndPopulateShop();

        // Setelah semua siap, barulah pemain bisa berinteraksi penuh.
        Debug.Log("Shop siap digunakan.");
    }
    
    void SetupInitialState()
    {
        previewPriceText.text = "Price";
        buyButton.gameObject.SetActive(false);
        if (successPopup != null) successPopup.SetActive(false);
        if (failedPopup != null) failedPopup.SetActive(false);
    }

    // MODIFIKASI: Mengubah menjadi async Task agar bisa di-await
    async Task LoadPlayerDataAndPopulateShop()
    {
        var userSnapshot = await dbRef.Child("users").Child(currentUserUID).GetValueAsync();
        if (!userSnapshot.Exists) return;

        playerFinpoin = userSnapshot.Child("finPoin").Exists ? (long)userSnapshot.Child("finPoin").Value : 0;
        playerFinpoinText.text = "$ " + playerFinpoin.ToString("N0");

        ownedAvatars.Clear(); // Bersihkan daftar lama sebelum mengisi
        if (userSnapshot.Child("owned_avatar").Exists)
        {
            foreach (var child in userSnapshot.Child("owned_avatar").Children)
            {
                ownedAvatars.Add(child.Value.ToString());
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
        Sprite[] allAvatars = Resources.LoadAll<Sprite>("Avatars");
        foreach (Sprite avatarSprite in allAvatars)
        {
            AvatarShopItem itemData = itemDefinitions.FirstOrDefault(item => item.avatarAssetName == avatarSprite.name);
            
            if (itemData == null) {
                Debug.LogWarning($"Tidak ditemukan definisi harga untuk avatar: {avatarSprite.name}. Item ini tidak akan ditampilkan di shop.");
                continue;
            }
            GameObject newItem = Instantiate(shopItemPrefab, shopItemParent);
            AvatarShopItemUI itemUI = newItem.GetComponent<AvatarShopItemUI>();
            itemUI.Setup(avatarSprite, () => OnItemSelected(avatarSprite, itemData));
            if (ownedAvatars.Contains(itemData.avatarAssetName))
            {
                itemUI.itemButton.interactable = false; // Tombol tetap non-aktif
                itemUI.SetOwnedStatus(true);           // Tampilkan label "Owned"
            }
            else
            {
                itemUI.SetOwnedStatus(false);          // Pastikan label "Owned" tersembunyi
            }
        }
    }

    public void OnItemSelected(Sprite avatarSprite, AvatarShopItem itemData)
    {
        selectedItem = itemData;
        previewAvatarImage.sprite = avatarSprite;
        previewPriceText.text = "$ " + itemData.price.ToString("N0");
        buyButton.gameObject.SetActive(true);
        if (ownedAvatars.Contains(itemData.avatarAssetName) || playerFinpoin < itemData.price)
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
        if (ownedAvatars.Contains(selectedItem.avatarAssetName))
        {
            Debug.Log("Sudah memiliki item ini!");
            return;
        }
        buyButton.interactable = false;
        long newFinpoin = playerFinpoin - selectedItem.price;
        await dbRef.Child("users").Child(currentUserUID).Child("finPoin").SetValueAsync(newFinpoin);
        await dbRef.Child("users").Child(currentUserUID).Child("owned_avatar").Push().SetValueAsync(selectedItem.avatarAssetName);
        playerFinpoin = newFinpoin;
        ownedAvatars.Add(selectedItem.avatarAssetName);
        playerFinpoinText.text = "$ " + playerFinpoin.ToString("N0");
        Debug.Log("Pembelian berhasil!");
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