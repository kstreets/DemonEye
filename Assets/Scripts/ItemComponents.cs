using System;
using System.Collections.Generic;
using UnityEngine;
using VInspector;

public static class ItemComponents {
    
    [Serializable]
    public class MapSpawning {
        public bool spawnsOnAll;
        [ShowIf(nameof(spawnsOnAll), false)]
        public MapData firstSpawnMap;
        public List<MapData> spawnsOnMaps;
    }
    
    [Serializable]
    public class TraderSpawning {
        [Range(1, 10)] public int levelRequired;
        [MinMaxSlider(1, 15)] public Vector2Int stockRange;
        public List<ItemWithCount> barterRequirements;
    }
    
}
