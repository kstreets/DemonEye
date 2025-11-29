using UnityEngine;

[CreateAssetMenu(fileName = "MapReference", menuName = "Scriptable Objects/MapReference")]
public class MapData : ScriptableObject {

    public string displayName;
    public string sceneReference;
    public RaidSpawnPattern waves;

}
