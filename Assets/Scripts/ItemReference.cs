using System;
using UnityEngine;

public class ItemReference : MonoBehaviour {

    public Item item;
    [NonSerialized] private int _dropCount;
    
    public int dropCount {
        get => _dropCount <= 0 ? 1 : _dropCount;
        set => _dropCount = value;
    }
}
