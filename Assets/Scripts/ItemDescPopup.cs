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
    public ImageTextGroup augmentImageTextGroup;
    public DemonEyeDescList demonEyeDesc;
    
    public void Show(ItemInstance itemInstance, Vector2? position = default) {
        gameObject.SetActive(true);
        
        Item item = itemInstance.ItemRef;
        nameText.text = item.displayName;
        SetTags(itemInstance, item);
        SetMetaInfo(itemInstance, item);
        SetDescription(itemInstance, item);
        
        if (position.HasValue) {
            transform.position = position.Value;
            TweenPopUp(rectTransform);
        }
    }
    
    public void Hide() {
        nameText.text = string.Empty;
        descText.text = string.Empty;
        gameObject.SetActive(false);
    }

    private void SetTags(ItemInstance itemInstance, Item item) {
        Item.Rarity itemRarity = item.GetRarity();
        Color itemRarityColor = styles.GetColorForRarity(itemRarity);

        typeTagGroup.gameObject.SetActive(true);
        typeTagGroup.image.color = itemRarityColor;
        
        if (item.type == gameInstance.quickUseType) {
            typeTagGroup.textMesh.text = "Quick Use";
        } 
        else if (item.type == gameInstance.eyeModifierType) {
            typeTagGroup.textMesh.text = "Eye Modifier";
        }
        else if (item.type == gameInstance.wearableModifierType) {
            typeTagGroup.textMesh.text = "Wearable Modifier";
        }
        else if (item.type == gameInstance.backpackType) {
            typeTagGroup.textMesh.text = "Backpack";
        }
        else {
            typeTagGroup.gameObject.SetActive(false);
        }
        
        augmentedTagGroup.gameObject.SetActive(false);
        if (itemInstance.TryGetUuidObject(out var uuidObj) && uuidObj is Augment) {
            augmentedTagGroup.gameObject.SetActive(true);
            augmentedTagGroup.image.color = itemRarityColor;
        }
        
        rarityTagGroup.image.color = itemRarityColor;
        rarityTagGroup.textMesh.text = itemRarity.ToString();
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
        augmentImageTextGroup.gameObject.SetActive(false);
        
        descText.gameObject.SetActive(!itemInstance.isDemonEye);
        demonEyeDesc.gameObject.SetActive(itemInstance.isDemonEye);
        
        if (itemInstance.isDemonEye) {
            demonEyeDesc.UpdateDisplay(gameInstance.ConstructModifierSet(itemInstance.nestedUuids));
        }
        else {
            descText.text = item.GetDescription();
        }
        
        if (itemInstance.TryGetUuidObject(out var uuidObject) && uuidObject is Augment augment) {
            augmentImageTextGroup.gameObject.SetActive(true);
            augmentImageTextGroup.textMesh.text = augment.GetDescription();
        }
        
        if (item.type == gameInstance.quickUseType && !itemInstance.traderOwned) {
            descText.text += $"<line-height=150%>\n<sprite=5 color=#{ColorUtility.ToHtmlStringRGBA(styles.inputIconTint)}> " +
                             $"<size=80%>{ColorText("Right click to consume", styles.inputIconTint)}</size>";
        }
    }

    public void SetLayoutVertical() {
        FitPopupSize(rectTransform, tagsLayoutGroup, nameText, bodyLayoutGroup);
    }
    
    public void SetLayoutHorizontal() { }
    
}
