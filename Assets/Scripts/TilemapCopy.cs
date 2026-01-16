using UnityEngine;
using UnityEngine.Tilemaps;
using VInspector;

[RequireComponent(typeof(Tilemap))]
public class TilemapCopy : MonoBehaviour {

    public Tilemap baseTilemap;
    public Tilemap lavaTilemap;

    [Button]
    private void Copy() {
        Tilemap resultingTilemap = GetComponent<Tilemap>();
        
        BoundsInt bounds = lavaTilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin) {
            if (baseTilemap.GetTile(pos)) {
                resultingTilemap.SetTile(pos, baseTilemap.GetTile(pos));
            }
            else if (lavaTilemap.GetTile(pos)) {
                resultingTilemap.SetTile(pos, lavaTilemap.GetTile(pos));
            }
        }
    }

}
