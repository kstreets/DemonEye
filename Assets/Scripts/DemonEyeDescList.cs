using UnityEngine;
using UnityEngine.UI;
using static Game;

public class DemonEyeDescList : MonoBehaviour {
    
    public VerticalLayoutGroup verticalLayout;
    public DemonEyeDescElement[] elements;
    
    public void UpdateDisplay(EyeUpgradeSet eyeUpgradeSet) {
        for (int i = 0; i < elements.Length; i++) {
            DemonEyeDescElement demonEyeDescElm = elements[i];
            
            if (!eyeUpgradeSet.elements.IndexInRange(i)) {
                demonEyeDescElm.gameObject.SetActive(false);
                continue;
            }
            
            demonEyeDescElm.gameObject.SetActive(true);
            demonEyeDescElm.UpdateDisplay(eyeUpgradeSet.elements[i]);
        }
    }
    
    public void HideAllElements() {
        foreach (DemonEyeDescElement demonEyeDescElm in elements) {
            demonEyeDescElm.gameObject.SetActive(false);
        }
    }

}
