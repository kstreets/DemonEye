using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class MapInstance : MonoBehaviour {

    public Transform spawnPositionsParent;
    public Transform resourceParent;
    public Transform exitPortalsParent;
    public TilemapRenderer mainTilemapRenderer;
    public CoolerGrid grid;

}
