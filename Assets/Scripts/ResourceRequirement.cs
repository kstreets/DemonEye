using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class ResourceRequirement : MonoBehaviour {

    public Image itemImage;
    public TextMeshProUGUI itemNameAndQuantityText;
    public Styles styles;

    public void Set(Item item, int requiredCount, int ownedCount) {
        itemImage.sprite = item.inventorySprite;
        Color textColor = ownedCount >= requiredCount ? styles.increaseDescColor : styles.decreaseDescColor;
        itemNameAndQuantityText.text = $"{item.displayName}\n{ColorText(ownedCount.ToString(), textColor)}/{requiredCount}";
    }

}
