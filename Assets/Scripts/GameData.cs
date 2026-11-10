using System;
using System.Collections.Generic;
using Febucci.TextAnimatorForUnity;
using PrimeTween;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Steamworks;
using UnityEngine.InputSystem;
using static Game;

[Serializable]
public class GameData {
    
    [Serializable]
    public class Config {
        public ArtificialInventory startingTutorialInventory;
        public ArtificialInventory starterStashInventory;
        public Styles styles;
        public EnemyColors enemyColors;
        public GameplayConfig gameplay;
        public Trader trader;
        public DemonEyeLevels demonEyeLevels;
        public UpgradePath eyeForgeUpgradePath;
        public MapData tutorialSlaughterMap;
        public List<MapData> maps;
        public const int demonEyeCoreUpgradeCount = 5;
    }
    
    [Serializable]
    public class DropPools {
        public DropPool rockStones;
        public DropPool eyeUpgrades;
        public DropPool body;
        public DropPool trader;
        public DropPool bushes;
        public DropPool chests;
    }
    
    [Serializable]
    public class Prefabs {
        public GameObject audioSource;
        public GameObject player;
        public GameObject itemDrop;
        public GameObject rockSmokePrefab;
        public GameObject baseProjectile;
        public GameObject boneShatterProjectile;
        public GameObject soulProjectile;
        public GameObject firebossProjectile;
        public GameObject meteor;
        public GameObject bloodDrop;
        public GameObject poisonDebuff;
        public GameObject explosion;
        public GameObject gooProjectile;
        public GameObject poisonPool;
        public GameObject poisonCloud;
        public GameObject smallFireballProjectile;
        public GameObject piercingProjectile;
        public GameObject projectileImpact;
        public GameObject teleportIn;
        public GameObject teleportOut;
        public GameObject expressExitPortal;
        public GameObject bloodSplatter;
        public GameObject runSmoke;
        public GameObject slamSmoke;
        public GameObject blast;
        public GameObject inventorySlot;
        public GameObject lootReveal;
        public GameObject eyeForgeSlot;
        public GameObject damageNumber;
        public GameObject forgeExplosion;
        public GameObject forgeDust;
        public GameObject upgradeFractureParticles;
        public GameObject questSelectionToggle;
        public GameObject quest;
        public GameObject notification;
        public GameObject bloodBubble;
        public GameObject eyeUpgradeReveal;
        public GameObject altarSoulSwirl;
        public GameObject attackCircle;
        public GameObject meteorExplosion;
        public GameObject muzzleFlash;
    }
    
    [Serializable]
    public class ItemTypes {
        public ItemType quickUse;
        public ItemType backpack;
        public ItemType eye;
        public ItemType demonEye;
        public ItemType eyeUpgrade;
        public ItemType wearableModifier;
        public ItemType sellable;
        public ItemType resource;
    }
    
    [Serializable]
    public class Quests {
        public QuestGraphRuntime graph;
        public Dictionary<int, Quest.State> stateLookupFromUuid = new();
        public Queue<QuestPackage> reservedPkgs = new();
        public List<QuestPackage> activePkgs = new();
        public QuestPackage presentingPkg;
    }
    
    [Serializable]
    public class ItemRefs {
        public Item bloodMushroom;
    }
    
    [Serializable]
    public class SkillUpgradePaths {
        public SkillUpgradePath haste;
        public SkillUpgradePath intellect;
        public SkillUpgradePath lifeBlood;
        public SkillUpgradePath strength;
    }
    
    [Serializable]
    public class Camera {
        public UnityEngine.Camera main;
        public CameraShake cameraShake;
        public CameraAimOffset aimOffset;
        public CinemachineCamera cinemachine;
        public PixelPerfectCamera pixelPerfect;
        
        [NonSerialized] public int defaultPPU;
        [NonSerialized] public CameraLookahead lookAhead = new();
    }

    [Serializable]
    public class Blood {
        public SpriteRenderer overlay;
        public Material stampMaterial;
        public Sprite[] splatterMasks;
        public int texelsPerUnit = 48;

        [NonSerialized] public RenderTexture mask;
        [NonSerialized] public Rect maskWorldRect;
        [NonSerialized] public Mesh stampQuad;
        [NonSerialized] public UnityEngine.Rendering.CommandBuffer cmd;
        [NonSerialized] public MaterialPropertyBlock materialProperyBlock;
        [NonSerialized] public readonly List<BloodStamp> pendingStamps = new();
    }

    [Serializable]
    public class Curves {
        public AnimationCurve hitFlash;
        public AnimationCurve bounce;
        public AnimationCurve shake;
        public AnimationCurve pentagramFill;
        public AnimationCurve pentagramItemShake;
        public AnimationCurve questBurn;
        public AnimationCurve questBurnEmbers;
        public AnimationCurve skillBurn;
        public AnimationCurve skillBurnEmbers;
        public AnimationCurve discoverSlotTimingCurve;
        public AnimationCurve altarBubbleRate;
    }
    
    [Serializable] 
    public class UI {
        public TMP_FontAsset mainFontAsset;
        public RectTransform mainCanvasRectTransform;
        public CanvasScaler mainCanvasScaler;
        public InputPrompt interactionPrompt;
        public RawImage controllerCrosshair;
        public Minimap minimap;
        public ItemDescPopup itemDescPopupInv;
        public ItemDescPopup itemDescPopupPickup;
        public MechanicDescPopup mechanicDescPopup;
        public UIHintPopUp hintPopup;
        public LevelUpNotification levelUpNotification;
        public RectTransform hideoutParent;
        public RectTransform settingsParent;
        public RectTransform hideoutPanelsParent;
        public RectTransform hotBarParent;
        public RectTransform notificationParent;
        public TextMeshProUGUI teleportingIntoRaidHeader;
        public ItemUI dragAndDropItemUI;
        public Image animatedBgImage;
        public ButtonFeel menuBackButton;
        public MessagePopup messagePopup;
        public TextMeshProUGUI smallRaidText;
        public TypewriterComponent smallRaidTextTypewriter;
        public TextMeshProUGUI largeRaidText;
        public TypewriterComponent largeRaidTextTypewriter;
        public TypewriterComponent openingDialogueTypewriter;
        public TypewriterComponent traderTutorialTypewriter;
        public CanvasGroup traderTutorialDialogueCanvasGroup;
        public GameObject traderTutorialDialogueBox;
        public RectTransform lootInventoryPanel;
        public LayoutElement lootInventoryLayoutElement;
        public RectTransform lootInventoryParent;
        public GameObject lootSearchingText;
        public RectTransform portalArrow;
        public RectTransform damageNumbersParent;
        public RectTransform currencyForSmallScreensParent;
        
        public RectTransform quickUseHeaderText;
        public RectTransform stashPanelHeaderText;
        public ToggleButtonGroup[] navToggleGroups; // Toggle groups the controller can switch with the bumpers/triggers, based on their NavigationMode
        
        [NonSerialized] public ScreenScalingMaintainer[] screenScalingMaintainers;
        [NonSerialized] public float damageNumberFontSize; // Store to prevent scaling issues when other things borrow damage numbers (player healthbar damage)
    }
    
    [Serializable]
    public class InputPrompts {
        public InputGlyphSheet xboxGlyphs;
        public InputGlyphSheet keyboardMouseGlyphs;
        
        public RectTransform hideoutParent;
        public GameObject inRaidParent;
        public InputPrompt hotBarLeft;
        public InputPrompt hotBarRight;

        public List<InputPrompt> hideoutPrompts;
        public List<InputPrompt> raidPrompts;
        public List<InputPrompt> hotBarPrompts;
        
        [NonSerialized] public List<InputPrompt> allBoundPrompts = new();
    }
    
    [Serializable]
    public class PlayerInfo {
        public GameObject parent;
        public GameObject healthBarParent;
        public GameObject weightBarParent;
        public GameObject soulsCurrencyParent;
        public GameObject coinsCurrencyParent;
        public GameObject bleedDebuffIcon;
        public IconBar healthBar;
        public IconBar weightBar;
        public TextMeshProUGUI soulsCurrencyText;
        public TextMeshProUGUI coinCurrencyText;
        public RectTransform damageNumberSpawnPos;
    }
    
    [Serializable]
    public class RaidInfo {
        public GameObject parent;
        public TextMeshProUGUI waveText;
    }
    
    [Serializable]
    public class BossBar {
        public GameObject parent;
        public Image fillImage;
        public TextMeshProUGUI nameTextMesh;
    }
    
    [Serializable]
    public class MainMenu {
        public RectTransform parent;
        public RectTransform logo;
        public GameObject hideoutNotifier;
        public ButtonFeel playButton;
        public ButtonFeel hideoutButton;
        public ButtonFeel settingsButton;
        public ButtonFeel exitButton;
    } 
    
    [Serializable]
    public class PauseMenu {
        public RectTransform panel;
        public ButtonFeel resumeButton;
        public ButtonFeel settingsButton;
        public ButtonFeel suicideButton;
        [NonSerialized] public bool paused;
        [NonSerialized] public bool showingSettings;
        [NonSerialized] public readonly List<AudioSource> pausedAudioSources = new();
    }
    
    [Serializable]
    public class HideoutTabs {
        public RectTransform parent;
        public ToggleButtonGroup toggleGroup;
        public ToggleButton characterButton;
        public ToggleButton eyeForgeButton;
        public ToggleButton traderButton;
        public ToggleButton questsButton;
        public ToggleButton skillsButton;
    }
    
    [Serializable]
    public class PlayerPanel {
        public RectTransform panel;
        public LayoutElement panelLayoutElement;
        public RectTransform playerHalfParent;
        public RectTransform pocketParent;
        public RectTransform quickUseParent;
        public RectTransform inventoryParent;
        public ButtonFeel transferToStashButton;
        public TextMeshProUGUI healthText;
        public TextMeshProUGUI weightText;
        public Image previewImage;
        public EquipedStatsPanel equipedStatsPanel;
    }
    
    [Serializable]
    public class StashPanel {
        public RectTransform panel;
        public RectTransform inventoryParent;
    }
    
    [Serializable]
    public class EyeForgePanel {
        public RectTransform panel;
        public TextMeshProUGUI panelTitle;
        public ToggleButtonGroup toggleButtonGroup;
        public ToggleButton forgeToggle;
        public ToggleButton levelUpToggle;
        public Image burnEffectImage;
        
        public GameObject forgingParent;
        public RectTransform pentagramParent;
        public Image pentagramFillImage;
        public TextMeshProUGUI forgeHintTextMesh;
        public ButtonFeel forgeButton;
        
        public GameObject levelUpParent;
        public TextMeshProUGUI subHeaderTextMesh;
        public ResourceRequirementList levelUpRequirementList;
        public ButtonFeel levelUpButton;
        public GameObject maxLevelReachedNotifier;
        
        [NonSerialized] public Sequence animatingInUpgradesSequence;
        [NonSerialized] public int showingPentagramLevel;
    }
    
    [Serializable]
    public class EyeForgeDetailsPanel {
        public RectTransform panel;
        public TextMeshProUGUI panelHeaderText;
        public GameObject upgradeHeader;
        public Image upgradeFromNumeral;
        public Image upgradeToNumeral;
        public DemonEyeDescList demonEyeDesc;
    }
    
    [Serializable]
    public class TraderPanel {
        public RectTransform panel;
        public RectTransform inventoryParent;
        public TraderRepBar repBar;
        public TextMeshProUGUI itemRefreshTimeText;
        public TypewriterComponent shopTextTypewriter;
    }
    
    [Serializable]
    public class TransactionPanel {
        public RectTransform panel;
        public global::TransactionPanel transaction;
        public RectTransform inventoryParent;
    }

    [Serializable]
    public class MapPanels {
        public MapSelectionPanel mapSelectionPanel;
        public MapConfirmationPanel confirmationPanel;
    }
    
    [Serializable]
    public class QuestsPanel {
        public RectTransform panel;
        public RectTransform questsParent;
        public RectTransform questSelectionParent;
        public ToggleButtonGroup toggleButtonGroup;
        public TraderRepBar traderRepBar;
        public Image scortchedOverlayImage;
    }
    
    [Serializable]
    public class SkillsPanel {
        public global::SkillsPanel panel;
        public PlayerStatsPanel playerStatsPanel;
    }
    
    [Serializable]
    public class Audio {
        public DynamicClip ambientClip;
        public DynamicClip shootClip;
        public DynamicClip stoneBreakClip;
        public DynamicClip stoneHitClip;
        public DynamicClip altarSoulsClip;
        public DynamicClip altarBubbleClip;
        public DynamicClip altarBloodExplosionClip;
        public DynamicClip projectileImpact;
        public DynamicClip bloodBurstClip;
        public DynamicClip footStepClip;
        public DynamicClip teleportInClip;
        public DynamicClip teleportOutClip;
        public DynamicClip portalSpawnClip;
        public DynamicClip portalDespawnClip;
        public DynamicClip nextWaveStingerClip;
        public DynamicClip finalWaveStingerClip;
        public DynamicClip lootRevealClip;
        public DynamicClip slotRevealClip;
        public DynamicClip lootingBodyClip;
        public DynamicClip lootingBushClip;
        public DynamicClip lootingBodyLoop;
        public DynamicClip lootingBushLoop;
        public DynamicClip rarityRevealClip;
        public DynamicClip togglePressClip;
        public DynamicClip coinSplashClip;
        public DynamicClip cashRegisterClip;
        public DynamicClip purchaseClip;
        public DynamicClip itemMoveClip;
        public DynamicClip itemSelectClip;
        public DynamicClip forgingClip;
        public DynamicClip startForgingClip;
        public DynamicClip burnClip;
        public DynamicClip deathStingerClip;
        public DynamicClip textCharAppearClip;
        public DynamicClip extractionMelody;
        public DynamicClip reflectedShotClip;
        public DynamicClip overheatBlastClip;
        public DynamicClip boneShatterClip;
        public DynamicClip soulVolleyClip;
        public DynamicClip explosionClip;
        
        public Dictionary<int, List<DynamicClipRecord>> records = new(50);
        public Dictionary<AudioSource, int> generationLookup = new();
        public List<AudioClipHandle> loopingSources = new();
        // Sources playing a cannotInterrupt clip, held out of reservedSources until the clip finishes
        public List<AudioSource> uninterruptibleSources = new();
        // Playing clips whose volume is re-applied every frame from the player's distance to them
        public List<PositionalVolume> positionalVolumes = new();
        public Queue<AudioSource> reservedSources;
    }
    
    [Serializable]
    public class Music {
        public AudioMixerGroup masterGroup;
        public AudioMixerGroup mainMenuGroup;
        public AudioMixerGroup gameplayGroup;
        public AudioClip mainMenuMusic;
        public List<GameplaySong> gameplaySongs;
        public AudioMixerSnapshot defaultSnapshot;
        public AudioMixerSnapshot lowPassSnapshot;
        
        [NonSerialized] public AudioSource source;
        [NonSerialized] public AudioClip lastGameplaySong;
        [NonSerialized] public Tween fadingOutTween;
        [NonSerialized] public Tween fadingInTween;
        [NonSerialized] public SongTransition songTransition;
        [NonSerialized] public float timeCurSongStarted;
        [NonSerialized] public bool menuMusicActive;
        [NonSerialized] public int menuLoopsBeforeBreak;
        [NonSerialized] public float menuBreakEndTime;
        [NonSerialized] public AudioMixerSnapshot[] gameplayLowpassSnapshots;
        [NonSerialized] public float[] gameplaySnapshotWeights;
    }
    
    [Serializable]
    public class GameplaySong {
        public AudioClip clip;
        public List<MusicIntensity> intensities; // Waves only play songs that list their intensity. Left empty counts as Low.
    }

    [Serializable]
    public class Settings {
        public AudioMixer gameAudioMixer;
        public AudioMixer musicAudioMixer;
        
        public GameObject settingsParent;
        public ButtonFeel applyChangesButton;
        public ButtonFeel resetPageButton; // Resets whichever page is showing
        public ButtonFeel backButton;
        
        public ToggleButtonGroup toggleGroup;
        public ToggleButton audioToggle;
        public ToggleButton displayToggle;
        public ToggleButton gameToggle;
        public ToggleButton controlsToggle;

        public RectTransform audioParent;
        public RectTransform displayParent;
        public RectTransform gameParent;
        public RectTransform controlsParent;
        [NonSerialized] public RectTransform curPage;

        public RectTransform steamInputBanner;
        public ButtonFeel steamInputBindingsButton;
        public ScrollRect controlsScrollRect;
        public RectTransform controlsContent;
        public InputBindingRow controlsRowPrefab;
        public InputPrompt cancelRebindPrompt;
        public InputPrompt unbindPrompt;
        [NonSerialized] public readonly List<InputBindingRow> controlsRows = new();
        [NonSerialized] public readonly List<GameAction> rebindableActions = new();
        // Not shown in the controls page, but rebinding onto one of their bindings still warns
        [NonSerialized] public readonly List<GameAction> conflictOnlyActions = new();
        
        public SingleSetting fullscreenMode;
        public SingleSetting resolution;
        public SingleSetting targetMonitor;
        public SingleSetting fpsLimit;
        public SingleSetting vsync;
        
        public SingleSetting masterVolume;
        public SingleSetting musicVolume;
        public SingleSetting gameVolume;
        
        public SingleSetting cameraShake;
        public SingleSetting cameraDistance;
        public SingleSetting showHoverHints;
        public SingleSetting itemPopupDelay;
        public SingleSetting uncMode;
        
        [NonSerialized] public SingleSetting[] all;
        [NonSerialized] public SettingsState curSettingsState;
    }
    
    public class HotBar {
        public List<GameAction> quickUseActions;
        public InventorySlotUI[] slotUIs;
        public int selectedIndex; // Used by Quick Use Selected, on controller or when it is bound on keyboard and mouse
    }
    
    public class Inputs {
        public GameAction aim;
        public GameAction attack;
        public GameAction move;
        public GameAction interact;
        public GameAction inventory;
        
        public GameAction select;
        
        public GameAction sellSingleItem;
        public GameAction sellStack;
        public GameAction quickSell;
        public GameAction tradeItems;
        public GameAction purchase;
        
        public GameAction place;
        public GameAction placeSingle;
        public GameAction cancel;
        public GameAction useItem;
        public GameAction quickMove;
        public GameAction quickDrop;
        public GameAction splitStack;
        public GameAction back;
        public GameAction pause;
        public GameAction quickUse1;
        public GameAction quickUse2;
        public GameAction quickUse3;
        public GameAction quickUse4;
        public GameAction quickUseSelected;
        public GameAction quickUsePrevious;
        public GameAction quickUseNext;
        public GameAction teleport;
        public GameAction menuMove;
        public GameAction menuTabLeft;
        public GameAction menuTabRight;
        public GameAction menuSecondaryTabLeft;
        public GameAction menuSecondaryTabRight;
        public GameAction advanceDialogue;
        public GameAction cancelRebind;
        public GameAction unbind;

        public readonly List<GameAction> all = new();

        // Keyed by the Unity action's id
        public readonly Dictionary<Guid, GameAction> gameActionLookup = new();

        // Rebuilt every frame from game state, last is the top. When actions on different layers share a binding, the higher one wins.
        public readonly List<InputActionMap> layerStack = new();
        public readonly HashSet<string> claimedUnityBindings = new();
        public readonly HashSet<EInputActionOrigin> claimedSteamOrigins = new();

        public const string GlobalLayer = "Global";
        public const string GameplayLayer = "Gameplay";
        public const string SelectionLayer = "Selection";
        public const string MenuLayer = "Menu";
        public const string InventoryLayer = "Inventory";
        public const string PlayerInventoryLayer = "PlayerInventory";
        public const string SellingLayer = "Selling";
        public const string BuyingLayer = "Buying";
        public const string SelectedItemLayer = "SelectedItem";
        public const string DialogueLayer = "Dialogue";
        public const string RebindingLayer = "Rebinding";

        public enum Device { KeyboardMouse, Controller, ControllerWithMouse }
        public Device device;
        public float lastDeviceSwitchTime;
        public Gamepad gamepadUsedForCurrentGlyphs;
        public bool glyphsForController;
        public bool glyphsNeedRefresh;

        public bool steamInputInitalized;
        public bool activelyUsingSteamInput;
        
        public InputActionSetHandle_t steamActionSet;
        public readonly InputHandle_t[] steamControllers = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
        public InputHandle_t activeSteamController;
    }
    
    public class EntityPools {
        public EntityPool<Entity> itemDrop;
        public EntityPool<Entity> bloodDrop;
        public EntityPool<Projectile> projectile;
        public EntityPool<Projectile> boneShatterProjectile;
        public EntityPool<Projectile> gooProjectile;
        public EntityPool<Projectile> smallFireballProjectile;
        public EntityPool<Projectile> fireBossProjectile;
        public EntityPool<Projectile> piercingShotProjectile;
        public EntityPool<Projectile> soulProjectile;
        public EntityPool<Entity> meteor;
        public EntityPool<Entity> poisonDebuff;
        public EntityPool<Entity> explosion;
        public EntityPool<Hazard> poisonPool;
        public EntityPool<Entity> poisonCloud;
        public EntityPool<Entity> projectileImpact;
        public EntityPool<Entity> teleportIn;
        public EntityPool<Entity> teleportOut;
        public EntityPool<Entity> expressExitPortal;
        public EntityPool<Entity> bloodSplatter;
        public EntityPool<Entity> runSmoke;
        public EntityPool<Entity> damageNumber;
        public EntityPool<Entity> forgeExplosion;
        public EntityPool<Entity> forgeDust;
        public EntityPool<Entity> upgradeFractureParticles;
        public EntityPool<Entity> blast;
        public EntityPool<Entity> lootReveal;
        public EntityPool<Entity> notification;
        public EntityPool<Entity> bloodBubble;
        public EntityPool<Entity> eyeUpgradeReveal;
        public EntityPool<Entity> altarSoulSwirl;
        public EntityPool<Entity> attackCircle;
        public EntityPool<Entity> meteorExplosion;
        public EntityPool<Entity> muzzleFlash;
    }
    
    public class States {
        public State mainMenu;
        public State settingsMenu;
        public State mapSelection;
        public State hideout;
        public State raid;
        public State gameOver;
        public State winExit;
        public State earlyExit;
        public StateMachine gameStateMachine;
    }
    
    public class Entities {
        public List<Entity> all = new();
        public List<Projectile> projectiles = new();
        public List<Projectile> soulTrackingProjectiles = new();
        public List<Hazard> hazards = new();
        public List<Enemy> enemies = new();
        public Enemy boss;
        public Dictionary<GameObject, Entity> lookup = new();
        public Player player;
    }
    
    public class Resources {
        public Dictionary<int, UuidScriptableObject> lookup = new();
        public List<Item> items = new();
        public List<DropPool> dropPools = new();
        public List<DropPool> globalDropPools = new();
        public List<DropPool> mapSpecificDropPools = new();
        public HashSet<int> takenUuids = new();
    }
    
    public class Inventories {
        public Inventory player;
        public Inventory stash;
        public Inventory eyeForge;
        public Inventory transaction;
        public Inventory trader;
        public Inventory lootPtr;
        public InventorySlotUI[] lootSlotUis;
        public List<Inventory> all = new();
    }
    
    public class DemonEye {
        public DemonEyeInstance equiped;
        public ItemInstance equipedItem;
        public Dictionary<int, DemonEyeInstance> instanceFromItemId = new();
        public readonly DemonEyeInstance empty = new();
    }
    
    public class Trinkets {
        public Trinket equiped;
        public ref TrinketData data => ref gameInstance.curRaid.data.trinkets;
    }
    
    public class CurrentGamingSession {
        public int raidsEntered;
    }
    
    public class CurrentRaid {
        public RaidState state;
        public bool stateSwitchedThisFrame;
        
        public MapData map;
        public MapInstance mapInstance;
        public MapLoadingState mapLoadingState;
        
        public Vector2 lastPlayerGridPos;
        public Limiter flowFieldLimiter;
        
        public List<Vector2> teleportingInPositions = new();
        public List<Portal> activeExitPortals = new();
    
        public Dictionary<GameObject, InventorySlot[]> bushSlotsLookup = new();
        public Dictionary<GameObject, InventorySlot[]> deadBodySlotsLookup = new();
        public Dictionary<GameObject, InventorySlot[]> chestSlotsLookup = new();
        
        // Data that gets reset every time a new raid starts
        public struct Data {
            public DamagingData damaging;
            public InteractionData interactions;
            public TrinketData trinkets;
            public Limiter reteleportLimitter;
            public int soulsGained;
            public bool exitPortalCutscenePlayed;
        }
        public Data data;
    } 
    
    public class HideoutState {
        public int pentagramLevelIndex;
    }
    
    [Flags]
    public enum PersistentFlags {
        None                     = 0,
        BloodMushroomsUnlocked   = 1 << 0,
        HasExtracted             = 1 << 2,
        FirstMapPlayedOnce       = 1 << 3,
        FirstMapPlayedTwice      = 1 << 4,
        DemoEndShown             = 1 << 5,
        ShowStashTransferButton  = 1 << 6,
        TutorialCompleted        = 1 << 7,
    }
    
    [Flags] 
    public enum FrameFlags {
        None            = 0,
        EarlyExitTaken  = 1 << 0,
        ExitTaken       = 1 << 1,
        SkillUpgraded   = 1 << 2,
        BleedStopped    = 1 << 3,
        PostInitRaid    = 1 << 4,
        DemonEyeChanged = 1 << 5,
        SearchingBody   = 1 << 6,
        SearchingBush   = 1 << 7,
        PickedUpLoot    = 1 << 8,
        ShotRock        = 1 << 9,
        SummonedUpgrade = 1 << 10,
        SoldToTrader    = 1 << 11,
        TookConsumable  = 1 << 12,
        InventoryOpened = 1 << 13,
    }
    
    public class PerFrameData {
        public FrameFlags flags;
        public Dictionary<EnemyData, int> enemyKillCount = new();
        
        // Data to auto reset every frame
        public struct Data {
            public int healing;
            public int enemyBloodDropped;
            public ItemInstance foundSearchItem;
        }
        public Data data;
    }
    
    public class ControllerNavigation {
        public RectTransform selected;
        public Vector2 pointerPos;
        public bool hasPointerPos;
        public State lastNavGameState;
        public Vector2 lastNavDir;
        public float repeatTimer;
        public GameObject submitPressedOn;
        public bool waitingForNavRelease; // Stick/d-pad input is ignored until it's let go
        public float lastTimePlayerMovedSelection;
        public Transform[] ignoredRoots;
        public PointerEventData pointerEventData;
        public readonly Dictionary<Transform, NavPanel> navPanels = new();
        public readonly List<NavPanel> recentPanels = new(); // Highest priority first, then most recently used
        public readonly List<RectTransform> possibleSelections = new();
        public readonly Vector3[] navCorners = new Vector3[4];
        public const float stickDeadzone = 0.7f;
        public const float repeatDelay = 0.25f;
        public const float repeatInterval = 0.1f;
        public const int pointerEventId = -100;
    }
    
    public class Tutorial {
        public StateMachine stateMachine;
        public State entryState;
        public State openingDialogue;
        public State firstCraftingState;
        public State craftingDemonEyeState;
        public State equipingDemonEyeState;
        public State waitingToEnterSlaughterMap;
        
        public State inSlaughterMap;
        public State diedInSlaughterMap;
        public State firstTraderMeeting;
        public State firstHideoutVisit;
        
        public State hideoutCharacter; 
        public State hideoutForge; 
        public State hideoutTrader; 
        public State hideoutQuests; 
        public State hideoutSkills; 
        public State completed; // Must stay last, being on the last state is what marks the tutorial as finished

        public readonly Dialogue dialogue = new();
        public TypewriterComponent dialogueTypewriter;
    }

    public class Cutscene {
        public bool playing;
        public Transform cameraTarget; // The camera follows this while panning so Cinemachine's damping still applies
        public bool restoreLookahead;
        public bool showDemoEndOnMainMenu;
    }

}
