using System;
using System.Collections.Generic;
using System.Linq;
using StagPoint.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class CoolerGrid : MonoBehaviour {

    [Serializable]
    public class PrecomputedCellData {
        public Vector2 position;
        public bool traversable;
    }

    [HideInInspector] public List<PrecomputedCellData> precomputedCells;

    public int width = 100;
    public int height = 100;
    public float cellSize = 0.32f;

    // Approximate ratio between and straight diagonal 
    private const int dijkstraOrthogonalDist = 7;
    private const int dijkstraDiagonalDist = 10;

    private List<GridCell> spawnCells = new(30);
    private Vector2 gridGameObjectPosition;
    private Vector2 predicetedPlayerPos;
    private float totalSpawnCellsWeight;
    private float lastUpdateTime;
    
    private JobHandle? flowFieldJobHandle;
    private NativeArray<int> nativeDistances;
    private NativeArray<bool> nativeTraversables; 
    private NativeArray<Vector2> nativePositions;
    private NativeArray<Vector2> flowFieldJobResults;

    private struct GridCell {
        public Vector2 position;
        public bool traversable;
        
        // These values get updated during runtime
        public Vector2 flowDir;
        public int distFromPlayerCell;
        public float spawnWeight;
        public bool isObstacleObstructed;
    }
    
    // Much faster to read/write because of memory locality
    private GridCell[] gridCells; 

    public void Init() {
        gridGameObjectPosition = transform.position;
        nativeDistances = new(precomputedCells.Count, Allocator.Persistent);
        nativeTraversables = new(precomputedCells.Count, Allocator.Persistent);
        nativePositions = new(precomputedCells.Count, Allocator.Persistent);
        flowFieldJobResults = new(precomputedCells.Count, Allocator.Persistent);
        
        gridCells = new GridCell[precomputedCells.Count];
        for (int i = 0; i < precomputedCells.Count; i++) {
            PrecomputedCellData precomputedCellData = precomputedCells[i];
            gridCells[i] = new() {
                position = precomputedCellData.position,
                traversable = precomputedCellData.traversable,
            };
        }
    }

    public void Deinit() {
        flowFieldJobHandle?.Complete();
        nativeDistances.Dispose();
        nativeTraversables.Dispose();
        nativePositions.Dispose();
        flowFieldJobResults.Dispose();
    }

    public void AddObstacle(Vector2 position, int cellRadius) {
        for (int y = -cellRadius; y <= cellRadius; y++) {
            for (int x = -cellRadius; x <= cellRadius; x++) {
                Vector2 offset = new Vector2(x, y) * cellSize;

                int index = GetCellIndexAtPosition(position + offset);
                if (index < 0 || index >= gridCells.Length) continue;
                
                ref bool obstructed = ref gridCells[index].isObstacleObstructed;
                obstructed = true;
            }
        }
    }

    public void ClearObstacle(Vector2 position, int cellRadius) {
        for (int y = -cellRadius; y <= cellRadius; y++) {
            for (int x = -cellRadius; x <= cellRadius; x++) {
                Vector2 offset = new Vector2(x, y) * cellSize;
                
                int index = GetCellIndexAtPosition(position + offset);
                if (index < 0 || index >= gridCells.Length) continue;
                
                ref bool obstructed = ref gridCells[index].isObstacleObstructed;
                obstructed = false;
            }
        }
    }

    public void FeedPlayerVelocity(Vector2 playerPos, Vector2 playerVelocity) {
        const float lookAheadTime = 2.1f;
        const float lookAheadSpeed = 1.3f;
        predicetedPlayerPos = Vector2.Lerp(predicetedPlayerPos, playerPos + playerVelocity * lookAheadTime, Time.deltaTime * lookAheadSpeed);
    }

    public Vector3 GetSpawnPosition(Vector2 playerPosition, int innerCellRadius, int outerCellRadius) {
        bool needToRecalculate = lastUpdateTime != Time.time;
        lastUpdateTime = Time.time;

        if (needToRecalculate) {
            if (TryGetCellAtPosition(playerPosition, out GridCell playerCell)) {
                UpdateDataForSpawnCells(playerCell, innerCellRadius, outerCellRadius);
            }
            else {
                return Vector2.zero;
            }
        }

        Vector2 slightRandomOffset = Random.insideUnitCircle * (cellSize * 0.90f);
        
        List<GridCell> sortedCells = spawnCells.OrderByDescending(static cell => cell.spawnWeight).ToList();
        int maxIndex = Mathf.RoundToInt(sortedCells.Count * 0.2f);
        if (maxIndex > 0 && maxIndex < sortedCells.Count) {
            return sortedCells[Random.Range(0, maxIndex)].position; 
        }
        
        return spawnCells[0].position + slightRandomOffset;
    }
    
    public void ScheduleFlowFieldCalculation(Vector2 sourcePosition) {
        int sourceIndex = GetCellIndexAtPosition(sourcePosition);
        if (sourceIndex < 0 || sourceIndex >= gridCells.Length) return;
        
        for (int i = 0; i < gridCells.Length; i++) {
            nativeDistances[i] = i == sourceIndex ? 0 : int.MaxValue;
            nativeTraversables[i] = gridCells[i].isObstacleObstructed ? false : gridCells[i].traversable;
            nativePositions[i] = gridCells[i].position;
            flowFieldJobResults[i] = Vector2.zero;
        }

        DijkstraJob dijkstraJob = new() {
            distances = nativeDistances,
            traversable = nativeTraversables,
            gridWitdh = width,
            gridHeight = height,
            startingIndex = sourceIndex,
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
        flowFieldJobHandle = flowFieldJob.Schedule(gridCells.Length, 128, dijkstraJobHandle);
    }
    
    public void CompleteFlowFieldCalculation() {
        if (!flowFieldJobHandle.HasValue) return;
        
        flowFieldJobHandle?.Complete();

        for (int i = 0; i < gridCells.Length; i++) {
            ref GridCell cell = ref gridCells[i];  // Must be an array for this to work
            cell.flowDir = flowFieldJobResults[i];
            cell.distFromPlayerCell = nativeDistances[i];
        }
    }

    public Vector2 GetFlowFieldDirection(Vector2 position) {
        if (TryGetCellAtPosition(position, out GridCell cell)) {
            return cell.flowDir;
        }
        return Vector2.zero;
    }

    private bool TryGetCellAtPosition(Vector2 position, out GridCell cell) {
        Vector2 posInGridSpace = position - gridGameObjectPosition;

        int x = Mathf.FloorToInt(posInGridSpace.x / cellSize);
        int y = Mathf.FloorToInt(posInGridSpace.y / cellSize);

        int index = y * width + x;
        if (gridCells.IndexInRange(index)) {
            cell = gridCells[index];
            return true;
        }

        cell = new();
        return false;
    }

    private int GetCellIndexAtPosition(Vector2 position) {
        Vector2 posInGridSpace = position - gridGameObjectPosition;
        int x = Mathf.FloorToInt(posInGridSpace.x / cellSize);
        int y = Mathf.FloorToInt(posInGridSpace.y / cellSize);
        return y * width + x;
    }

    private void UpdateDataForSpawnCells(GridCell playerCell, int innerRadius, int outerRadius) {
        spawnCells.Clear();

        const float maxDistScaler = 1.3f;
        float maxDistCellCanBeFromPlayer = (outerRadius * maxDistScaler) * cellSize;

        for (int y = -outerRadius; y <= outerRadius; y++) {
            for (int x = -outerRadius; x <= outerRadius; x++) {
                bool isCellWereWorkingOn = x == 0 && y == 0;
                bool isInsideInnerRadius = Mathf.Abs(x) <= innerRadius && Mathf.Abs(y) <= innerRadius;
                if (isCellWereWorkingOn || isInsideInnerRadius) continue;

                Vector2 neighborPos = playerCell.position + new Vector2(x, y) * cellSize;
                if (!TryGetCellAtPosition(neighborPos, out GridCell neighbor)) continue;
                if (!neighbor.traversable || neighbor.isObstacleObstructed) continue;
                    
                // Player is in unreacheable position, add anyways so we have something in the list
                if (neighbor.distFromPlayerCell == int.MaxValue) {
                    spawnCells.Add(neighbor); 
                    Debug.Log("Player is unreachable");
                    continue;
                }
                    
                // Convert dijkstra distance into world distance
                float distFromPlayer = (neighbor.distFromPlayerCell / (float)dijkstraOrthogonalDist) * cellSize;
                if (distFromPlayer > maxDistCellCanBeFromPlayer) continue;
                    
                spawnCells.Add(neighbor);
            }
        }

        totalSpawnCellsWeight = 0f;

        ContactFilter2D filter = new() {
            useLayerMask = true,
            layerMask = Masks.EnemyMask,
        };
        List<Collider2D> colList = UnityEngine.Pool.ListPool<Collider2D>.Get();

        float distFromCellToPredictedPlayerPos = Vector2.Distance(predicetedPlayerPos, playerCell.position);
        Vector2 dirToPredictedPos = (predicetedPlayerPos - playerCell.position).normalized;
        
        const float minDistToIncludeDirectionWeight = 0.12f;
        bool addDirectionalWeight = distFromCellToPredictedPlayerPos > minDistToIncludeDirectionWeight;

        float expandedSizeForEnemyTesting = cellSize * 5f;
        for (int i = 0; i < spawnCells.Count; i++) {
            GridCell cell = spawnCells[i];
            const float enemyWeight = 1f;
            const float dirWeight = 2f;

            int enemyCount = Physics2D.OverlapCircle(cell.position, expandedSizeForEnemyTesting, filter, colList);
            float weight = (1f / (enemyCount + 1f)) * enemyWeight;

            if (addDirectionalWeight) {
                Vector2 dir = (cell.position - playerCell.position).normalized;
                weight += (Vector2.Dot(dir, dirToPredictedPos) + 1 * 0.5f) * dirWeight;
            }

            cell.spawnWeight = weight;
            spawnCells[i] = cell;
            
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
                    int dist = neighborIsDiagonal ? dijkstraDiagonalDist : dijkstraOrthogonalDist;
                        
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
        // Temporarily increase edge radius so our overlap tests detect edge colliders
        EdgeCollider2D[] allEdgeColliders = FindObjectsByType<EdgeCollider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EdgeCollider2D edgeCol in allEdgeColliders) {
            edgeCol.edgeRadius = 0.02f;
        }
        
        const float traversableRatioPerTile = 0.8f;
        Vector2 traversableTestBoxSize = new Vector2(cellSize, cellSize) * traversableRatioPerTile;

        precomputedCells = new();
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                Vector2 pos = CalculateCellPosition(x, y);
                
                bool traversable = true;
                if (Physics2D.OverlapBox(pos, traversableTestBoxSize, 0f, Masks.StaticLevelMask)) {
                    traversable = false;
                }

                precomputedCells.Add(new() {
                    position = pos,
                    traversable = traversable,
                });
            }
        }
        
        // Reset edge radius for edge colliders back to 0
        foreach (EdgeCollider2D edgeCol in allEdgeColliders) {
            edgeCol.edgeRadius = 0f;
        }
        
        EditorUtility.SetDirty(this);
    }

    [VInspector.Button("Clear")]
    public void Clear() {
        precomputedCells = null;
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
        
        if (precomputedCells == null) {
            Generate();
        }
        
        Init();
        ScheduleFlowFieldCalculation(sourcePosForTesting.position);
        CompleteFlowFieldCalculation();
        Deinit();
    }

    public bool drawGizmos;
    public bool showFlowField;
    
    private void OnDrawGizmos() {
        if (!drawGizmos) return;
        
        Color nonGeneratedColor = new(175f / 255f, 66f / 255f, 26f / 255f);
        Color gridColor = new(154f / 255f, 1f, 0f, 0.1f);
        Color gridFill = new(45f / 255f, 1f, 0f, 0.2f);
        
        if (precomputedCells == null || precomputedCells.Count == 0) {
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
        
        // if (cells != null && showFlowField) {
        //     foreach (GridCell cell in cells) {
        //         Vector2 flowDir = cell.flowDir;
        //         if (flowDir == Vector2.zero) continue;
        //
        //         Vector2 cellPos = cell.position;
        //         DebugExtension.DrawArrow(cellPos, flowDir * 0.05f);
        //     }
        //
        //     return;
        // }

        if (precomputedCells != null && !showFlowField) {
            foreach (PrecomputedCellData cell in precomputedCells) {
                if (!cell.traversable) continue;
                
                Bounds bounds = new() {
                    center = cell.position,
                    size = Vector3.one * cellSize,
                };
                
                DebugExtension.DrawBounds(bounds, gridColor);
                Gizmos.color = gridFill;
                Gizmos.DrawCube(cell.position, Vector3.one * cellSize);
            }
            
            return;
        }
        
    }

#endif 
    
}