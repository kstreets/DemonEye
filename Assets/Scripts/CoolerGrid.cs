using System;
using System.Collections.Generic;
using StagPoint.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class CoolerGrid : MonoBehaviour {

    [Serializable]
    public class GridCell {
        public Vector2 position;
        public bool traversable;
    }

    [HideInInspector] public List<GridCell> cells;
    [HideInInspector] public List<Vector2> flowField;

    public int width = 100;
    public int height = 100;
    public float cellSize = 0.32f;

    private List<GridCell> spawnCells = new(30);
    private List<float> spawnCellWeights = new(30);
    private List<int> distances;
    private Vector2 gridGameObjectPosition;
    private float totalSpawnCellsWeight;
    private float lastUpdateTime;
    
    private JobHandle? flowFieldJobHandle;
    private NativeArray<int> nativeDistances;
    private NativeArray<bool> nativeTraversables; 
    private NativeArray<Vector2> nativePositions;
    private NativeArray<Vector2> flowFieldJobResults;

    public void Init() {
        gridGameObjectPosition = transform.position;
        flowField = new(100);
        nativeDistances = new(cells.Count, Allocator.Persistent);
        nativeTraversables = new(cells.Count, Allocator.Persistent);
        nativePositions = new(cells.Count, Allocator.Persistent);
        flowFieldJobResults = new(cells.Count, Allocator.Persistent);
    }

    public void Deinit() {
        flowFieldJobHandle?.Complete();
        nativeDistances.Dispose();
        nativeTraversables.Dispose();
        nativePositions.Dispose();
        flowFieldJobResults.Dispose();
    }

    public Vector3 GetSpawnPosition(Vector2 playerPosition) {
        bool needToRecalculate = lastUpdateTime != Time.time;
        lastUpdateTime = Time.time;

        if (needToRecalculate) {
            GridCell playerCell = GetCellAtPosition(playerPosition);
            if (playerCell == null) {
                return Vector3.zero;
            }

            UpdateDataForSpawnCells(playerCell, 12, 20);
        }

        Vector2 slightRandomOffset = Random.insideUnitCircle * (cellSize * 0.90f);

        float rand = Random.value * totalSpawnCellsWeight;
        for (int i = 0; i < spawnCells.Count; i++) {
            if (rand < spawnCellWeights[i]) {
                return spawnCells[i].position + slightRandomOffset;
            }

            rand -= spawnCellWeights[i];
        }

        return spawnCells[0].position + slightRandomOffset;
    }
    
    public void ScheduleFlowFieldCalculation(Vector2 sourcePosition) {
        GridCell sourceNode = GetCellAtPosition(sourcePosition);
        if (sourceNode == null) return;

        for (int i = 0; i < cells.Count; i++) {
            GridCell cell = cells[i];
            nativeDistances[i] = cell == sourceNode ? 0 : int.MaxValue;
            nativeTraversables[i] = cell.traversable;
            nativePositions[i] = cell.position;
            flowFieldJobResults[i] = Vector2.zero;
        }

        DijkstraJob dijkstraJob = new() {
            distances = nativeDistances,
            traversable = nativeTraversables,
            gridWitdh = width,
            gridHeight = height,
            startingIndex = cells.IndexOf(sourceNode),
        };

        FlowFieldJob flowFieldJob = new() {
            distances = nativeDistances,
            traversables = nativeTraversables,
            positions = nativePositions,
            results = flowFieldJobResults,
            gridWitdh = width,
            gridHeight = height,
        };

        JobHandle dijkstraJobHandle = dijkstraJob.Schedule();
        flowFieldJobHandle = flowFieldJob.Schedule(cells.Count, 128, dijkstraJobHandle);
    }
    
    public void CompleteFlowFieldCalculation() {
        if (!flowFieldJobHandle.HasValue) return;
        
        flowFieldJobHandle?.Complete();
        
        flowField.Clear();
        foreach (Vector2 result in flowFieldJobResults) {
            flowField.Add(result);
        }
    }

    public Vector2 GetFlowFieldDirection(Vector2 position) {
        GridCell cellAtPos = GetCellAtPosition(position);
        
        if (cellAtPos == null) {
            return Vector2.zero;
        }
        
        int cellIndex = cells.IndexOf(cellAtPos);
        return flowField[cellIndex];
    }

    private GridCell GetCellAtPosition(Vector2 position) {
        Vector2 posInGridSpace = position - gridGameObjectPosition;

        int x = Mathf.FloorToInt(posInGridSpace.x / cellSize);
        int y = Mathf.FloorToInt(posInGridSpace.y / cellSize);

        int index = y * width + x;
        if (cells.IndexInRange(index)) {
            return cells[index];
        }

        return null;
    }

    private void UpdateDataForSpawnCells(GridCell cell, int innerRadius, int outerRadius) {
        spawnCells.Clear();

        for (int y = -outerRadius; y <= outerRadius; y++) {
            for (int x = -outerRadius; x <= outerRadius; x++) {
                bool isCellWereWorkingOn = x == 0 && y == 0;
                bool isInsideInnerRadius = Mathf.Abs(x) <= innerRadius && Mathf.Abs(y) <= innerRadius;
                if (isCellWereWorkingOn || isInsideInnerRadius) continue;

                Vector2 neighborPos = cell.position + new Vector2(x, y) * cellSize;
                GridCell neighbor = GetCellAtPosition(neighborPos);
                if (neighbor != null && neighbor.traversable) {
                    spawnCells.Add(neighbor);
                }
            }
        }

        spawnCellWeights.Clear();
        totalSpawnCellsWeight = 0f;

        ContactFilter2D filter = new() {
            useLayerMask = true,
            layerMask = Masks.EnemyMask,
        };
        List<Collider2D> colList = UnityEngine.Pool.ListPool<Collider2D>.Get();

        float expandedSizeForEnemyTesting = cellSize * 5f;
        foreach (GridCell nCell in spawnCells) {
            int enemyCount = Physics2D.OverlapCircle(nCell.position, expandedSizeForEnemyTesting, filter, colList);
            float weight = 1f / ((enemyCount + 1f) * 3f);
            spawnCellWeights.Add(weight);
            totalSpawnCellsWeight += weight;
        }

        UnityEngine.Pool.ListPool<Collider2D>.Release(colList);
    }

    private Vector2 CalculateCellPosition(int widthIndex, int heightIndex) {
        Vector3 posOffset = new(cellSize / 2f, cellSize / 2f, 0f);
        Vector2 cellPos = transform.position + posOffset + new Vector3(widthIndex * cellSize, heightIndex * cellSize);
        return cellPos;
    }

    [BurstCompile]
    private struct DijkstraJob : IJob {
        public int startingIndex;
        public NativeArray<int> distances;
        [ReadOnly] public NativeArray<bool> traversable;
        public int gridWitdh;
        public int gridHeight;

        private struct QueueItem : IComparable<QueueItem>, IEquatable<QueueItem> {
            public int distance;
            public int indexIntoArrays;
            public int CompareTo(QueueItem other) => distance < other.distance ? -1 : 1;
            public bool Equals(QueueItem other) => indexIntoArrays == other.indexIntoArrays;
        }

        public void Execute() {
            NativeArray<bool> visited = new(distances.Length, Allocator.Temp);
            NativePriorityQueue<QueueItem> unvisited = new(distances.Length, Allocator.Temp);
            
            unvisited.Enqueue(new() {
                distance = distances[startingIndex],
                indexIntoArrays = startingIndex,
            });
            
            while (unvisited.Length > 0) {
                QueueItem curCell = unvisited.Dequeue();
                if (visited[curCell.indexIntoArrays]) continue;

                visited[curCell.indexIntoArrays] = true;

                for (int i = 0; i < 9; i++) {
                    int neighborIndex = GetNeighborIndex(gridWitdh, gridHeight, curCell.indexIntoArrays, i);
                    if (neighborIndex == -1 || !traversable[neighborIndex]) continue;
                    
                    bool neighborIsDiagonal = i == 0 || i == 2 || i == 6 || i == 8;
                    int dist = neighborIsDiagonal ? 10 : 7; // Approximate ratio
                        
                    int distFromCurToNeighbor = distances[curCell.indexIntoArrays] + dist;
                    if (distFromCurToNeighbor < distances[neighborIndex]) {
                        distances[neighborIndex] = distFromCurToNeighbor;

                        if (!visited[neighborIndex]) {
                            unvisited.Enqueue(new() {
                                distance = distFromCurToNeighbor,
                                indexIntoArrays = neighborIndex,
                            });
                        }
                        else {
                            QueueItem updatedNeighborItem = new() {
                                distance = distFromCurToNeighbor,
                                indexIntoArrays = neighborIndex,
                            };
                            unvisited.UpdateItem(updatedNeighborItem);
                        }
                    }
                }
            }
            
            unvisited.Dispose();
            visited.Dispose();
        }
    }

    [BurstCompile]
    private struct FlowFieldJob : IJobParallelFor {
        [ReadOnly] public NativeArray<int> distances;
        [ReadOnly] public NativeArray<bool> traversables;
        [ReadOnly] public NativeArray<Vector2> positions;
        [WriteOnly] public NativeArray<Vector2> results;
        public int gridWitdh;
        public int gridHeight;

        public void Execute(int index) {
            int smallestDist = int.MaxValue;
            int closestCellIndex = -1;
            
            for (int i = 0; i < 9; i++) {
                if (i == 4) continue;
                if (!traversables[index] && (i == 0 || i == 2 || i == 6 || i == 8)) continue;
                int neighborIndex = GetNeighborIndex(gridWitdh, gridHeight, index, i);
                
                if (neighborIndex == -1 || !traversables[neighborIndex]) continue;

                if (closestCellIndex == -1) {
                    smallestDist = distances[neighborIndex];
                    closestCellIndex = neighborIndex;
                    continue;
                }

                if (distances[neighborIndex] < smallestDist) {
                    smallestDist = distances[neighborIndex];
                    closestCellIndex = neighborIndex;
                }
            }
            
            if (closestCellIndex == -1) {
                results[index] = Vector2.zero;
                return;
            }

            if (!traversables[index]) {
                results[index] = (positions[closestCellIndex] - positions[index]).normalized;
                return;
            }

            Vector2 averagePosition = Vector2.zero;
            int averageCount = 0;
            for (int i = 0; i < 9; i++) {
                if (i == 4) continue;
                int neighborIndex = GetNeighborIndex(gridWitdh, gridHeight, index, i);

                if (neighborIndex == -1 || !traversables[neighborIndex]) continue;
                if (distances[neighborIndex] == smallestDist) {
                    averagePosition += positions[neighborIndex];
                    averageCount += 1;
                }
            }

            averagePosition /= averageCount;
            Vector2 finalDir = (averagePosition - positions[index]).normalized;

            if (Mathf.Approximately(finalDir.x, 0f) && Mathf.Approximately(finalDir.y, 0f)) {
                results[index] = (positions[closestCellIndex] - positions[index]).normalized;
            }
            else {
                results[index] = finalDir;
            }
        }
    }

    [BurstCompile]
    private static int GetNeighborIndex(int width, int height, int curIndex, int neighborId) {
        int row = curIndex / width;
        int col = curIndex % width;

        switch (neighborId) {
            case 0:  return (row > 0 && col > 0) ? curIndex - width - 1 : -1;                  // bottom-left
            case 1:  return (row > 0) ? curIndex - width : -1;                                 // bottom-center
            case 2:  return (row > 0 && col < width - 1) ? curIndex - width + 1 : -1;          // bottom-right
            case 3:  return (col > 0) ? curIndex - 1 : -1;                                     // middle-left
            case 4:  return -1;                                                                // the cell were finding the neighbors of
            case 5:  return (col < width - 1) ? curIndex + 1 : -1;                             // middle-right
            case 6:  return (row < height - 1 && col > 0) ? curIndex + width - 1 : -1;         // top-left
            case 7:  return (row < height - 1) ? curIndex + width : -1;                        // top-center
            case 8:  return (row < height - 1 && col < width - 1) ? curIndex + width + 1 : -1; // top-right
            default: return -1;
        }
    }

#if UNITY_EDITOR

    [VInspector.Button("Generate")]
    public void Generate() {
        const float traversableRatioPerTile = 0.8f;
        Vector2 traversableTestBoxSize = new Vector2(cellSize, cellSize) * traversableRatioPerTile;

        cells = new();
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector2 pos = CalculateCellPosition(x, y);
                
                bool traversable = true;
                if (Physics2D.OverlapBox(pos, traversableTestBoxSize, 0f, Masks.StaticLevelMask)) {
                    traversable = false;
                }

                cells.Add(new() {
                    position = pos,
                    traversable = traversable,
                });
            }
        }
        
        EditorUtility.SetDirty(this);
    }

    [VInspector.Button("Clear")]
    public void Clear() {
        cells = null;
        flowField = null;
        EditorUtility.SetDirty(this);
    }

    [VInspector.Button("Snap Position to Cell Factor")]
    private void SnapPositionToGrid() {
        float size = cellSize;
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x / size) * size;
        pos.y = Mathf.Round(pos.y / size) * size;
        pos.z = Mathf.Round(pos.z / size) * size;
        transform.position = pos;
        EditorUtility.SetDirty(this);
    }
    
    public Transform sourcePosForTesting;
    
    [VInspector.Button("Generate Flow Field")]
    private void GenerateFlowField() {
        if (sourcePosForTesting == null) {
            Debug.LogError("Must assign sourcePosForTesting");
            return;
        }
        
        if (cells == null) {
            Generate();
        }
        
        Init();
        ScheduleFlowFieldCalculation(sourcePosForTesting.position);
        CompleteFlowFieldCalculation();
        Deinit();
    }

    private List<GridCell> GetNeighborsSlow(GridCell cell) {
        List<GridCell> neighbors = new();

        for (int y = -1; y <= 1; y++) {
            for (int x = -1; x <= 1; x++) {
                bool isCellWereWorkingOn = x == 0 && y == 0;
                if (isCellWereWorkingOn) continue;

                Vector2 neighborPos = cell.position + new Vector2(x, y) * cellSize;
                GridCell neighbor = GetCellAtPosition(neighborPos);
                if (neighbor != null && neighbor.traversable) {
                    neighbors.Add(neighbor);
                }
            }
        }

        return neighbors;
    }
    
    public bool drawGizmos;
    
    private void OnDrawGizmos() {
        if (!drawGizmos) return;
        
        Color nonGeneratedColor = new(175f / 255f, 66f / 255f, 26f / 255f);
        Color gridColor = new(154f / 255f, 1f, 0f, 0.1f);
        Color gridFill = new(45f / 255f, 1f, 0f, 0.2f);
        
        if (flowField != null && cells != null && flowField.Count == cells.Count) {
            for (int i = 0; i < cells.Count; i++) {
                Vector2 flowDir = flowField[i];
                if (flowDir == Vector2.zero) continue;
                Vector2 cellPos = cells[i].position;
                DebugExtension.DrawArrow(cellPos, flowDir * 0.05f);
            }
            return;
        }
        
        if (cells == null) {
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    Bounds bounds = new() {
                        center = CalculateCellPosition(i, j),
                        size = Vector3.one * cellSize,
                    };
                    DebugExtension.DrawBounds(bounds, nonGeneratedColor);
                }
            }

            return;
        }
        
        foreach (GridCell cell in cells) {
            if (!cell.traversable) continue;
            
            Bounds bounds = new() {
                center = cell.position,
                size = Vector3.one * cellSize,
            };
            DebugExtension.DrawBounds(bounds, gridColor);
            Gizmos.color = gridFill;
            Gizmos.DrawCube(cell.position, Vector3.one * cellSize);
        }
    }

#endif 
    
}