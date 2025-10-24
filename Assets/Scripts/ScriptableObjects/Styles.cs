using UnityEngine;

[CreateAssetMenu(fileName = "Styles", menuName = "Scriptable Objects/Styles")]
public class Styles : ScriptableObject {

    public Color commonTextColor;
    public Color uncommonTextColor;
    public Color rareTextColor;
    public Color legendaryTextColor;

    public int rarityFontSize;
    public Vector4 nonSelectedHideoutTabMargin;
    public Vector4 selectedHideoutTabMargin;

    public Vector4 normalButtonTextMargin;
    public Vector4 pressedButtonTextMargin;

    public Color nonSelectedTraderBackground;
    public Color selectedTraderBackground;
    public Color nonSelectedTraderHeadshotTint;

}
