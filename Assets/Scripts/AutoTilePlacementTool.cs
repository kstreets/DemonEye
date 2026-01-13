using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VInspector;
using Random = UnityEngine.Random;

public class AutoTilePlacementTool : MonoBehaviour {

    public WaveFunctionCollapseTileRuleset ruleset;
    public Tilemap tilemap;
    public TileBase failedTile;
    public bool placeFailedTile;
    
    private Dictionary<Vector3Int, WaveTile> waveTileLookup = new();
    private static GUID emptyGUID => new();

    private class WaveTile {
        public bool collapsed;
        public Vector3Int cellPosition;
        public List<string> states;
    }

    [Button]
    private void Generate() {
        waveTileLookup.Clear();
        List<WaveTile> waveTiles = new();

        List<string> allTileIds = new();
        allTileIds.AddRange(ruleset.rules.Select(rule => rule.tileGuid));

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
                    wTile.states.Remove(emptyGUID.ToString());
                }
                else {
                    wTile.states = new() { emptyGUID.ToString() };
                }

                waveTiles.Add(wTile);
                waveTileLookup.Add(pos, wTile);  
            }
        }
        
        const int maxIterations = 10000;
        int curIteration = 0;
        
        while (!WaveHasCollapsed(waveTiles) && curIteration < maxIterations) {
            curIteration++;
            
            WaveTile collapsingWaveTile = GetLowestEntropy(waveTiles);
            
            string randomID = collapsingWaveTile.states[Random.Range(0, collapsingWaveTile.states.Count)];
            collapsingWaveTile.states = new() { randomID };
            collapsingWaveTile.collapsed = true;

            if (PropagateWaveFromCollapsed(collapsingWaveTile)) continue;
            
            // Mark the collapsed tile as problematic since its wave caused an invalid state of another tile
            if (placeFailedTile) {
                tilemap.SetTile(collapsingWaveTile.cellPosition, failedTile);
            }
            Debug.Log("Wave function collapse failed");
            return;
        }
        
        for (int x = dims.xMin; x < dims.xMax; x++) {
            for (int y = dims.yMin; y < dims.yMax; y++) {
                Vector3Int pos = new(x, y, 0);
                if (!waveTileLookup.TryGetValue(pos, out WaveTile tile)) continue;
                if (tile.states[0] == emptyGUID.ToString()) continue;
                if (!GUID.TryParse(tile.states[0], out GUID tileGUID)) {
                    Debug.Log($"Failed to parse GUID: {tile.states[0]}");
                    continue;
                }
                tilemap.SetTile(pos, tileGUID.LoadAsset<TileBase>());
            }
        }
    }

    private bool WaveHasCollapsed(List<WaveTile> superPositions) {
        return superPositions.All(position => position.collapsed);
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

    private bool PropagateWaveFromCollapsed(WaveTile collapsedWaveTile) {
        Queue<WaveTile> queue = new();
        queue.Enqueue(collapsedWaveTile);

        while (queue.Count > 0) { 
            WaveTile cell = queue.Dequeue();
            
            Vector3Int northPos = new(cell.cellPosition.x, cell.cellPosition.y + 1, 0);
            Vector3Int southPos = new(cell.cellPosition.x, cell.cellPosition.y - 1, 0);
            Vector3Int westPos = new(cell.cellPosition.x - 1, cell.cellPosition.y, 0);
            Vector3Int eastPos = new(cell.cellPosition.x + 1, cell.cellPosition.y, 0);
            List<Vector3Int> neighborPositions = new() { northPos, southPos, westPos, eastPos, };
            
            for (int i = 0; i < neighborPositions.Count; i++) {
                Vector3Int nPos = neighborPositions[i];
                if (!waveTileLookup.TryGetValue(nPos, out WaveTile neighborTile)) continue;
                if (!CheckToReducePossibleStates(cell, neighborTile, (Direction)i)) continue;
                if (neighborTile.states.Count <= 0) return false;
                queue.Enqueue(neighborTile);
            }
        } 
        return true;
    }

    private enum Direction { North, South, West, East }

    private bool CheckToReducePossibleStates(WaveTile referenceWaveTile, WaveTile updatingWaveTile, Direction direction) {
        int prevStateCount = updatingWaveTile.states.Count;
        
        HashSet<string> allowed = new();
        
        foreach (string possibleState in referenceWaveTile.states) {
            WaveFunctionCollapseTileRuleset.TileRule rule = ruleset.rules.Find(e => e.tileGuid == possibleState);
            
            List<string> possibleStates = direction switch {
                Direction.North => rule.northNeighbors,
                Direction.East  => rule.eastNeighbors,
                Direction.South => rule.southNeighbors,
                Direction.West  => rule.westNeighbors,
            };
            
            if (possibleStates == null) continue;

            foreach (string state in possibleStates) {
                allowed.Add(state);
            }
        }

        for (int i = updatingWaveTile.states.Count - 1; i >= 0; i--) {
            if (!allowed.Contains(updatingWaveTile.states[i])) {
                updatingWaveTile.states.RemoveAt(i);
            }
        }
        
        return prevStateCount != updatingWaveTile.states.Count;
    }

}
