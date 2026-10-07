using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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
    public Curves curves;
    public UI ui;
    public InputPrompts inputPrompts;
    public PlayerInfo playerInfo;
    public RaidInfo raidInfo;
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
    
    [NonSerialized] public readonly GameData.Input input = new();
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
    
    private static readonly int unscaledTimeShaderId = Shader.PropertyToID("_UnscaledTime");
    
    private void Update() {
        UpdateMenuNavigation(); // !
        // Animated UI shaders use this instead of the Time node so they keep animating while the game is paused
        Shader.SetGlobalFloat(unscaledTimeShaderId, Time.unscaledTime);
        states.gameStateMachine.Tick();
        if (!pauseMenu.paused) {
            UpdateDialogue(); // Outside of the tutorial because cutscenes use dialogue too
            UpdateTutorial(); // !
        }
        DemonEyeTween.Update();
        UpdateQuests(); // !
        UpdateInputPrompts();
        ClearPerFrameData();
        CheckForScreenSizeChange();
        CheckForInputDeviceChange();
        
#if UNITY_EDITOR
        if (pauseMenu.paused) {
            // Pausing owns the time scale
        }
        else if (Mouse.current != null && Mouse.current.middleButton.isPressed) {
            Time.timeScale = 4f;
        }
        else {
            Time.timeScale = 1f;
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
        Cursor.visible = !usingController;
        ShowMainMenuUI();
        PlayMusic(music.mainMenuMusic, MusicOption.Fast);
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
        UpdateGameplayMusic();
        CheckForExitPortalCutscene();
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
        persistentFlags |= PersistentFlags.HasExtracted;
        SaveGameState();
        AnimateEarlyExitSequence(() => states.gameStateMachine.SetStateIfNotCurrent(states.mainMenu));
        StopMusic(MusicOption.Fast); // Needs to be after animation sequence because it stops all tweens
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
        }
        persistentFlags |= PersistentFlags.HasExtracted;
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

        ui.deathBgImage.enabled = false;
        curRaid.mapInstance.gameObject.SetActive(true);

        int randomSpawnIndex = Random.Range(0, curRaid.mapInstance.spawnPositionsParent.childCount);
        Vector2 randomSpawnPos = curRaid.mapInstance.spawnPositionsParent.GetChild(randomSpawnIndex).position;
        
        player.position = randomSpawnPos;
        player.gameObject.SetActive(false);
        
        Vector3 cameraWarpTarget = new(player.position.x, player.position.y, camera.cinemachine.transform.position.z);
        camera.cinemachine.ForceCameraPosition(cameraWarpTarget, Quaternion.identity);
        camera.cinemachine.Follow = player.trans;
        
        ShowRaidUI();
        InitMapGrid();
        CreateDropPoolsForMap(curRaid.map);
        InitSpawnManager(curRaid.map.spawning);
        SpawnMapResources(curRaid.mapInstance.resourceParent);
        SpawnInitialExitPortals(curRaid.mapInstance.exitPortalsParent, curRaid.map.exitPortalsCount);
        AnimateRaidEnterSequence();
        PlayAudioClip(audio.ambientClip, Vector2.zero, loop: true);
        
        WaterFeature.waterSettings = curRaid.map.waterSettings;
        thisFrame.flags |= FrameFlags.PostInitRaid;
    }
    
    private void UpdateRaidState() {
        RaidState prevState = curRaid.state;
        
        if (spawnManager.BeforeLastWave) {
            curRaid.state = RaidState.InitialWaves;
        }
        else if (!spawnManager.FinishedSpawningThisWave || entities.enemies.Count > 0) {
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
        entities.projectiles.Clear();
        entities.soulTrackingProjectiles.Clear();
        DeinitMapGrid();
        DestroyEntities(EntityLifetime.Level);
        UnloadCurrentMapAsync();
    }

    // *******************************
    // Animation Sequences
    // *******************************
    
    private Sequence raidEnterSequence;
    
    private void AnimateRaidEnterSequence() {
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
        raidEnterSequence.Chain(Tween.Custom(camera.pixelPerfect.assetsPPU, camera.defaultPPU, 0.25f, ease: Ease.OutQuad, onValueChange: val => {
            camera.pixelPerfect.assetsPPU = (int)val;
        }));
    }

    private void AnimateGameOverSequence(Action onCompleteCallback) {
        Tween.StopAll();
        DemonEyeTween.StopAll();
        
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
        player.gameObject.layer = LayerMask.NameToLayer("UI");
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
            player.animator.Play(PlayerAnimations.death);
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
        
        sequence.ChainDelay(0.25f);
        
        ui.deathBgImage.enabled = true;
        ui.deathBgImage.fillAmount = 1f;
        sequence.Chain(Tween.Alpha(ui.deathBgImage, 0f, 1f, 0.75f, Ease.InOutQuad));
        
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

    [NonSerialized] public bool usingController; // Whether the controller selection is used instead of the mouse cursor
    [NonSerialized] public bool onSteamDeck;

    // Whether buttons and prompts work like a controller. On the Steam Deck they always do, even while its touchpad is moving the cursor.
    public bool UsingControllerControls => usingController || onSteamDeck;

    private void InitInputDevice() {
        onSteamDeck = DetectSteamDeck();
        usingController = onSteamDeck;
    }

    // Steam sets SteamDeck=1 for games running on the Deck, for both Linux builds and Windows builds running through Proton.
    // The APU names (LCD and OLED models) are a fallback for when the game isn't launched through Steam.
    private bool DetectSteamDeck() {
        if (Environment.GetEnvironmentVariable("SteamDeck") == "1") return true;
        string processor = SystemInfo.processorType;
        return processor.Contains("AMD Custom APU 0405") || processor.Contains("AMD Custom APU 0932");
    }

    // Switches between mouse & keyboard and controller based on whichever was used last
    private void CheckForInputDeviceChange() {
        // On the Steam Deck buttons can be used along with the touchpad's cursor, so only moving the selection gets rid of the cursor
        bool gamepadUsed = onSteamDeck ? GamepadNavigatedThisFrame() : GamepadUsedThisFrame();
        bool switchToController = !usingController && gamepadUsed;
        // The Steam Deck's touchpads act as a mouse, so they switch to the cursor. It has no keyboard, its buttons are all on the controller.
        bool switchToKeyboardMouse = usingController && (MouseUsedThisFrame() || (!onSteamDeck && KeyboardUsedThisFrame()));
        if (!switchToController && !switchToKeyboardMouse) return;

        usingController = switchToController;
        input.lastDeviceSwitchTime = Time.unscaledTime;
        MenuNavigationOnInputDeviceChanged();
        InputIconsOnInputDeviceChanged();
    }

    // Either stick or the d-pad
    private bool GamepadNavigatedThisFrame() {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return false;

        const float deadzone = ControllerNavigation.stickDeadzone;
        return gamepad.leftStick.ReadValue().magnitude > deadzone
            || gamepad.rightStick.ReadValue().magnitude > deadzone
            || gamepad.dpad.ReadValue() != Vector2.zero;
    }

    private bool GamepadUsedThisFrame() {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null) return false;
        if (GamepadNavigatedThisFrame()) return true;

        foreach (InputControl control in gamepad.allControls) {
            // Synthetic buttons are the stick directions, which are handled above with a deadzone
            if (control is ButtonControl { synthetic: false } button && button.wasPressedThisFrame) {
                return true;
            }
        }
        return false;
    }

    private bool MouseUsedThisFrame() {
        Mouse mouse = Mouse.current;
        return mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame);
    }

    private bool KeyboardUsedThisFrame() {
        return Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
    }

}