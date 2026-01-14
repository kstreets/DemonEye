using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VInspector;

[CreateAssetMenu(fileName = "WaveFunctionCollapseTileRuleset", menuName = "Scriptable Objects/WaveFunctionCollapseTileRuleset")]
public class WaveFunctionCollapseTileRuleset : ScriptableObject {
    
    [Serializable]
    public class TileRule {
        public int tileIndex;
        public List<int> northNeighbors = new();
        public List<int> southNeighbors = new();
        public List<int> westNeighbors = new();
        public List<int> eastNeighbors = new();
    }

    public List<TileRule> rules;
    public List<string> indexToGuid;
    public int emptyStateIndex;
    
    private Dictionary<GUID, TileRule> tileToRuleLookup;
    private static GUID emptyGUID => new();

    [Button]
    public void GenerateRuleset() {
        Tilemap tilemap = FindAnyObjectByType<Tilemap>();
        if (!tilemap) {
            Debug.Log("Could not find tilemap to operate on");
            return;
        }
        
        tilemap.CompressBounds();
        BoundsInt dims = tilemap.cellBounds;
        
        Undo.RecordObject(this, "Generation of Tile Rules");

        HashSet<GUID> uniqueTileAssets = new();
        foreach (Vector3Int pos in dims.allPositionsWithin) {
            TileBase tile = tilemap.GetTile(pos);
            uniqueTileAssets.Add(tile ? tile.AssetGUID() : emptyGUID);
        }

        rules = new();
        indexToGuid = new();
        tileToRuleLookup = new();
        foreach (GUID tileGuid in uniqueTileAssets) {
            if (tileGuid.Empty()) {
                emptyStateIndex = indexToGuid.Count;
            }
            TileRule rule = new() {
                tileIndex = indexToGuid.Count,
                northNeighbors = new(),
                southNeighbors = new(),
                eastNeighbors = new(),
                westNeighbors = new(),
            };
            rules.Add(rule);
            indexToGuid.Add(tileGuid.ToString());
            tileToRuleLookup.Add(tileGuid, rule);
        }
        
        foreach (Vector3Int pos in dims.allPositionsWithin) {
            TileBase tile = tilemap.GetTile(pos);
            TileRule rule = tileToRuleLookup[tile.AssetGUID()];
            UpdateTilesRule(rule, tilemap, pos);
        }
        
        EditorUtility.SetDirty(this); 
    }
    
    private void UpdateTilesRule(TileRule rule, Tilemap tilemap, Vector3Int position) {
        TileBase northTile = tilemap.GetTile(new(position.x, position.y + 1, 0));
        TileBase southTile = tilemap.GetTile(new(position.x, position.y - 1, 0));
        TileBase westTile = tilemap.GetTile(new(position.x - 1, position.y, 0));
        TileBase eastTile = tilemap.GetTile(new(position.x + 1, position.y, 0));
        
        List<TileBase> neighbors = new() {
            northTile, southTile, westTile, eastTile,
        };
        
        List<List<int>> rulesNeighborLists = new() {
            rule.northNeighbors, rule.southNeighbors, rule.westNeighbors, rule.eastNeighbors,
        };

        for (int i = 0; i < 4; i++) {
            TileBase tile = neighbors[i];
            int tileIndex = tileToRuleLookup[tile.AssetGUID()].tileIndex; 
            List<int> ruleList = rulesNeighborLists[i];
            if (!ruleList.Contains(tileIndex)) {
                ruleList.Add(tileIndex);
            }
        }
    }
    
}