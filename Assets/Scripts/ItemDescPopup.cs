using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class ItemDescPopup : MonoBehaviour, ILayoutSelfController {

    public bool dontFitToSize;
    public Styles styles;
    public RectTransform rectTransform;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI maxStackText;
    public TextMeshProUGUI weightText;
    public GameObject metaInfoParent;
    public GameObject consumePromtParent;
    public TextMeshProUGUI consumePromtText;
    public HorizontalLayoutGroup tagsLayoutGroup;
    public VerticalLayoutGroup bodyLayoutGroup;
    public ImageTextGroup typeTagGroup;
    public ImageTextGroup rarityTagGroup;
    public ImageTextGroup augmentedTagGroup;
    public AugmentDescription augmentDesc;
    public DemonEyeDescList demonEyeDesc;
    
    public const float screenPadding = 25f;
    
    public bool IsShowing => gameObject.activeInHierarchy;
    
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
            nameText.text = $"{itemInstance.demonEyeName} {ColorSprite(GetTextMeshRomanNumeralIndex(itemInstance.DemonEyeLevel), styles.headerTextColor)}";
            return;
        }
        nameText.text = item.displayName;
    }

    private void SetTags(Item item) {
        Item.Rarity itemRarity = item.GetRarity();
        Color itemRarityColor = styles.GetTextColorForRarity(itemRarity);

        typeTagGroup.gameObject.SetActive(true);
        typeTagGroup.image.color = itemRarityColor;
        
        if (item.type == gameInstance.itemTypes.quickUse) {
            typeTagGroup.textMesh.text = "Quick Use";
        } 
        else if (item.type == gameInstance.itemTypes.eyeUpgrade) {
            typeTagGroup.textMesh.text = "Blood Rune";
        }
        else if (item.type == gameInstance.itemTypes.wearableModifier) {
            typeTagGroup.textMesh.text = "Trinket";
        }
        else if (item.type == gameInstance.itemTypes.backpack) {
            typeTagGroup.textMesh.text = "Backpack";
        }
        else if (item.type == gameInstance.itemTypes.sellable) {
            typeTagGroup.textMesh.text = "Sellable";
        }
        else if (item.type == gameInstance.itemTypes.resource) {
            typeTagGroup.textMesh.text = "Resource";
        }
        else {
            typeTagGroup.gameObject.SetActive(false);
        }
        
        augmentedTagGroup.gameObject.SetActive(item.IsAugmented);
        augmentedTagGroup.image.color = itemRarityColor;
            
        rarityTagGroup.image.color = itemRarityColor;
        rarityTagGroup.textMesh.text = itemRarity.ToString();
    }

    private void SetMetaInfo(ItemInstance itemInstance, Item item) {
        if (itemInstance.traderOwned) {
            metaInfoParent.gameObject.SetActive(false);
            consumePromtParent.gameObject.SetActive(false);
            return;
        }
        
        metaInfoParent.gameObject.SetActive(true);
        
        int sellOrBuyPrice = 0;
        if (item.type == gameInstance.itemTypes.demonEye) { 
            sellOrBuyPrice = gameInstance.GetDemonEyeSellPrice(itemInstance);
        }
        else {
            bool itemIsOwnedByTrader = itemInstance.traderOwned;
            sellOrBuyPrice = itemIsOwnedByTrader ? item.buyPrice : item.GetSellPrice() * itemInstance.count;
        }
        
        priceText.text = $"<sprite=0>{ColorText(sellOrBuyPrice.ToString("N0"), styles.coinCurrencyColor)}";
        
        maxStackText.text = $"{itemInstance.count} / {item.MaxStackCount:N0}";
        
        string tintedWeightSprite = $"<sprite=2 color=#{ColorUtility.ToHtmlStringRGBA(styles.underWeightColor)}>";
        weightText.text = tintedWeightSprite + ColorText((item.Weight * itemInstance.count).ToString(), styles.underWeightColor);
        
        consumePromtParent.gameObject.SetActive(item.type == gameInstance.itemTypes.quickUse);
    }

    private void SetDescription(ItemInstance itemInstance, Item item) {
        augmentDesc.gameObject.SetActive(false);
        
        descText.gameObject.SetActive(!itemInstance.isDemonEye);
        demonEyeDesc.gameObject.SetActive(itemInstance.isDemonEye);
        
        if (itemInstance.isDemonEye) {
            demonEyeDesc.UpdateDisplay(gameInstance.EyeUpgradeSetFromIds(itemInstance.nestedUuids), showCountsAsIncrease: false);
        }
        else {
            descText.text = item.GetDescription();
        }
        
        if (item.IsAugmented) {
            augmentDesc.gameObject.SetActive(true);
            augmentDesc.descTextMesh.text = item.augmentCreatedFrom.GetDescription();
        }
    }

    public void SetLayoutVertical() {
        if (dontFitToSize) return;
        
        FitPopupSize(rectTransform, tagsLayoutGroup, nameText, bodyLayoutGroup);
        
        // Keep popup from going offscreen
        {
            float screenPadding = ItemDescPopup.screenPadding * (gameInstance?.CanvasScale ?? 1f);
            Rect worldRect = rectTransform.WorldRectIgnoreScale();
            float minY = worldRect.yMin;
            float maxY = worldRect.yMax;
            float minX = worldRect.xMin;
            float maxX = worldRect.xMax;
        
            bool offBottomOfScreen = minY < screenPadding;
            if (offBottomOfScreen) {
                float verticalCorrection = screenPadding - minY;
                rectTransform.position += new Vector3(0f, verticalCorrection, 0f);
            }
        
            bool offTopOfScreen = maxY > Screen.height - screenPadding;
            if (offTopOfScreen) {
                float verticalCorrection = (maxY - (Screen.height - screenPadding));
                rectTransform.position -= new Vector3(0f, verticalCorrection, 0f);
            }
            
            bool offLeftOfScreen = minX < screenPadding;
            if (offLeftOfScreen) {
                float horizontalCorrection = screenPadding - minX;
                rectTransform.position += new Vector3(horizontalCorrection, 0f, 0f);
            }
            
            bool offRightOfScreen = maxX > Screen.width - screenPadding;
            if (offRightOfScreen) {
                float horizontalCorrection = (maxX - (Screen.width - screenPadding));
                rectTransform.position -= new Vector3(horizontalCorrection, 0f, 0f);
            }
        }
    }
    
    public void SetLayoutHorizontal() { }
    
}
