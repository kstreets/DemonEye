using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using EffectsIndicies = Game.Entity.EffectsIndicies;

public partial class Game : MonoBehaviour {

    public static Game gameInstance;
    
    public GameData.Config config;
    public GameData.DropPools dropPools;
    public GameData.Prefabs prefabs;
    public GameData.ItemTypes itemTypes;
    public GameData.Quests quests;
    public GameData.ItemRefs itemRefs;
    public GameData.SkillUpgradePaths skillUpgradePaths;
    public GameData.Camera camera;
    public GameData.Curves curves;
    public GameData.UI ui;
    public GameData.PlayerInfo playerInfo;
    public GameData.RaidInfo raidInfo;
    public GameData.MainMenu mainMenu;
    public GameData.HideoutTabs hideoutTabs;
    public GameData.PlayerPanel playerPanel;
    public GameData.StashPanel stashPanel;
    public GameData.EyeForgePanel eyeForgePanel;
    public GameData.EyeForgeDetailsPanel eyeForgeDetailsPanel;
    public GameData.TraderPanel traderPanel;
    public GameData.TransactionPanel transactionPanel;
    public GameData.MapSelectionPanel mapSelectionPanel;
    public GameData.QuestsPanel questsPanel;
    public GameData.SkillsPanel skillsPanel;
    public GameData.Audio audio; 
    
    [NonSerialized] public readonly GameData.Input input = new();
    [NonSerialized] public readonly GameData.EntityPools entityPools = new();
    [NonSerialized] public readonly GameData.States states = new();
    [NonSerialized] public readonly GameData.Entities entities = new();
    [NonSerialized] public readonly GameData.Resources res = new();
    [NonSerialized] public readonly GameData.SavePaths savePaths = new();
    [NonSerialized] public readonly GameData.DemonEye demonEye = new();
    [NonSerialized] public readonly GameData.Trinkets trinkets = new();
    [NonSerialized] public readonly GameData.CurrentRaid curRaid = new();
    [NonSerialized] public readonly GameData.Inventories inventories = new();
    [NonSerialized] public readonly GameData.HotBar hotBar = new();
    
    public static Action<InventorySlot[]> onSoldItemsToTrader;
    public static Action<string> customQuestEvent;
    
    public static Player player => gameInstance.entities.player;
    
    private void Start() {
        gameInstance = this;
        InitGame();
    }

    private void Update() {
        states.gameStateMachine.Tick();
        DemonEyeTween.Update();
        UpdateTrader();
        UpdateQuests();
        
#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.middleButton.isPressed) {
            Time.timeScale = 4f;
        }
        else {
            Time.timeScale = 1f;
        }
#endif
    }

    private void FixedUpdate() {
        states.gameStateMachine.Tick(StateMachine.UpdateMode.FixedUpdate);
    }

    private void LateUpdate() {
        states.gameStateMachine.Tick(StateMachine.UpdateMode.LateUpdate);
    }

    private void OnApplicationQuit() {
        SaveTrader();
    }

    private void UpdateTimers() {
        curRaid.temp.interactionData.discoverItemTimer.Tick();
    }

    private void OnMainMenuStateEnter() {
        Cursor.visible = true;
        ShowMainMenuUI();
    }

    private void OnMainMenuStateExit() {
        CloseMainMenuUI();
    }

    private void OnHideoutStateEnter() {
        ShowHideoutUI();
        RefreshSkillsPanel();
    }

    private void OnHideoutStateExit() {
        CloseHideoutUI();
        SavePlayerData();
        SaveTrader();
        SaveInventory(inventories.player);
        SaveInventory(inventories.stash);
        SaveInventory(inventories.eyeForge);
        SaveActiveQuestProgresses();
    }

    private void OnHideoutStateUpdate() {
        UpdateInventory();
        RefreshTransactionUI();
        UpdateForgeState();
        UpdateForgePanel();
        UpdateForgeInfoPanel();
        RefreshAllInventoryDisplays();
        UpdateGraySlots();
    }

    private void OnHideoutStateLateUpdate() {
        UpdatePlayerPanelUI();
        UpdateDragAndDropItemToCursor();
        UpdateCurrencyNumbers();
    }

    private void OnMapSelectionEnter() {
        ShowMapSelectionUI();
    }

    private void OnMapSelectionExit() {
        CloseMapSelectionUI();
    }

    private void OnMapSelectionUpdate() {
        CheckForHotBarInteractions();
        UpdateInventory();
        RefreshAllInventoryDisplays();
    }

    private void OnMapSelectionLateUpdate() {
        UpdatePlayerPanelUI();
        UpdateDragAndDropItemToCursor();
    }

    private void OnRaidStateEnter() {
        InitRaid();
        PlayAmbience();
    }

    private void OnRaidStateExit() {
        DeinitPlayer();
        ClosePlayerInventory();
        CloseLootInventory();
        HideInventoryItemPopup();
        HideUIElementPopup();
        CloseRaidUI();
        StopAmbience();
    }

    private void OnRaidStateUpdate() {
        UpdateRaidState();
        UpdateMapGrid();
        UpdateTimers();
        CheckForInteractions();
        CheckForHotBarInteractions();
        UpdateInventory();
        UpdatePlayer();
        UpdateProjectiles();
        UpdateSpawnManager();
        UpdateEnemies();
        RefreshAllInventoryDisplays();
    }

    private void OnRaidStateFixedUpdate() {
        FixedUpdateEnemies();
    }

    private void OnRaidStateLateUpdate() {
        UpdateInRaidUI();
        UpdatePlayerPanelUI();
        UpdateHotBarUI();
        UpdateDragAndDropItemToCursor();
        UpdateCurrencyNumbers();
    }

    private void OnEarlyExitEnter() {
        OnSaveWhenRaidIsOver();
        AnimateEarlyExitSequence(() => states.gameStateMachine.SetStateIfNotCurrent(states.mainMenu));
    }
    
    private void OnEarlyExitExit() {
        DeinitRaid();
    }
    
    private void OnWinExitEnter() {
        var maps = config.maps;
        int nextMapIndex = maps.IndexOf(curRaid.map) + 1;
        bool unlockNextMap = maps.IndexInRange(nextMapIndex) && !maps[nextMapIndex].isUnlocked;
        if (unlockNextMap) {
            maps[nextMapIndex].isUnlocked = true;
            SaveMaps();
        }
        OnSaveWhenRaidIsOver();
        AnimateGameWinSequence(() => states.gameStateMachine.SetStateIfNotCurrent(states.mainMenu));
    }

    private void OnWinExitExit() {
        DeinitRaid();
    }

    private void OnGameOverEnter() {
        ClearInventory(inventories.player);
        OnSaveWhenRaidIsOver();
        AnimateGameOverSequence(() => states.gameStateMachine.SetStateIfNotCurrent(states.mainMenu)); 
    }
    
    private void OnGameOverExit() {
        player.health = FullPlayerHealth;
        DeinitRaid();
    }
    
    public enum RaidState { None, InitialWaves, FinalWave, PostFinalWave }
    
    private void InitRaid() {
        curRaid.state = RaidState.None;
        curRaid.temp.Reset();
        
        Cursor.visible = false;
        ShowRaidUI();

        ui.deathBgImage.enabled = false;
        curRaid.mapInstance.gameObject.SetActive(true);

        int randomSpawnIndex = Random.Range(0, curRaid.mapInstance.spawnPositionsParent.childCount);
        Vector2 randomSpawnPos = curRaid.mapInstance.spawnPositionsParent.GetChild(randomSpawnIndex).position;
        
        player.position = randomSpawnPos;
        player.gameObject.SetActive(false);
        
        Vector3 cameraWarpTarget = new(player.position.x, player.position.y, camera.cinemachine.transform.position.z);
        camera.cinemachine.ForceCameraPosition(cameraWarpTarget, Quaternion.identity);
        camera.cinemachine.Follow = player.trans;
        
        InitMapGrid();
        InitSpawnManager(curRaid.map.waves);
        SpawnMapResources(curRaid.mapInstance.resourceParent);
        SpawnInitialExitPortals(curRaid.mapInstance.exitPortalsParent, curRaid.map.exitPortalsCount);
        AnimateRaidEnterSequence();
    }
    
    private void UpdateRaidState() {
        RaidState prevState = curRaid.state;
        
        if (spawnManager.timeUntilFinalPhase >= 0f) {
            curRaid.state = RaidState.InitialWaves;
        }
        else if (!spawnManager.isFinishedSpawning || enemies.Count > 0) {
            curRaid.state = RaidState.FinalWave;
        }
        else {
            curRaid.state = RaidState.PostFinalWave;
        }

        curRaid.stateSwitchedThisFrame = prevState != curRaid.state;
        
        if (curRaid.stateSwitchedThisFrame && curRaid.state == RaidState.FinalWave) {
            PlayAudioClip(audio.finalWaveStingerClip, player.position);
        }

        if (curRaid.stateSwitchedThisFrame && curRaid.state == RaidState.PostFinalWave) {
            Tween.Delay(0.25f, static () => {
                gameInstance.AnimateLargeRaidText(ColorText("Map Cleared!", Styles.instance.increaseDescColor), 1.8f);
                gameInstance.SpawnFinalExitPortal();
            });
        }
    }
    
    private void DeinitRaid() {
        enemies.Clear();
        projectiles.Clear();
        DeinitMapGrid();
        DestroyEntities(EntityLifetime.Level);
        UnloadCurrentMapAsync();
    }
    
    private void OnSaveWhenRaidIsOver() {
        SaveInventory(inventories.player);
        SavePlayerData();
        SaveActiveQuestProgresses();
    }
    
    private void OnMapLoaded(MapData map) {
        CreateDropPoolsForMap(map);
        states.gameStateMachine.SetStateIfNotCurrent(states.raid);
    }
    
    // *******************************
    // Animation Sequences
    // *******************************
    
    private Sequence raidEnterSequence;
    
    private void AnimateRaidEnterSequence() {
        int initialPPU = camera.pixelPerfect.assetsPPU;
        camera.pixelPerfect.assetsPPU = 80;
            
        raidEnterSequence = Sequence.Create();
            
        ui.deathBgImage.enabled = true;
        ui.deathBgImage.fillAmount = 1f;
        raidEnterSequence.Chain(Tween.Alpha(ui.deathBgImage, 1f, 0f, 0.5f, Ease.InCubic));
            
        raidEnterSequence.ChainDelay(0.25f);

        raidEnterSequence.ChainCallback(() => {
            Entity inTeleportEntity = SpawnEntity(entityPools.teleportIn, OffsetY(player.position, -0.05f), Quaternion.identity);
            DestroyEntity(inTeleportEntity, CurrentClipLength(inTeleportEntity.animator));
            PlayAudioClip(audio.teleportInClip, inTeleportEntity.position);
        });
            
        raidEnterSequence.ChainDelay(0.35f);
        raidEnterSequence.ChainCallback(() => {
            player.gameObject.SetActive(true);
            InitPlayer();
        });
        raidEnterSequence.Chain(Tween.Scale(player.trans, 0f, 1f, 0.2f, Ease.InOutBack));
            
        raidEnterSequence.ChainDelay(0.6f);
        raidEnterSequence.Chain(Tween.Custom(camera.pixelPerfect.assetsPPU, initialPPU, 0.25f, ease: Ease.OutQuad, onValueChange: val => {
            camera.pixelPerfect.assetsPPU = (int)val;
        }));
    }

    private void AnimateGameOverSequence(Action onCompleteCallback) {
        Tween.StopAll();
        
        foreach (Entity entity in entities.all) {
            if (entity.rigidbody) {
                entity.rigidbody.linearVelocity = Vector2.zero;
            }
            if (entity.animator) {
                entity.animator.enabled = false;
            }
        }
        
        player.spriteRenderer.sortingLayerName = "DeathWipe";
        
        player.GetEffect(EffectsIndicies.HitFlash).Complete();
        player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
        player.matPropertyBlock.SetFloat(damageFlashTintPropertyId, 1f);
        player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        
        ui.deathBgImage.enabled = true;
        ui.deathBgImage.fillAmount = 0f;
        ui.deathBgImage.color = ui.deathBgImage.color.Alpha(1f);

        Sequence sequence = Sequence.Create();
        sequence.ChainDelay(0.25f);
        sequence.Chain(Tween.UIFillAmount(ui.deathBgImage, 1f, 1f, Ease.InOutQuad));
        sequence.ChainCallback(() => {
            player.animator.enabled = true;
            player.animator.Play(player.deathAnim);
        });
        
        sequence.Group(Tween.Custom(1f, 0f, 0.5f, val => {
            player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
            player.matPropertyBlock.SetFloat(damageFlashTintPropertyId, val);
            player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        }, Ease.OutExpo));
        
        int initialPPU = camera.pixelPerfect.assetsPPU;
        
        sequence.Group(Tween.Custom(camera.pixelPerfect.assetsPPU, 80, 0.8f, val => {
            camera.pixelPerfect.assetsPPU = (int)val;
        }, Ease.InOutQuad));

        sequence.Group(Tween.Delay(0.25f, () => AnimateLargeRaidText(ColorText("YOU DIED", Styles.instance.decreaseDescColor), 1f)));
        
        sequence.ChainDelay(1f);
        
        ui.animatedBgImage.gameObject.SetActive(true);
        ui.animatedBgImage.color = new(1f, 1f, 1f, 0f);
        sequence.Chain(Tween.Alpha(ui.animatedBgImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.5f));

        sequence.Group(Tween.Scale(player.trans, Vector3.zero, 1.5f, Ease.InOutQuint, startDelay: 0.35f));
        
        sequence.OnComplete(() => {
            player.spriteRenderer.sortingLayerName = "Entity";
            player.trans.localScale = Vector3.one;
            camera.pixelPerfect.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }
    
    private void AnimateGameWinSequence(Action onCompleteCallback) {
        Entity outTeleportFxEntity = SpawnEntity(entityPools.teleportOut, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(audio.teleportOutClip, outTeleportFxEntity.position);
        player.gameObject.SetActive(false);
        
        Sequence sequence = Sequence.Create();

        int initialPPU = camera.pixelPerfect.assetsPPU;
        sequence.Chain(Tween.Custom(camera.pixelPerfect.assetsPPU, 80, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            camera.pixelPerfect.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.15f);
        
        ui.deathBgImage.enabled = true;
        ui.deathBgImage.fillAmount = 1f;
        sequence.Chain(Tween.Alpha(ui.deathBgImage, 0f, 1f, 0.75f, Ease.InOutQuad));
        
        ui.animatedBgImage.gameObject.SetActive(true);
        ui.animatedBgImage.color = new(1f, 1f, 1f, 0f);
        sequence.Group(Tween.Alpha(ui.animatedBgImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.1f));
        sequence.ChainDelay(0.15f);

        sequence.OnComplete(() => {
            player.gameObject.SetActive(true);
            camera.pixelPerfect.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }
    
    private void AnimateEarlyExitSequence(Action onCompleteCallback) {
        Entity outTeleportFxEntity = SpawnEntity(entityPools.teleportOut, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(audio.teleportOutClip, outTeleportFxEntity.position);
        player.gameObject.SetActive(false);
        
        Sequence sequence = Sequence.Create();

        int initialPPU = camera.pixelPerfect.assetsPPU;
        sequence.Chain(Tween.Custom(camera.pixelPerfect.assetsPPU, 80, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            camera.pixelPerfect.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.05f);
        sequence.Chain(Tween.Scale(exitPortalTakenByPlayer.transform, Vector3.zero, 0.25f, Ease.InOutBounce));
        
        sequence.ChainDelay(0.15f);
        
        ui.deathBgImage.enabled = true;
        ui.deathBgImage.fillAmount = 1f;
        sequence.Chain(Tween.Alpha(ui.deathBgImage, 0f, 1f, 0.75f, Ease.InOutQuad));
        
        sequence.Group(Tween.Delay(0.35f, () => AnimateLargeRaidText(ColorText("EARLY EXIT TAKEN", Styles.instance.increaseDescColor), 3.8f)));
        
        ui.animatedBgImage.gameObject.SetActive(true);
        ui.animatedBgImage.color = new(1f, 1f, 1f, 0f);
        sequence.Group(Tween.Alpha(ui.animatedBgImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.1f));
        sequence.ChainDelay(1.6f);

        sequence.OnComplete(() => {
            player.gameObject.SetActive(true);
            camera.pixelPerfect.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }

}