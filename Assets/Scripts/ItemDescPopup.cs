using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class ItemDescPopup : MonoBehaviour, ILayoutSelfController {

    public Styles styles;
    public RectTransform rectTransform;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI metaInfoText;
    public HorizontalLayoutGroup tagsLayoutGroup;
    public VerticalLayoutGroup bodyLayoutGroup;
    public ImageTextGroup augmentedTagGroup;
    public ImageTextGroup typeTagGroup;
    public ImageTextGroup rarityTagGroup;
    public AugmentDescription augmentDesc;
    public DemonEyeDescList demonEyeDesc;
    
    public void Show(ItemInstance itemInstance, Vector2? position = default) {
        gameObject.SetActive(true);
        
        Item item = itemInstance.ItemRef;
        SetName(itemInstance, item);
        SetTags(item);
        SetMetaInfo(itemInstance, item);
        SetDescription(itemInstance, item);
        
        if (position.HasValue) {
            transform.position = position.Value;
            TweenPopUp(rectTransform);
        }
        
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
    
    public void Hide() {
        nameText.text = string.Empty;
        descText.text = string.Empty;
        gameObject.SetActive(false);
    }
    
    private void SetName(ItemInstance itemInstance, Item item) {
        if (itemInstance.isDemonEye) {
            nameText.text = itemInstance.demonEyeName;
            return;
        }
        nameText.text = item.displayName;
    }

    private void SetTags(Item item) {
        Item.Rarity itemRarity = item.GetRarity();
        Color itemRarityColor = styles.GetColorForRarity(itemRarity);

        typeTagGroup.gameObject.SetActive(true);
        typeTagGroup.image.color = itemRarityColor;
        
        if (item.type == gameInstance.itemTypes.quickUse) {
            typeTagGroup.textMesh.text = "Quick Use";
        } 
        else if (item.type == gameInstance.itemTypes.eyeUpgrade) {
            typeTagGroup.textMesh.text = "Eye Upgrade";
        }
        else if (item.type == gameInstance.itemTypes.wearableModifier) {
            typeTagGroup.textMesh.text = "Wearable Modifier";
        }
        else if (item.type == gameInstance.itemTypes.backpack) {
            typeTagGroup.textMesh.text = "Backpack";
        }
        else {
            typeTagGroup.gameObject.SetActive(false);
        }
        
        augmentedTagGroup.gameObject.SetActive(false);
        if (item.IsAugmented) {
            augmentedTagGroup.gameObject.SetActive(true);
            augmentedTagGroup.image.color = itemRarityColor;
        }
            
        rarityTagGroup.image.color = itemRarityColor;
        rarityTagGroup.textMesh.text = itemRarity.ToString();
    }

    private void SetMetaInfo(ItemInstance itemInstance, Item item) {
        int sellOrBuyPrice = 0;
        if (item.type == gameInstance.itemTypes.demonEye) { 
            sellOrBuyPrice = gameInstance.GetDemonEyeSellPrice(itemInstance);
        }
        else {
            bool itemIsOwnedByTrader = itemInstance.traderOwned;
            sellOrBuyPrice = itemIsOwnedByTrader ? item.buyPrice : item.GetSellPrice() * itemInstance.count;
        }
        
        string coinText = $"<sprite=0>{ColorText(sellOrBuyPrice.ToString("N0"), styles.coinCurrencyColor)}";
        
        string tintedWeightSprite = $"<sprite=2 color=#{ColorUtility.ToHtmlStringRGBA(styles.underWeightColor)}>";
        string weightText = tintedWeightSprite + ColorText((item.Weight * itemInstance.count).ToString(), styles.underWeightColor);
        
        metaInfoText.text = coinText + "  " + weightText;
    }

    private void SetDescription(ItemInstance itemInstance, Item item) {
        augmentDesc.gameObject.SetActive(false);
        
        descText.gameObject.SetActive(!itemInstance.isDemonEye);
        demonEyeDesc.gameObject.SetActive(itemInstance.isDemonEye);
        
        if (itemInstance.isDemonEye) {
            demonEyeDesc.UpdateDisplay(gameInstance.EyeUpgradeSetFromIds(itemInstance.nestedUuids));
        }
        else {
            descText.text = item.GetDescription();
        }
        
        if (item.IsAugmented) {
            augmentDesc.gameObject.SetActive(true);
            augmentDesc.descTextMesh.text = item.augmentCreatedFrom.GetDescription();
            augmentDesc.stackCountTextMesh.gameObject.SetActive(false);
        }
        
        if (item.type == gameInstance.itemTypes.quickUse && !itemInstance.traderOwned) {
            descText.text += $"<line-height=150%>\n<sprite=5 color=#{ColorUtility.ToHtmlStringRGBA(styles.inputIconTint)}> " +
                             $"<size=80%>{ColorText("Right click to consume", styles.inputIconTint)}</size>";
        }
    }

    public void SetLayoutVertical() {
        FitPopupSize(rectTransform, tagsLayoutGroup, nameText, bodyLayoutGroup);
        
        // Keep popup from going offscreen
        {
            float minY = rectTransform.WorldRectIgnoreScale().yMin;
            float maxY = rectTransform.WorldRectIgnoreScale().yMax;
        
            const float screenPadding = 25f;
        
            bool offBottomOfScreen = minY < 0f;
            if (offBottomOfScreen) {
                float verticalCorrection = Mathf.Abs(minY) + screenPadding;
                rectTransform.position += new Vector3(0f, verticalCorrection, 0f);
            }
        
            bool offTopOfScreen = maxY > Screen.height;
            if (offTopOfScreen) {
                float verticalCorrection = (maxY - Screen.height) + screenPadding;
                rectTransform.position -= new Vector3(0f, verticalCorrection, 0f);
            }
        }
    }
    
    public void SetLayoutHorizontal() { }
    
}
