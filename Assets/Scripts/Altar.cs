using System;
using UnityEngine;

public class Altar : MonoBehaviour {
    
    public static readonly int bloodDrainAnimHash = Animator.StringToHash("BloodDrain");
    
    public Animator bloodPoolAnimator;
    public ParticleSystem bloodExplosionParticles;
    public BubbleSpawner bloodBubbles;
    public Transform soulSwirlSpawnPoint;
    
    [NonSerialized] public Item summoningItem;
    [NonSerialized] public bool used;
    [NonSerialized] public ItemDrop summonedItemDrop;
    
}
