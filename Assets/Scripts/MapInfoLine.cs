using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapInfoLine : MonoBehaviour {
    
    public Image image;
    public TextMeshProUGUI textMesh;
    
    public void Show(Sprite icon, string text) {
        image.sprite = icon;
        textMesh.text = text;
        gameObject.SetActive(true);
    }
    
}
