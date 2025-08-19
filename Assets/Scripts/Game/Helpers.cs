using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public partial class GameManager {

    private bool InRaid => gameStateMachine.CurState == raidState;
    
    private bool RollProbability(float probability) {
        return Random.value < probability;
    }
    
    private Vector3 RandomOffset360(float minDist, float maxDist) {
        return Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward) * Vector3.right * Random.Range(minDist, maxDist);
    }
    
    private void SaveToFile(string path, object obj) {
        BinaryFormatter bf = new();
        using FileStream file = File.Create(path);
        bf.Serialize(file, obj);
    }

    private T LoadFromFile<T>(string path) where T : class {
        if (File.Exists(path)) {
            BinaryFormatter bf = new();
            using FileStream file = File.Open(path, FileMode.Open);
            return (T)bf.Deserialize(file);
        }
        return null;
    }

}
