using UnityEngine;

[CreateAssetMenu(fileName = "PriceCategory", menuName = "Scriptable Objects/PriceCategory")]
public class PriceCategory : ScriptableObject {

    public int commonPrice;
    public int uncommonPrice;
    public int rarePrice;
    public int epicPrice;
    public int legendaryPrice;

}
