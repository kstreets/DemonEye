using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RaidSpawnPattern", menuName = "Scriptable Objects/Raid Spawn Pattern")]
public class RaidSpawnPattern : ScriptableObject {

    [Serializable]
    public class EnemyBatch {
        public int enemyCount;
        public EnemyData enemyData;
    }

    [Serializable]
    public class SpawnPhase {
        public float phaseDuration;
        public float spawnDuration;
        public AnimationCurve spawnRateCurve;
        public List<EnemyBatch> enemyBatches;
    }
    
    public float timeBeforeFirstPhase;
    public List<SpawnPhase> spawnPhases;
    
}
