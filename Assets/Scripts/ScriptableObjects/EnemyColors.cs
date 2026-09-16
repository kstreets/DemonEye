using UnityEngine;

[CreateAssetMenu(fileName = "EnemyColors", menuName = "Scriptable Objects/EnemyColors")]
public class EnemyColors : ScriptableObject {
    
    public Color enemyDissolveBloodColor; 
    public Color enemyDissolvePetrifyColor; 
    public Vector3 poisonHSV;
    public Vector3 petrifiedHSV;
    
}
