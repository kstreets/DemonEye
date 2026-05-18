using System;
using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public partial class Game {
    
    [Serializable]
    private class MapSaves {
        public List<bool> unlockStates;
    }
    
    private void SaveMaps() {
        MapSaves mapSaves = new() {
            unlockStates = new(maps.Count),
        };
        foreach (MapData mapData in gameData.config.maps) {
            mapSaves.unlockStates.Add(mapData.isUnlocked);    
        }
        SaveToFile(gameData.savePaths.mapUnlocks, mapSaves);
    }
    
    private void InitMapSaves() {
        MapSaves mapSaves = LoadFromFile<MapSaves>(gameData.savePaths.mapUnlocks);
        if (mapSaves == null) return;
        
        var maps = gameData.config.maps;
        
        if (maps.Count != mapSaves.unlockStates.Count) {
            Debug.Log("Maps save does not match current maps. Saves are not going to be loaded");
            return;
        }
        
        for (int i = 0; i < maps.Count; i++) {
            maps[i].isUnlocked = mapSaves.unlockStates[i];
        }
    }

    public enum MapLoadingState { Unloaded, Loaded, Loading, Unloading }

    public void LoadMapAsync(MapData mapData, Action onLoadedCallback) {
        if (LoadingMapInProgress()) return;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(mapData.sceneReference, LoadSceneMode.Additive);
        if (loadOperation == null) return;

        gameData.curRaid.mapLoadingState = MapLoadingState.Loading;
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!loadOperation.isDone) {
                yield return null;
            }
            
            gameData.curRaid.mapLoadingState = MapLoadingState.Loaded;
            gameData.curRaid.map = mapData;

            List<GameObject> loadedMapRoots = ListPool<GameObject>.Get();
            
            Scene loadedMapScene = SceneManager.GetSceneByName(mapData.sceneReference);
            loadedMapScene.GetRootGameObjects(loadedMapRoots);
            
            foreach (GameObject root in loadedMapRoots) {
                if (!root.TryGetComponent(out MapInstance map)) continue;
                gameData.curRaid.mapInstance = map;
                map.gameObject.SetActive(false);
                break;
            }
            
            ListPool<GameObject>.Release(loadedMapRoots);
            onLoadedCallback?.Invoke();
        }
    }

    public void UnloadCurrentMapAsync() {
        if (UnloadingMapInProgress()) return;
            
        gameData.curRaid.mapInstance.gameObject.SetActive(false); 
        
        Scene loadedMap = SceneManager.GetSceneByName(gameData.curRaid.map.sceneReference);
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedMap);

        if (unloadOperation == null) return;
        
        gameData.curRaid.mapLoadingState = MapLoadingState.Unloading;
        gameData.curRaid.map = null;
        
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!unloadOperation.isDone) {
                yield return null;
            }
            gameData.curRaid.mapLoadingState = MapLoadingState.Unloaded;
        }
    } 
    
    public bool LoadingMapInProgress() {
        return gameData.curRaid.mapLoadingState == MapLoadingState.Loading;
    }
    
    public bool UnloadingMapInProgress() {
        return gameData.curRaid.mapLoadingState == MapLoadingState.Unloading;
    }

    private void SpawnMapResources(Transform resourceSpawnParent) {
        // Clear past resource lookups
        gameData.curRaid.deadBodySlotsLookup.Clear();
        gameData.curRaid.bushSlotsLookup.Clear();
        
        ResourceSpawn[] resourceSpawns = resourceSpawnParent.GetComponentsInChildren<ResourceSpawn>();
        foreach (ResourceSpawn resourceSpawn in resourceSpawns) { 
            GameObject prefab = resourceSpawn.GetPrefabToSpawn();
            if (!prefab) continue;
            
            int obstacleCellRadius = prefab.TryGetComponent(out GridObstacle gridObstacle) ? gridObstacle.cellRadius : 0;
            Entity resourceEntity = SpawnResource<Entity>(prefab, resourceSpawn.transform, obstacleCellRadius);

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
        
        if (QuestIsActive(pickPocketQuest)) {
            InventorySlot[] chosenDeadbody = gameData.curRaid.deadBodySlotsLookup.RandomValue();
            for (int i = 0; i < chosenDeadbody.Length; i++) {
                if (chosenDeadbody[i].itemInstance == null) {
                    chosenDeadbody[i].itemInstance = new() {
                        itemOrInstanceUuid = pickPocketQuest.objectives[1].targetItem.uuid,
                        count = 1,
                        notDiscovered = true,
                    };
                    break;
                }
            }
        }
    }
    
    private T SpawnResource<T>(GameObject resourcePrefab, Transform spawnPoint, int obstacleCellRadius = 0) where T : Entity, new() {
        T resource = SpawnEntity<T>(resourcePrefab, spawnPoint.position, spawnPoint.rotation);
        if (obstacleCellRadius > 0) {
            gameData.curRaid.mapInstance.grid.AddObstacle(resource.position, obstacleCellRadius);
            resource.obstacleCellRadius = obstacleCellRadius;
            resource.obstaclePosition = resource.position;
        }
        return resource;
    }
    
    private void InitDeadBody(Entity entity) {
        using var _ = ListPool<Item>.Get(out var items);
            
        int maxDeadBodyItemCount = Random.Range(2, 6);
        GetUniqueItemsFromDropPool(bodyDropPool, maxDeadBodyItemCount, ref items);
        InventorySlot[] lootInventory = CreateLootInventoryFromItems(items, bodyDropPool, stackTaperRate: 0.15f);
        
        float eyeUpgradeOnBodyChance = gameData.curRaid.map.eyeUpgradeOnBodyChance;
        while (RollProbability(eyeUpgradeOnBodyChance)) {
            eyeUpgradeOnBodyChance -= gameData.curRaid.map.consecutiveEyeUpgradeChanceReductionOnBody;
            bool success = AppendToLootInventory(lootInventory, GetItemFromDropPool(eyeUpgradesDropPool), 1);
            if (!success) break;
        }
        
        gameData.curRaid.deadBodySlotsLookup.Add(entity.gameObject, lootInventory);
    }
    
    private void InitBush(Entity entity) {
        using var _ = ListPool<Item>.Get(out var items);
        int maxBushItemCount = Random.Range(1, 3);
        GetUniqueItemsFromDropPool(bushesDropPool, maxBushItemCount, ref items);
        gameData.curRaid.bushSlotsLookup.Add(entity.gameObject, CreateLootInventoryFromItems(items, bushesDropPool, stackTaperRate: 0.15f)); 
    }
    
    private void InitMapGrid() {
        gameData.curRaid.mapInstance.grid.Init();
        gameData.curRaid.lastPlayerGridPos = new(float.MaxValue, float.MaxValue);
    }

    private void DeinitMapGrid() {
        gameData.curRaid.mapInstance.grid.Deinit();
    }
    
    private void UpdateMapGrid() {
        CoolerGrid grid = gameData.curRaid.mapInstance.grid;
        grid.FeedPlayerVelocity(player.position, player.velocity);
        grid.UpdateFlowFieldFromPreviousJob();

        const float fixedUpdateRate = 1f / 6f;
        const float slowFixedUpdateRate = 1f / 2f; // Spawning enemies relies on updated flow field distances so we can't just not update them
        float curUpdateRate = enemies.Count > 0 ? fixedUpdateRate : slowFixedUpdateRate;
        if (!gameData.curRaid.flowFieldLimiter.TimeHasPassed(curUpdateRate)) return;
            
        Vector2 curPlayerGridPos = grid.GetCellPosition(player.position);
        bool playerMovedCells = curPlayerGridPos != gameData.curRaid.lastPlayerGridPos;
        if (playerMovedCells) {
            grid.ScheduleFlowFieldCalculation(player.position);
            gameData.curRaid.lastPlayerGridPos = curPlayerGridPos;
        }
    }
    
    private List<ExitPortal> activeExitPortals = new();
    private ExitPortal exitPortalTakenByPlayer;
    
    private class ExitPortal {
        public Transform transform;
        public Sequence summoningPortalSequence;
        public Sequence closingCountdownSequence;
        public bool hasBeenSummoned;
        public bool canTake;
    }
    
    private void StartSummoningExitPortal(Transform exitPortalTrans) {
        ExitPortal portal = GetExitPortalFromTransform(exitPortalTrans);
        portal.hasBeenSummoned = true;
        
        portal.summoningPortalSequence = Sequence.Create();
        portal.summoningPortalSequence.ChainDelay(gameplayConfig.portalPostSummonDelay);
        portal.summoningPortalSequence.Chain(Tween.Scale(portal.transform, Vector3.one, 0.25f, Ease.OutBack));
        
        portal.summoningPortalSequence.OnComplete(portal, static (portal) => {
            portal.canTake = true;
            gameInstance.StartClosingExitPortal(portal);
        });
    }
    
    private void StartClosingExitPortal(ExitPortal portal) {
        portal.closingCountdownSequence = Sequence.Create();
        portal.closingCountdownSequence.ChainDelay(gameplayConfig.portalActiveDuration);
        portal.closingCountdownSequence.ChainCallback(portal, static (portal) => {
            portal.canTake = false;
            gameInstance.activeExitPortals.Remove(portal);
            Tween.Scale(portal.transform, Vector3.zero, 0.25f, Ease.OutCubic);
        });
    }
    
    private ExitPortal GetExitPortalFromTransform(Transform trans) {
        foreach (ExitPortal portal in activeExitPortals) {
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
        
        using var _ = ListPool<Transform>.Get(out List<Transform> possibleExitPortals);
        
        foreach (Transform portal in exitPortalParent) {
            portal.gameObject.SetActive(false);
            if (Vector2.Distance(player.position, portal.position) > 5) {
                possibleExitPortals.Add(portal);
            }
        }
        
        Assert.IsTrue(possibleExitPortals.Count >= exitPortalsCount, 
            $"Not enough portals on map, and or spaced too close together. Expected {exitPortalsCount} got {possibleExitPortals.Count}");
        
        possibleExitPortals.Shuffle();
        
        for (int i = 0; i < exitPortalsCount; i++) {
            activeExitPortals.Add(new() {
                transform = possibleExitPortals[i],
            });
            possibleExitPortals[i].gameObject.SetActive(true);
            possibleExitPortals[i].transform.localScale = Vector3.one * 0.25f;
        }
    }
    
    private void SpawnFinalExitPortal() {
        for (int i = 0; i < 100; i++) {
            Vector2 randomPos = (Vector2)player.position + Random.insideUnitCircle * Random.Range(0.5f, 1.5f);
            if (Physics.OverlapCircle(randomPos, 0.2f, Masks.StaticLevelMask).Count > 0) continue;
            
            Transform exitPortalParent = gameData.curRaid.mapInstance.exitPortalsParent;
            int randomSpawnIndex = Random.Range(0, exitPortalParent.childCount);
            Transform newExitPortalTrans = exitPortalParent.GetChild(randomSpawnIndex);
            
            newExitPortalTrans.gameObject.SetActive(true);
            newExitPortalTrans.position = randomPos;
            
            activeExitPortals.Add(new() {
                transform = newExitPortalTrans,
                hasBeenSummoned = true,
                canTake = true,
            });
            
            Tween.Scale(newExitPortalTrans, 0f, 1f, 0.5f, Ease.OutBack);
            PlayAudioClip(portalSpawnClip, newExitPortalTrans.position);
            return;
        }
        
        // This is a fail safe incase we couldn't spawn the final portal
        gameData.states.gameStateMachine.SetState(gameData.states.winExit);
    }
    

}
