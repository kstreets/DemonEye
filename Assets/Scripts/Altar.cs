using System;
using System.Collections.Generic;
using UnityEngine;

public class Altar : MonoBehaviour {
    
    public static readonly int bloodDrainAnimHash = Animator.StringToHash("BloodDrain");
    
    public Animator bloodPoolAnimator;
    public ParticleSystem bloodExplosionParticles;
    public List<Transform> bloodBubbleSpawns;
    public Transform soulSwirlSpawnPoint;
    
    [NonSerialized] public Item summoningItem;
    
}
