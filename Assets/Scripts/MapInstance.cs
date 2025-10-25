using System;
using UnityEngine;

[Serializable]
public class MapInstance : MonoBehaviour {

    public Transform spawnPositionsParent;
    public Transform resourceParent;
    public RaidSpawnPattern waves;
    public CoolerGrid grid;

}
