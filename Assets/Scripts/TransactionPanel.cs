using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
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
            barterPurchaseButton.Disable();
            moneyPurchaseButton.Disable();
            return;
        }
        
        Item item = itemInstance.ItemRef;
        
        bool canBarter = item.traderSpawning.barterRequirements.Count > 0;
        foreach (ItemWithCount barterReq in item.traderSpawning.barterRequirements) {
            Assert.IsNotNull(barterReq.item, $"Null barter item for {item.displayName}. Fix it or remove it.");
            if (gameInstance.GetOwnedCountOfItem(barterReq.item) < barterReq.count) {
                canBarter = false;
                break;
            }
        }
        
        barterPurchaseButton.SetClickableState(canBarter);
        moneyPurchaseButton.SetClickableState(player.coinCurrency >= item.buyPrice);
        
        purchasingItemImage.sprite = item.inventorySprite;
        purchasingItemDesc.Show(itemInstance);

        for (int i = 0; i < item.traderSpawning.barterRequirements.Count; i++) {
            ItemWithCount barterReq = item.traderSpawning.barterRequirements[i];
            ResourceRequirement resReq = resourceRequirements[i];
            resReq.gameObject.SetActive(true);
            resReq.Set(barterReq.item, barterReq.count, gameInstance.GetOwnedCountOfItem(barterReq.item));
        }

        string buyPriceString = ColorText(item.buyPrice.ToString("N0"), styles.coinCurrencyColor);
        moneyPurchaseButton.text.text = $"Purchase for <sprite=0>{buyPriceString}";
    }

    public void UpdateSellPrice(int sellPrice) {
        sellParent.gameObject.SetActive(true);
        purchasingParent.gameObject.SetActive(false);
        
        string sellPriceString = ColorText(sellPrice.ToString("N0"), styles.coinCurrencyColor);
        sellInfoText.text = $"Sell for <sprite=0>{sellPriceString}";
    }

}