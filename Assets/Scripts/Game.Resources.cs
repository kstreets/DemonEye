using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Game {
    
    // public Dictionary<int, UuidScriptableObject> resourceLookup = new();
    // public Dictionary<EyeUpgradeItem, List<Augment>> augmentsPerModifierItemLookup = new();
    //
    // [NonSerialized] public List<Item> allItems = new();
    // [NonSerialized] public List<DropPool> allDropPools = new();
    
    // private void LoadAllResources() {
    //     LoadAllItems();
    //     LoadAllDropPools();
    // }
    //
    // private void LoadAllItems() {
    //      UuidScriptableObject[] resourceObjects = Resources.LoadAll<UuidScriptableObject>(string.Empty);
    //      foreach (UuidScriptableObject res in resourceObjects) {
    //          resourceLookup.Add(res.uuid, res);
    //          if (res is Item item) {
    //              allItems.Add(item);
    //          }
    //
    //          if (res is Augment augment) {
    //              augment.CreateAugmentItemFromDerived();
    //              if (augmentsPerModifierItemLookup.TryGetValue(augment.eyeUpgradeDerivedFrom, out var augmentList)) {
    //                  augmentList.Add(augment);
    //              }
    //              else {
    //                  augmentsPerModifierItemLookup.Add(augment.eyeUpgradeDerivedFrom, new() { augment });
    //              }
    //          }
    //      }
    // }
    //
    // private void LoadAllDropPools() {
    //     DropPool[] dropPoolSOs = Resources.LoadAll<DropPool>(string.Empty);
    //     foreach (DropPool dropPoolSO in dropPoolSOs) {
    //         allDropPools.Add(dropPoolSO);
    //     }
    // }
    
}