using System;
using System.Collections.Generic;
using Febucci.TextAnimatorForUnity;
using PrimeTween;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Game;

[Serializable]
public class GameData {
    
    [Serializable]
    public class Config {
        public StartingItemsConfig startingItems;
        public Styles styles;
        public GameplayConfig gameplay;
        public Trader trader;
        public DemonEyeLevels demonEyeLevels;
        public List<MapData> maps;
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
        public GameObject bloodDrop;
        public GameObject poisonDebuff;
        public GameObject explosion;
        public GameObject gooProjectile;
        public GameObject piercingProjectile;
        public GameObject projectileImpact;
        public GameObject teleportIn;
        public GameObject teleportOut;
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
    }
    
    [Serializable]
    public class ItemTypes {
        public ItemType quickUse;
        public ItemType backpack;
        public ItemType eye;
        public ItemType demonEye;
        public ItemType gem;
        public ItemType eyeUpgrade;
        public ItemType wearableModifier;
    }
    
    [Serializable]
    public class Quests {
        public QuestGraphRuntime graph;
        public Dictionary<int, Quest.State> stateLookupFromUuid = new();
        public Quest pickPocketQuest;
        public Quest overweightExtractQuest;
        
        public Queue<QuestPackage> reservedPkgs = new();
        public List<QuestPackage> activePkgs = new();
        public QuestPackage presentingPkg;
    }
    
    [Serializable]
    public class ItemRefs {
        public Item demonEye;
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
        public CinemachineCamera cinemachine;
        public PixelPerfectCamera pixelPerfect;
        [NonSerialized] public int defaultPPU;
    }
    
    [Serializable]
    public class Curves {
        public AnimationCurve hitFlash;
        public AnimationCurve bounce;
        public AnimationCurve shake;
        public AnimationCurve pentagramFill;
        public AnimationCurve pentagramItemShake;
        public AnimationCurve questBurn;
        public AnimationCurve discoverSlotTimingCurve;
    }
    
    [Serializable] 
    public class UI {
        public RectTransform mainCanvasRectTransform;
        public Minimap minimap;
        public ItemDescPopup itemDescPopupInv;
        public ItemDescPopup itemDescPopupPickup;
        public MechanicDescPopup mechanicDescPopup;
        public UIElementPopup uiElementPopup;
        public RectTransform hideoutParent;
        public RectTransform hotBarParent;
        public ItemUI dragAndDropItemUI;
        public Image animatedBgImage;
        public Image deathBgImage;
        public ButtonFeel menuBackButton;
        public TextMeshProUGUI smallRaidText;
        public TypewriterComponent smallRaidTextTypewriter;
        public TextMeshProUGUI largeRaidText;
        public TypewriterComponent largeRaidTextTypewriter;
        public RectTransform lootInventoryPanel;
        public RectTransform lootInventoryParent;
        public GameObject lootSearchingText;
        public TextMeshProUGUI interactPrompt;
        public TextMeshProUGUI interactDetails;
        public RectTransform portalArrow;
        public RectTransform damageNumbersParent;
    }
    
    [Serializable]
    public class PlayerInfo {
        public GameObject parent;
        public GameObject healthBarParent;
        public GameObject weightBarParent;
        public GameObject soulsCurrencyParent;
        public GameObject coinsCurrencyParent;
        public GameObject bleedDebuffIcon;
        public Image healthBarFillImage;
        public Image weightBarFillImage;
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
    public class MainMenu {
        public RectTransform parent;
        public RectTransform logo;
        public ButtonFeel playButton;
        public ButtonFeel hideoutButton;
        public ButtonFeel settingsButton;
        public ButtonFeel exitButton;
    } 
    
    [Serializable]
    public class HideoutTabs {
        public RectTransform parent;
        public Sprite nonSelectedSprite;
        public Sprite selectedSprite;
        public Button characterButton;
        public Button eyeForgeButton;
        public Button traderButton;
        public Button questsButton;
        public Button skillsButton;
        public TextMeshProUGUI characterText;
        public TextMeshProUGUI eyeForgeText;
        public TextMeshProUGUI traderText;
        public TextMeshProUGUI questsText;
        public TextMeshProUGUI skillsText;
    }
    
    [Serializable]
    public class PlayerPanel {
        public RectTransform panel;
        public RectTransform pocketParent;
        public RectTransform quickUseParent;
        public RectTransform inventoryParent;
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
        public RectTransform pentagramParent;
        public Image pentagramFillImage;
        public ButtonFeel forgeButton;
    }
    
    [Serializable]
    public class EyeForgeDetailsPanel {
        public RectTransform panel;
        public TextMeshProUGUI text;
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
    public class MapSelectionPanel {
        public RectTransform panel;
        public Button[] buttons;
    }
    
    [Serializable]
    public class QuestsPanel {
        public RectTransform panel;
        public RectTransform questsParent;
        public RectTransform questSelectionParent;
        public ToggleButtonGroup toggleButtonGroup;
        public TraderRepBar traderRepBar;
        public Image scortchedOverlayImage;
        public ParticleSystem emberParticles;
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
        public DynamicClip projectileImpact;
        public DynamicClip bloodBurstClip;
        public DynamicClip footStepClip;
        public DynamicClip teleportInClip;
        public DynamicClip teleportOutClip;
        public DynamicClip portalSpawnClip;
        public DynamicClip portalDespawnClip;
        public DynamicClip finalWaveStingerClip;
        public DynamicClip lootRevealClip;
        public DynamicClip slotRevealClip;
        public DynamicClip lootingBodyClip;
        public DynamicClip lootingBushClip;
        public DynamicClip lootingBodyLoop;
        public DynamicClip lootingBushLoop;
        public DynamicClip rarityRevealClip;
        
        public Dictionary<int, List<DynamicClipRecord>> records = new(50);
        public Dictionary<AudioSource, int> generationLookup = new();
        public List<AudioClipHandle> loopingSources = new();
        public Queue<AudioSource> reservedSources;
    }
    
    public class Input {
        public InputAction move;
        public InputAction interact;
        public InputAction inventory;
        public InputAction selectItem;
        public InputAction placeSingleItem;
        public InputAction useItem;
        public InputAction moveStack;
        public InputAction splitStack;
        public InputAction escape;
        public InputAction quickUse1;
        public InputAction quickUse2;
        public InputAction quickUse3;
        public InputAction quickUse4;
    }
    
    public class EntityPools {
        public EntityPool<Entity> itemDrop;
        public EntityPool<Entity> bloodDrop;
        public EntityPool<Projectile> projectile;
        public EntityPool<Projectile> boneShatterProjectile;
        public EntityPool<Projectile> gooProjectile;
        public EntityPool<Projectile> piercingShotProjectile;
        public EntityPool<Projectile> soulProjectile;
        public EntityPool<Entity> poisonDebuff;
        public EntityPool<Entity> explosion;
        public EntityPool<Entity> projectileImpact;
        public EntityPool<Entity> teleportIn;
        public EntityPool<Entity> teleportOut;
        public EntityPool<Entity> bloodSplatter;
        public EntityPool<Entity> runSmoke;
        public EntityPool<Entity> damageNumber;
        public EntityPool<Entity> forgeExplosion;
        public EntityPool<Entity> forgeDust;
        public EntityPool<Entity> upgradeFractureParticles;
        public EntityPool<Entity> blast;
        public EntityPool<Entity> lootReveal;
    }
    
    public class States {
        public State mainMenu;
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
        public List<Enemy> enemies = new();
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
    
    public class HotBar {
        public List<InputAction> quickUseActions;
        public InventorySlotUI[] slotUIs;
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
    
    public class CurrentRaid {
        public RaidState state;
        public bool stateSwitchedThisFrame;
        
        public MapData map;
        public MapInstance mapInstance;
        public MapLoadingState mapLoadingState;
        
        public Vector2 lastPlayerGridPos;
        public Limiter flowFieldLimiter;
        
        public List<Vector2> teleportingInPositions = new();
        public Dictionary<GameObject, InventorySlot[]> bushSlotsLookup = new();
        public Dictionary<GameObject, InventorySlot[]> deadBodySlotsLookup = new();
        
        // Data that gets reset every time a new raid starts
        public struct Data {
            public DamagingData damaging;
            public InteractionData interactions;
            public TrinketData trinkets;
            public Limiter reteleportLimitter;
        }
        public Data data;
    } 
    
    [Flags]
    public enum PersistentFlags {
        None                   = 0,
        BloodMushroomsUnlocked = 1 << 0,
    }
    
    [Flags] 
    public enum FrameFlags {
        None            = 0,
        EarlyExitTaken  = 1 << 0,
        ExitTaken       = 1 << 1,
        SkillUpgraded   = 1 << 2,
        BleedStopped    = 1 << 3,
        PostRaidInit    = 1 << 4,
        DemonEyeChanged = 1 << 5,
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
    
}
