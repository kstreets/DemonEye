using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Tilemaps;
using VInspector;
using Random = UnityEngine.Random;
using Unity.Mathematics;
using Unity.VisualScripting;
using Yohash.PriorityQueue;
using Debug = UnityEngine.Debug;

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
    
    private Dictionary<Vector3Int, WaveTile> waveTileLookup = new();
    private FastPriorityQueue<WaveTile> priorityQueue;

    [Button]
    private void GenerateBaseTiles() {
        backgroundTilemap.ClearAllTiles();
        Perform(wfcRuleset.baseMapRuleset);
    }

    [Button]
    private void GenerateFinal() {
        backgroundTilemap.ClearAllTiles();
        backgroundTilemap.CompressBounds();
        
        waveTileLookup.Clear();
        neighborPositions = new Vector3Int[4];

        priorityQueue = new(10000); // Chosen arbitrarily
        
        List<WaveTile> waveTiles = new();

        tilemap.CompressBounds();
        BoundsInt dims = tilemap.cellBounds;
        int initialBitArraySize = wfcRuleset.idToGuid.Count;
        
        FixedBitSet256 allowedStates = new(initialBitArraySize, true);
        for (int i = 0; i < wfcRuleset.baseMapRuleset.rules.Length; i++) {
            if (wfcRuleset.baseMapRuleset.rules[i] != null) {
                allowedStates.Clear(i);
            }
        }
        
        // Initialize all the superpositions
        for (int x = dims.xMin - 10; x < dims.xMax + 10; x++) {
            for (int y = dims.yMin - 10; y < dims.yMax + 10; y++) {
                Vector3Int pos = new(x, y, 0);
                TileBase tile = tilemap.GetTile(pos);
                WaveTile wTile = new() { cellPosition = pos };
                
                if (tile) {
                    wTile.states = new(initialBitArraySize, false);
                    int index = wfcRuleset.idToGuid.FindIndex(x => x == tile.AssetGUID().ToString());
                    wTile.states.Set(index);
                }
                else {
                    wTile.states = new(allowedStates);
                    wTile.states.Clear(wfcRuleset.emptyStateIndex);
                }

                waveTiles.Add(wTile);
                waveTileLookup.Add(pos, wTile);
                priorityQueue.Enqueue(wTile, wTile.states.Count());
            }
        }
        
        const int maxIterations = 10000;
        int curIteration = 0;
        
        while (priorityQueue.Count() > 0 && curIteration < maxIterations) {
            curIteration++;
            
            WaveTile collapsingWaveTile = priorityQueue.Dequeue();

            int randomID = collapsingWaveTile.states.RandomSetIndex();
            collapsingWaveTile.states.ClearAll();
            collapsingWaveTile.states.Set(randomID);
            collapsingWaveTile.collapsedIndex = randomID;
            collapsingWaveTile.collapsed = true;

            // if (collapsingWaveTile.collapsedIndex != wfcRuleset.emptyStateIndex) {
            //     string tileGuidAsString = wfcRuleset.idToGuid[collapsingWaveTile.collapsedIndex];
            //     if (GUID.TryParse(tileGuidAsString, out GUID tileGuid)) {
            //         tilemap.SetTile(collapsingWaveTile.cellPosition, tileGuid.LoadAsset<TileBase>());
            //     }
            // }
            
            if (PropagateWaveFromCollapsed(wfcRuleset.superMapRuleset, collapsingWaveTile)) continue;
            
            // Mark the collapsed tile as problematic since its wave caused an invalid state of another tile
            // if (placeFailedTile) {
            //     tilemap.SetTile(collapsingWaveTile.cellPosition, failedTile);
            // }
            Debug.Log("Wave function collapse failed");
            return;
        }
        
        for (int x = dims.xMin - 9; x < dims.xMax + 9; x++) {
            for (int y = dims.yMin - 9; y < dims.yMax + 9; y++) {
                Vector3Int pos = new(x, y, 0);
                if (!waveTileLookup.TryGetValue(pos, out WaveTile tile)) continue;
                if (tile.collapsedIndex == wfcRuleset.emptyStateIndex) continue;
                
                string tileGuidAsString = wfcRuleset.idToGuid[tile.collapsedIndex];
                if (GUID.TryParse(tileGuidAsString, out GUID tileGuid)) {
                    if (tilemap.GetTile(pos)) continue;
                    // Settting it on a different tilemap
                    backgroundTilemap.SetTile(pos, tileGuid.LoadAsset<TileBase>());
                }
            }
        }
    }

    private void Perform(WaveFunctionCollapseTileRuleset.Ruleset ruleset) {
        waveTileLookup.Clear();
        neighborPositions = new Vector3Int[4];

        priorityQueue = new(10000); // Chosen arbitrarily
        
        List<WaveTile> waveTiles = new();

        tilemap.CompressBounds();
        BoundsInt dims = tilemap.cellBounds;
        int initialBitArraySize = ruleset.rules.Length;

        // Initialize all the superpositions
        for (int x = dims.xMin - 1; x < dims.xMax + 1; x++) {
            for (int y = dims.yMin - 1; y < dims.yMax + 1; y++) {
                Vector3Int pos = new(x, y, 0);
                TileBase tile = tilemap.GetTile(pos);
                WaveTile wTile = new() { cellPosition = pos };
                
                if (tile) {
                    wTile.states = new(initialBitArraySize, true);
                    wTile.states.Clear(wfcRuleset.emptyStateIndex);
                }
                else {
                    wTile.states = new(initialBitArraySize, false);
                    wTile.states.Set(wfcRuleset.emptyStateIndex);
                }

                waveTiles.Add(wTile);
                waveTileLookup.Add(pos, wTile);  
                priorityQueue.Enqueue(wTile, wTile.states.Count());
            }
        }
        
        const int maxIterations = 10000;
        int curIteration = 0;
        
        while (priorityQueue.Count() > 0 && curIteration < maxIterations) {
            curIteration++;
            
            WaveTile collapsingWaveTile = priorityQueue.Dequeue();

            int randomID = collapsingWaveTile.states.RandomSetIndex();
            collapsingWaveTile.states.ClearAll();
            collapsingWaveTile.states.Set(randomID);
            collapsingWaveTile.collapsedIndex = randomID;
            collapsingWaveTile.collapsed = true;

            if (PropagateWaveFromCollapsed(ruleset, collapsingWaveTile)) continue;
            
            // Mark the collapsed tile as problematic since its wave caused an invalid state of another tile
            // if (placeFailedTile) {
            //     tilemap.SetTile(collapsingWaveTile.cellPosition, failedTile);
            // }
            Debug.Log("Wave function collapse failed");
            return;
        }
        
        for (int x = dims.xMin; x < dims.xMax; x++) {
            for (int y = dims.yMin; y < dims.yMax; y++) {
                Vector3Int pos = new(x, y, 0);
                if (!waveTileLookup.TryGetValue(pos, out WaveTile tile)) continue;
                if (tile.collapsedIndex == wfcRuleset.emptyStateIndex) continue;
                
                string tileGuidAsString = wfcRuleset.idToGuid[tile.collapsedIndex];
                if (GUID.TryParse(tileGuidAsString, out GUID tileGuid)) {
                    tilemap.SetTile(pos, tileGuid.LoadAsset<TileBase>());
                }
            }
        }
    }

    private Queue<WaveTile> propagationQueue = new(1000);
    private Vector3Int[] neighborPositions;
    
    private bool PropagateWaveFromCollapsed(WaveFunctionCollapseTileRuleset.Ruleset ruleset, WaveTile collapsedWaveTile) {
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
                priorityQueue.UpdatePriority(neighborTile, neighborTile.states.Count());
                propagationQueue.Enqueue(neighborTile);
            }
            
        } 
        return true;
    }

    private enum Direction { North, South, East, West }
    private FixedBitSet256 allowedBitSet = new();
    
    private bool CheckToReducePossibleStates(WaveFunctionCollapseTileRuleset.Ruleset ruleset, WaveTile referenceWaveTile, WaveTile updatingWaveTile, Direction direction) {
        int prevStateCount = updatingWaveTile.states.Count();
        
        allowedBitSet.ClearAll();

        if (referenceWaveTile.collapsed) {
            WaveFunctionCollapseTileRuleset.TileRule rule = ruleset.rules[referenceWaveTile.collapsedIndex];
            FixedBitSet256 possibleStates = direction switch {
                Direction.North => rule.northNeighbors,
                Direction.South => rule.southNeighbors,
                Direction.East => rule.eastNeighbors,
                Direction.West => rule.westNeighbors,
            };
            allowedBitSet.Or(possibleStates);
        }
        else {
            for (int i = 0; i < 256; i++) {
                if (referenceWaveTile.states.IsSet(i)) {
                    WaveFunctionCollapseTileRuleset.TileRule rule = ruleset.rules[i];
                    
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