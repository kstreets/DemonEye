using UnityEngine;
using UnityEngine.UI;
using static Game;

public class DemonEyeDescList : MonoBehaviour {
    
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
    
    public void HideAllElements() {
        foreach (DemonEyeDescElement demonEyeDescElm in elements) {
            demonEyeDescElm.gameObject.SetActive(false);
        }
    }

}
