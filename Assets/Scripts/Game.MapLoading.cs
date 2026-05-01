using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public partial class Game {
    
    private enum MapLoadingState { Unloaded, Loaded, Loading, Unloading }
    private MapLoadingState mapLoadingState;
    
    private MapData loadedMapData;
    private MapInstance loadedMapInst;

    public void LoadMapAsync(MapData mapData, Action onLoadedCallback) {
        if (LoadingMapInProgress()) return;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(mapData.sceneReference, LoadSceneMode.Additive);
        if (loadOperation == null) return;

        mapLoadingState = MapLoadingState.Loading;
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!loadOperation.isDone) {
                yield return null;
            }
            
            mapLoadingState = MapLoadingState.Loaded;
            loadedMapData = mapData;

            List<GameObject> loadedMapRoots = ListPool<GameObject>.Get();
            
            Scene loadedMapScene = SceneManager.GetSceneByName(mapData.sceneReference);
            loadedMapScene.GetRootGameObjects(loadedMapRoots);
            
            foreach (GameObject root in loadedMapRoots) {
                if (!root.TryGetComponent(out MapInstance map)) continue;
                loadedMapInst = map;
                map.gameObject.SetActive(false);
                break;
            }
            
            ListPool<GameObject>.Release(loadedMapRoots);
            onLoadedCallback?.Invoke();
        }
    }

    public void UnloadCurrentMapAsync() {
        if (UnloadingMapInProgress()) return;
            
        loadedMapInst.gameObject.SetActive(false); 
        
        Scene loadedMap = SceneManager.GetSceneByName(loadedMapData.sceneReference);
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(loadedMap);

        if (unloadOperation == null) return;
        
        mapLoadingState = MapLoadingState.Unloading;
        loadedMapData = null;
        
        StartCoroutine(WaitForSceneToLoad());

        IEnumerator WaitForSceneToLoad() {
            while (!unloadOperation.isDone) {
                yield return null;
            }
            mapLoadingState = MapLoadingState.Unloaded;
        }
    } 
    
    public bool LoadingMapInProgress() {
        return mapLoadingState == MapLoadingState.Loading;
    }
    
    public bool UnloadingMapInProgress() {
        return mapLoadingState == MapLoadingState.Unloading;
    }

    private void SpawnMapResources(Transform resourceSpawnParent) {
        // Clear past resource lookups
        deadBodySlotsLookup.Clear();
        bushSlotsLookup.Clear();
        
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
            InventorySlot[] chosenDeadbody = deadBodySlotsLookup.RandomValue();
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
            loadedMapInst.grid.AddObstacle(resource.position, obstacleCellRadius);
            resource.obstacleCellRadius = obstacleCellRadius;
            resource.obstaclePosition = resource.position;
        }
        return resource;
    }
    
    private Dictionary<GameObject, InventorySlot[]> deadBodySlotsLookup = new();
    
    private void InitDeadBody(Entity entity) {
        using var _ = ListPool<Item>.Get(out var items);
            
        int maxDeadBodyItemCount = Random.Range(2, 6);
        GetUniqueItemsFromDropPool(bodyDropPool, maxDeadBodyItemCount, items);
            
        float eyeUpgradeOnBodyChance = loadedMapData.eyeUpgradeOnBodyChance;
        while (RollProbability(eyeUpgradeOnBodyChance) && items.Count < lootInventoryPtr.slots.Length) {
            eyeUpgradeOnBodyChance -= loadedMapData.consecutiveEyeUpgradeChanceReductionOnBody;
            items.Add(GetItemFromDropPool(eyeUpgradesDropPool));
        }
        
        deadBodySlotsLookup.Add(entity.gameObject, CreateLootInventoryFromItems(items, DropOrigin.Body, stackTaperRate: 0.15f));
    }
    
    private Dictionary<GameObject, InventorySlot[]> bushSlotsLookup = new();
    
    private void InitBush(Entity entity) {
        using var _ = ListPool<Item>.Get(out var items);
        int maxBushItemCount = Random.Range(1, 3);
        GetUniqueItemsFromDropPool(bushesDropPool, maxBushItemCount, items);
        bushSlotsLookup.Add(entity.gameObject, CreateLootInventoryFromItems(items, DropOrigin.Bush, stackTaperRate: 0.15f)); 
    }
    
    private InventorySlot[] CreateLootInventoryFromItems(List<Item> items, DropOrigin dropOrigin, float stackTaperRate) {
        using var _ = ListPool<ItemInstance>.Get(out var itemInstances);
            
        foreach (Item item in items) {
            int stackCount = 1;
            
            float taperingChance = Mathf.Lerp(GetDropChanceOfItem(item, dropOrigin), 0f, stackTaperRate);
            while (RollProbability(taperingChance)) {
                stackCount++;
                taperingChance = Mathf.Lerp(taperingChance, 0f, stackTaperRate);
            }
                    
            itemInstances.Add(new() {
                itemOrInstanceUuid = item.uuid, 
                count = stackCount,
                notDiscovered = true,
            });
        }
        
        return CreateLootInventoryInstance(itemInstances);
    }
    
}
