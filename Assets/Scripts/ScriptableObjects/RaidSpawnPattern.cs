using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

[Serializable]
[CreateAssetMenu(fileName = "RaidSpawnPattern", menuName = "Scriptable Objects/Raid Spawn Pattern")]
public class RaidSpawnPattern : ScriptableObject {

    [Serializable]
    public class EnemyBatch {
        public int enemyCount;
        public EnemyData enemyData;
    }

    [Serializable]
    public class Variant {
        [HideInInspector] 
        public string name;
        public float phaseDuration;
        public float spawnDuration;
        [MinMaxSlider(0, 40)]
        public Vector2Int spawnCellRange; 
        [MinMaxSlider(0, 40)]
        public Vector2Int repositionCellRange; 
        public AnimationCurve spawnRateCurve;
        public List<EnemyBatch> enemyBatches;
    }
    
    [Serializable]
    public class PhasePool {
        [HideInInspector] 
        public string name;
        public List<Variant> variants = new();
    }
    
    public float timeBeforeFirstPhase;
    public float timeBeforePortalSpawns;
    public float delayBetweenEnemyRepositions;
    public int maxEnemyRepositionCount;
    public List<PhasePool> phasePools;
    
#if UNITY_EDITOR

    private void OnValidate() {
        if (phasePools == null || phasePools.Count <= 0) return;
        
        for (int i = 0; i < phasePools.Count; i++) {
            PhasePool pool = phasePools[i];
            pool.name = $"Wave {i}";
            for (int j = 0; j < pool.variants.Count; j++) {
                pool.variants[j].name = $"Variant {j}";
            }
        }
        
        foreach (Variant variant in phasePools[^1].variants) {
            variant.phaseDuration = variant.spawnDuration;
        }
    }

#endif
    
}
