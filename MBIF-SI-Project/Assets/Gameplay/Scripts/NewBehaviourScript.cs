using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SellingPhaseUI : MonoBehaviour
{
    public GameObject sectorsParent;

    [System.Serializable]
    public class CategoryUI
    {
        [Header("UI Elements")]
        public TextMeshProUGUI countText;
        public TextMeshProUGUI CurrentStockText;
        public TextMeshProUGUI CurrentPriceSectorText;
        public Button plusButton;
        public Button minusButton;

        [Header("Status Variables")]
        public int count = 0;
        public int CurrentStock;
        public int CurrentPriceSector = 5;
    }

    public CategoryUI Infrastuktur = new CategoryUI();
    public CategoryUI Mining = new CategoryUI();
    public CategoryUI Consumen = new CategoryUI();
    public CategoryUI Finance = new CategoryUI();
    public TextMeshProUGUI TotalEarnings;

    private int totalEarnings;

    private void Start()
    {
        SetupCategory(Infrastuktur);
        SetupCategory(Mining);
        SetupCategory(Consumen);
        SetupCategory(Finance);
        UpdateTotalEarnings();
    }

    private void SetupCategory(CategoryUI category)
    {
        category.plusButton.onClick.AddListener(() =>
        {
            if (category.count < category.CurrentStock)
            {
                category.count++;
                UpdateCategoryUI(category);
                UpdateTotalEarnings();
            }
        });

        category.minusButton.onClick.AddListener(() =>
        {
            if (category.count > 0)
            {
                category.count--;
                UpdateCategoryUI(category);
                UpdateTotalEarnings();
            }
        });

        UpdateCategoryUI(category);
    }

    private void UpdateCategoryUI(CategoryUI category)
    {
        category.countText.text = category.count.ToString();
        category.CurrentStockText.text = category.CurrentStock.ToString();
        category.CurrentPriceSectorText.text = category.CurrentPriceSector.ToString();
    }

    private void UpdateTotalEarnings()
    {
        totalEarnings = (Infrastuktur.count * Infrastuktur.CurrentPriceSector)
                      + (Mining.count * Mining.CurrentPriceSector)
                      + (Consumen.count * Consumen.CurrentPriceSector)
                      + (Finance.count * Finance.CurrentPriceSector);

        TotalEarnings.text = totalEarnings.ToString();
    }

    // Method untuk reset UI jika diperlukan (misalnya saat reset semester)
    public void ResetSellingPhase()
    {
        ResetCategory(Infrastuktur);
        ResetCategory(Mining);
        ResetCategory(Consumen);
        ResetCategory(Finance);
        UpdateTotalEarnings();
    }

    private void ResetCategory(CategoryUI category)
    {
        category.count = 0;
        // kamu bisa set CurrentStock ulang jika perlu
        UpdateCategoryUI(category);
    }
}
