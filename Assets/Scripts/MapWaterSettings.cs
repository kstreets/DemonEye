using UnityEngine;

[CreateAssetMenu(fileName = "MapWaterSettings", menuName = "Scriptable Objects/MapWaterSettings")]
public class MapWaterSettings : ScriptableObject {
    
    public float waveSpeed;
    public float waveStride;
    public float waveHeight;
    public int waterLineLength;
    
    public int reflectionLength;
    [Range(0f, 1f)]
    public float startReflectionFade;
    [Range(0f, 1f)]
    public float endReflectionFade;
    
}
