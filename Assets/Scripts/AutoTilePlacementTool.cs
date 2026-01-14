using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Tilemaps;
using VInspector;
using Random = UnityEngine.Random;

public class AutoTilePlacementTool : MonoBehaviour {

    public WaveFunctionCollapseTileRuleset ruleset;
    public Tilemap tilemap;
    public TileBase failedTile;
    public bool placeFailedTile;
    
    private Dictionary<Vector3Int, WaveTile> waveTileLookup = new();

    private class WaveTile {
        public bool collapsed;
        public Vector3Int cellPosition;
        public List<int> states;
    }

    [Button]
    private void Generate() {
        waveTileLookup.Clear();
        neighborPositions = new Vector3Int[4];
        
        List<WaveTile> waveTiles = new();

        List<int> allTileIds = new();
        allTileIds.AddRange(ruleset.rules.Select(rule => rule.tileIndex));
        
        BoundsInt dims = tilemap.cellBounds;
        
        // Initialize all the superpositions
        for (int x = dims.xMin - 1; x < dims.xMax + 1; x++) {
            for (int y = dims.yMin - 1; y < dims.yMax + 1; y++) {
                Vector3Int pos = new(x, y, 0);
                TileBase tile = tilemap.GetTile(pos);
                WaveTile wTile = new() { cellPosition = pos };
                
                if (tile) {
                    wTile.states = new(allTileIds);
                    // Remove the empty state for an existing tile so that WFC will not produce an empty tile
                    wTile.states.Remove(ruleset.emptyStateIndex);
                }
                else {
                    wTile.states = new() { ruleset.emptyStateIndex };
                }

                waveTiles.Add(wTile);
                waveTileLookup.Add(pos, wTile);  
            }
        }
        
        Profiler.BeginSample("AutoTile.WaveFunctionCollapse");
        
        const int maxIterations = 10000;
        int curIteration = 0;
        
        while (!WaveHasCollapsed(waveTiles) && curIteration < maxIterations) {
            curIteration++;
            
            Profiler.BeginSample("AutoTile.LowestEntropy");
            WaveTile collapsingWaveTile = GetLowestEntropy(waveTiles);
            Profiler.EndSample();
            
            int randomID = collapsingWaveTile.states[Random.Range(0, collapsingWaveTile.states.Count)];
            collapsingWaveTile.states.Clear();
            collapsingWaveTile.states.Add(randomID);
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
                if (tile.states[0] == ruleset.emptyStateIndex) continue;
                
                string tileGuidAsString = ruleset.indexToGuid[tile.states[0]];
                if (GUID.TryParse(tileGuidAsString, out GUID tileGuid)) {
                    tilemap.SetTile(pos, tileGuid.LoadAsset<TileBase>());
                }
            }
        }
    }

    private bool WaveHasCollapsed(List<WaveTile> superPositions) {
        foreach (WaveTile wTile in superPositions) {
            if (!wTile.collapsed) {
                return false;
            }
        }
        return true;
    }

    private WaveTile GetLowestEntropy(List<WaveTile> superPositions) {
        WaveTile lowest = null;
        foreach (WaveTile position in superPositions) {
            if (position.collapsed) continue;
            
            if (lowest == null) {
                lowest = position;
                continue;
            }
            
            if (position.states.Count < lowest.states.Count) {
                lowest = position;
            }
        }
        return lowest;
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
                if (neighborTile.states.Count <= 0) {
                    return false;
                }
                propagationQueue.Enqueue(neighborTile);
            }
        } 
        return true;
    }

    private enum Direction { North, South, West, East }
    private HashSet<int> allowedPossibleStates = new(1000);
    
    private bool CheckToReducePossibleStates(WaveTile referenceWaveTile, WaveTile updatingWaveTile, Direction direction) {
        allowedPossibleStates.Clear();
        int prevStateCount = updatingWaveTile.states.Count;
        
        foreach (int possibleState in referenceWaveTile.states) {
            WaveFunctionCollapseTileRuleset.TileRule rule = ruleset.rules[possibleState];
            
            List<int> possibleStates = direction switch {
                Direction.North => rule.northNeighbors,
                Direction.East  => rule.eastNeighbors,
                Direction.South => rule.southNeighbors,
                Direction.West  => rule.westNeighbors,
            };
            
            if (possibleStates == null) continue;

            foreach (int state in possibleStates) {
                allowedPossibleStates.Add(state);
            }
        }

        for (int i = updatingWaveTile.states.Count - 1; i >= 0; i--) {
            if (!allowedPossibleStates.Contains(updatingWaveTile.states[i])) {
                updatingWaveTile.states.RemoveAt(i);
            }
        }
        
        return prevStateCount != updatingWaveTile.states.Count;
    }

}
