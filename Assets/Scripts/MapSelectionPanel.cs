using System.Collections.Generic;
using UnityEngine;

public class MapSelectionPanel : MonoBehaviour {
    
    public RectTransform rectTransform;
    public MapSelection[] selectors;
    
    public void UpdateSelectorStates(List<MapData> maps) {
        for (int i = 0; i < selectors.Length; i++) {
            // Newly unlocked maps stay looking locked until their unlock burn plays
            selectors[i].SetState(maps[i], showAsLocked: maps[i].state.unlockRevealPending);
        }
    }
    
}
