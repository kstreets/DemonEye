using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Game;

public class Augment : UuidScriptableObject {

    public List<ModifierItem> derivedModifiers;

    public bool MeetsRequirements(List<int> uuids) {
        foreach (ModifierItem mod in derivedModifiers) {
            int countInUuids = uuids.Count(id => id == mod.uuid);
            int countInDerived = derivedModifiers.Count(m => m.uuid == mod.uuid);
            if (countInUuids < countInDerived) return false;
        }
        return true;    
    }

    public virtual void AddInstanceToEnemy(Enemy enemy) { }
    public virtual void AddInstanceToEye(DemonEyeInstance eyeInstance) { }
    
    public virtual string GetDescription() {
        return string.Empty;
    }

}