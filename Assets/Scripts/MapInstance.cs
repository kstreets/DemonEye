using System;
using UnityEngine;

[Serializable]
public class MapInstance : MonoBehaviour {

    public Transform spawnPositionsParent;
    public Transform resourceParent;
    public Transform exitPortalsParent;
    public RaidSpawnPattern waves;
    public CoolerGrid grid;

}
