using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "DynamicClip", menuName = "Scriptable Objects/DynamicClip")]
public class DynamicClip : ScriptableObject {

    public AudioClip[] clips;
    public AudioMixerGroup mixerGroup;
    public AudioRolloffMode rolloffMode;
    public float minDistance = 1f;
    public float maxDistance = 500f;
    public float minPitch = 1f;
    public float maxPitch = 1f;
    public int maxSimultaneous;
    // How far the sound pans toward the side it's on, 0 keeps it centered
    [Range(0f, 1f)] public float directionality;

    [Header("Local Area")]
    public float localAreaCooldownTime;
    public float localAreaDistance;
    
}
