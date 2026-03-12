using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class ItemDescPopup : MonoBehaviour {

    public Styles styles;
    
    public RectTransform rectTransform;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI metaInfoText;
    public ContentSizeFitter nameContentFitter;
    public ContentSizeFitter descContentFitter;
    public RectTransform tagsParent;
    
    public Image tag1;
    public Image tag2;
    public TextMeshProUGUI tag1Text;
    public TextMeshProUGUI tag2Text;
    public ContentSizeFitter tag1ContentFitter;
    public ContentSizeFitter tag2ContentFitter;

    public void Set(ItemInstance itemInstance) {
        Item item = itemInstance.ItemRef;
        SetName(item);
        SetTags(item);
        SetMetaInfo(itemInstance, item);
        SetDescription(itemInstance, item);
    }
    
    private void SetName(Item item) {
        nameText.text = item.displayName;
        nameContentFitter.ForceRecalculate();
    }

    private void SetTags(Item item) {
        Item.Rarity itemRarity = item.GetRarity();
        Color itemRarityColor = styles.GetColorForRarity(itemRarity);
        float tagTextPadding = styles.tagTextPadding;

        tag1.gameObject.SetActive(true);
        tag1.color = itemRarityColor;
        
        if (item.type == inst.quickUseType) {
            tag1Text.text = "Quick Use";
        } 
        else if (item.type == inst.eyeModifierType) {
            tag1Text.text = "Eye Modifier";
        }
        else if (item.type == inst.wearableModifierType) {
            tag1Text.text = "Wearable Modifier";
        }
        else if (item.type == inst.backpackType) {
            tag1Text.text = "Backpack";
        }
        else {
            tag1.gameObject.SetActive(false);
        }
        
        tag1ContentFitter.ForceRecalculate();
        tag1.rectTransform.ResizeWidth(tag1Text.rectTransform.rect.width + tagTextPadding);
        
        tag2.color = itemRarityColor;
        tag2Text.text = itemRarity.ToString();
        tag2ContentFitter.ForceRecalculate();
        tag2.rectTransform.ResizeWidth(tag2Text.rectTransform.rect.width + tagTextPadding);
    }

    private void SetMetaInfo(ItemInstance itemInstance, Item item) {
        int sellOrBuyPrice = 0;
        if (item.type == inst.demonEyeType) { 
            sellOrBuyPrice = inst.GetDemonEyeSellPrice(itemInstance);
        }
        else {
            bool itemIsOwnedByTrader = itemInstance.traderOwned;
            sellOrBuyPrice = itemIsOwnedByTrader ? item.buyPrice : item.GetSellPrice() * itemInstance.count;
        }
                             
        string coinText = $"<sprite=0>{ColorText(sellOrBuyPrice.ToString("N0"), styles.coinCurrencyColor)}";
        
        string tintedWeightSprite = $"<sprite=2 color=#{ColorUtility.ToHtmlStringRGBA(styles.underWeightColor)}>";
        string weightText = tintedWeightSprite + ColorText(item.Weight.ToString(), styles.underWeightColor);
        
        metaInfoText.text = coinText + "  " + weightText;
    }

    private void SetDescription(ItemInstance itemInstance, Item item) {
        if (item.type == inst.demonEyeType) {
            DemonEyeInstance eyeInstance = inst.eyeInstanceFromItemId[itemInstance.itemOrInstanceUuid];
            string eyeDescription = "";
            foreach (EquipedAugmentInstance augmentInstance in eyeInstance.augmentInstances) {
                eyeDescription += $"{augmentInstance.Augment.GetDescription()}\n";
            }
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                eyeDescription += inst.GetDemonEyeModDescription(modInstance.ModifierItem, modInstance.stackCount, null);
            }
            descText.text = eyeDescription;
        }
        else {
            descText.text = item.GetDescription();
        }
        
        if (item.type == inst.quickUseType && !itemInstance.traderOwned) {
            descText.text += $"<line-height=150%>\n<sprite=5 color=#{ColorUtility.ToHtmlStringRGBA(styles.inputIconTint)}> " +
                             $"<size=80%>{ColorText("Right click to consume", styles.inputIconTint)}</size>";
        } 
        
        descContentFitter.ForceRecalculate();
    }
    
}
