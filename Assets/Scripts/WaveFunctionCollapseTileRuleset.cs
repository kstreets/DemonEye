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
        public GUID tileId;
        public List<GUID> northNeighbors = new();
        public List<GUID> southNeighbors = new();
        public List<GUID> westNeighbors = new();
        public List<GUID> eastNeighbors = new();
    }
    
    public List<TileRule> rules;

    [Button]
    public void GenerateRuleset() {
        Tilemap tilemap = FindAnyObjectByType<Tilemap>();
        if (!tilemap) {
            Debug.Log("Could not find tilemap to operate on");
            return;
        }
        
        tilemap.CompressBounds();
        
        Undo.RecordObject(this, "Generation of Tile Rules");

        rules = new();
        
        BoundsInt dims = tilemap.cellBounds;
        
        for (int x = dims.xMin; x < dims.xMax; x++) {
            for (int y = dims.yMin; y < dims.yMax; y++) {
                TileBase tile = tilemap.GetTile(new(x, y, 0));
                
                GUID tileId = tile ? AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(tile)) : new();
                TileRule tileRule = rules.Find(e => e.tileId == tileId);

                if (tileRule == null) {
                    tileRule = new() { tileId = tileId };
                    rules.Add(tileRule);
                }
                
                UpdateTilesRule(tileRule, tilemap, new(x, y, 0));
            }
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
        
        List<List<GUID>> rulesNeighborLists = new() {
            rule.northNeighbors, rule.southNeighbors, rule.westNeighbors, rule.eastNeighbors,
        };

        for (int i = 0; i < 4; i++) {
            TileBase tile = neighbors[i]; 
            GUID tileId = tile ? AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(tile)) : new();
            
            List<GUID> ruleList = rulesNeighborLists[i];
            if (!ruleList.Contains(tileId)) ruleList.Add(tileId);
        }
    }

}
