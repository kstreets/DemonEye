using System.Collections.Generic;
using UnityEngine;

public class MapSelectionPanel : MonoBehaviour {
    
    public RectTransform rectTransform;
    public MapSelection[] selectors;
    
    public void UpdateSelectorStates(List<MapData> maps) {
        for (int i = 0; i < selectors.Length; i++) {
            selectors[i].SetState(maps[i]);
        }
    }
    
}
