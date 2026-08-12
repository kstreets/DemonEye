using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using static GameData;

public partial class Game {
    
    private void InitMaps(GameState gameState) {
        List<MapData> maps = config.maps;
        
        if (gameState == null) {
            for (int i = 0; i < maps.Count; i++) {
                maps[i].state = new() {
                    isUnlocked = i == 0,
                    bloodMushroomSpawns = new(),
                };
            }
            return;
        }
        
        if (maps.Count != gameState.mapStates.Count) {
            Debug.Log("Maps save does not match current maps. Saves are not going to be loaded");
            return;
        }
        
        for (int i = 0; i < maps.Count; i++) {
            maps[i].state = gameState.mapStates[i];
        }
    }
    
    public enum MapLoadingState { Unloaded, Loaded, Loading, Unloading }
    
    public void LoadMapAsync(MapData mapData) {
        bool alreadyLoading = curRaid.mapLoadingState == MapLoadingState.Loading;
        if (alreadyLoading) return;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(mapData.sceneReference, LoadSceneMode.Additive);
        if (loadOperation == null) return;

        curRaid.mapLoadingState = MapLoadingState.Loading;
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!loadOperation.isDone) {
                yield return null;
            }
            
            curRaid.mapLoadingState = MapLoadingState.Loaded;
            curRaid.map = mapData;

            using var _ = ListPool<GameObject>.Get(out var loadedMapRoots);
            
            Scene loadedMapScene = SceneManager.GetSceneByName(mapData.sceneReference);
            loadedMapScene.GetRootGameObjects(loadedMapRoots);
            
            foreach (GameObject root in loadedMapRoots) {
                if (!root.TryGetComponent(out MapInstance map)) continue;
                curRaid.mapInstance = map;
                map.gameObject.SetActive(false);
                break;
            }
            
            states.gameStateMachine.SetStateIfNotCurrent(states.raid);
        }
    }
    
    public void UnloadCurrentMapAsync() {
        bool alreadyUnloading = curRaid.mapLoadingState == MapLoadingState.Unloading;
        if (alreadyUnloading) return;
            
        curRaid.mapInstance.gameObject.SetActive(false); 
        
        Scene loadedMap = SceneManager.GetSceneByName(curRaid.map.sceneReference);
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedMap);

        if (unloadOperation == null) return;
        
        curRaid.mapLoadingState = MapLoadingState.Unloading;
        curRaid.map = null;
        
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!unloadOperation.isDone) {
                yield return null;
            }
            curRaid.mapLoadingState = MapLoadingState.Unloaded;
        }
    }

    private void SpawnMapResources(Transform resourceSpawnParent) {
        // Clear past resource lookups
        curRaid.deadBodySlotsLookup.Clear();
        curRaid.bushSlotsLookup.Clear();
        
        ResourceSpawn[] resourceSpawns = resourceSpawnParent.GetComponentsInChildren<ResourceSpawn>();
        foreach (ResourceSpawn resourceSpawn in resourceSpawns) { 
            GameObject prefab = resourceSpawn.GetPrefabToSpawn();
            if (!prefab) continue;
            
            prefab.TryGetComponent(out GridObstacle gridObstacle);
            Entity resourceEntity = SpawnResource<Entity>(prefab, resourceSpawn.transform, gridObstacle);

            switch (resourceEntity.gameObject.tag) {
                case Tags.Mineable:
                    resourceEntity.health = 50;
                    break;
                case Tags.DeadBody:
                    InitDeadBody(resourceEntity);
                    break;
                case Tags.Bush:
                    InitBush(resourceEntity); 
                    break;
            }
        } 
        
        foreach (ResourceSpawn resourceSpawn in resourceSpawns) {
            Destroy(resourceSpawn.gameObject);
        }
        
        // Spawn blood mushrooms from previous raid
        {
            foreach (Vector2 bloodMushroomSpawn in curRaid.map.state.bloodMushroomSpawns) {
                SpawnItemAsEntity(itemRefs.bloodMushroom, 1, bloodMushroomSpawn, Quaternion.identity);
            }
            curRaid.map.state.bloodMushroomSpawns.Clear();
        }
    }
    
    private T SpawnResource<T>(GameObject resourcePrefab, Transform spawnPoint, [CanBeNull] GridObstacle obstacle) where T : Entity, new() {
        T resource = SpawnEntity<T>(resourcePrefab, spawnPoint.position, spawnPoint.rotation);
        if (obstacle != null) {
            Vector2 obstaclePos = obstacle.Center(spawnPoint.position);
            int obstacleRadius = obstacle.Radius(curRaid.mapInstance.grid.cellSize);
            curRaid.mapInstance.grid.AddObstacle(obstaclePos, obstacleRadius);
            resource.gridObstaclePos = obstaclePos;
            resource.gridObstacleRadius = obstacleRadius;
        }
        return resource;
    }
    
    private void InitDeadBody(Entity entity) {
        using var _ = ListPool<Item>.Get(out var items);
            
        int maxDeadBodyItemCount = Random.Range(2, 6);
        GetUniqueItemsFromDropPool(dropPools.body, maxDeadBodyItemCount, ref items);
        InventorySlot[] lootInventory = CreateLootInventoryFromItems(items, dropPools.body, stackTaperRate: 0.15f);
        
        float eyeUpgradeOnBodyChance = curRaid.map.eyeUpgradeOnBodyChance;
        while (RollProbability(eyeUpgradeOnBodyChance)) {
            eyeUpgradeOnBodyChance -= curRaid.map.consecutiveEyeUpgradeChanceReductionOnBody;
            bool success = AppendToLootInventory(lootInventory, GetItemFromDropPool(dropPools.eyeUpgrades), 1);
            if (!success) break;
        }
        
        curRaid.deadBodySlotsLookup.Add(entity.gameObject, lootInventory);
    }
    
    private void InitBush(Entity entity) {
        using var _ = ListPool<Item>.Get(out var items);
        int maxBushItemCount = Random.Range(1, 3);
        GetUniqueItemsFromDropPool(dropPools.bushes, maxBushItemCount, ref items);
        curRaid.bushSlotsLookup.Add(entity.gameObject, CreateLootInventoryFromItems(items, dropPools.bushes, stackTaperRate: 0.15f)); 
    }
    
    private void InitMapGrid() {
        curRaid.mapInstance.grid.Init();
        curRaid.lastPlayerGridPos = new(float.MaxValue, float.MaxValue);
    }

    private void DeinitMapGrid() {
        curRaid.mapInstance.grid.Deinit();
    }
    
    private void UpdateMapGrid() {
        CoolerGrid grid = curRaid.mapInstance.grid;
        grid.FeedPlayerVelocity(player.position, player.velocity);
        grid.UpdateFlowFieldFromPreviousJob();

        const float fixedUpdateRate = 1f / 6f;
        const float slowFixedUpdateRate = 1f / 2f; // Spawning enemies relies on updated flow field distances so we can't just not update them
        float curUpdateRate = entities.enemies.Count > 0 ? fixedUpdateRate : slowFixedUpdateRate;
        if (!curRaid.flowFieldLimiter.TimeHasPassed(curUpdateRate)) return;
            
        Vector2 curPlayerGridPos = grid.GetCellPosition(player.position);
        bool playerMovedCells = curPlayerGridPos != curRaid.lastPlayerGridPos;
        if (playerMovedCells) {
            grid.ScheduleFlowFieldCalculation(player.position);
            curRaid.lastPlayerGridPos = curPlayerGridPos;
        }
    }
    
    private List<Portal> activeExitPortals = new();
    private Portal exitPortalTakenByPlayer;
    
    private Portal GetExitPortalFromTransform(Transform trans) {
        foreach (Portal portal in activeExitPortals) {
            if (portal.transform == trans) {
                return portal;
            }
        }
        Assert.IsTrue(false, "We should not be requesting a portal from a non-valid transform");
        return null;
    }
    
    private void SpawnInitialExitPortals(Transform exitPortalParent, int exitPortalsCount) {
        Assert.IsTrue(exitPortalsCount > 0, $"{nameof(exitPortalsCount)} needs to be 1 or more");
        
        activeExitPortals.Clear();
        exitPortalTakenByPlayer = null;
        
        using var _ = ListPool<Portal>.Get(out List<Portal> possibleExitPortals);
        
        foreach (Transform portalTrans in exitPortalParent) {
            portalTrans.gameObject.SetActive(false);
            if (Vector2.Distance(player.position, portalTrans.position) > 5) {
                possibleExitPortals.Add(portalTrans.GetComponent<Portal>());
            }
        }
        
        Assert.IsTrue(possibleExitPortals.Count >= exitPortalsCount, 
            $"Not enough portals on map, and or spaced too close together. Expected {exitPortalsCount} got {possibleExitPortals.Count}");
        
        possibleExitPortals.Shuffle();
        
        for (int i = 0; i < exitPortalsCount; i++) {
            Portal portal = possibleExitPortals[i];
            portal.Init();
            portal.gameObject.SetActive(true);
            activeExitPortals.Add(portal);
        }
    }
    
    private bool SpawnFinalExitPortal() {
        for (int i = 0; i < 100; i++) {
            Vector2 randomPos = (Vector2)player.position + Random.insideUnitCircle * Random.Range(0.5f, 1.5f);
            if (Physics.OverlapCircle(randomPos, 0.2f, Masks.StaticLevelMask).Count > 0) continue;
            
            Transform exitPortalParent = curRaid.mapInstance.exitPortalsParent;
            int randomSpawnIndex = Random.Range(0, exitPortalParent.childCount);
            Transform newExitPortalTrans = exitPortalParent.GetChild(randomSpawnIndex);
            
            newExitPortalTrans.gameObject.SetActive(true);
            newExitPortalTrans.position = randomPos;
            
            activeExitPortals.Add(newExitPortalTrans.GetComponent<Portal>());
            
            Tween.Scale(newExitPortalTrans, 0f, 1f, 0.5f, Ease.OutBack);
            PlayAudioClip(audio.portalSpawnClip, newExitPortalTrans.position);
            return true;
        }
        return false;
    }
    
    private void PlantBloodMushroom(Vector2 pos) {
        if (!persistentFlags.HasFlag(PersistentFlags.BloodMushroomsUnlocked)) return;
        
        const float chanceToPlant = 0.067f;
        if (!RollProbability(chanceToPlant)) return;
        
        const float minSpacing = 0.1f;
        List<Vector2> spawns = curRaid.map.state.bloodMushroomSpawns;
        foreach (Vector2 spawn in spawns) {
            if (Vector2.SqrMagnitude(spawn - pos) < minSpacing) return;
        }
        spawns.Add(pos);
    }

}
