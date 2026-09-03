using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
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
    public ResourceRequirementList resourceRequirementList;
    // public List<ResourceRequirement> resourceRequirements;
    public ButtonFeel barterPurchaseButton;
    public ButtonFeel moneyPurchaseButton;
    public GameObject outOfStockNotifier;
    
    public void UpdateBuyItem(ItemInstance itemInstance) {
        sellParent.gameObject.SetActive(false);
        purchasingParent.gameObject.SetActive(true);
        outOfStockNotifier.gameObject.SetActive(false);

        resourceRequirementList.HideAll();
        
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
        
        bool canPurchase = player.state.coinCurrency >= item.buyPrice;
        
        bool itemIsOutOfStock = itemInstance.count <= 0;
        if (itemIsOutOfStock) {
            canBarter = false;
            canPurchase = false;
            outOfStockNotifier.gameObject.SetActive(true);
        }
        
        barterPurchaseButton.SetClickableState(canBarter);
        moneyPurchaseButton.SetClickableState(canPurchase);
        
        purchasingItemDesc.Show(itemInstance);
        resourceRequirementList.Show(item.traderSpawning.barterRequirements);

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