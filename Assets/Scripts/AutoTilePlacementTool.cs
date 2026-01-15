using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
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

public class AutoTilePlacementTool : MonoBehaviour {

    public WaveFunctionCollapseTileRuleset ruleset;
    public Tilemap tilemap;
    public TileBase failedTile;
    public bool placeFailedTile;
    
    private Dictionary<Vector3Int, WaveTile> waveTileLookup = new();
    private FastPriorityQueue<WaveTile> priorityQueue;

    private class WaveTile : FastPriorityQueueNode {
        public bool collapsed;
        public Vector3Int cellPosition;
        public FixedBitSet256 states;
        public int collapsedIndex;
    }

    [Button]
    private void Generate() {
        waveTileLookup.Clear();
        neighborPositions = new Vector3Int[4];

        priorityQueue = new(2000); // Chosen arbitrarily
        
        List<WaveTile> waveTiles = new();

        List<int> allTileIds = new();
        allTileIds.AddRange(ruleset.rules.Select(rule => rule.tileIndex));
        
        BoundsInt dims = tilemap.cellBounds;
        int initialBitArraySize = ruleset.rules.Count;

        // Initialize all the superpositions
        for (int x = dims.xMin - 1; x < dims.xMax + 1; x++) {
            for (int y = dims.yMin - 1; y < dims.yMax + 1; y++) {
                Vector3Int pos = new(x, y, 0);
                TileBase tile = tilemap.GetTile(pos);
                WaveTile wTile = new() { cellPosition = pos };
                
                if (tile) {
                    wTile.states = new(initialBitArraySize, true);
                    wTile.states.Clear(ruleset.emptyStateIndex);
                }
                else {
                    wTile.states = new(initialBitArraySize, false);
                    wTile.states.Set(ruleset.emptyStateIndex);
                }

                waveTiles.Add(wTile);
                waveTileLookup.Add(pos, wTile);  
                priorityQueue.Enqueue(wTile, wTile.states.Count());
            }
        }
        
        Profiler.BeginSample("AutoTile.WaveFunctionCollapse");
        
        const int maxIterations = 10000;
        int curIteration = 0;
        
        while (priorityQueue.Count() > 0 && curIteration < maxIterations) {
            curIteration++;
            
            Profiler.BeginSample("AutoTile.LowestEntropy");
            WaveTile collapsingWaveTile = priorityQueue.Dequeue();
            Profiler.EndSample();

            int randomID = collapsingWaveTile.states.RandomSetIndex();
            collapsingWaveTile.states.ClearAll();
            collapsingWaveTile.states.Set(randomID);
            collapsingWaveTile.collapsedIndex = randomID;
            collapsingWaveTile.collapsed = true;

            if (PropagateWaveFromCollapsed(collapsingWaveTile)) continue;
            
            // Mark the collapsed tile as problematic since its wave caused an invalid state of another tile
            if (placeFailedTile) {
                tilemap.SetTile(collapsingWaveTile.cellPosition, failedTile);
            }
            Debug.Log("Wave function collapse failed");
            return;
        }
        
        Profiler.EndSample();
        
        for (int x = dims.xMin; x < dims.xMax; x++) {
            for (int y = dims.yMin; y < dims.yMax; y++) {
                Vector3Int pos = new(x, y, 0);
                if (!waveTileLookup.TryGetValue(pos, out WaveTile tile)) continue;
                if (tile.collapsedIndex == ruleset.emptyStateIndex) continue;
                
                string tileGuidAsString = ruleset.indexToGuid[tile.collapsedIndex];
                if (GUID.TryParse(tileGuidAsString, out GUID tileGuid)) {
                    tilemap.SetTile(pos, tileGuid.LoadAsset<TileBase>());
                }
            }
        }
    }

    private Queue<WaveTile> propagationQueue = new(1000);
    private Vector3Int[] neighborPositions;
    
    private bool PropagateWaveFromCollapsed(WaveTile collapsedWaveTile) {
        propagationQueue.Clear();
        propagationQueue.Enqueue(collapsedWaveTile);

        while (propagationQueue.Count > 0) { 
            WaveTile cell = propagationQueue.Dequeue();
            
            Vector3Int northPos = new(cell.cellPosition.x, cell.cellPosition.y + 1, 0);
            Vector3Int southPos = new(cell.cellPosition.x, cell.cellPosition.y - 1, 0);
            Vector3Int westPos = new(cell.cellPosition.x - 1, cell.cellPosition.y, 0);
            Vector3Int eastPos = new(cell.cellPosition.x + 1, cell.cellPosition.y, 0);
            neighborPositions[0] = northPos;
            neighborPositions[1] = southPos;
            neighborPositions[2] = westPos;
            neighborPositions[3] = eastPos;
            
            for (int i = 0; i < neighborPositions.Length; i++) {
                Vector3Int nPos = neighborPositions[i];
                if (!waveTileLookup.TryGetValue(nPos, out WaveTile neighborTile)) continue;
                if (!CheckToReducePossibleStates(cell, neighborTile, (Direction)i)) continue;
                if (neighborTile.states.Count() <= 0) {
                    return false;
                }
                priorityQueue.UpdatePriority(neighborTile, neighborTile.states.Count());
                propagationQueue.Enqueue(neighborTile);
            }
        } 
        return true;
    }

    private enum Direction { North, South, West, East }
    private FixedBitSet256 allowedBitSet = new();
    
    private bool CheckToReducePossibleStates(WaveTile referenceWaveTile, WaveTile updatingWaveTile, Direction direction) {
        int prevStateCount = updatingWaveTile.states.Count();
        
        allowedBitSet.ClearAll();
        
        for (int i = 0; i < 256; i++) {
            if (referenceWaveTile.states.IsSet(i)) {
                WaveFunctionCollapseTileRuleset.TileRule rule = ruleset.rules[i];
                
                FixedBitSet256 possibleStates = direction switch {
                    Direction.North => rule.northNeighbors,
                    Direction.East  => rule.eastNeighbors,
                    Direction.South => rule.southNeighbors,
                    Direction.West  => rule.westNeighbors,
                };
                
                allowedBitSet.Or(possibleStates);
            }
        }
        
        updatingWaveTile.states.And(allowedBitSet);
        return prevStateCount != updatingWaveTile.states.Count();
    }

}