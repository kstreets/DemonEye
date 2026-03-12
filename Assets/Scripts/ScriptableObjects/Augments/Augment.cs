using UnityEngine;
using static Game;
using VInspector;

public class Augment : UuidScriptableObject {

    public ModifierItem modifierDerivedFrom;
    
    public virtual void AddInstanceToEnemy(Enemy enemy) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance) { }
    
    public virtual string GetDescription() {
        return string.Empty;
    }
    
    #if UNITY_EDITOR
    
    [Button]
    private void AddToStash() {
        if (!Application.isPlaying) {
            Debug.Log("Can only add to stash inventory when game is playing");
            return;
        }
        ItemInstance itemInstance = new() {
            itemOrInstanceUuid = uuid,
            nestedUuids = new() { uuid },
            count = 1,
        };
        inst.TryAddItemToInventory(inst.stashInventory, itemInstance);
    }
    
    #endif 

}
