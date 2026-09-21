using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Game;

public class ResourceRequirement : MonoBehaviour {

    public Image itemImage;
    public TextMeshProUGUI itemNameAndQuantityText;
    public Styles styles;
    
    public enum TextOption { IncludeItemName, ExcludeItemName }

    public void Set(Item item, int requiredCount, int ownedCount, TextOption option) {
        itemImage.sprite = item.inventorySprite;
        Color textColor = ownedCount >= requiredCount ? styles.increaseDescColor : styles.decreaseDescColor;
        if (option == TextOption.IncludeItemName) {
            itemNameAndQuantityText.text = $"{item.displayName}\n{ColorText(ownedCount.ToString(), textColor)}/{requiredCount}";
            return;
        }
        itemNameAndQuantityText.text = $"{ColorText(ownedCount.ToString(), textColor)}/{requiredCount}";
    }

}
