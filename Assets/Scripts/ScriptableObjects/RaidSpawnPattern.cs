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
    public class SpawnPhase {
        public float phaseDuration;
        public float spawnDuration;
        [MinMaxSlider(0, 25)]
        public Vector2Int spawnCellRange; 
        [MinMaxSlider(0, 25)]
        public Vector2Int repositionCellRange; 
        public AnimationCurve spawnRateCurve;
        public List<EnemyBatch> enemyBatches;
    }
    
    public float timeBeforeFirstPhase;
    public float timeBeforePortalSpawns;
    public List<SpawnPhase> spawnPhases;
    
}
