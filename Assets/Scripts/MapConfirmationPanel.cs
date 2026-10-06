using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapConfirmationPanel : MonoBehaviour {
    
    public TextMeshProUGUI mapNameTextMesh;
    public RawImage mapPreviewImage;
    public MapInfoLine[] infoLines;
    public ButtonFeel teleportButton;
    
    public Sprite difficultyIcon;
    public Sprite waveIcon;
    public Sprite extractionIcon;
    public Sprite lootIcon;
    
    [NonSerialized] public MapData selectedMap;
    private List<MapInfoLine> reservedInfoLines = new(7);
    
    public void Display(MapData map) {
        selectedMap = map;
        mapNameTextMesh.text = map.displayName;
        mapPreviewImage.texture = map.previewImage;
        
        QueueAllInfoLines();
        reservedInfoLines.PopLast().Show(difficultyIcon, $"{map.difficultyText} Difficulty");
        reservedInfoLines.PopLast().Show(waveIcon, $"{map.spawning.phasePools.Count} Waves");
        reservedInfoLines.PopLast().Show(extractionIcon, $"{map.exitPortalsCount} Extraction Portals");
        DisplayLootIncrease(map.uncommonLootRarityIncrease, Item.Rarity.Uncommon);
        DisplayLootIncrease(map.rareLootRarityIncrease, Item.Rarity.Rare);
        DisplayLootIncrease(map.epicLootRarityIncrease, Item.Rarity.Epic);
        DisplayLootIncrease(map.legendaryLootRarityIncrease, Item.Rarity.Legendary);
    }
    
    private void DisplayLootIncrease(float lootIncrease, Item.Rarity rarityType) {
        if (lootIncrease <= 0f) return;
        Color rarityColor = Game.gameInstance.config.styles.GetTextColorForRarity(rarityType);
        string text = $"+{lootIncrease * 100f:0.#}% {rarityType.ToString()} Item Drops";
        reservedInfoLines.PopLast().Show(lootIcon, Game.ColorText(text, rarityColor));
    }
    
    private void QueueAllInfoLines() {
        foreach (MapInfoLine mapInfoLine in infoLines) {
            mapInfoLine.gameObject.SetActive(false);
            reservedInfoLines.Add(mapInfoLine);
        }
    }
    
}
