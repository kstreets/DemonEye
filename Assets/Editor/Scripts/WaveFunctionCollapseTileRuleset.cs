using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "WaveFunctionCollapseTileRuleset", menuName = "Scriptable Objects/WaveFunctionCollapseTileRuleset")]
public class WaveFunctionCollapseTileRuleset : ScriptableObject {
    
    [Serializable]
    public class TileRule {
        public int tileIdOrIndex;
        public FixedBitSet256 northNeighbors;
        public FixedBitSet256 southNeighbors;
        public FixedBitSet256 westNeighbors;
        public FixedBitSet256 eastNeighbors;
    }

    [Serializable]
    public class Ruleset {
        public TileRule[] rules;
    }

    public Ruleset baseMapRuleset;
    public Ruleset superMapRuleset;
    public List<string> idToGuid = new();
    public int emptyStateIndex;
    
    [HideInInspector] public string generationMetaString;

    private Dictionary<GUID, TileRule> tileToRuleLookup = new();
    
    private static GUID emptyGUID => new();
    
    public void GenerateRuleset() {
        Tilemap[] allTilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        if (allTilemaps.Length != 2) {
            Debug.Log("Requires just 2 tilemaps in the scene");
            return;
        }
        
        (Tilemap baseTilemap, Tilemap lavaTilemap) = (allTilemaps[0], allTilemaps[1]);

        if (baseTilemap.TryGetComponent(out TilemapCollider2D _)) {
            (baseTilemap, lavaTilemap) = (lavaTilemap, baseTilemap);
        }

        Tilemap superTilemap = new GameObject().AddComponent<Tilemap>();
        CombineTilemaps(baseTilemap, lavaTilemap, superTilemap);
        
        Undo.RecordObject(this, "Generation of Tile Rules");

        baseTilemap.CompressBounds();
        superTilemap.CompressBounds();

        HashSet<GUID> baseTileAssets = new();
        foreach (Vector3Int pos in baseTilemap.cellBounds.allPositionsWithin) {
            TileBase tile = baseTilemap.GetTile(pos);
            baseTileAssets.Add(tile ? tile.AssetGUID() : emptyGUID);
        }
        
        InitializeDataFromHashSet(baseTileAssets);
        baseMapRuleset = GenerateMapRulesetForTilemap(baseTilemap);
        
        HashSet<GUID> bgTileAssets = new();
        foreach (Vector3Int pos in superTilemap.cellBounds.allPositionsWithin) {
            TileBase tile = superTilemap.GetTile(pos);
            bgTileAssets.Add(tile ? tile.AssetGUID() : emptyGUID);
        }
        
        AppendDataFromHashSet(bgTileAssets);
        
        superMapRuleset = GenerateMapRulesetForTilemap(superTilemap);
        generationMetaString = $"Generated from sample tilemap :: {DateTime.Now}";

        EditorUtility.SetDirty(this); 
        
        DestroyImmediate(superTilemap.gameObject);
    }

    private void CombineTilemaps(Tilemap baseTilemap, Tilemap lavaTilemap, Tilemap result) {
        BoundsInt bounds = lavaTilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin) {
            if (baseTilemap.GetTile(pos)) {
                result.SetTile(pos, baseTilemap.GetTile(pos));
            }
            else if (lavaTilemap.GetTile(pos)) {
                result.SetTile(pos, lavaTilemap.GetTile(pos));
            }
        }
    }

    private void InitializeDataFromHashSet(HashSet<GUID> uniqueTileAssets) {
        emptyStateIndex = -1;
        tileToRuleLookup = new();
        idToGuid = new();
        
        foreach (GUID tileGuid in uniqueTileAssets) {
            if (tileGuid.Empty()) {
                emptyStateIndex = idToGuid.Count;
            }
            
            TileRule rule = new() {
                tileIdOrIndex = idToGuid.Count,
                northNeighbors = new(),
                southNeighbors = new(),
                eastNeighbors = new(),
                westNeighbors = new(),
            };
            
            idToGuid.Add(tileGuid.ToString());
            tileToRuleLookup.Add(tileGuid, rule);
        }
        
        // We did not have an empty tile
        if (emptyStateIndex == -1) { 
            emptyStateIndex = idToGuid.Count;
            
            TileRule rule = new() {
                tileIdOrIndex = idToGuid.Count,
                northNeighbors = new(),
                southNeighbors = new(),
                eastNeighbors = new(),
                westNeighbors = new(),
            };
            
            idToGuid.Add(emptyGUID.ToString());
            tileToRuleLookup.Add(emptyGUID, rule);
        }
    }

    private void AppendDataFromHashSet(HashSet<GUID> uniqueTileAssets) {
        foreach (GUID tileGuid in uniqueTileAssets) {
            if (tileToRuleLookup.ContainsKey(tileGuid)) continue;
            
            if (tileGuid.Empty()) {
                emptyStateIndex = idToGuid.Count;
            }
            
            TileRule rule = new() {
                tileIdOrIndex = idToGuid.Count,
                northNeighbors = new(),
                southNeighbors = new(),
                eastNeighbors = new(),
                westNeighbors = new(),
            };
            
            idToGuid.Add(tileGuid.ToString());
            tileToRuleLookup.Add(tileGuid, rule);
        }
    }

    private Ruleset GenerateMapRulesetForTilemap(Tilemap tilemap) {
        Ruleset rSet = new();
        rSet.rules = new TileRule[idToGuid.Count];

        HashSet<TileRule> uniqueRules = new();

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin) {
            TileBase tile = tilemap.GetTile(pos);
            TileRule rule = tileToRuleLookup[tile.AssetGUID()];
            UpdateTilesRule(rule, tilemap, pos);
            uniqueRules.Add(rule);
        }
        
        foreach (TileRule rule in uniqueRules) {
            rSet.rules[rule.tileIdOrIndex] = rule;
        }

        return rSet;
    }
    
    private void UpdateTilesRule(TileRule rule, Tilemap tilemap, Vector3Int position) {
        TileBase northTile = tilemap.GetTile(new(position.x, position.y + 1, 0));
        TileBase southTile = tilemap.GetTile(new(position.x, position.y - 1, 0));
        TileBase westTile = tilemap.GetTile(new(position.x - 1, position.y, 0));
        TileBase eastTile = tilemap.GetTile(new(position.x + 1, position.y, 0));
        
        List<TileBase> neighbors = new() {
            northTile, southTile, westTile, eastTile,
        };
        
        List<FixedBitSet256> updatingNeighborStates = new() {
            rule.northNeighbors, rule.southNeighbors, rule.westNeighbors, rule.eastNeighbors,
        };

        for (int i = 0; i < neighbors.Count; i++) {
            TileBase tile = neighbors[i];
            int tileIndex = tileToRuleLookup[tile.AssetGUID()].tileIdOrIndex;
            
            FixedBitSet256 neighborStates = updatingNeighborStates[i];
            neighborStates.Set(tileIndex);
        }
    }
    
}

[CustomEditor(typeof(WaveFunctionCollapseTileRuleset))]
public class WaveFunctionCollapseTileRulesetInspector : Editor {
    
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Generate Ruleset")) {
            ((WaveFunctionCollapseTileRuleset)target).GenerateRuleset();
        }

        EditorGUILayout.LabelField(((WaveFunctionCollapseTileRuleset)target).generationMetaString, EditorStyles.helpBox);
    }
    
}