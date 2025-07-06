using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSo", menuName = "Scriptable Objects/ItemData")]
public class ItemData : UuidScriptableObject {

    [SerializeField] public Sprite inventorySprite;

}
