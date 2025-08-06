using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWaves", menuName = "Scriptable Objects/EnemyWaves")]
public class EnemyWaves : ScriptableObject {

    [Serializable]
    public class UnitWave {
        public int enemyCount;
        public EnemyData enemyData;
    }

    [Serializable]
    public class WaveData {
        public float waveDuration;
        public float spawnDuration;
        public AnimationCurve spawnRateCurve;
        public List<UnitWave> waveUnits;
    }
    
    public float timeBeforeFirstWave;
    public List<WaveData> waves;
    
}
