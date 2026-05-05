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
    
    private VisualElement root => rootVisualElement;
    private ObjectField MapField => root.Q<ObjectField>("MapField");
    private VisualElement ItemPoolContainer => root.Q<VisualElement>("ItemPool");
    private Button RefreshButton => root.Q<Button>("RefreshBttn");
    private Button PlaceItemsButton => root.Q<Button>("PlaceItemsBttn");
    
    private List<MapData> Maps => _gameMapsBackingField ??= FindFirstObjectByType<Game>().maps;
    private List<MapData> _gameMapsBackingField;

    [MenuItem("Window/UI Toolkit/GameplayTestingWindow")]
    public static void ShowExample() {
        GameplayTestingWindow wnd = GetWindow<GameplayTestingWindow>();
        wnd.titleContent = new("GameplayTestingWindow");
    }

    public void CreateGUI() {
        VisualElement labelFromUXML = visualTreeAsset.Instantiate();
        root.Add(labelFromUXML);
        
        MapField.RegisterCallback<ChangeEvent<Object>>(OnMapDataChanged);
        RefreshButton.RegisterCallback<ClickEvent>(OnRefreshClicked);
        PlaceItemsButton.RegisterCallback<ClickEvent>(OnPlaceItemsClicked);
        
        // Restore selected map after domain reload
        MapField.value = currentMap;
    }
    
    private void OnRefreshClicked(ClickEvent e) {
        ListItemPoolForMap();
    }
    
    private void OnPlaceItemsClicked(ClickEvent e) {
        if (!Application.isPlaying || currentMap == null) return;
        
        MapData selectedMap = MapField.value as MapData;
        Game game = Game.gameInstance;
        
        game.CreateDropPoolsForMap(selectedMap); 
        
        for (int i = 0; i < 5; i++) {
            Item item = game.GetItemFromDropPool(game.eyeUpgradesDropPool, selectedMap);
            game.TryAddItemToInventory(game.stashInventory, item, 1);
        }
    }
    
    private void OnMapDataChanged(ChangeEvent<Object> changeEvent) {
        currentMap = changeEvent.newValue as MapData;
        ListItemPoolForMap();
    }
    
    private void ListItemPoolForMap() {
        ItemPoolContainer.Clear();
        if (currentMap == null) return;
        
        List<IEyeUpgrade> eyeUpgrades = LoadAllEyeUpgrades();
        eyeUpgrades = eyeUpgrades.Where(ItemIsApartOfCurrentDropPool).ToList();
        eyeUpgrades.Sort(CompareItemsSpawnMaps);
        
        foreach (IEyeUpgrade eyeUpgrade in eyeUpgrades) {
            Image image = new() {
            sprite = eyeUpgrade.InventorySprite,
            style = { width = 40, height = 40, backgroundColor = GetBackgroundColorByMap(eyeUpgrade) },
            tooltip = eyeUpgrade.IsAugment ? $"{eyeUpgrade.DisplayName} (Augmented)" : $"{eyeUpgrade.DisplayName}",
            };
            
            image.RegisterCallback<ClickEvent>((_) => {
                Selection.activeObject = eyeUpgrade.GetUuidObject;
            });
            
            ItemPoolContainer.Add(image);
        } 
    }
    
    private bool ItemIsApartOfCurrentDropPool(IEyeUpgrade eyeUpgrade) {
        MapData curSelectedMap = MapField.value as MapData;
        if (eyeUpgrade.SpawnsOnAllMaps) return true;
        if (PassedFirstMapSpawn(eyeUpgrade.FirstSpawnMap)) return true;
        return eyeUpgrade.SpawnsOnMaps.Contains(curSelectedMap);
        
        bool PassedFirstMapSpawn(MapData firstMapItemSpawn) {
            if (curSelectedMap == null || firstMapItemSpawn == null) return false;
            return Maps.IndexOf(curSelectedMap) >= Maps.IndexOf(firstMapItemSpawn);
        }
    }
    
    private int CompareItemsSpawnMaps(IEyeUpgrade x, IEyeUpgrade y) {
        int xIndex = x.SpawnsOnAllMaps ? -1 : Maps.IndexOf(x.FirstSpawnMap);
        int yIndex = y.SpawnsOnAllMaps ? -1 : Maps.IndexOf(y.FirstSpawnMap);
        return xIndex.CompareTo(yIndex);
    }
    
    private List<IEyeUpgrade> LoadAllEyeUpgrades() {
        List<IEyeUpgrade> eyeUpgrades = new();
        UuidScriptableObject[] resourceObjects = Resources.LoadAll<UuidScriptableObject>(string.Empty);
        foreach (UuidScriptableObject res in resourceObjects) {
            if (res is Augment augment) {
                eyeUpgrades.Add(augment);
            }
            else if (res is EyeUpgradeItem eyeUpgrade) {
                eyeUpgrades.Add(eyeUpgrade);
            }
        }
        return eyeUpgrades;
    }
    
    private Color GetBackgroundColorByMap(IEyeUpgrade eyeUpgrade) {
        int index = eyeUpgrade.SpawnsOnAllMaps ? 0 : Maps.IndexOf(eyeUpgrade.FirstSpawnMap);
        string colorString = index % 2 == 0 ? "#383838" : "#2A2A2A";
        ColorUtility.TryParseHtmlString(colorString, out Color color);
        return color;
    }
    
}
