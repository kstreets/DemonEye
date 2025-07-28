using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWaves", menuName = "Scriptable Objects/EnemyWaves")]
public class EnemyWaves : ScriptableObject {

    [Serializable]
    public class WaveData {
        public int enemyCount;
        public float waveDuration;
        public float spawnDuration;
        public AnimationCurve spawnRateCurve;
    }
    
    public float timeBeforeFirstWave;
    public List<WaveData> waves;
    
}
