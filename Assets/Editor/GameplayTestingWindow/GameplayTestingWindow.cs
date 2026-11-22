using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class GameplayTestingWindow : EditorWindow {
    
    [SerializeField] VisualTreeAsset visualTreeAsset;
    [SerializeField] MapData currentMap;
    [SerializeField] bool hideInactive;
    [SerializeField] bool overrideWaves;
    [SerializeField] int startWaveIndex;
    
    VisualElement root => rootVisualElement;
    ObjectField MapField => root.Q<ObjectField>("MapField");
    Toggle HideInactiveToggle => root.Q<Toggle>("HideInactive");
    VisualElement ItemPoolContainer => root.Q<VisualElement>("ItemPool");
    Button RefreshButton => root.Q<Button>("RefreshBttn");
    Button CreateEyeButton => root.Q<Button>("CreateEyeBttn");
    ObjectField TestingInventoryField => root.Q<ObjectField>("TestingInventoryField");
    Button EquipTestingInventoryButton => root.Q<Button>("EquipTestingInventoryBttn");
    Button PlayerDamageButton => root.Q<Button>("PlayerDamageBttn");
    Button PlayerBleedButton => root.Q<Button>("PlayerBleedBttn");
    Button MoneyButton => root.Q<Button>("MoneyBttn");
    Button SoulsButton => root.Q<Button>("SoulsBttn");
    SliderInt TraderLevelSlider => root.Q<SliderInt>("TraderLevelSlider");
    Toggle SkipTutorialToggle => root.Q<Toggle>("SkipTutorial");
    List<MapData> Maps => FindFirstObjectByType<GameRunner>().config.maps;

    [MenuItem("Window/UI Toolkit/Gameplay Testing")]
    public static void ShowExample() {
        GameplayTestingWindow wnd = GetWindow<GameplayTestingWindow>();
        wnd.titleContent = new("GameplayTestingWindow");
    }

    public void CreateGUI() {
        VisualElement labelFromUXML = visualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
        
        MapField.RegisterCallback<ChangeEvent<Object>>(OnMapDataChanged);
        HideInactiveToggle.RegisterCallback<ChangeEvent<bool>>(OnHideInactiveToggled);
        RefreshButton.RegisterCallback<ClickEvent>(OnRefreshClicked);
        CreateEyeButton.RegisterCallback<ClickEvent>(OnCreateEyeClicked);
        EquipTestingInventoryButton.RegisterCallback<ClickEvent>(OnEquipTestingInventory);
        PlayerDamageButton.RegisterCallback<ClickEvent>(OnDamagePlayer);
        PlayerBleedButton.RegisterCallback<ClickEvent>(OnMakePlayerBleed);
        MoneyButton.RegisterCallback<ClickEvent>(OnGiveMoney);
        SoulsButton.RegisterCallback<ClickEvent>(OnGiveSouls);
        TraderLevelSlider.RegisterValueChangedCallback(OnTraderLevelChanged);

        // Kept in EditorPrefs instead of on the window so the game can read it when it starts
        SkipTutorialToggle.value = EditorPrefs.GetBool(Game.skipTutorialEditorPrefKey);
        SkipTutorialToggle.RegisterValueChangedCallback(changeEvent => EditorPrefs.SetBool(Game.skipTutorialEditorPrefKey, changeEvent.newValue));
        
        TestingInventoryField.value = LoadTestingInventoryFromEditorPrefs();
        TestingInventoryField.RegisterValueChangedCallback(OnTestingInventoryChanged);

        // Restore settings after domain reload
        MapField.value = currentMap;
        HideInactiveToggle.value = hideInactive;
    }

    void OnEnable() {
        EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
    }

    void OnDisable() {
        EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
    }

    // Disable being able to change things because we can only inject a custom spawn pattern when entering playmode
    void OnPlaymodeStateChanged(PlayModeStateChange changeEvent) {
        bool enabled = changeEvent is PlayModeStateChange.EnteredEditMode or PlayModeStateChange.ExitingPlayMode;
        MapField?.SetEnabled(enabled);
    }

    void OnSelectionChange() { 
        RefreshImageBackgrounds();
    }

    void OnHideInactiveToggled(ChangeEvent<bool> changeEvent) {
        hideInactive = changeEvent.newValue;
        ListItemPoolForMap();
    }

    void OnRefreshClicked(ClickEvent e) {
        ListItemPoolForMap();
    }

    void OnCreateEyeClicked(ClickEvent e) {
        if (!Application.isPlaying || currentMap == null) return;
        
        Game.CreateDropPoolsForMap(currentMap); 
        List<Game.ItemInstance> itemInstances = new();
        for (int i = 0; i < 5; i++) {
            Item item = Game.GetItemFromDropPool(Game.dropPools.eyeUpgrades, currentMap);
            itemInstances.Add(new(item));
        }
        
        Game.ItemInstance demonEye = Game.CreateNewDemonEyeItemInstance(Game.randomDemonEyeNames.GetRandom(), itemInstances);
        Game.TryAddItemToInventory(Game.inventories.stash, demonEye);
    }

    void OnMapDataChanged(ChangeEvent<Object> changeEvent) {
        MapData prevMap = changeEvent.previousValue as MapData;
        prevMap?.SetRaidSpawnPatternInjection(null);
        
        currentMap = changeEvent.newValue as MapData;
        currentMap?.SetRaidSpawnPatternInjection(overrideWaves ? InjectRaidSpawnPattern : null);
        
        ListItemPoolForMap();
    }

    void ListItemPoolForMap() {
        ItemPoolContainer.Clear();
        if (currentMap == null) return;
        
        List<IEyeUpgrade> eyeUpgrades = LoadAllEyeUpgrades();
        eyeUpgrades.Sort(CompareItemsSpawnMaps);
        
        if (hideInactive) {
            eyeUpgrades = eyeUpgrades.Where(ItemIsApartOfCurrentDropPool).ToList();
        }
        
        foreach (IEyeUpgrade eyeUpgrade in eyeUpgrades) {
            Image image = new() {
                sprite = eyeUpgrade.InventorySprite,
                style = { width = 40, height = 40, backgroundColor = GetBackgroundColor(eyeUpgrade) },
                tooltip = eyeUpgrade.IsAugment ? $"{eyeUpgrade.DisplayName} (Augmented)" : $"{eyeUpgrade.DisplayName}",
                userData = eyeUpgrade,
            };
            
            image.RegisterCallback<ClickEvent>(_ => OnImageClicked(eyeUpgrade));
            ItemPoolContainer.Add(image);
        } 
    }

    bool ItemIsApartOfCurrentDropPool(IEyeUpgrade itemInterface) {
        MapData curSelectedMap = MapField.value as MapData;
        if (itemInterface.MapSpawning.spawnsOnAll) return true;
        if (PassedFirstMapSpawn(itemInterface.MapSpawning.firstSpawnMap)) return true;
        return itemInterface.MapSpawning.spawnsOnMaps.Contains(curSelectedMap);
        
        bool PassedFirstMapSpawn(MapData firstMapItemSpawn) {
            if (curSelectedMap == null || firstMapItemSpawn == null) return false;
            return Maps.IndexOf(curSelectedMap) >= Maps.IndexOf(firstMapItemSpawn);
        }
    }

    int CompareItemsSpawnMaps(IEyeUpgrade x, IEyeUpgrade y) {
        return GetMapIndex(x).CompareTo(GetMapIndex(y));
    }
    
    // Returns -1 if spawns on all maps and int.MaxValue if not assigned to any map 
    int GetMapIndex(IEyeUpgrade eyeUpgrade) {
        if (eyeUpgrade.MapSpawning.spawnsOnAll) {
            return -1;
        } 
        if (eyeUpgrade.MapSpawning.firstSpawnMap != null) {
            int index = Maps.IndexOf(eyeUpgrade.MapSpawning.firstSpawnMap);
            return index == -1 ? int.MaxValue : index;
        }
        return int.MaxValue;
    }

    List<IEyeUpgrade> LoadAllEyeUpgrades() {
        List<IEyeUpgrade> eyeUpgrades = new();
        UuidScriptableObject[] resourceObjects = Resources.LoadAll<UuidScriptableObject>(string.Empty);
        foreach (UuidScriptableObject res in resourceObjects) {
            if (res is Augment augment) {
                eyeUpgrades.Add(augment);
            }
            else if (res is EyeUpgrade eyeUpgrade) {
                eyeUpgrades.Add(eyeUpgrade);
            }
        }
        return eyeUpgrades;
    }

    Color GetBackgroundColor(IEyeUpgrade eyeUpgrade) {
        int curMapIndex = Maps.IndexOf(currentMap);
        int firstMapItemIndex = GetMapIndex(eyeUpgrade);
        
        string colorString;
        if (Selection.activeObject == eyeUpgrade.UuidObject) {
            colorString = "#2C5D87";
        }
        else if (curMapIndex == 0 && firstMapItemIndex == -1) {
            colorString = "#4D4D4D";
        }
        else if (firstMapItemIndex == int.MaxValue) {
            colorString = "#BD6351";
        }
        else if (firstMapItemIndex < curMapIndex) {
            colorString = "#383838";
        }
        else if (curMapIndex == firstMapItemIndex) {
            colorString = "#4D4D4D";
        }
        else {
            colorString = "#2A2A2A";
        }
        
        ColorUtility.TryParseHtmlString(colorString, out Color color);
        return color;
    }

    void OnImageClicked(IEyeUpgrade eyeUpgrade) {
        if (Selection.activeObject == eyeUpgrade.UuidObject) {
            Selection.activeObject = null;
            return;
        }
        Selection.activeObject = eyeUpgrade.UuidObject;
    }

    void RefreshImageBackgrounds() {
        ItemPoolContainer.Query<Image>().ForEach(img => img.style.backgroundColor = GetBackgroundColor(img.userData as IEyeUpgrade));
    }

    RaidSpawnPattern InjectRaidSpawnPattern() {
        if (startWaveIndex <= 0) {
            return currentMap.spawning;
        }
        
        RaidSpawnPattern clonedWaves = Instantiate(currentMap.spawning);
        clonedWaves.timeBeforeFirstPhase = 5f;
        clonedWaves.phasePools.RemoveRange(0, startWaveIndex);
        return clonedWaves;
    }

    TestingInventory SelectedTestingInventory => TestingInventoryField.value as TestingInventory;
    
    // Kept in EditorPrefs instead of on the window so the game can read it when it starts
    static TestingInventory LoadTestingInventoryFromEditorPrefs() {
        string guid = EditorPrefs.GetString(Game.testingInventoryEditorPrefKey);
        if (string.IsNullOrEmpty(guid)) return null;
        return AssetDatabase.LoadAssetAtPath<TestingInventory>(AssetDatabase.GUIDToAssetPath(guid));
    }

    void OnTestingInventoryChanged(ChangeEvent<Object> changeEvent) {
        if (SelectedTestingInventory == null) {
            EditorPrefs.DeleteKey(Game.testingInventoryEditorPrefKey);
        }
        else {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(SelectedTestingInventory));
            EditorPrefs.SetString(Game.testingInventoryEditorPrefKey, guid);
        }

        // Entering play mode loads it automatically, so only need to load it manually when assigned mid play
        if (Application.isPlaying && SelectedTestingInventory != null) {
            Game.LoadTestingInventoryOntoPlayer(SelectedTestingInventory);
        }
    }

    void OnEquipTestingInventory(ClickEvent e) {
        if (!Application.isPlaying || SelectedTestingInventory == null) return;
        Game.LoadTestingInventoryOntoPlayer(SelectedTestingInventory);
    }

    void OnDamagePlayer(ClickEvent e) {
        if (!Application.isPlaying) return;
        Game.DamagePlayer(10, Game.PlayerDamageType.Normal, null);
    }

    void OnMakePlayerBleed(ClickEvent e) {
        if (!Application.isPlaying) return;
        Game.player.bleeding = true;
    }

    void OnGiveMoney(ClickEvent e) {
        if (!Application.isPlaying) return;
        Game.player.state.coinCurrency += 5000;
    }

    void OnGiveSouls(ClickEvent e) {
        if (!Application.isPlaying) return;
        Game.curRaid.data.souls += 500;
    }

    void OnTraderLevelChanged(ChangeEvent<int> changeEvent) {
        if (!Application.isPlaying) return;
        
        int traderLevelIndex = changeEvent.newValue - 1; 
        GameData.Config config = Game.config;
        int repNeeded = config.trader.levels.prefixedLevels[traderLevelIndex];
        Game.config.trader.state.reputation = repNeeded;
        Game.FillTraderInventoryWithItems();
    }
    
}