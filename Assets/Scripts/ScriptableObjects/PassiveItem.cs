using UnityEngine;

[CreateAssetMenu(fileName = "PassiveItem", menuName = "Scriptable Objects/PassiveItem")]
public class PassiveItem : Item {
    
    public override string GetDescription() {
        return GetStackDescription(1);
    }

    public virtual string GetStackDescription(int stackCount) {
        return "Passive item has no description";
    }

}