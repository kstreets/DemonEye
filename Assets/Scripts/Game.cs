using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using static GameData;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using EffectsIndicies = Game.Entity.EffectsIndicies;

public partial class Game : MonoBehaviour {

    public static Game gameInstance;
    
    public Config config;
    public DropPools dropPools;
    public Prefabs prefabs;
    public ItemTypes itemTypes;
    public Quests quests;
    public ItemRefs itemRefs;
    public SkillUpgradePaths skillUpgradePaths;
    public GameData.Camera camera;
    public Blood blood;
    public Curves curves;
    public UI ui;
    public InputPrompts inputPrompts;
    public PlayerInfo playerInfo;
    public RaidInfo raidInfo;
    public BossBar bossBar;
    public MainMenu mainMenu;
    public PauseMenu pauseMenu;
    public HideoutTabs hideoutTabs;
    public PlayerPanel playerPanel;
    public StashPanel stashPanel;
    public EyeForgePanel eyeForgePanel;
    public EyeForgeDetailsPanel eyeForgeDetailsPanel;
    public TraderPanel traderPanel;
    public GameData.TransactionPanel transactionPanel;
    public MapPanels mapPanels;
    public QuestsPanel questsPanel;
    public GameData.SkillsPanel skillsPanel;
    public Audio audio;
    public Music music;
    public Settings settings;
    
    [NonSerialized] public readonly Inputs inputs = new();
    [NonSerialized] public readonly EntityPools entityPools = new();
    [NonSerialized] public readonly States states = new();
    [NonSerialized] public readonly Entities entities = new();
    [NonSerialized] public readonly GameData.Resources res = new();
    [NonSerialized] public readonly DemonEye demonEye = new();
    [NonSerialized] public readonly Trinkets trinkets = new();
    [NonSerialized] public readonly CurrentGamingSession curSession = new();
    [NonSerialized] public readonly CurrentRaid curRaid = new();
    [NonSerialized] public readonly Inventories inventories = new();
    [NonSerialized] public readonly HotBar hotBar = new();
    [NonSerialized] public readonly PerFrameData thisFrame = new();
    [NonSerialized] public readonly ControllerNavigation controllNav = new();
    [NonSerialized] public readonly Tutorial tutorial = new();
    [NonSerialized] public readonly Cutscene cutscene = new();

    
    [NonSerialized] public HideoutState hideoutState;
    [NonSerialized] public PersistentFlags persistentFlags;

    private void Start() {
        gameInstance = this;
        InitGame();
    }

    private void OnApplicationQuit() {
        SetDeathWipe(0f, 0f);
        ClearDamageVignette();
        ShutdownInput(); // Before the platform, since Steam Input has to shut down before Steam
        ShutdownPlatform();
    }

    private void Update() {
        UpdatePlatform();
        UpdateInput();
        UpdateMenuNavigation(); // !
        states.gameStateMachine.Tick();
        UpdateTutorial(); // !
        UpdateDialogue();
        DemonEyeTween.Update();
        UpdatePositionalVolumes();
        UpdateMenuMusic();
        UpdateQuests(); // !
        UpdateInputPrompts();
        UpdateSettings();
        CheckForScreenSizeChange();
        UpdateCursorStyle();
        UpdateGlobalShaders();
        ClearPerFrameData();

#if UNITY_EDITOR
        if (!pauseMenu.paused) {
            if (Mouse.current != null && Mouse.current.middleButton.isPressed) {
                Time.timeScale = 4f;
            }
            else {
                Time.timeScale = 1f;
            }
        }
        
        if (Keyboard.current.slashKey.wasPressedThisFrame) {
            for (int i = entities.enemies.Count - 1; i >= 0; i--) {
                DestroyEntity(entities.enemies[i]);
                entities.enemies.RemoveAt(i);
            }
            spawnManager.spawnTimeIndex = int.MaxValue;
            spawnManager.timeInCurPhase = 9999999f;
            spawnManager.startNextWaveDelay = 0f;
        }
#endif
    }
    
    private void FixedUpdate() {
        states.gameStateMachine.Tick(StateMachine.UpdateMode.FixedUpdate);
    }

    private void LateUpdate() {
        states.gameStateMachine.Tick(StateMachine.UpdateMode.LateUpdate);
    }
    
    private void UpdateTimers() {
        curRaid.data.interactions.discoverItemTimer.Tick();
    }
    
    private void ClearPerFrameData() {
        thisFrame.flags = FrameFlags.None;
        thisFrame.enemyKillCount.Clear();
        thisFrame.data.Reset();
    }

    private void OnMainMenuStateEnter() {
        Cursor.visible = ShowCursorInMenus;
        ShowMainMenuUI();
        OnMenuMusicEnter();
        TryShowDemoEndDialogue();
    }

    private void OnMainMenuStateExit() {
        CloseMainMenuUI();
    }
    
    private void OnHideoutStateEnter() {
        ShowHideoutUI();
        SuppressInventoryPopup();
        RefreshSkillsPanel();
        UpdateHideoutNotifiers();
        // Make the pentagram default to crafting mode
        eyeForgePanel.toggleButtonGroup.ManualyToggle(eyeForgePanel.forgeToggle);
        TutorialOnHideoutEnter();
    }

    private void OnHideoutStateExit() {
        CloseHideoutUI();
        SaveGameState();
    }
    
    private void OnHideoutStateUpdate() {
        UpdateHideoutNotifiers();
        UpdateInventory();
        UpdateUIHints();
        UpdateTransactionUI();
        UpdateForgeState();
        UpdateForgeInfoPanel();
        RefreshAllInventoryDisplays();
        UpdateGraySlots();
        UpdateForgePanel();
    }

    private void OnHideoutStateLateUpdate() {
        UpdatePlayerPanelUI();
        UpdateDragAndDropItemToCursor();
        UpdateCurrencyNumbers();
    }

    private void OnMapSelectionEnter() {
        ShowMapSelectionUI();
        SuppressInventoryPopup();
        PlayPendingMapUnlockBurns();
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
        DemonEyeOnRaidEnter();
        OnMenuMusicExit();
        StopMusic(MusicOption.Smooth);
    }

    private void OnRaidStateExit() {
        ResumeRaid();
        DeinitPlayer();
        ClosePlayerInventory();
        CloseLootInventory();
        HideInventoryItemPopup();
        HideInteractionPopup();
        HideHint();
        CloseRaidUI();
        StopAllAudioClips();
        TraderOnExitRaid();
        DestroyAllProjectiles();
        DestroyAllHazards();
    }

    private void OnRaidStateUpdate() {
        UpdateRaidState();
        UpdateMapGrid();
        UpdateTimers();
        CheckForInteractions();
        CheckForHotBarInteractions();
        UpdateInventory();
        UpdatePlayer();
        UpdateAimLookAhead();
        UpdateProjectiles();
        UpdateHazards();
        UpdateSpawnManager();
        UpdateEnemies();
        RefreshAllInventoryDisplays();
        UpdateGameplayMusic();
        CheckForExitPortalCutscene();
    }

    private void OnRaidStateFixedUpdate() {
        FixedUpdateEnemies();
    }

    private void OnRaidStateLateUpdate() {
        FlushBloodStamps();
        UpdateInRaidUI();
        UpdatePlayerPanelUI();
        UpdateHotBarUI();
        UpdateDragAndDropItemToCursor();
        UpdateCurrencyNumbers();
    }

    private void OnEarlyExitEnter() {
        persistentFlags |= PersistentFlags.HasExtracted;
        FlagStashTransferButtonIfPocketsHaveItems();
        SaveGameState();
        AnimateEarlyExitSequence(() => states.gameStateMachine.SetStateIfNotCurrent(states.mainMenu));
        StopMusic(MusicOption.Fast); // Needs to be after animation sequence because it stops all tweens
    }
    
    // The transfer button only moves pocket/backpack items, so there's nothing for it to do if they're empty
    private void FlagStashTransferButtonIfPocketsHaveItems() {
        if (GetInventoryItemCountInRange(inventories.player, playerPocketRange) > 0) {
            persistentFlags |= PersistentFlags.ShowStashTransferButton;
        }
    }

    private void OnEarlyExitExit() {
        DeinitRaid();
    }
    
    private void OnWinExitEnter() {
        var maps = config.maps;
        int nextMapIndex = maps.IndexOf(curRaid.map) + 1;
        bool unlockNextMap = maps.IndexInRange(nextMapIndex) && !maps[nextMapIndex].state.isUnlocked;
        if (unlockNextMap) {
            maps[nextMapIndex].state.isUnlocked = true;
            maps[nextMapIndex].state.unlockRevealPending = true;
        }
        
        persistentFlags |= PersistentFlags.HasExtracted;
        FlagStashTransferButtonIfPocketsHaveItems();

        // Beating the second map is the end of the demo, the trader thanks the player once they're back in the main menu
        bool beatSecondMap = maps.Count > 1 && curRaid.map == maps[1];
        if (beatSecondMap && !persistentFlags.HasFlag(PersistentFlags.DemoEndShown)) {
            persistentFlags |= PersistentFlags.DemoEndShown;
            cutscene.showDemoEndOnMainMenu = true;
        }

        SaveGameState();
        AnimateGameWinSequence(() => states.gameStateMachine.SetStateIfNotCurrent(states.mainMenu));
    }

    private void OnWinExitExit() {
        DeinitRaid();
    }

    private void OnGameOverEnter() {
        ClearInventory(inventories.player);
        SaveGameState();
        AnimateGameOverSequence(() => states.gameStateMachine.SetStateIfNotCurrent(states.mainMenu)); 
        StopMusic(MusicOption.Fast); // Needs to be after animation sequence because it stops all tweens
        PlayAudioClip(audio.deathStingerClip);
    }
    
    private void OnGameOverExit() {
        // Pooled entities would otherwise come back without physics
        foreach (Entity entity in entities.all) {
            if (entity.rigidbody) {
                entity.rigidbody.simulated = true;
            }
        }
        RemovePoisonedEffect(player); // Kept through the death animation, removed before the level entities are destroyed
        player.health = FullPlayerHealth();
        DeinitRaid();
    }
    
    public enum RaidState { None, InitialWaves, FinalWave, PostFinalWave }
    
    private void InitRaid() {
        curRaid.state = RaidState.None;
        curRaid.data.Reset();
        curRaid.teleportingInPositions.Clear();
        
        curSession.raidsEntered++;
        
        Cursor.visible = false;

        SetDeathWipe(0f, 0f);
        ClearDamageVignette();
        curRaid.mapInstance.gameObject.SetActive(true);

        int randomSpawnIndex = Random.Range(0, curRaid.mapInstance.spawnPositionsParent.childCount);
        Vector2 randomSpawnPos = curRaid.mapInstance.spawnPositionsParent.GetChild(randomSpawnIndex).position;
        
        player.position = randomSpawnPos;
        player.gameObject.SetActive(false);
        
        Vector3 cameraWarpTarget = new(player.position.x, player.position.y, camera.cinemachine.transform.position.z);
        camera.cinemachine.ForceCameraPosition(cameraWarpTarget, Quaternion.identity);
        camera.cinemachine.Follow = player.trans;
        ResetAimLookAhead();
        
        ShowRaidUI();
        InitMapGrid();
        InitRaidBlood();
        CreateDropPoolsForMap(curRaid.map);
        InitSpawnManager(curRaid.map.spawning);
        SpawnInitialExitPortals(curRaid.mapInstance.exitPortalsParent, curRaid.map.exitPortalsCount);
        SpawnMapResources(curRaid.mapInstance.resourceParent); // After portals since some resources depend on which portals spawned
        AnimateRaidEnterSequence();
        PlayAudioClip(audio.ambientClip, Vector2.zero, loop: true);
        
        thisFrame.flags |= FrameFlags.PostInitRaid;
    }
    
    private void UpdateRaidState() {
        if (curRaid.mapInstance.isBossMap) return;
        
        RaidState prevState = curRaid.state;
        
        if (spawnManager.BeforeLastWave) {
            curRaid.state = RaidState.InitialWaves;
        }
        else if (!spawnManager.FinishedSpawningThisWave || entities.enemies.Count > 0 || spawnManager.SwarmPending) {
            curRaid.state = RaidState.FinalWave;
        }
        else {
            curRaid.state = RaidState.PostFinalWave;
        }

        curRaid.stateSwitchedThisFrame = prevState != curRaid.state;
        
        if (curRaid.stateSwitchedThisFrame && curRaid.state == RaidState.FinalWave) {
            PlayAudioClip(audio.finalWaveStingerClip, player.position, cannotInterrupt: true);
        }
        else if (spawnManager.waveStartedThisFrame) {
            PlayAudioClip(audio.nextWaveStingerClip, player.position, cannotInterrupt: true);
        }

        if (curRaid.stateSwitchedThisFrame && curRaid.state == RaidState.PostFinalWave) {
            Tween.Delay(0.25f, static () => {
                gameInstance.AnimateLargeRaidText(ColorText("Map Cleared!", gameInstance.config.styles.increaseDescColor), 1.8f);
                bool spawnedOk = gameInstance.SpawnFinalExitPortal();
                if (!spawnedOk) { // This is a fail safe incase we couldn't spawn the final portal
                    gameInstance.states.gameStateMachine.SetState(gameInstance.states.winExit);
                }
            });
        }
    }
    
    private void DeinitRaid() {
        entities.enemies.Clear();
        ClearRangedAttackManager();
        entities.boss = null;
        entities.projectiles.Clear();
        entities.soulTrackingProjectiles.Clear();
        entities.hazards.Clear();
        DeinitMapGrid();
        DeinitRaidBlood();
        DestroyEntities(EntityLifetime.Level);
        UnloadCurrentMapAsync();
    }

    // *******************************
    // Animation Sequences
    // *******************************
    
    private int ZoomedInCameraPPU => Mathf.RoundToInt(camera.defaultPPU * 1.6f);

    private Sequence raidEnterSequence;
    
    private void AnimateRaidEnterSequence() {
        camera.pixelPerfect.assetsPPU = ZoomedInCameraPPU;
            
        raidEnterSequence = Sequence.Create();
            
        SetDeathWipe(1f, 1f);
        raidEnterSequence.Chain(TweenDeathWipeAlpha(1f, 0f, 0.5f, Ease.InCubic));
            
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
        raidEnterSequence.Chain(Tween.Custom(camera.pixelPerfect.assetsPPU, camera.defaultPPU, 0.25f, ease: Ease.OutQuad, onValueChange: val => {
            camera.pixelPerfect.assetsPPU = (int)val;
        }));
    }

    private void AnimateGameOverSequence(Action onCompleteCallback) {
        Tween.StopAll();
        DemonEyeTween.StopAll();
        DismissNotificationsAfterTweensStopped();
        FadeoutAimLookAhead();

        foreach (Entity entity in entities.all) {
            if (entity.rigidbody) {
                entity.rigidbody.linearVelocity = Vector2.zero;
                entity.rigidbody.simulated = false; // Stops overlapping enemies from pushing each other apart, restored in OnGameOverExit
            }
            if (entity.animator) {
                entity.animator.enabled = false;
            }
        }
        
        // We want the player to be infront of the UI, so we move it to the UI layer temporarily
        player.gameObject.layer = LayerMask.NameToLayer("DeathWipe");
        player.spriteRenderer.sortingLayerName = "DeathWipe";

        player.GetEffect(EffectsIndicies.HitFlash).Complete();
        player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
        player.matPropertyBlock.SetFloat(ShaderIds.damageFlashTint, 1f);
        player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        
        SetDeathWipe(0f, 1f);

        Sequence sequence = Sequence.Create();
        sequence.ChainDelay(0.25f);
        sequence.Chain(TweenDeathWipeFill(0f, 1f, 1f, Ease.InOutQuad));
        sequence.ChainCallback(() => {
            player.animator.enabled = true;
            player.animator.Play(PlayerAnimations.death);
        });
        
        sequence.Group(Tween.Custom(1f, 0f, 0.5f, val => {
            player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
            player.matPropertyBlock.SetFloat(ShaderIds.damageFlashTint, val);
            player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        }, Ease.OutExpo));
        
        int initialPPU = camera.pixelPerfect.assetsPPU;
        
        sequence.Group(Tween.Custom(camera.pixelPerfect.assetsPPU, ZoomedInCameraPPU, 0.8f, val => {
            camera.pixelPerfect.assetsPPU = (int)val;
        }, Ease.InOutQuad));

        sequence.Group(Tween.Delay(0.25f, () => AnimateLargeRaidText(ColorText("YOU DIED", gameInstance.config.styles.decreaseDescColor), 1f)));
        
        sequence.ChainDelay(1f);
        
        ui.animatedBgImage.gameObject.SetActive(true);
        ui.animatedBgImage.color = new(1f, 1f, 1f, 0f);
        sequence.Chain(Tween.Alpha(ui.animatedBgImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.5f));

        sequence.Group(Tween.Scale(player.trans, Vector3.zero, 1.5f, Ease.InOutQuint, startDelay: 0.35f));
        
        sequence.OnComplete(() => {
            player.spriteRenderer.sortingLayerName = "Entity";
            player.gameObject.layer = LayerMask.NameToLayer("Player");
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
        sequence.Chain(Tween.Custom(camera.pixelPerfect.assetsPPU, ZoomedInCameraPPU, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            camera.pixelPerfect.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.15f);
        
        SetDeathWipe(1f, 0f);
        sequence.Chain(TweenDeathWipeAlpha(0f, 1f, 0.75f, Ease.InOutQuad));

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
        
        Tween.Delay(0.15f, () => PlayAudioClip(audio.extractionMelody, Vector2.zero, cannotInterrupt: true));
        
        Sequence sequence = Sequence.Create();

        int initialPPU = camera.pixelPerfect.assetsPPU;
        sequence.Chain(Tween.Custom(camera.pixelPerfect.assetsPPU, ZoomedInCameraPPU, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            camera.pixelPerfect.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.25f);
        
        SetDeathWipe(1f, 0f);
        sequence.Chain(TweenDeathWipeAlpha(0f, 1f, 0.75f, Ease.InOutQuad));

        sequence.Group(Tween.Delay(0.35f, () => AnimateLargeRaidText(ColorText("SUCCESSFUL EXTRACT", gameInstance.config.styles.increaseDescColor), 3.8f)));
        
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
    
    private Vector2Int lastScreenSize;
    
    private void CheckForScreenSizeChange() {
        if (lastScreenSize == ScreenSize) return;
        SettingsOnScreenSizeChanged();
        UIOnScreenSizeChanged();
        lastScreenSize = ScreenSize;
    }

}