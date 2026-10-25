using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArtificialInventory", menuName = "Scriptable Objects/Artificial Inventory")]
public class ArtificialInventory : ScriptableObject {

    public int coinCurrency;
    public List<ItemWithCount> itemsWithCounts;
    
}