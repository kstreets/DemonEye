using UnityEngine;

public static partial class Game {
    static void InitResources() {
        LoadAllItems(); 
        LoadAllDropPools();
    }

    static void LoadAllItems() {
        UuidScriptableObject[] resourceObjects = Resources.LoadAll<UuidScriptableObject>(string.Empty);
        
        foreach (UuidScriptableObject resObject in resourceObjects) {
            res.lookup.Add(resObject.uuid, resObject);
            res.takenUuids.Add(resObject.uuid);
            
            if (resObject is Item item) {
                res.items.Add(item);
            }
            if (resObject is Augment augment) {
                augment.CreateAugmentItemFromDerived();
            }
        }
    }

    static void LoadAllDropPools() {
        DropPool[] dropPoolSOs = Resources.LoadAll<DropPool>(string.Empty);
        
        foreach (DropPool dropPool in dropPoolSOs) {
            dropPool.Init();
            res.dropPools.Add(dropPool);
            if (dropPool.isMapSpecific) {
                res.mapSpecificDropPools.Add(dropPool);
                continue;
            }
            res.globalDropPools.Add(dropPool);
        }
        
        foreach (Item item in res.items) {
            RegisterItemToDropPools(item, res.globalDropPools);
            if (item.HasAugmentedVariants) {
                foreach (Augment augment in item.augmentedVariants) {
                    RegisterItemToDropPools(augment.augmentedEyeUpgrade, res.globalDropPools);
                }
            }
        }
    }

    static int GenerateNewItemUuid() {
        int newItemId = UuidScriptableObject.GetIntUuid();
        while (res.takenUuids.Contains(newItemId)) {
            newItemId = UuidScriptableObject.GetIntUuid();
        }
        res.takenUuids.Add(newItemId);
        return newItemId; 
    }
    
}