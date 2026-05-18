using System;
using System.Collections.Generic;
using Febucci.TextAnimatorForUnity;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Game;

[Serializable]
public class GameData {
    
    [Serializable]
    public class Config {
        public StartingItemsConfig startingItems;
        public TraderLevels traderLevels;
        public Styles styles;
        public GameplayConfig gameplay;
        public List<MapData> maps;
    }
    public Config config;
    
    [Serializable]
    public class DropPools {
        public DropPool rockStonesDropPool;
        public DropPool eyeUpgradesDropPool;
        public DropPool bodyDropPool;
        public DropPool traderDropPool;
        public DropPool foragingDropPool;
        public DropPool bushesDropPool;
        public DropPool chestsDropPool;
    }
    public DropPools dropPools;
    
    [Serializable]
    public class Prefabs {
        public GameObject audioSource;
        public GameObject player;
        public GameObject itemDrop;
        public GameObject rockSmokePrefab;
        public GameObject baseProjectile;
        public GameObject boneShatterProjectile;
        public GameObject bloodDrop;
        public GameObject poisonDebuff;
        public GameObject explosion;
        public GameObject boomonExplosion;
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
        public GameObject eyeForgeSlot;
        public GameObject damageNumber;
        public GameObject forgeExplosion;
        public GameObject forgeDustExplosion;
        public GameObject questSelectionToggle;
        public GameObject quest;
    }
    public Prefabs prefabs;
    
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
    public ItemTypes itemTypes;
    
    [Serializable]
    public class Quests {
        public QuestGraphRuntime questGraph;
        public Quest pickPocketQuest;
    }
    public Quests quests;
    
    [Serializable]
    public class ItemRefs {
        public Item demonEye;
    }
    public ItemRefs itemRefs;
    
    [Serializable]
    public class SkillUpgradePaths {
        public SkillUpgradePath haste;
        public SkillUpgradePath intellect;
        public SkillUpgradePath lifeBlood;
        public SkillUpgradePath strength;
    }
    public SkillUpgradePaths skillUpgradePaths;
    
    [Serializable]
    public class Camera {
        public Camera main;
        public CinemachineCamera cinemachine;
        public PixelPerfectCamera pixelPerfect;
    }
    public Camera camera;
    
    [Serializable]
    public class Curves {
        public AnimationCurve hitFlash;
        public AnimationCurve bounce;
        public AnimationCurve shake;
        public AnimationCurve pentagramFill;
        public AnimationCurve pentagramItemShake;
    }
    public Curves curves;
    
    [Serializable] 
    public class UI {
        public RectTransform mainCanvasRectTransform;
        public ItemDescPopup itemDescPopup;
        public MechanicDescPopup mechanicDescPopup;
        public UIElementPopup uiElementPopup;
        public RectTransform hideoutParent;
        public RectTransform hotBarParent;
        public ItemUI dragAndDropItemUI;
        public Image animatedBackgroundImage;
        public Image deathBackgroundImage;
        public ButtonFeel menuBackButton;
        public TextMeshProUGUI smallRaidText;
        public TypewriterComponent smallRaidTextTypewriter;
        public TextMeshProUGUI largeRaidText;
        public TypewriterComponent largeRaidTextTypewriter;
        public RectTransform lootInventoryPanel;
        public RectTransform lootInventoryParent;
        public GameObject lootSearchingText;
        public TextMeshProUGUI interactPrompt;
        public TextMeshProUGUI interactionDetails;
        public RectTransform portalArrow;
        public RectTransform damageNumbersParent;
    }
    public UI ui;
    
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
    }
    public PlayerInfo playerInfo;
    
    [Serializable]
    public class RaidInfo {
        public GameObject parent;
        public GameObject finalWaveCountdownParent;
        public TextMeshProUGUI finalWaveCountdownText;
        public GameObject exitPortalCountdownParent;
        public TextMeshProUGUI exitPortalCountdownText;
        public GameObject exitPortalActiveNotifier;
        public GameObject finalWaveActiveNotifier;
        public GameObject finalExitPortalNotifier;
    }
    public RaidInfo raidInfo;
    
    [Serializable]
    public class MainMenu {
        public RectTransform parent;
        public RectTransform logo;
        public ButtonFeel playButton;
        public ButtonFeel hideoutButton;
        public ButtonFeel settingsButton;
        public ButtonFeel exitButton;
    } 
    public MainMenu mainMenu;
    
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
    public HideoutTabs hideoutTabs;
    
    [Serializable]
    public class PlayerPanel {
        public RectTransform panel;
        public RectTransform equipmentParent;
        public RectTransform pocketParent;
        public RectTransform pocketsBackpackParent;
        public RectTransform passiveParent;
        public RectTransform inventoryParent;
        public TextMeshProUGUI panelHealthText;
        public TextMeshProUGUI panelWeightText;
        public Image previewImage;
        public EquipedStatsPanel equipedStatsPanel;
    }
    public PlayerPanel playerPanel;
    
    [Serializable]
    public class StashPanel {
        public RectTransform panel;
        public RectTransform inventoryParent;
    }
    public StashPanel stashPanel;
    
    [Serializable]
    public class EyeForgePanel {
        public RectTransform panel;
        public RectTransform pentagramParent;
        public Image pentagramFillImage;
    }
    public EyeForgePanel eyeForgePanel;
    
    [Serializable]
    public class EyeForgeDetailsPanel {
        public RectTransform panel;
        public ButtonFeel eyeButton;
        public TextMeshProUGUI text;
        public DemonEyeDescList demonEyeDesc;
    }
    public EyeForgeDetailsPanel eyeForgeDetailsPanel;
    
    [Serializable]
    public class TraderPanel {
        public RectTransform panel;
        public RectTransform inventoryParent;
        public TraderRepBar repBar;
        public TextMeshProUGUI itemRefreshTimeText;
    }
    
    [Serializable]
    public class TransactionPanel {
        public global::TransactionPanel panel;
        public RectTransform inventoryParent;
    }
    public TransactionPanel transactionPanel;

    [Serializable]
    public class MapSelectionPanel {
        public RectTransform mapSelectionPanel;
        public Button[] mapSelectionButtons;
    }
    public MapSelectionPanel mapSelectionPanel;
    
    [Serializable]
    public class QuestsPanel {
        public RectTransform panel;
        public RectTransform parent;
        public RectTransform questSelectionParent;
        public ToggleButtonGroup questToggleButtonGroup;
        public TraderRepBar traderRepBar;
    }
    public QuestsPanel questsPanel;
    
    [Serializable]
    public class SkillsPanel {
        public global::SkillsPanel panel;
        public PlayerStatsPanel playerStatsPanel;
    }
    public SkillsPanel skillsPanel;
    
    [Serializable]
    public class Audio {
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
        
        [NonSerialized] public AudioSource ambienceSource;
        public Dictionary<int, List<DynamicClipRecord>> records = new(50);
        public Queue<AudioSource> reservedSources;
    }
    public Audio audio; 
    
    public class Input {
        public InputAction moveInputAction;
        public InputAction interactInputAction;
        public InputAction inventoryInputAction;
        public InputAction selectItemInputAction;
        public InputAction placeSingleItemInputAction;
        public InputAction useItemInputAction;
        public InputAction moveStackInputAction;
        public InputAction splitStackInputAction;
        public InputAction escapeInputAction;
        public InputAction quickUse1Action;
        public InputAction quickUse2Action;
        public InputAction quickUse3Action;
        public InputAction quickUse4Action;
    }
    public Input input = new();
    
    public class EntityPools {
        public EntityPool<Entity> itemDrop;
        public EntityPool<Entity> bloodDrop;
        public EntityPool<Projectile> projectile;
        public EntityPool<Projectile> boneShatterProjectile;
        public EntityPool<Projectile> gooProjectile;
        public EntityPool<Projectile> piercingShotProjectile;
        public EntityPool<Entity> poisonDebuff;
        public EntityPool<Entity> explosion;
        public EntityPool<Entity> projectileImpact;
        public EntityPool<Entity> teleportIn;
        public EntityPool<Entity> teleportOut;
        public EntityPool<Entity> bloodSplatter;
        public EntityPool<Entity> runSmoke;
        public EntityPool<Entity> damageNumber;
        public EntityPool<Entity> forgeExplosion;
        public EntityPool<Entity> forgeDustExplosion;
        public EntityPool<Entity> blast;
    }
    public EntityPools entityPools = new();
    
    public class States {
        public State mainMenuState;
        public State mapSelectionState;
        public State hideoutState;
        public State raidState;
        public State gameOverState;
        public State winExitState;
        public State earlyExitState;
        public StateMachine gameStateMachine;
    }
    public States states = new();
    
    public class Entities {
        public List<Entity> all = new();
        public Dictionary<GameObject, Entity> lookup = new();
        public Player player;
    }
    public Entities entities = new();
    
    public class Resources {
        public Dictionary<int, UuidScriptableObject> lookup = new();
        public Dictionary<EyeUpgradeItem, List<Augment>> eyeUpgradeAugmentsLookup = new();
        public List<Item> items = new();
        public List<DropPool> dropPools = new();
        public List<DropPool> globalDropPools = new();
        public List<DropPool> mapSpecificDropPools = new();
    }
    public Resources res = new();
    
}
