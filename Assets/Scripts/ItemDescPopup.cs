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
    public TextMeshProUGUI augmentDescText;
    public ContentSizeFitter nameContentFitter;
    public ContentSizeFitter descContentFitter;
    public RectTransform augmentBlockRectTransform;
    public ContentSizeFitter augmentDescContentFitter;
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
        
        if (item.type == gameInstance.quickUseType) {
            tag1Text.text = "Quick Use";
        } 
        else if (item.type == gameInstance.eyeModifierType) {
            tag1Text.text = "Eye Modifier";
        }
        else if (item.type == gameInstance.wearableModifierType) {
            tag1Text.text = "Wearable Modifier";
        }
        else if (item.type == gameInstance.backpackType) {
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
        if (item.type == gameInstance.demonEyeType) { 
            sellOrBuyPrice = gameInstance.GetDemonEyeSellPrice(itemInstance);
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
        augmentBlockRectTransform.gameObject.SetActive(false);
        
        if (item.type == gameInstance.demonEyeType) {
            DemonEyeInstance eyeInstance = gameInstance.eyeInstanceFromItemId[itemInstance.itemOrInstanceUuid];
            string eyeDescription = "";
            foreach (EquipedAugmentInstance augmentInstance in eyeInstance.augmentInstances) {
                eyeDescription += $"{augmentInstance.Augment.GetDescription()}\n";
            }
            foreach (EquipedModInstance modInstance in eyeInstance.modInstances) {
                eyeDescription += gameInstance.GetDemonEyeModDescription(modInstance.ModifierItem, modInstance.stackCount, null);
            }
            descText.text = eyeDescription;
        }
        else {
            descText.text = item.GetDescription();
        }
        
        if (itemInstance.TryGetUuidObject(out var uuidObject) && uuidObject is Augment augment) {
            augmentBlockRectTransform.gameObject.SetActive(true);
            augmentDescText.text = augment.GetDescription();
            augmentDescContentFitter.ForceRecalculate();

            Rect rect = augmentDescText.rectTransform.rect;
            augmentBlockRectTransform.sizeDelta = new(augmentBlockRectTransform.rect.x, rect.y);
        }
        
        if (item.type == gameInstance.quickUseType && !itemInstance.traderOwned) {
            descText.text += $"<line-height=150%>\n<sprite=5 color=#{ColorUtility.ToHtmlStringRGBA(styles.inputIconTint)}> " +
                             $"<size=80%>{ColorText("Right click to consume", styles.inputIconTint)}</size>";
        } 
        
        descContentFitter.ForceRecalculate();
    }
    
}
