using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Game {
    
    private void InitResources() {
        LoadAllItems(); 
        LoadAllDropPools();
    }
    
    private void LoadAllItems() {
        UuidScriptableObject[] resourceObjects = Resources.LoadAll<UuidScriptableObject>(string.Empty);
        
        foreach (UuidScriptableObject resObject in resourceObjects) {
            res.lookup.Add(resObject.uuid, resObject);
            if (resObject is Item item) {
                res.items.Add(item);
            }
            if (resObject is Augment augment) {
                augment.CreateAugmentItemFromDerived();
                if (res.eyeUpgradeAugmentsLookup.TryGetValue(augment.derivedFrom, out var augmentList)) {
                    augmentList.Add(augment);
                }
                else {
                    res.eyeUpgradeAugmentsLookup.Add(augment.derivedFrom, new() { augment });
                }
            }
        }
    }
    
    private void LoadAllDropPools() {
        DropPool[] dropPoolSOs = Resources.LoadAll<DropPool>(string.Empty);
        
        foreach (DropPool dropPool in dropPoolSOs) {
            dropPool.items = new();
            res.dropPools.Add(dropPool);
            if (dropPool.isMapSpecific) {
                res.mapSpecificDropPools.Add(dropPool);
                continue;
            }
            res.globalDropPools.Add(dropPool);
        }
        
        foreach (Item item in res.items) {
            RegisterItemToDropPools(item, res.globalDropPools);
        }
    }
    
    private int GenerateNewItemUuid() {
        int newItemId = UuidScriptableObject.GetIntUuid();
        while (res.lookup.ContainsKey(newItemId)) {
            newItemId = UuidScriptableObject.GetIntUuid();
        }
        return newItemId; 
    }
    
}