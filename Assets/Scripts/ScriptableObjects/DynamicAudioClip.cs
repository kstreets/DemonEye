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

    [Header("Local Area")]
    public float localAreaCooldownTime;
    public float localAreaDistance;
    
}
