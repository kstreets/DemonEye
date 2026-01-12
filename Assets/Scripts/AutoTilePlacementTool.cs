using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using VInspector;
using Random = UnityEngine.Random;

public class AutoTilePlacementTool : MonoBehaviour {

    public Tilemap tilemap;
    public WaveFunctionCollapseTileRuleset ruleset;
    private Dictionary<Vector3Int, TileSuperPosition> superPositionLookup = new();

    private class TileSuperPosition {
        public bool collapsed;
        public Vector3Int cellPosition;
        public List<GUID> states;
    }

    [Button]
    private void Generate() {
        superPositionLookup.Clear();
        
        BoundsInt dims = tilemap.cellBounds;
        Debug.Log(dims.size);

        List<TileSuperPosition> superPositions = new();

        List<GUID> allTileIds = new();
        allTileIds.AddRange(ruleset.rules.Select(rule => rule.tileId));

        // Initialize all the superpositions
        
        for (int x = dims.xMin - 1; x < dims.xMax + 1; x++) {
            for (int y = dims.yMin + 1; y < dims.yMax - 1; y++) {
                Vector3Int pos = new(x, y, 0);
                TileBase tile = tilemap.GetTile(pos);
                GUID tileId = tile ? AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(tile)) : new();
                
                TileSuperPosition superPosition = new() {
                    cellPosition = pos,
                    states = tileId == new GUID() ? new() { new() } : new(allTileIds),
                };

                superPositions.Add(superPosition);
                superPositionLookup.Add(pos, superPosition);  
            }
        }
        
        int maxIterations = 10000;
        int curIteration = 0;
        while (!WaveHasCollapsed(superPositions) && curIteration < maxIterations) {
            curIteration++;
            
            TileSuperPosition collapsingTile = GetLowestEntropy(superPositions);
            GUID randomID = collapsingTile.states[Random.Range(0, collapsingTile.states.Count)];
            collapsingTile.states = new() { randomID };
            collapsingTile.collapsed = true;
            
            UpdateNeighbors(collapsingTile);
        }
        
        
        for (int x = dims.xMin; x < dims.xMax; x++) {
            for (int y = dims.yMin; y < dims.yMax; y++) {
                Vector3Int pos = new(x, y, 0);
                if (superPositionLookup.TryGetValue(pos, out TileSuperPosition tile)) {
                    if (tile.states.Count <= 0) continue;
                    if (tile.states[0].Empty()) continue;
                    TileBase tileAsset = AssetDatabase.LoadAssetByGUID<TileBase>(tile.states[0]);
                    tilemap.SetTile(pos, tileAsset);
                }
            }
        }
    }

    private bool WaveHasCollapsed(List<TileSuperPosition> superPositions) {
        foreach (TileSuperPosition position in superPositions) {
            if (!position.collapsed) {
                return false;
            }
        }
        return true;
    }

    private TileSuperPosition GetLowestEntropy(List<TileSuperPosition> superPositions) {
        TileSuperPosition lowest = null;
        foreach (TileSuperPosition position in superPositions) {
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

    private void UpdateNeighbors(TileSuperPosition collapsedTile) {
        Queue<TileSuperPosition> queue = new();
        queue.Enqueue(collapsedTile);

        while (queue.Count > 0) {
            TileSuperPosition cell = queue.Dequeue();
            
            Vector3Int northPos = new(cell.cellPosition.x, cell.cellPosition.y + 1, 0);
            Vector3Int southPos = new(cell.cellPosition.x, cell.cellPosition.y - 1, 0);
            Vector3Int westPos = new(cell.cellPosition.x - 1, cell.cellPosition.y, 0);
            Vector3Int eastPos = new(cell.cellPosition.x + 1, cell.cellPosition.y, 0);
            List<Vector3Int> neighborPositions = new() { northPos, southPos, westPos, eastPos, };

            for (int i = 0; i < neighborPositions.Count; i++) {
                Vector3Int nPos = neighborPositions[i];
                if (superPositionLookup.TryGetValue(nPos, out TileSuperPosition neighborTile)) {
                    if (CheckToReducePossibleStates(cell, neighborTile, (Direction)i)) {
                        queue.Enqueue(neighborTile);
                    }
                }
            }
        } 
    }

    private enum Direction { North, South, West, East }

    private bool CheckToReducePossibleStates(TileSuperPosition referenceTile, TileSuperPosition updatingTile, Direction direction) {
        int prevStateCount = updatingTile.states.Count;
        
        HashSet<GUID> allowed = new();
        
        foreach (GUID possibleState in referenceTile.states) {
            WaveFunctionCollapseTileRuleset.TileRule rule = ruleset.rules.Find(e => e.tileId == possibleState);
            
            List<GUID> possibleStates = direction switch {
                Direction.North => rule.northNeighbors,
                Direction.East  => rule.eastNeighbors,
                Direction.South => rule.southNeighbors,
                Direction.West  => rule.westNeighbors,
            };
            
            if (possibleStates == null) continue;

            foreach (GUID state in possibleStates) {
                allowed.Add(state);
            }
        }

        for (int i = updatingTile.states.Count - 1; i >= 0; i--) {
            if (!allowed.Contains(updatingTile.states[i]))
                updatingTile.states.RemoveAt(i);
        }

        return prevStateCount != updatingTile.states.Count;
    }

}
