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

}
