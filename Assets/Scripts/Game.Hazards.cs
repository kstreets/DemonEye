using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Game {

    public class Hazard : Entity {
        public int damage;
        public LayerMask layerMask;
        public float tickInterval;
        public float nextTickTime;
        public float endTime;
    }

    private static void OnSpawnHazard(Hazard hazard) {
        hazard.damage = default;
        hazard.layerMask = default;
        hazard.tickInterval = default;
        hazard.nextTickTime = default;
        hazard.endTime = default;
    }

    private Hazard SpawnHazard(EntityPool<Hazard> pool, Vector2 spawnPos, int damage, LayerMask layerMask, float duration, float tickInterval, float spawnDelay = 0f) {
        Hazard hazard = SpawnEntity(pool, spawnPos, Quaternion.identity);
        Assert.IsNotNull(hazard.damageShape, "Hazards need a DamageShape to know what they're damaging");

        hazard.damage = damage;
        hazard.layerMask = layerMask;
        hazard.tickInterval = tickInterval;
        hazard.nextTickTime = Time.time + spawnDelay;
        hazard.endTime = Time.time + duration + spawnDelay;
        
        if (spawnDelay > 0f) {
            hazard.gameObject.SetActive(false);
            Delay(hazard, spawnDelay, FinalizeHazardSpawn);
        }
        else {
            FinalizeHazardSpawn(hazard);
        }

        return hazard;
    }
    
    private static void FinalizeHazardSpawn(Hazard hazard) {
        hazard.gameObject.SetActive(true);
        gameInstance.entities.hazards.Add(hazard);
    }

    private void UpdateHazards() {
        List<Hazard> hazards = entities.hazards;

        for (int i = hazards.Count - 1; i >= 0; i--) {
            Hazard hazard = hazards[i];

            if (Time.time >= hazard.endTime) {
                DestroyHazard(hazard);
                hazards.RemoveAt(i);
                continue;
            }

            if (Time.time >= hazard.nextTickTime) {
                hazard.nextTickTime += hazard.tickInterval;
                DamageEntitiesInShape(hazard, hazard.damageShape, hazard.damage, hazard.layerMask);
            }
        }
    }

    private void DestroyAllHazards() {
        List<Hazard> hazards = entities.hazards;
        for (int i = hazards.Count - 1; i >= 0; i--) {
            DestroyHazard(hazards[i]);
            hazards.RemoveAt(i);
        }
    }

    private void DestroyHazard(Hazard hazard) {
        const float despawnTime = 0.25f;
        Tween.Scale(hazard.trans, 0f, despawnTime, Ease.InBack);
        DestroyEntity(hazard, despawnTime);
    }

}
