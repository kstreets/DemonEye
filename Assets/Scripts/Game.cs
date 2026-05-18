using System;
using System.Collections.Generic;
using Febucci.TextAnimatorForUnity;
using PrimeTween;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using VInspector;
using Assert = UnityEngine.Assertions.Assert;
using Vector3 = UnityEngine.Vector3;
using EffectsIndicies = Game.Entity.EffectsIndicies;

public partial class Game : MonoBehaviour {

    public static Game gameInstance;
    
    public GameData gameData;
    
    public StartingItemsConfig startingItems;
    public Styles styles;
    public GameplayConfig gameplayConfig;
    
    [Foldout("Quests")]
    public QuestGraphRuntime questGraph;
    public Quest pickPocketQuest;
    [EndFoldout]

    [Foldout("Maps")]
    public List<MapData> maps;
    [EndFoldout]
    
    [Foldout("DropPools")]
    public DropPool rockStonesDropPool;
    public DropPool eyeUpgradesDropPool;
    public DropPool bodyDropPool;
    public DropPool traderDropPool;
    public DropPool foragingDropPool;
    public DropPool bushesDropPool;
    public DropPool chestsDropPool;
    [EndFoldout]
    
    [Foldout("Pooling Prefabs")]
    public GameObject itemDropPrefab;
    public GameObject baseProjectilePrefab;
    public GameObject boneShatterProjectilePrefab;
    public GameObject bloodDropPrefab;
    public GameObject poisonDebuffPrefab;
    public GameObject explosionPrefab;
    public GameObject boomonExplosionPrefab;
    public GameObject gooProjectilePrefab;
    public GameObject piercingProjectilePrefab;
    public GameObject projectileImpactPrefab;
    public GameObject teleportInPrefab;
    public GameObject teleportOutPrefab;
    public GameObject bloodSplatterPrefab;
    public GameObject runSmokePrefab;
    public GameObject slamSmokePrefab;
    public GameObject blastPrefab;
    [EndFoldout]
    
    [Foldout("Item Type Refs")]
    public ItemType quickUseType;
    public ItemType backpackType;
    public ItemType eyeType;
    public ItemType demonEyeType;
    public ItemType gemType;
    public ItemType eyeUpgradeType;
    public ItemType wearableModifierType;
    [EndFoldout]

    [Foldout("Skill Upgrade Paths")]
    public SkillUpgradePath hasteUpgradePath;
    public SkillUpgradePath intellectUpgradePath;
    public SkillUpgradePath lifeBloodUpgradePath;
    public SkillUpgradePath strengthUpgradePath;
    [EndFoldout]
    
    public Camera mainCamera;
    public CinemachineCamera cinemachineCamera;
    public PixelPerfectCamera pixelPerfectCamera;

    public GameObject playerPrefab;

    public Item demonEyeItem;
    
    [Foldout("Effects")]
    public AnimationCurve hitFlashCurve;
    public AnimationCurve bounceCurve;
    public AnimationCurve shakeCurve;
    [EndFoldout]

    [Foldout("UI/Prefabs")]
    public GameObject inventorySlotPrefab;
    public GameObject eyeForgeSlotPrefab;
    public GameObject rockSmokePrefab;
    public GameObject damageNumberPrefab;
    public GameObject forgeExplosionPrefab;
    public GameObject forgeDustExplosionPrefab;
    public GameObject questSelectionTogglePrefab;
    public GameObject questPrefab;
    [EndFoldout]

    [Foldout("UI/MiscRefs")]
    public RectTransform mainCanvasRectTransform;
    public ItemDescPopup itemDescPopup;
    public MechanicDescPopup mechanicDescPopup;
    public UIElementPopup uiElementPopup;
    public RectTransform hideoutParent;
    public RectTransform hotBarParent;
    public ItemUI dragAndDropItemUI;
    public Image menuBackgroundImage;
    public Image deathBackgroundImage;
    public ButtonFeel menuBackButton;
    public TextMeshProUGUI smallRaidText;
    public TypewriterComponent smallRaidTextTypewriter;
    public TextMeshProUGUI largeRaidText;
    public TypewriterComponent largeRaidTextTypewriter;
    [EndFoldout]
    
    [Foldout("UI/Main Menu")]
    public RectTransform mainMenuParent;
    public RectTransform mainMenuLogo;
    public ButtonFeel mainMenuPlayButton;
    public ButtonFeel mainMenuHideoutButton;
    public ButtonFeel mainMenuSettingsButton;
    public ButtonFeel mainMenuExitButton;
    [EndFoldout]
    
    [Foldout("UI/HideoutTabs")]
    public RectTransform hideoutTabsParent;
    public Sprite tabNonSelectedSprite;
    public Sprite tabSelectedSprite;
    public Button characterTabButton;
    public Button eyeForgeTabButton;
    public Button traderTabButton;
    public Button questsTabButton;
    public Button skillsTabButton;
    public TextMeshProUGUI characterTabText;
    public TextMeshProUGUI eyeForgeTabText;
    public TextMeshProUGUI traderTabText;
    public TextMeshProUGUI questsTabText;
    public TextMeshProUGUI skillsTabText;
    [EndFoldout]

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

        deathBackgroundImage.enabled = false;
        gameData.curRaid.mapInstance.gameObject.SetActive(true);

        int randomSpawnIndex = Random.Range(0, gameData.curRaid.mapInstance.spawnPositionsParent.childCount);
        Vector2 randomSpawnPos = gameData.curRaid.mapInstance.spawnPositionsParent.GetChild(randomSpawnIndex).position;
        
        player.position = randomSpawnPos;
        player.gameObject.SetActive(false);
        
        Vector3 cameraWarpTarget = new(player.position.x, player.position.y, cinemachineCamera.transform.position.z);
        cinemachineCamera.ForceCameraPosition(cameraWarpTarget, Quaternion.identity);
        cinemachineCamera.Follow = player.trans;
        
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
                gameInstance.AnimateLargeRaidText(ColorText("Map Cleared!", gameInstance.styles.increaseDescColor), 1.8f);
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
        int initialPPU = pixelPerfectCamera.assetsPPU;
        pixelPerfectCamera.assetsPPU = 80;
            
        raidEnterSequence = Sequence.Create();
            
        deathBackgroundImage.enabled = true;
        deathBackgroundImage.fillAmount = 1f;
        raidEnterSequence.Chain(Tween.Alpha(deathBackgroundImage, 1f, 0f, 0.5f, Ease.InCubic));
            
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
        raidEnterSequence.Chain(Tween.Custom(pixelPerfectCamera.assetsPPU, initialPPU, 0.25f, ease: Ease.OutQuad, onValueChange: val => {
            pixelPerfectCamera.assetsPPU = (int)val;
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
        
        deathBackgroundImage.enabled = true;
        deathBackgroundImage.fillAmount = 0f;
        deathBackgroundImage.color = deathBackgroundImage.color.Alpha(1f);

        Sequence sequence = Sequence.Create();
        sequence.ChainDelay(0.25f);
        sequence.Chain(Tween.UIFillAmount(deathBackgroundImage, 1f, 1f, Ease.InOutQuad));
        sequence.ChainCallback(() => {
            player.animator.enabled = true;
            player.animator.Play(player.deathAnim);
        });
        
        sequence.Group(Tween.Custom(1f, 0f, 0.5f, val => {
            player.spriteRenderer.GetPropertyBlock(player.matPropertyBlock);
            player.matPropertyBlock.SetFloat(damageFlashTintPropertyId, val);
            player.spriteRenderer.SetPropertyBlock(player.matPropertyBlock);
        }, Ease.OutExpo));
        
        int initialPPU = pixelPerfectCamera.assetsPPU;
        
        sequence.Group(Tween.Custom(pixelPerfectCamera.assetsPPU, 80, 0.8f, val => {
            pixelPerfectCamera.assetsPPU = (int)val;
        }, Ease.InOutQuad));

        sequence.Group(Tween.Delay(0.25f, () => AnimateLargeRaidText(ColorText("YOU DIED", styles.decreaseDescColor), 1f)));
        
        sequence.ChainDelay(1f);

        menuBackgroundImage.gameObject.SetActive(true);
        menuBackgroundImage.color = new(1f, 1f, 1f, 0f);
        sequence.Chain(Tween.Alpha(menuBackgroundImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.5f));

        sequence.Group(Tween.Scale(player.trans, Vector3.zero, 1.5f, Ease.InOutQuint, startDelay: 0.35f));
        
        sequence.OnComplete(() => {
            player.spriteRenderer.sortingLayerName = "Entity";
            player.trans.localScale = Vector3.one;
            pixelPerfectCamera.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }
    
    private void AnimateGameWinSequence(Action onCompleteCallback) {
        Entity outTeleportFxEntity = SpawnEntity(gameData.entityPools.teleportOut, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(teleportOutClip, outTeleportFxEntity.position);
        player.gameObject.SetActive(false);
        
        Sequence sequence = Sequence.Create();

        int initialPPU = pixelPerfectCamera.assetsPPU;
        sequence.Chain(Tween.Custom(pixelPerfectCamera.assetsPPU, 80, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            pixelPerfectCamera.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.15f);
        
        deathBackgroundImage.enabled = true;
        deathBackgroundImage.fillAmount = 1f;
        sequence.Chain(Tween.Alpha(deathBackgroundImage, 0f, 1f, 0.75f, Ease.InOutQuad));
        
        menuBackgroundImage.gameObject.SetActive(true);
        menuBackgroundImage.color = new(1f, 1f, 1f, 0f);
        sequence.Group(Tween.Alpha(menuBackgroundImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.1f));
        sequence.ChainDelay(0.15f);

        sequence.OnComplete(() => {
            player.gameObject.SetActive(true);
            pixelPerfectCamera.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }
    
    private void AnimateEarlyExitSequence(Action onCompleteCallback) {
        Entity outTeleportFxEntity = SpawnEntity(gameData.entityPools.teleportOut, player.position, Quaternion.identity);
        DestroyEntity(outTeleportFxEntity, CurrentClipLength(outTeleportFxEntity.animator));
        PlayAudioClip(teleportOutClip, outTeleportFxEntity.position);
        player.gameObject.SetActive(false);
        
        Sequence sequence = Sequence.Create();

        int initialPPU = pixelPerfectCamera.assetsPPU;
        sequence.Chain(Tween.Custom(pixelPerfectCamera.assetsPPU, 80, 0.5f, ease: Ease.InOutQuad, onValueChange: val => {
            pixelPerfectCamera.assetsPPU = (int)val;
        }));
        
        sequence.ChainDelay(0.05f);
        sequence.Chain(Tween.Scale(exitPortalTakenByPlayer.transform, Vector3.zero, 0.25f, Ease.InOutBounce));
        
        sequence.ChainDelay(0.15f);
        
        deathBackgroundImage.enabled = true;
        deathBackgroundImage.fillAmount = 1f;
        sequence.Chain(Tween.Alpha(deathBackgroundImage, 0f, 1f, 0.75f, Ease.InOutQuad));
        
        sequence.Group(Tween.Delay(0.35f, () => AnimateLargeRaidText(ColorText("EARLY EXIT TAKEN", styles.increaseDescColor), 3.8f)));
        
        menuBackgroundImage.gameObject.SetActive(true);
        menuBackgroundImage.color = new(1f, 1f, 1f, 0f);
        sequence.Group(Tween.Alpha(menuBackgroundImage, 0f, 1f, 1f, Ease.InCubic, startDelay: 0.1f));
        sequence.ChainDelay(1.6f);

        sequence.OnComplete(() => {
            player.gameObject.SetActive(true);
            pixelPerfectCamera.assetsPPU = initialPPU;
            onCompleteCallback?.Invoke();
        });
    }

}