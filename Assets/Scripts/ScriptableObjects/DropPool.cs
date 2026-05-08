using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DropPool", menuName = "Scriptable Objects/DropPool")]
public class DropPool : ScriptableObject {
    
    public bool reduceDuplicates;
    public bool isMapSpecific;
    
    [NonSerialized] public List<Item> items;
    [NonSerialized] public Item lastDroppedItem;
    
    public bool HasItems => items.Count > 0;
    
}