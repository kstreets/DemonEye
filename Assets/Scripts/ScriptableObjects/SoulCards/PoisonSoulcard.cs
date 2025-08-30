using UnityEngine;

public struct PoisonInstance {
    public float probability;
}

[CreateAssetMenu(fileName = "PoisonSoulcard", menuName = "Scriptable Objects/Soulcards/PoisonSoulcard")]
public class PoisonSoulcard : Soulcard {

    public float probability;

    public override void AddInstanceToEnemy(GameManager.Enemy enemy, int stackCount) {
        if (enemy.poisoned) return;
        
        float prob = GetProbability(stackCount);
        if (GameManager.RollProbability(prob)) {
            enemy.poisoned = true;
        }
    }
    
    public override void AddInstanceToEye(GameManager.DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.poison = new() {
            probability = GetProbability(stackCount),
        };
    }
    
    public override string GetStackDescription(int stackCount) {
        return $"{DisplayProbability(GetProbability(stackCount))} chance for a projectile to poison an enemy.";
    }

    private float GetProbability(int stackCount) {
        return probability * stackCount;
    }

}
