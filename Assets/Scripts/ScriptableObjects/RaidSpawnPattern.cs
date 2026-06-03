using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

[Serializable]
[CreateAssetMenu(fileName = "RaidSpawnPattern", menuName = "Scriptable Objects/Raid Spawn Pattern")]
public class RaidSpawnPattern : ScriptableObject {
    
    public enum WaveType { Normal, PhantomSwarm, }

    [Serializable]
    public class EnemyBatch {
        public int enemyCount;
        public EnemyData enemyData;
    }

    [Serializable]
    public class SpawnPhase {
        public WaveType waveTypeType;
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
        public List<SpawnPhase> spawnPhases;
    }
    
    public float timeBeforeFirstPhase;
    public float timeBeforePortalSpawns;
    public float delayBetweenEnemyRepositions;
    public int maxEnemyRepositionCount;
    public List<SpawnPhase> spawnPhases;
    public List<PhasePool> phasePools;
    
#if UNITY_EDITOR

    private void OnValidate() {
        if (spawnPhases == null || spawnPhases.Count <= 0) return;
        spawnPhases[^1].phaseDuration = spawnPhases[^1].spawnDuration;
    }

#endif
    
}
