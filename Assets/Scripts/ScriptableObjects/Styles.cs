using UnityEngine;

[CreateAssetMenu(fileName = "Styles", menuName = "Scriptable Objects/Styles")]
public class Styles : ScriptableObject {
    
    public Color commonTextColor;
    public Color uncommonTextColor;
    public Color rareTextColor;
    public Color epicTextColor;
    public Color legendaryTextColor;
    
    public Color commonColor;
    public Color uncommonColor;
    public Color rareColor;
    public Color epicColor;
    public Color legendaryColor;

    public Color headerTextColor;
    public Color popInTextColor;
    
    public Color coinCurrencyColor;
    public Color inputIconTint;

    public Color underWeightColor;
    public Color startingOverWeightColor;
    public Color endingOverWeightColor;

    public Vector4 nonSelectedHideoutTabMargin;
    public Vector4 selectedHideoutTabMargin;

    public Vector4 normalButtonTextMargin;
    public Vector4 pressedButtonTextMargin;

    public Color nonSelectedTraderBackground;
    public Color selectedTraderBackground;
    public Color nonSelectedTraderHeadshotTint;

    public Color grayedOutItemTint;
    public Color grayedOutOverlay;
    public Color selectedUnderlay;
    public Color outOfStockCountColor;

    public Color normalDamageColor;
    public Color critDamageColor;
    public Color bleedDamageColor;
    public Color hemorrhageDamageColor;
    public Color poisonDamageColor;

    public Color increaseDescColor;
    public Color decreaseDescColor;
    public Color timeDescColor;

    public Color GetTextColorForRarity(Item.Rarity rarity) {
        return rarity switch {
            Item.Rarity.Common    => commonTextColor,
            Item.Rarity.Uncommon  => uncommonTextColor,
            Item.Rarity.Rare      => rareTextColor,
            Item.Rarity.Epic      => epicTextColor,
            Item.Rarity.Legendary => legendaryTextColor,
            _                     => commonTextColor,
        };
    }
    
    public Color GetColorForRarity(Item.Rarity rarity) {
        return rarity switch {
            Item.Rarity.Common    => commonColor,
            Item.Rarity.Uncommon  => uncommonColor,
            Item.Rarity.Rare      => rareColor,
            Item.Rarity.Epic      => epicColor,
            Item.Rarity.Legendary => legendaryColor,
            _                     => commonColor,
        };
    }

}
