using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject {

    [Serializable]
    public class ItemDrop {
        public GameObject itemPrefab;
        public float dropChance;
    }
    
    public GameObject enemyPrefab;
    public ItemDrop[] itemDrops;
    public float speed;
    public int health;
    public int damage;

}
