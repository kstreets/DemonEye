using UnityEngine;
using VInspector;

public class ResourceSpawnGroup : MonoBehaviour {

    [MinMaxSlider(0, 10)]
    public Vector2Int spawnCountRange = new(0, 1);

    public void RollChildren() {
        ResourceSpawn[] spawns = GetComponentsInChildren<ResourceSpawn>();
        foreach (ResourceSpawn spawn in spawns) {
            spawn.groupRoll = ResourceSpawn.GroupRoll.NotPicked;
        }

        for (int i = 0; i < spawnCountRange.y; i++) {
            ResourceSpawn picked = PickUnpickedSpawn(spawns, guaranteed: i < spawnCountRange.x);
            if (picked == null) break;
            picked.groupRoll = ResourceSpawn.GroupRoll.Picked;
        }
    }

    private static ResourceSpawn PickUnpickedSpawn(ResourceSpawn[] spawns, bool guaranteed) {
        float total = 0f;
        foreach (ResourceSpawn spawn in spawns) {
            if (!CanPick(spawn)) continue;
            total += Mathf.Min(spawn.TotalSpawnChance, 1f);
        }
        if (total <= 0f) return null;

        // Chances only get scaled down when they add up past 100%
        if (!guaranteed) {
            total = total < 1f ? 1f : total;
        }

        float prefixedSum = 0f;
        float random = Random.Range(0f, total);
        foreach (ResourceSpawn spawn in spawns) {
            if (!CanPick(spawn)) continue;
            prefixedSum += Mathf.Min(spawn.TotalSpawnChance, 1f);
            if (random < prefixedSum) {
                return spawn;
            }
        }

        return null;
    }

    private static bool CanPick(ResourceSpawn spawn) {
        return spawn.groupRoll == ResourceSpawn.GroupRoll.NotPicked && !spawn.IsBlockedByExitPortal;
    }

}
