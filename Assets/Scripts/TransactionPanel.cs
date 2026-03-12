using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class TransactionPanel : MonoBehaviour {

    public Styles styles;
    
    [Header("Toggles")]
    public ToggleButtonGroup toggleGroup;
    public ToggleButton sellToggle;
    public ToggleButton buyToggle;
    
    [Header("Selling")]
    public Transform sellParent;
    public TextMeshProUGUI sellInfoText;
    public ButtonFeel sellButton;
    
    [Header("Purchasing")]
    public Transform purchasingParent;
    public ItemDescPopup purchasingItemDesc;
    public Image purchasingItemImage;
    public List<ResourceRequirement> resourceRequirements;
    public ButtonFeel barterPurchaseButton;
    public ButtonFeel moneyPurchaseButton;
    
    public void UpdateBuyItem(ItemInstance itemInstance) {
        sellParent.gameObject.SetActive(false);
        purchasingParent.gameObject.SetActive(true);

        foreach (ResourceRequirement resReq in resourceRequirements) {
            resReq.gameObject.SetActive(false);
        }
        
        if (itemInstance == null) {
            return;
        }

        purchasingItemImage.sprite = itemInstance.ItemRef.inventorySprite;
        purchasingItemDesc.Set(itemInstance);
        inst.FitPopupSize(purchasingItemDesc.rectTransform, purchasingItemDesc.tagsParent.rect, purchasingItemDesc.nameText.rectTransform.rect, purchasingItemDesc.descText.rectTransform.rect);

        for (int i = 0; i < itemInstance.ItemRef.barterRequirements.Count; i++) {
            ItemWithCount barterReq = itemInstance.ItemRef.barterRequirements[i];
            ResourceRequirement resReq = resourceRequirements[i];
            resReq.gameObject.SetActive(true);
            resReq.Set(barterReq.item, barterReq.count, inst.GetOwnedCountOfItem(barterReq.item));
        }

        string buyPriceString = ColorText(itemInstance.ItemRef.buyPrice.ToString("N0"), styles.coinCurrencyColor);
        moneyPurchaseButton.text.text = $"Purchase for <sprite=0>{buyPriceString}";
    }

    public void UpdateSellPrice(int sellPrice) {
        sellParent.gameObject.SetActive(true);
        purchasingParent.gameObject.SetActive(false);
        
        string sellPriceString = ColorText(sellPrice.ToString("N0"), styles.coinCurrencyColor);
        sellInfoText.text = $"Sell for <sprite=0>{sellPriceString}";
    }

}