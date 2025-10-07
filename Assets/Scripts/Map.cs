using System;
using UnityEngine;

[Serializable]
public class Map : MonoBehaviour {

    public Transform spawnPositionsParent;
    public Transform resourceParent;
    public RaidSpawnPattern waves;
    public CoolerGrid grid;

}
