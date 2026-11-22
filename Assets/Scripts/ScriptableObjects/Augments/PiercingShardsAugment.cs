using UnityEngine;
using static Game;

[CreateAssetMenu(menuName = "Scriptable Objects/Augments/PiercingShards")]
public class PiercingShardsAugment : Augment {

    public struct InstanceData {
        public int goThroughCount;
    }

    public int startingGoThroughCount;

    public override void AddInstanceToEye(DemonEyeInstance eyeInstance, int stackCount) {
        eyeInstance.piercingShardsAugment = new() {
            goThroughCount = GetGoThroughCount(stackCount),
        };
    }

    public override string GetDescription(int stackCount = 1) {
        int throughCount = GetGoThroughCount(stackCount);
        string enemies = throughCount == 1 ? "enemy" : "enemies";
        return $"Bone fragments pass through {DisplayNumber(throughCount)} more {enemies} before stopping";
    }

    int GetGoThroughCount(int stackCount) {
        return startingGoThroughCount + (stackCount - 1);
    }

}
