using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VInspector;
using Yohash.PriorityQueue;
using Debug = UnityEngine.Debug;
using static WaveFunctionCollapseTileRuleset;

public class AutoTilePlacementTool : MonoBehaviour {

    public WaveFunctionCollapseTileRuleset wfcRuleset;
    public Tilemap tilemap;
    public Tilemap backgroundTilemap;
    public TileBase failedTile;
    public bool placeFailedTile;
    
    private class WaveTile : FastPriorityQueueNode {
        public bool collapsed;
        public Vector3Int cellPosition;
        public FixedBitSet256 states;
        public int collapsedIndex;
    }

    private List<WaveTile> waveTiles = new();
    private Dictionary<Vector3Int, WaveTile> waveTileLookup = new();
    private FastPriorityQueue<WaveTile> wfcPriorityQueue;

    [Button]
    private void GenerateBaseTiles() {
        backgroundTilemap.ClearAllTiles();
        InitalizeData(wfcRuleset.baseMapRuleset, out BoundsInt dims, out int initialBitArraySize);
        
        foreach (WaveTile wTile in waveTiles) {
            bool tileExistsAtPos = tilemap.GetTile(wTile.cellPosition);
            if (tileExistsAtPos) {
                wTile.states = new(initialBitArraySize, true);
                wTile.states.Clear(wfcRuleset.emptyStateIndex);
            }
            else {
                wTile.states = new(initialBitArraySize, false);
                wTile.states.Set(wfcRuleset.emptyStateIndex);
            }
        }
        
        Perform(wfcRuleset.baseMapRuleset);
        
        for (int x = dims.xMin; x < dims.xMax; x++) {
            for (int y = dims.yMin; y < dims.yMax; y++) {
                PlaceTile(tilemap, x, y);
            }
        }
    }
    
    [Button]
    private void GenerateFinal() {
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
            TileBase tile = tilemap.GetTile(wTile.cellPosition);
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
        
        Perform(wfcRuleset.superMapRuleset);
        
        for (int x = dims.xMin - tilemapExpansion; x < dims.xMax + tilemapExpansion; x++) {
            for (int y = dims.yMin - tilemapExpansion; y < dims.yMax + tilemapExpansion; y++) {
                if (tilemap.GetTile(new(x, y))) continue;
                PlaceTile(backgroundTilemap, x, y);
            }
        }
    }

    private void InitalizeData(Ruleset ruleset, out BoundsInt tilemapDimensions, out int initialBitArraySize, int tilemapExpansion = 0) {
        waveTiles.Clear();
        waveTileLookup.Clear();
        neighborPositions = new Vector3Int[4];
        wfcPriorityQueue = new(10000); // Chosen arbitrarily

        tilemap.CompressBounds();
        tilemapDimensions = tilemap.cellBounds;
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

    private void PlaceTile(Tilemap tilemap, int x, int y) {
        Vector3Int pos = new(x, y, 0);
        if (!waveTileLookup.TryGetValue(pos, out WaveTile tile)) return;
        if (tile.collapsedIndex == wfcRuleset.emptyStateIndex) return;
        
        string tileGuidAsString = wfcRuleset.idToGuid[tile.collapsedIndex];
        if (GUID.TryParse(tileGuidAsString, out GUID tileGuid)) {
            tilemap.SetTile(pos, tileGuid.LoadAsset<TileBase>());
        }
    }

    private void Perform(Ruleset ruleset) {
        foreach (WaveTile wTile in waveTiles) {
            wfcPriorityQueue.Enqueue(wTile, wTile.states.Count());
        }
        
        const int maxIterations = 10000;
        int curIteration = 0;
        
        while (wfcPriorityQueue.Count() > 0 && curIteration < maxIterations) {
            curIteration++;
            
            WaveTile collapsingWaveTile = wfcPriorityQueue.Dequeue();

            int randomID = collapsingWaveTile.states.RandomSetIndex();
            collapsingWaveTile.states.ClearAll();
            collapsingWaveTile.states.Set(randomID);
            collapsingWaveTile.collapsedIndex = randomID;
            collapsingWaveTile.collapsed = true;

            if (!PropagateWaveFromCollapsed(ruleset, collapsingWaveTile)) {
                Debug.Log("Wave function collapse failed");
                return;
            }
        }
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
                if (!waveTileLookup.TryGetValue(nPos, out WaveTile neighborTile)) continue;
                if (!CheckToReducePossibleStates(ruleset, cell, neighborTile, (Direction)i)) continue;
                if (neighborTile.states.Count() <= 0) {
                    if (placeFailedTile) {
                        tilemap.SetTile(nPos, failedTile);
                    }
                    return false;
                }
                wfcPriorityQueue.UpdatePriority(neighborTile, neighborTile.states.Count());
                propagationQueue.Enqueue(neighborTile);
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
            FixedBitSet256 possibleStates = direction switch {
                Direction.North => rule.northNeighbors,
                Direction.South => rule.southNeighbors,
                Direction.East => rule.eastNeighbors,
                Direction.West => rule.westNeighbors,
            };
            allowedBitSet.Or(possibleStates);
        }
        else {
            for (int i = 0; i < FixedBitSet256.bitCount; i++) {
                if (referenceWaveTile.states.IsSet(i)) {
                    TileRule rule = ruleset.rules[i];
                    
                    FixedBitSet256 possibleStates = direction switch {
                        Direction.North => rule.northNeighbors,
                        Direction.South => rule.southNeighbors,
                        Direction.East => rule.eastNeighbors,
                        Direction.West => rule.westNeighbors,
                    };
                    
                    allowedBitSet.Or(possibleStates);
                }
            }
        }
        
        updatingWaveTile.states.And(allowedBitSet);
        return prevStateCount != updatingWaveTile.states.Count();
    }

}