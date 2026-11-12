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
        public float maxDuration;
        public float spawnDuration;
        public float startNextPhaseEarlyDelay;
        [MinMaxSlider(0, 40)]
        public Vector2Int spawnCellRange; 
        [MinMaxSlider(0, 40)]
        public Vector2Int repositionCellRange;
        public int reteleportCount = 2;
        public float reteleportInterval = 1f;
        public AnimationCurve spawnRateCurve;
        public List<EnemyBatch> enemyBatches;
    }
    
    [Serializable]
    public class Phase {
        [HideInInspector] public string name;
        public Game.MusicIntensity musicIntensity;
    }
    
    [Serializable]
    public class PhasePool : Phase {
        public List<Variant> variants = new();
    }
    
    [Serializable]
    public class BossPhase : PhasePool {
        [Range(0f, 1f)]
        public float trashRefillAtPercentage;
        public float minTrashRefillDelay;
        public EnemyData raidBoss;
    }
    
    [Serializable]
    public class Swarm {
        [Range(0f, 1f)]
        public float chance;
        [MinMaxSlider(1, 20)]
        public Vector2Int waveRange = new(1, 1);
        public PhasePool pool;
    }

    public float timeBeforeFirstPhase;
    public float timeBeforePortalSpawns;
    public List<PhasePool> phasePools;
    public BossPhase bossPhase;
    public Swarm swarm;
    
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
        
        if (swarm.pool != null) {
            swarm.pool.name = "Swarm";
            int maxSwarmWave = bossPhase.raidBoss != null ? phasePools.Count - 1 : phasePools.Count;
            if (swarm.waveRange.y > maxSwarmWave) {
                swarm.waveRange.Clamp(Vector2Int.one, new(maxSwarmWave, maxSwarmWave));
            }
            for (int j = 0; j < swarm.pool.variants.Count; j++) {
                swarm.pool.variants[j].name = $"Variant {j}";
            }
        }
        
        
        bossPhase.name = "Boss Wave";
        for (int j = 0; j < bossPhase.variants.Count; j++) {
            bossPhase.variants[j].name = $"Variant {j}";
        }

        // Nothing comes after the final wave so its max duration is unused
        if (bossPhase.raidBoss == null) {
            foreach (Variant variant in phasePools[^1].variants) {
                variant.maxDuration = variant.spawnDuration;
            }
        }
    }

#endif
    
}
