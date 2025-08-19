using System;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Scriptable Objects/ObjectPool")]
public class ObjectPool : ScriptableObject {
    
    public int initialCount;
    public GameObject prefab;
    [NonSerialized] public Queue<GameObject> availableQueue = new();

}