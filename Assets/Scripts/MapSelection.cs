using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSelection : MonoBehaviour {
    
    public Image icon;
    public TextMeshProUGUI mapNameTextMesh;
    public TextMeshProUGUI subNameTextMesh;
    public GameObject iconFrame;
    public Sprite lockedIcon;
    public ButtonFeel selectionButton;
    
    public void SetState(MapData map) {
        mapNameTextMesh.text = map.displayName;
        
        selectionButton.gameObject.SetActive(map.state.isUnlocked);
        selectionButton.SetClickableState(map.state.isUnlocked);
        
        if (map.state.isUnlocked) {
            icon.sprite = map.icon;
            iconFrame.SetActive(true);
            return;
        }
        
        icon.sprite = lockedIcon;
        iconFrame.SetActive(false);
        subNameTextMesh.text = map.unlockRequirement; 
    }
    
}
