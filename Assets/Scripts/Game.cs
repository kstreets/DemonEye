using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using VInspector;
using Vector3 = UnityEngine.Vector3;
using EffectsIndicies = Game.Entity.EffectsIndicies;

public partial class Game : MonoBehaviour {

    public static Game gameInstance;
    public GameData gameData;
    
    [Foldout("UI/PlayerPanel")]
    public RectTransform playerPanel;
    public RectTransform playerEquipmentParent;
    public RectTransform playerPocketParent;
    public RectTransform playerPocketsBackpackParent;
    public RectTransform playerPassiveParent;
    public RectTransform playerInventoryParent;
    public TextMeshProUGUI playerPanelHealthText;
    public TextMeshProUGUI playerPanelWeightText;
    public Image playerPreviewImage;
    public EquipedStatsPanel equipedStatsPanel;
    [EndFoldout]
    
    [Foldout("UI/StashPanel")]
    public RectTransform stashPanel;
    public RectTransform stashInventoryParent;
    [EndFoldout]
    
    [Foldout("UI/EyeForgePanel")]
    public RectTransform eyeForgePanel;
    public RectTransform crucibleParent;
    public Image pentagramFillImage;
    public AnimationCurve pentagramFillCurve;
    public AnimationCurve itemShakeCurve;
    [EndFoldout]
    
    [Foldout("UI/ForgeDetailsPanel")]
    public RectTransform forgeDetailsPanel;
    public ButtonFeel forgeEyeButton;
    public TextMeshProUGUI forgeDetailsForgeText;
    public DemonEyeDescList forgeDetailsDemonEyeDesc;
    [EndFoldout]
    
    [Foldout("UI/TraderPanel")]
    public RectTransform traderTransactionPanel;
    public RectTransform traderInventoryPanel;
    public RectTransform traderInventoryParent;
    public RectTransform traderTransactionInventoryParent;
    public TraderRepBar traderRepBarInTrading;
    public TextMeshProUGUI traderItemRefreshTimeText;
    public TransactionPanel transactionPanel;
    [EndFoldout]

    [Foldout("UI/MapSelectionPanel")]
    public RectTransform mapSelectionPanel;
    public Button[] mapSelectionButtons;
    [EndFoldout]
    
    [Foldout("UI/QuestsPanel")]
    public RectTransform questsPanel;
    public RectTransform questsParent;
    public RectTransform questSelectionParent;
    public ToggleButtonGroup questToggleButtonGroup;
    public TraderRepBar traderRepBarInQuests;
    [EndFoldout]
    
    [Foldout("UI/SkillsTab")]
    public SkillsPanel skillsPanel;
    public PlayerStatsPanel playerStatsPanel;
    [EndFoldout]

    [Foldout("UI/Health&Currency")]
    public GameObject playerInfoParent;
    public GameObject healthBarParent;
    public GameObject weightBarParent;
    public GameObject soulsCurrencyParent;
    public GameObject coinsCurrencyParent;
    public GameObject bleedDebuffIcon;
    public TextMeshProUGUI soulsCurrencyText;
    public TextMeshProUGUI coinCurrencyText;
    [EndFoldout]
    
    [Foldout("UI/InRaid")]
    public RectTransform lootInventoryPanel;
    public RectTransform lootInventoryParent;
    public GameObject lootSearchingText;
    public Image healthBarFillImage;
    public Image weightBarFillImage;
    public TextMeshProUGUI interactPrompt;
    public TextMeshProUGUI interactionDetails;
    public RectTransform portalArrow;
    [EndFoldout]

    [Foldout("UI/RaidInfoPanel")]
    public GameObject raidInfoPanelParent;
    public GameObject finalWaveCountdownParent;
    public TextMeshProUGUI finalWaveCountdownText;
    public GameObject exitPortalCountdownParent;
    public TextMeshProUGUI exitPortalCountdownText;
    public GameObject exitPortalActiveNotifier;
    public GameObject finalWaveActiveNotifier;
    public GameObject finalExitPortalNotifier;
    [EndFoldout]
    
    [Foldout("UI/DamageNumbers")]
    public RectTransform damageNumbersParent;
    [EndFoldout]
    
    [Foldout("UpgradePaths")]
    public UpgradePath crucibleUpgradePath; 
    [EndFoldout]
    
    [Foldout("TraderLevels")]
    public TraderLevels traderLevels;
    [EndFoldout]
    
    [Foldout("Sfx")]
    public GameObject dynamicAudioSourcePrefab;
    public DynamicClip shootClip;
    public DynamicClip stoneBreakClip;
    public DynamicClip stoneHitClip;
    public DynamicClip projectileImpact;
    public DynamicClip bloodBurstClip;
    public DynamicClip footStepClip;
    public DynamicClip teleportInClip;
    public DynamicClip teleportOutClip;
    public DynamicClip portalSpawnClip;
    public DynamicClip portalDespawnClip;
    public DynamicClip finalWaveStingerClip;
    public AudioClip ambienceClip;
    public AudioMixerGroup ambienceMixerGroup;
    [EndFoldout]
    
    public static Action<InventorySlot[]> onSoldItemsToTrader;
    public static Action<string> customQuestEvent;
    
    private void Start() {
        gameInstance = this;
        InitGame();
    }

    private void Update() {
        gameData.states.gameStateMachine.Tick();
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
        gameData.states.gameStateMachine.Tick(StateMachine.UpdateMode.FixedUpdate);
    }

    private void LateUpdate() {
        gameData.states.gameStateMachine.Tick(StateMachine.UpdateMode.LateUpdate);
    }

    private void OnApplicationQuit() {
        SaveTrader();
    }

    private void UpdateTimers() {
        gameData.curRaid.temp.interactionData.discoverItemTimer.Tick();
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
        SaveInventory(gameData.inventories.player);
        SaveInventory(gameData.inventories.stash);
        SaveInventory(gameData.inventories.eyeForge);
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
        AnimateEarlyExitSequence(() => gameData.states.gameStateMachine.SetStateIfNotCurrent(gameData.states.mainMenu));
    }
    
    private void OnEarlyExitExit() {
        DeinitRaid();
    }
    
    private void OnWinExitEnter() {
        var maps = gameData.config.maps;
        int nextMapIndex = maps.IndexOf(gameData.curRaid.map) + 1;
        bool unlockNextMap = maps.IndexInRange(nextMapIndex) && !maps[nextMapIndex].isUnlocked;
        if (unlockNextMap) {
            maps[nextMapIndex].isUnlocked = true;
            SaveMaps();
        }
        OnSaveWhenRaidIsOver();
        AnimateGameWinSequence(() => gameData.states.gameStateMachine.SetStateIfNotCurrent(gameData.states.mainMenu));
    }

    private void OnWinExitExit() {
        DeinitRaid();
    }

    private void OnGameOverEnter() {
        ClearInventory(gameData.inventories.player);
        OnSaveWhenRaidIsOver();
        AnimateGameOverSequence(() => gameData.states.gameStateMachine.SetStateIfNotCurrent(gameData.states.mainMenu)); 
    }
    
    private void OnGameOverExit() {
        player.health = FullPlayerHealth;
        DeinitRaid();
    }
    
    public enum RaidState { None, InitialWaves, FinalWave, PostFinalWave }
    
    private void InitRaid() {
        gameData.curRaid.state = RaidState.None;
        gameData.curRaid.temp.Reset();
        
        Cursor.visible = false;
        ShowRaidUI();

        gameData.ui.deathBgImage.enabled = false;
        gameData.curRaid.mapInstance.gameObject.SetActive(true);

        int randomSpawnIndex = Random.Range(0, gameData.curRaid.mapInstance.spawnPositionsParent.childCount);
        Vector2 randomSpawnPos = gameData.curRaid.mapInstance.spawnPositionsParent.GetChild(randomSpawnIndex).position;
        
        player.position = randomSpawnPos;
        player.gameObject.SetActive(false);
        
        Vector3 cameraWarpTarget = new(player.position.x, player.position.y, gameData.camera.cinemachine.transform.position.z);
        gameData.camera.cinemachine.ForceCameraPosition(cameraWarpTarget, Quaternion.identity);
        gameData.camera.cinemachine.Follow = player.trans;
        
        InitMapGrid();
        InitSpawnManager(gameData.curRaid.map.waves);
        SpawnMapResources(gameData.curRaid.mapInstance.resourceParent);
        SpawnInitialExitPortals(gameData.curRaid.mapInstance.exitPortalsParent, gameData.curRaid.map.exitPortalsCount);
        AnimateRaidEnterSequence();
    }
    
    private void UpdateRaidState() {
        RaidState prevState = gameData.curRaid.state;
        
        if (spawnManager.timeUntilFinalPhase >= 0f) {
            gameData.curRaid.state = RaidState.InitialWaves;
        }
        else if (!spawnManager.isFinishedSpawning || enemies.Count > 0) {
            gameData.curRaid.state = RaidState.FinalWave;
        }
        else {
            gameData.curRaid.state = RaidState.PostFinalWave;
        }

        gameData.curRaid.stateSwitchedThisFrame = prevState != gameData.curRaid.state;
        
        if (gameData.curRaid.stateSwitchedThisFrame && gameData.curRaid.state == RaidState.FinalWave) {
            PlayAudioClip(finalWaveStingerClip, player.position);
        }

        if (gameData.curRaid.stateSwitchedThisFrame && gameData.curRaid.state == RaidState.PostFinalWave) {
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
        SaveInventory(gameData.inventories.player);
        SavePlayerData();
        SaveActiveQuestProgresses();
    }
    
    private void OnMapLoaded(MapData map) {
        CreateDropPoolsForMap(map);
        gameData.states.gameStateMachine.SetStateIfNotCurrent(gameData.states.raid);
    }
    
    private void OnDemonEyeEquipmentChanged() {
        gameData.curRaid.temp.damagingData.Reset();
        if (gameData.demonEye.equiped != gameData.demonEye.empty) {
            customQuestEvent?.Invoke("FirstDemonEyeEquiped");
        }
    }
    
    // *******************************
    // Animation Sequences
    // *******************************
    
    private Sequence raidEnterSequence;
    
    private void AnimateRaidEnterSequence() {
        int initialPPU = gameData.camera.pixelPerfect.assetsPPU;
        gameData.camera.pixelPerfect.assetsPPU = 80;
            
        raidEnterSequence = Sequence.Create();
            
        gameData.ui.deathBgImage.enabled = true;
        gameData.ui.deathBgImage.fillAmount = 1f;
        raidEnterSequence.Chain(Tween.Alpha(gameData.ui.deathBgImage, 1f, 0f, 0.5f, Ease.InCubic));
            
        raidEnterSequence.ChainDelay(0.25f);

        raidEnterSequence.ChainCallback(() => {
            Entity inTeleportEntity = SpawnEntity(gameData.entityPools.teleportIn, OffsetY(player.position, -0.05f), Quaternion.identity);
            DestroyEntity(inTeleportEntity, CurrentClipLength(inTeleportEntity.animator));
            PlayAudioClip(teleportInClip, inTeleportEntity.position);
        });
            
        raidEnterSequence.ChainDelay(0.35f);
        raidEnterSequence.ChainCallback(() => {
            player.gameObject.SetActive(true);
            InitPlayer();
        });
        raidEnterSequence.Chain(Tween.Scale(player.trans, 0f, 1f, 0.2f, Ease.InOutBack));
            
        raidEnterSequence.ChainDelay(0.6f);
        raidEnterSequence.Chain(Tween.Custom(gameData.camera.pixelPerfect.assetsPPU, initialPPU, 0.25f, ease: Ease.OutQuad, onValueChange: val => {
            gameData.camera.pixelPerfect.assetsPPU = (int)val;
        }));
    }

    private void AnimateGameOverSequence(Action onCompleteCallback) {
        Tween.StopAll();
        
        foreach (Entity entity in gameData.entities.all) {
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
        
        gameData.ui.deathBgImage.enabled = true;
        gameData.ui.deathBgImage.fillAmount = 0f;
        gameData.ui.deathBgImage.color = gameData.ui.deathBgImage.color.Alpha(1f);

        Sequence sequence = Sequence.Create();
        sequence.ChainDelay(0.25f);
        sequence.Chain(Tween.UIFillAmount(gameData.ui.deathBgImage, 1f, 1f, Ease.InOutQuad));
        sequence.ChainCallback(() => {
            player.animator.enabled = true;
            player.animator.Play(player.deathAnim);
        });
        
        sequence.Group(Tween.Custom(1f, 0f, 0.5f, val => {
            player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
            player.matPropertyBlock.SetFloat(damageFlashTintPropertyId, val);
            player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        }, Ease.OutExpo));
        
        int initialPPU = gameData.camera.pixelPerfect.assetsPPU;
        
        sequence.Group(Tween.Custom(gameData.camera.pixelPerfect.assetsPPU, 80, 0.8f, val => {
            gameData.camera.pixelPerfect.assetsPPU = (int)val;
        }, Ease.InOutQuad));

        sequence.Group(Tween.Delay(0.25f, () => AnimateLargeRaidText(ColorText("YOU DIED", Styles.instance.decreaseDescColor), 1f)));
        
        sequence.ChainDelay(1f);
        
        gameData.ui.animatedBgImage.gameObject.SetActive(true);
        gameData.ui.animatedBgImage.color = new(1f, 1f, 1f, 0f);
        sequence.Chain(Tween.Alpha(gameData.ui.animatedBgImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.5f));

        sequence.Group(Tween.Scale(player.trans, Vector3.zero, 1.5f, Ease.InOutQuint, startDelay: 0.35f));
        
        sequence.OnComplete(() => {
            player.spriteRenderer.sortingLayerName = "Entity";
            player.trans.localScale = Vector3.one;
            gameData.camera.pixelPerfect.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }
    
    private void AnimateGameWinSequence(Action onCompleteCallback) {
        Entity outTeleportFxEntity = SpawnEntity(gameData.entityPools.teleportOut, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(teleportOutClip, outTeleportFxEntity.position);
        player.gameObject.SetActive(false);
        
        Sequence sequence = Sequence.Create();

        int initialPPU = gameData.camera.pixelPerfect.assetsPPU;
        sequence.Chain(Tween.Custom(gameData.camera.pixelPerfect.assetsPPU, 80, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            gameData.camera.pixelPerfect.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.15f);
        
        gameData.ui.deathBgImage.enabled = true;
        gameData.ui.deathBgImage.fillAmount = 1f;
        sequence.Chain(Tween.Alpha(gameData.ui.deathBgImage, 0f, 1f, 0.75f, Ease.InOutQuad));
        
        gameData.ui.animatedBgImage.gameObject.SetActive(true);
        gameData.ui.animatedBgImage.color = new(1f, 1f, 1f, 0f);
        sequence.Group(Tween.Alpha(gameData.ui.animatedBgImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.1f));
        sequence.ChainDelay(0.15f);

        sequence.OnComplete(() => {
            player.gameObject.SetActive(true);
            gameData.camera.pixelPerfect.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }
    
    private void AnimateEarlyExitSequence(Action onCompleteCallback) {
        Entity outTeleportFxEntity = SpawnEntity(gameData.entityPools.teleportOut, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(teleportOutClip, outTeleportFxEntity.position);
        player.gameObject.SetActive(false);
        
        Sequence sequence = Sequence.Create();

        int initialPPU = gameData.camera.pixelPerfect.assetsPPU;
        sequence.Chain(Tween.Custom(gameData.camera.pixelPerfect.assetsPPU, 80, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            gameData.camera.pixelPerfect.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.05f);
        sequence.Chain(Tween.Scale(exitPortalTakenByPlayer.transform, Vector3.zero, 0.25f, Ease.InOutBounce));
        
        sequence.ChainDelay(0.15f);
        
        gameData.ui.deathBgImage.enabled = true;
        gameData.ui.deathBgImage.fillAmount = 1f;
        sequence.Chain(Tween.Alpha(gameData.ui.deathBgImage, 0f, 1f, 0.75f, Ease.InOutQuad));
        
        sequence.Group(Tween.Delay(0.35f, () => AnimateLargeRaidText(ColorText("EARLY EXIT TAKEN", Styles.instance.increaseDescColor), 3.8f)));
        
        gameData.ui.animatedBgImage.gameObject.SetActive(true);
        gameData.ui.animatedBgImage.color = new(1f, 1f, 1f, 0f);
        sequence.Group(Tween.Alpha(gameData.ui.animatedBgImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.1f));
        sequence.ChainDelay(1.6f);

        sequence.OnComplete(() => {
            player.gameObject.SetActive(true);
            gameData.camera.pixelPerfect.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }

}