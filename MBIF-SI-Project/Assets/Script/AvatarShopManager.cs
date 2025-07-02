using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AvatarShopManager : MonoBehaviour
{
    [Header("Data Shop")]
    public List<AvatarItemData> shopItems; // Masukkan semua aset AvatarItemData Anda di sini

    [Header("Referensi UI")]
    public TextMeshProUGUI playerFinpoinText;
    public Transform shopItemParent; // GameObject "Content" dari ScrollView
    public GameObject shopItemPrefab;

    [Header("Panel Preview")]
    public GameObject previewPanel;
    public Image previewAvatarImage;
    public TextMeshProUGUI previewPriceText;
    public Button buyButton;

    [Header("Panel Feedback")]
    public GameObject successPopup;
    public GameObject failedPopup;

    private DatabaseReference dbRef;
    private FirebaseAuth auth;
    private string currentUserUID;

    private long playerFinpoin;
    private List<string> ownedAvatars = new List<string>();
    private AvatarItemData selectedItem;

    IEnumerator Start()
    {
        // Selalu tunggu Firebase siap
        yield return new WaitUntil(() => FirebaseInitializer.Instance != null && FirebaseInitializer.Instance.IsFirebaseReady);
        
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        auth = FirebaseAuth.DefaultInstance;

        if (auth.CurrentUser == null) {
            Debug.LogError("User belum login.");
            yield break;
        }
        currentUserUID = auth.CurrentUser.UserId;

        buyButton.onClick.AddListener(OnBuyButtonClicked);
        previewPanel.SetActive(false); // Sembunyikan preview di awal

        LoadPlayerDataAndPopulateShop();
    }

    async void LoadPlayerDataAndPopulateShop()
    {
        // Ambil data user dari Firebase
        var userSnapshot = await dbRef.Child("users").Child(currentUserUID).GetValueAsync();
        if (!userSnapshot.Exists) return;

        // Ambil Finpoin
        if (userSnapshot.Child("finPoin").Exists)
        {
            playerFinpoin = (long)userSnapshot.Child("finPoin").Value;
            playerFinpoinText.text = "$ " + playerFinpoin.ToString("N0");
        }

        // Ambil daftar avatar yang sudah dimiliki
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
        foreach (var itemData in shopItems)
        {
            GameObject newItem = Instantiate(shopItemPrefab, shopItemParent);
            ShopItemUI itemUI = newItem.GetComponent<ShopItemUI>();
            itemUI.Setup(itemData, this);

            // Jika sudah punya, buat tombol tidak bisa diklik
            if (ownedAvatars.Contains(itemData.avatarAssetName))
            {
                itemUI.itemButton.interactable = false;
            }
        }
    }

    public void OnItemSelected(AvatarItemData itemData)
    {
        selectedItem = itemData;
        previewPanel.SetActive(true);

        previewAvatarImage.sprite = itemData.avatarSprite;
        previewPriceText.text = "$ " + itemData.price.ToString("N0");

        // Cek lagi apakah sudah punya atau uang tidak cukup
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

        // Validasi lagi untuk keamanan
        if (playerFinpoin < selectedItem.price)
        {
            Debug.Log("Uang tidak cukup!");
            StartCoroutine(ShowFeedback(failedPopup));
            return;
        }
        if (ownedAvatars.Contains(selectedItem.avatarAssetName))
        {
            Debug.Log("Sudah memiliki item ini!");
            return;
        }

        // Proses Transaksi
        long newFinpoin = playerFinpoin - selectedItem.price;

        // 1. Update Finpoin di database
        await dbRef.Child("users").Child(currentUserUID).Child("finPoin").SetValueAsync(newFinpoin);

        // 2. Tambahkan nama aset avatar ke daftar "owned_avatar"
        await dbRef.Child("users").Child(currentUserUID).Child("owned_avatar").Push().SetValueAsync(selectedItem.avatarAssetName);

        // Update data lokal & UI
        playerFinpoin = newFinpoin;
        ownedAvatars.Add(selectedItem.avatarAssetName);
        playerFinpoinText.text = "$ " + playerFinpoin.ToString("N0");
        buyButton.interactable = false;

        Debug.Log("Pembelian berhasil!");
        StartCoroutine(ShowFeedback(successPopup));
        
        // Refresh tampilan shop untuk menonaktifkan tombol item yang baru dibeli
        foreach (Transform child in shopItemParent)
        {
            Destroy(child.gameObject);
        }
        PopulateShop();
    }

    IEnumerator ShowFeedback(GameObject feedbackPanel)
    {
        feedbackPanel.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        feedbackPanel.SetActive(false);
    }
}