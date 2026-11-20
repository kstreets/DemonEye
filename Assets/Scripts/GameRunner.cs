using UnityEngine;
using static GameData;

public class GameRunner : MonoBehaviour {

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

    void Awake() {
        Game.runner = this;
        Game.config = config;
        Game.dropPools = dropPools;
        Game.prefabs = prefabs;
        Game.itemTypes = itemTypes;
        Game.quests = quests;
        Game.itemRefs = itemRefs;
        Game.skillUpgradePaths = skillUpgradePaths;
        Game.camera = camera;
        Game.blood = blood;
        Game.curves = curves;
        Game.ui = ui;
        Game.inputPrompts = inputPrompts;
        Game.playerInfo = playerInfo;
        Game.raidInfo = raidInfo;
        Game.bossBar = bossBar;
        Game.mainMenu = mainMenu;
        Game.pauseMenu = pauseMenu;
        Game.hideoutTabs = hideoutTabs;
        Game.playerPanel = playerPanel;
        Game.stashPanel = stashPanel;
        Game.eyeForgePanel = eyeForgePanel;
        Game.eyeForgeDetailsPanel = eyeForgeDetailsPanel;
        Game.traderPanel = traderPanel;
        Game.transactionPanel = transactionPanel;
        Game.mapPanels = mapPanels;
        Game.questsPanel = questsPanel;
        Game.skillsPanel = skillsPanel;
        Game.audio = audio;
        Game.music = music;
        Game.settings = settings;
    }

    void Start() => Game.Start();
    void Update() => Game.Update();
    void FixedUpdate() => Game.FixedUpdate();
    void LateUpdate() => Game.LateUpdate();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void OnGUI() => Game.OnGUI();
#endif
    void OnApplicationQuit() => Game.OnApplicationQuit();

}
