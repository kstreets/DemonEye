using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class GameplayTestingWindow : EditorWindow {
    
    [SerializeField] private VisualTreeAsset visualTreeAsset;
    [SerializeField] private MapData currentMap;
    [SerializeField] private bool hideInactive;
    [SerializeField] private bool overrideWaves;
    [SerializeField] private int startWaveIndex;
    
    private VisualElement root => rootVisualElement;
    private ObjectField MapField => root.Q<ObjectField>("MapField");
    private Toggle HideInactiveToggle => root.Q<Toggle>("HideInactive");
    private VisualElement ItemPoolContainer => root.Q<VisualElement>("ItemPool");
    private Button RefreshButton => root.Q<Button>("RefreshBttn");
    private Button PlaceItemsButton => root.Q<Button>("PlaceItemsBttn");
    private Toggle OverrideWavesToggle => root.Q<Toggle>("OverrideWavesToggle");
    private SliderInt StartWaveSlider => root.Q<SliderInt>("StartWaveSlider");
    private Button PlayerDamageButton => root.Q<Button>("PlayerDamageBttn");
    private Button PlayerBleedButton => root.Q<Button>("PlayerBleedBttn");
    
    private List<MapData> Maps => FindFirstObjectByType<Game>().config.maps;

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
        PlaceItemsButton.RegisterCallback<ClickEvent>(OnPlaceItemsClicked);
        OverrideWavesToggle.RegisterCallback<ChangeEvent<bool>>(OnOverrideWavesToggle);
        StartWaveSlider.RegisterValueChangedCallback(OnStartWaveSliderChanged);
        PlayerDamageButton.RegisterCallback<ClickEvent>(OnDamagePlayer);
        PlayerBleedButton.RegisterCallback<ClickEvent>(OnMakePlayerBleed);
        
        // Restore settings after domain reload
        MapField.value = currentMap;
        HideInactiveToggle.value = hideInactive;
        OverrideWavesToggle.value = overrideWaves;
        StartWaveSlider.value = startWaveIndex;
    }

    private void OnEnable() {
        EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
    }
    
    private void OnDisable() {
        EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
    }

    // Disable being able to change things because we can only inject a custom spawn pattern when entering playmode
    private void OnPlaymodeStateChanged(PlayModeStateChange changeEvent) {
        bool enabled = changeEvent is PlayModeStateChange.EnteredEditMode or PlayModeStateChange.ExitingPlayMode;
        MapField?.SetEnabled(enabled);
        OverrideWavesToggle?.SetEnabled(enabled);
        StartWaveSlider?.SetEnabled(enabled);
    }
    
    private void OnSelectionChange() { 
        RefreshImageBackgrounds();
    }
    
    private void OnHideInactiveToggled(ChangeEvent<bool> changeEvent) {
        hideInactive = changeEvent.newValue;
        ListItemPoolForMap();
    }
    
    private void OnRefreshClicked(ClickEvent e) {
        ListItemPoolForMap();
    }
    
    // We need to rely on this toggle to clear the dependency injection because unity will destroy this window if it becomes not visible.
    // This also means that the dependency injection stays after the window has closed if this toggle does not change.
    private void OnOverrideWavesToggle(ChangeEvent<bool> changeEvent) {
        overrideWaves = changeEvent.newValue;
        currentMap?.SetRaidSpawnPatternInjection(overrideWaves ? InjectRaidSpawnPattern : null);
    }
    
    private void OnStartWaveSliderChanged(ChangeEvent<int> changeEvent) {
        startWaveIndex = changeEvent.newValue;
    }
    
    private void UpdateStartWaveSliderMinMax() {
        if (currentMap == null) {
            StartWaveSlider.lowValue = 0;
            StartWaveSlider.highValue = 0;
            return;
        }
        
        StartWaveSlider.lowValue = 0;
        StartWaveSlider.highValue = currentMap.waves.spawnPhases.Count - 1;
        StartWaveSlider.value = startWaveIndex;
    }
    
    private void OnPlaceItemsClicked(ClickEvent e) {
        if (!Application.isPlaying || currentMap == null) return;
        
        Game game = Game.gameInstance;
        game.CreateDropPoolsForMap(currentMap); 
        for (int i = 0; i < 5; i++) {
            Item item = game.GetItemFromDropPool(game.dropPools.eyeUpgrades, currentMap);
            game.TryAddItemToInventory(game.inventories.stash, item, 1);
        }
    }
    
    private void OnMapDataChanged(ChangeEvent<Object> changeEvent) {
        MapData prevMap = changeEvent.previousValue as MapData;
        prevMap?.SetRaidSpawnPatternInjection(null);
        
        currentMap = changeEvent.newValue as MapData;
        currentMap?.SetRaidSpawnPatternInjection(overrideWaves ? InjectRaidSpawnPattern : null);
        
        ListItemPoolForMap();
        UpdateStartWaveSliderMinMax();
    }
    
    private void ListItemPoolForMap() {
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
    
    private bool ItemIsApartOfCurrentDropPool(IEyeUpgrade itemInterface) {
        MapData curSelectedMap = MapField.value as MapData;
        if (itemInterface.MapSpawning.spawnsOnAll) return true;
        if (PassedFirstMapSpawn(itemInterface.MapSpawning.firstSpawnMap)) return true;
        return itemInterface.MapSpawning.spawnsOnMaps.Contains(curSelectedMap);
        
        bool PassedFirstMapSpawn(MapData firstMapItemSpawn) {
            if (curSelectedMap == null || firstMapItemSpawn == null) return false;
            return Maps.IndexOf(curSelectedMap) >= Maps.IndexOf(firstMapItemSpawn);
        }
    }
    
    private int CompareItemsSpawnMaps(IEyeUpgrade x, IEyeUpgrade y) {
        return GetMapIndex(x).CompareTo(GetMapIndex(y));
    }
    
    private int GetMapIndex(IEyeUpgrade eyeUpgrade) {
        if (eyeUpgrade.MapSpawning.spawnsOnAll) {
            return -1;
        } 
        if (eyeUpgrade.MapSpawning.firstSpawnMap != null) {
            int index = Maps.IndexOf(eyeUpgrade.MapSpawning.firstSpawnMap);
            bool mapNotInMapList = index == -1;
            return mapNotInMapList ? int.MaxValue : index;
        }
        return int.MaxValue;
    }
    
    private List<IEyeUpgrade> LoadAllEyeUpgrades() {
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
    
    private Color GetBackgroundColor(IEyeUpgrade eyeUpgrade) {
        int curMapIndex = Maps.IndexOf(currentMap);
        int firstMapItemIndex = GetMapIndex(eyeUpgrade);
        
        string colorString;
        if (Selection.activeObject == eyeUpgrade.UuidObject) {
            colorString = "#2C5D87";
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
    
    private void OnImageClicked(IEyeUpgrade eyeUpgrade) {
        if (Selection.activeObject == eyeUpgrade.UuidObject) {
            Selection.activeObject = null;
            return;
        }
        Selection.activeObject = eyeUpgrade.UuidObject;
    }
    
    private void RefreshImageBackgrounds() {
        ItemPoolContainer.Query<Image>().ForEach(img => img.style.backgroundColor = GetBackgroundColor(img.userData as IEyeUpgrade));
    }
    
    private RaidSpawnPattern InjectRaidSpawnPattern() {
        if (startWaveIndex <= 0) {
            return currentMap.waves;
        }
        
        RaidSpawnPattern clonedWaves = Instantiate(currentMap.waves);
        clonedWaves.timeBeforeFirstPhase = 5f;
        clonedWaves.spawnPhases.RemoveRange(0, startWaveIndex);
        return clonedWaves;
    }
    
    private void OnDamagePlayer(ClickEvent e) {
        if (!Application.isPlaying) return;
        Game.gameInstance.DamagePlayer(10, Game.PlayerDamageType.Normal, null);
    }
    
    private void OnMakePlayerBleed(ClickEvent e) {
        if (!Application.isPlaying) return;
        Game.player.bleeding = true;
    }
    
}