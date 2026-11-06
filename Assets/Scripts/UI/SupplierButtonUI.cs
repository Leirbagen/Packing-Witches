using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SupplierButtonUI : MonoBehaviour
{
    [Header("Configuration")]
    public int catalogIndex;
    public SupplierManager supplierManager;

    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI normalPriceText;
    public TextMeshProUGUI expressPriceText; 

    private void Start()
    {
        if (supplierManager != null && catalogIndex < supplierManager.supplierCatalog.Length)
        {
            var entry = supplierManager.supplierCatalog[catalogIndex];
            if (entry.productData != null)
            {
                if (itemNameText != null) itemNameText.text = entry.productData.productName;
                if (iconImage != null && entry.productData.icon != null) iconImage.sprite = entry.productData.icon;
                if (normalPriceText != null) normalPriceText.text = $"${entry.productData.price}";

                if (expressPriceText != null) expressPriceText.text = $"${entry.productData.price * 2} Exp";
            }
        }
    }

    public void OnClickNormalOrder()
    {
        if (supplierManager != null)
        {
            supplierManager.OrderSupply(catalogIndex, false); 
        }
    }

    public void OnClickExpressOrder()
    {
        if (supplierManager != null)
        {
            supplierManager.OrderSupply(catalogIndex, true); 
        }
    }
}