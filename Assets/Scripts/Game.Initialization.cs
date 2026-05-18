using UnityEngine;

public partial class Game {
    
    private void InitGameData() {
        InitResources();
        InitEntities();
        InitAudio();
    }
    
    private void InitResources() {
        LoadAllItems(); 
        LoadAllDropPools();
    }
    
    private void LoadAllItems() {
        GameData.Resources res = gameData.res;
        UuidScriptableObject[] resourceObjects = Resources.LoadAll<UuidScriptableObject>(string.Empty);
        
        foreach (UuidScriptableObject resObject in resourceObjects) {
            res.lookup.Add(resObject.uuid, resObject);
            if (resObject is Item item) {
                res.items.Add(item);
            }
            if (resObject is Augment augment) {
                augment.CreateAugmentItemFromDerived();
                if (res.eyeUpgradeAugmentsLookup.TryGetValue(augment.eyeUpgradeDerivedFrom, out var augmentList)) {
                    augmentList.Add(augment);
                }
                else {
                    res.eyeUpgradeAugmentsLookup.Add(augment.eyeUpgradeDerivedFrom, new() { augment });
                }
            }
        }
    }
    
    private void LoadAllDropPools() {
        GameData.Resources res = gameData.res;
        DropPool[] dropPoolSOs = Resources.LoadAll<DropPool>(string.Empty);
        
        foreach (DropPool dropPool in dropPoolSOs) {
            dropPool.items = new();
            res.dropPools.Add(dropPool);
            if (dropPool.isMapSpecific) {
                res.mapSpecificDropPools.Add(dropPool);
                continue;
            }
            res.globalDropPools.Add(dropPool);
        }
        
        foreach (Item item in res.items) {
            RegisterItemToDropPools(item, res.globalDropPools);
        }
    }
    
    private void InitEntities() {
        gameData.entities.player = MakePlayer();
    }
    
    private void InitAudio() {
        const int numberOfSources = 20;
        var reservedAudioSources = gameData.audio.reservedSources;
        var audioSourcePrefab = gameData.prefabs.audioSource;
        
        reservedAudioSources = new(numberOfSources);
        for (int i = 0; i < numberOfSources; i++) {
            GameObject audioGo = Instantiate(audioSourcePrefab, transform);
            reservedAudioSources.Enqueue(audioGo.GetComponent<AudioSource>());
        }
    }
    
}
