using UnityEngine;
using UnityEngine.UI;
using static Game;

public class DemonEyeDescPopup : MonoBehaviour, ILayoutElement {
    
    public VerticalLayoutGroup verticalLayout;
    public DemonEyeDescElement[] elements;
    
    public void UpdateDisplay(ModifierSet modifierSet) {
        for (int i = 0; i < elements.Length; i++) {
            DemonEyeDescElement demonEyeDescElm = elements[i];
            
            if (!modifierSet.elements.IndexInRange(i)) {
                demonEyeDescElm.gameObject.SetActive(false);
                continue;
            }
            
            demonEyeDescElm.gameObject.SetActive(true);
            demonEyeDescElm.UpdateDisplay(modifierSet.elements[i]);
        }
    }

    public void CalculateLayoutInputHorizontal() {
        
    }
    
    public void CalculateLayoutInputVertical() {
        preferredHeight = verticalLayout.preferredHeight;
        minHeight = preferredHeight;
    }
    
    public float minWidth { get; private set; }
    public float preferredWidth { get; private set; }
    public float flexibleWidth => 0f;
    
    public float minHeight { get; private set; }
    public float preferredHeight { get; private set; }
    public float flexibleHeight => 0f; 
    
    public int layoutPriority => 0;
}
