using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject {

    public GameObject enemyPrefab;
    public AnimatorOverrideController animatorOverride;
    public float speed;
    public int health;
    public int damage;
    public float changeToCauseBleed;
    public int soulWorthPerKill;

}
