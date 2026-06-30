using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Yohash.PriorityQueue;
using static WaveFunctionCollapseTileRuleset;

public class AutoTilePlacementWindow : EditorWindow {
    
    public WaveFunctionCollapseTileRuleset wfcRuleset;
    public Tilemap baseTilemap;
    public Tilemap backgroundTilemap;
    public TileBase failedTile;
    public TileBase collisionTile;
    public bool placeFailedTile;
    public bool ignoreProblematicTiles;
    
    private class WaveTile : FastPriorityQueueNode {
        public bool collapsed;
        public Vector3Int cellPosition;
        public FixedBitSet256 states;
        public int collapsedIndex;
        public bool ignore;
    }

    private List<WaveTile> waveTiles = new();
    private Dictionary<Vector3Int, WaveTile> waveTileLookup = new();
    private FastPriorityQueue<WaveTile> wfcPriorityQueue;
    
    [MenuItem("Tools/AutoTilePlacement")]
    public static void ShowWindow() {
        AutoTilePlacementWindow wnd = GetWindow<AutoTilePlacementWindow>("Auto Tile Placement");
        wnd.minSize = new(300, 150);
    }

    private void OnGUI() {
        const float margin = 10;
        Rect paddedRect = new(
            margin,
            margin,
            position.width - margin * 2,
            position.height - margin * 2
        );

        GUILayout.BeginArea(paddedRect);
        
        EditorGUILayout.LabelField("Auto Tile Placement Tool", EditorStyles.largeLabel);
        EditorGUILayout.Space();
        
        wfcRuleset = EditorGUILayout.ObjectField("Ruleset", wfcRuleset, typeof(WaveFunctionCollapseTileRuleset), false) as WaveFunctionCollapseTileRuleset;
        baseTilemap = EditorGUILayout.ObjectField("Base Tilemap", baseTilemap, typeof(Tilemap), true) as Tilemap;
        backgroundTilemap = EditorGUILayout.ObjectField("Background Tilemap", backgroundTilemap, typeof(Tilemap), true) as Tilemap;
        
        EditorGUILayout.Space();
        
        placeFailedTile = EditorGUILayout.Toggle("Place Failed Tile", placeFailedTile);
        failedTile = EditorGUILayout.ObjectField("Failed Tile", failedTile, typeof(TileBase), true) as TileBase;
        
        collisionTile = EditorGUILayout.ObjectField("Collision Tile", collisionTile, typeof(TileBase), true) as TileBase;
        
        EditorGUILayout.Space();
        
        ignoreProblematicTiles = EditorGUILayout.Toggle("Ignore Problematic Tiles", ignoreProblematicTiles);
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Base Tiles", GUILayout.Height(30f))) {
            GenerateBaseTiles();
        }
        if (GUILayout.Button("Generate Collision Tiles", GUILayout.Height(30f))) {
            GenerateCollision();
        }
        
        GUILayout.EndArea();
    }
    
    private void GenerateBaseTiles() {
        backgroundTilemap.ClearAllTiles();
        InitalizeData(wfcRuleset.baseMapRuleset, out BoundsInt dims, out int initialBitArraySize);
        
        foreach (WaveTile wTile in waveTiles) {
            bool tileExistsAtPos = baseTilemap.GetTile(wTile.cellPosition);
            if (tileExistsAtPos) {
                wTile.states = new(initialBitArraySize, true);
                wTile.states.Clear(wfcRuleset.emptyStateIndex);
            }
            else {
                wTile.states = new(initialBitArraySize, false);
                wTile.states.Set(wfcRuleset.emptyStateIndex);
            }
        }
        
        bool success = Perform(wfcRuleset.baseMapRuleset);
        if (!success) return;
        
        Undo.RecordObject(baseTilemap, "BaseTilemap Auto Placement");
        
        for (int x = dims.xMin; x < dims.xMax; x++) {
            for (int y = dims.yMin; y < dims.yMax; y++) {
                PlaceTile(baseTilemap, x, y);
            }
        }
        
        EditorUtility.SetDirty(baseTilemap); 
    }
    
    private void GenerateCollision() {
        if (collisionTile == null) {
            Debug.Log("Must assign a collision tile");
            return;
        }
        
        Undo.RecordObject(backgroundTilemap, "BackgroundTilemap Collision Placement");
        
        const int tilemapExpansion = 5;
        BoundsInt dims = GetTilemapDimensions();
        
        for (int x = dims.xMin - tilemapExpansion; x < dims.xMax + tilemapExpansion; x++) {
            for (int y = dims.yMin - tilemapExpansion; y < dims.yMax + tilemapExpansion; y++) {
                if (baseTilemap.GetTile(new(x, y))) continue;
                backgroundTilemap.SetTile(new(x, y), collisionTile);
            }
        }
        
        EditorUtility.SetDirty(backgroundTilemap); 
    }
    
    // This was used when we were placing water tiles
    private void GenerateFinal() {
        Undo.RecordObject(baseTilemap, "BaseTilemap Final Auto Placement");
        Undo.RecordObject(backgroundTilemap, "BackgroundTilemap Final Auto Placement");
    
        backgroundTilemap.ClearAllTiles();
        backgroundTilemap.CompressBounds();

        const int tilemapExpansion = 9;
        InitalizeData(wfcRuleset.superMapRuleset, out BoundsInt dims, out int initialBitArraySize, tilemapExpansion);
        
        FixedBitSet256 allowedStates = new(initialBitArraySize, true);
        for (int i = 0; i < wfcRuleset.baseMapRuleset.rules.Length; i++) {
            if (wfcRuleset.baseMapRuleset.rules[i] != null) {
                allowedStates.Clear(i);
            }
        }
        
        foreach (WaveTile wTile in waveTiles) {
            TileBase tile = baseTilemap.GetTile(wTile.cellPosition);
            if (tile) {
                wTile.states = new(initialBitArraySize, false);
                int index = wfcRuleset.idToGuid.FindIndex(x => x == tile.AssetGUID().ToString());
                wTile.states.Set(index);
            }
            else {
                wTile.states = new(allowedStates);
                wTile.states.Clear(wfcRuleset.emptyStateIndex);
            }
        }
        
        bool success = Perform(wfcRuleset.superMapRuleset);
        if (!success) return;
        
        for (int x = dims.xMin - tilemapExpansion; x < dims.xMax + tilemapExpansion; x++) {
            for (int y = dims.yMin - tilemapExpansion; y < dims.yMax + tilemapExpansion; y++) {
                if (baseTilemap.GetTile(new(x, y))) continue;
                PlaceTile(backgroundTilemap, x, y);
            }
        }
        
        EditorUtility.SetDirty(baseTilemap); 
        EditorUtility.SetDirty(backgroundTilemap); 
    }

    private void InitalizeData(Ruleset ruleset, out BoundsInt tilemapDimensions, out int initialBitArraySize, int tilemapExpansion = 0) {
        waveTiles.Clear();
        waveTileLookup.Clear();
        neighborPositions = new Vector3Int[4];
        wfcPriorityQueue = new(100000); // Chosen arbitrarily

        tilemapDimensions = GetTilemapDimensions();
        initialBitArraySize = ruleset.rules.Length;

        int offset = tilemapExpansion + 1;
        for (int x = tilemapDimensions.xMin - offset; x < tilemapDimensions.xMax + offset; x++) {
            for (int y = tilemapDimensions.yMin - offset; y < tilemapDimensions.yMax + offset; y++) {
                Vector3Int pos = new(x, y, 0);
                WaveTile wTile = new() { cellPosition = pos };
                waveTiles.Add(wTile);
                waveTileLookup.Add(pos, wTile);
            }
        }
    }
    
    private BoundsInt GetTilemapDimensions() {
        baseTilemap.CompressBounds();
        return baseTilemap.cellBounds;
    }

    private void PlaceTile(Tilemap destinationTilemap, int x, int y) {
        Vector3Int pos = new(x, y, 0);
        if (!waveTileLookup.TryGetValue(pos, out WaveTile tile)) return;
        if (tile.collapsedIndex == wfcRuleset.emptyStateIndex) return;
        
        string tileGuidAsString = wfcRuleset.idToGuid[tile.collapsedIndex];
        if (GUID.TryParse(tileGuidAsString, out GUID tileGuid)) {
            destinationTilemap.SetTile(pos, tileGuid.LoadAsset<TileBase>());
        }
    }

    private bool Perform(Ruleset ruleset) {
        foreach (WaveTile wTile in waveTiles) {
            wfcPriorityQueue.Enqueue(wTile, wTile.states.Count());
        }
        
        const int maxIterations = 10000;
        int curIteration = 0;
        
        while (wfcPriorityQueue.Count > 0 && curIteration < maxIterations) {
            curIteration++;
            
            WaveTile collapsingWaveTile = wfcPriorityQueue.Dequeue();
            if (collapsingWaveTile.ignore) continue;

            int randomID = collapsingWaveTile.states.RandomSetIndex();
            collapsingWaveTile.states.ClearAll();
            collapsingWaveTile.states.Set(randomID);
            collapsingWaveTile.collapsedIndex = randomID;
            collapsingWaveTile.collapsed = true;
            
            bool propagationSuccess = PropagateWaveFromCollapsed(ruleset, collapsingWaveTile);

            if (!propagationSuccess && !ignoreProblematicTiles) {
                Debug.Log("Wave function collapse failed");
                return false;
            }
        }

        return true;
    }
    
    private Queue<WaveTile> propagationQueue = new(1000);
    private Vector3Int[] neighborPositions;
    
    private bool PropagateWaveFromCollapsed(Ruleset ruleset, WaveTile collapsedWaveTile) {
        propagationQueue.Clear();
        propagationQueue.Enqueue(collapsedWaveTile);

        while (propagationQueue.Count > 0) { 
            WaveTile cell = propagationQueue.Dequeue();
            
            Vector3Int northPos = new(cell.cellPosition.x, cell.cellPosition.y + 1, 0);
            Vector3Int southPos = new(cell.cellPosition.x, cell.cellPosition.y - 1, 0);
            Vector3Int eastPos = new(cell.cellPosition.x + 1, cell.cellPosition.y, 0);
            Vector3Int westPos = new(cell.cellPosition.x - 1, cell.cellPosition.y, 0);
            neighborPositions[0] = northPos;
            neighborPositions[1] = southPos;
            neighborPositions[2] = eastPos;
            neighborPositions[3] = westPos;
            
            for (int i = 0; i < neighborPositions.Length; i++) {
                Vector3Int nPos = neighborPositions[i];
                if (!waveTileLookup.TryGetValue(nPos, out WaveTile neighborTile) || neighborTile.ignore) continue;

                bool reducedStates = CheckToReducePossibleStates(ruleset, cell, neighborTile, (Direction)i);
                if (!reducedStates) continue;
                
                if (neighborTile.states.Count() <= 0) {
                    if (!ignoreProblematicTiles) {
                        if (placeFailedTile) {
                            baseTilemap.SetTile(nPos, failedTile);
                        }
                        return false;
                    }
                    
                    // Mark tile as one to ignore and continue
                    neighborTile.ignore = true;
                    if (placeFailedTile) {
                        baseTilemap.SetTile(nPos, failedTile);
                    }
                    Debug.Log($"Discovered problematic tile at {nPos}");
                }

                wfcPriorityQueue.UpdatePriority(neighborTile, neighborTile.states.Count());
                if (!neighborTile.ignore) {
                    propagationQueue.Enqueue(neighborTile);
                }
            }
            
        } 
        
        return true;
    }

    private enum Direction { North, South, East, West }
    private FixedBitSet256 allowedBitSet = new();
    
    private bool CheckToReducePossibleStates(Ruleset ruleset, WaveTile referenceWaveTile, WaveTile updatingWaveTile, Direction direction) {
        int prevStateCount = updatingWaveTile.states.Count();
        
        allowedBitSet.ClearAll();

        if (referenceWaveTile.collapsed) {
            TileRule rule = ruleset.rules[referenceWaveTile.collapsedIndex];
            FixedBitSet256 possibleStates = GetDirectionRules(rule, direction);
            allowedBitSet.Or(possibleStates);
        }
        else {
            for (int i = 0; i < FixedBitSet256.bitCount; i++) {
                if (referenceWaveTile.states.IsSet(i)) {
                    TileRule rule = ruleset.rules[i];
                    FixedBitSet256 possibleStates = GetDirectionRules(rule, direction);
                    allowedBitSet.Or(possibleStates);
                }
            }
        }
        
        updatingWaveTile.states.And(allowedBitSet);
        return prevStateCount != updatingWaveTile.states.Count();
    }

    private FixedBitSet256 GetDirectionRules(TileRule rule, Direction direction) {
        return direction switch {
            Direction.North => rule.northNeighbors,
            Direction.South => rule.southNeighbors,
            Direction.East  => rule.eastNeighbors,
            Direction.West  => rule.westNeighbors,
        };
    }

}
