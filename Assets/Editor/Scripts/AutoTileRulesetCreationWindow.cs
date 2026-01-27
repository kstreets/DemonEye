using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using static WaveFunctionCollapseTileRuleset;

public class AutoTileRulesetCreationWindow : EditorWindow {

    private WaveFunctionCollapseTileRuleset sourceRuleset;
    private WaveFunctionCollapseTileRuleset destinationRuleset;
    private Texture2D spriteAtlas;
    private string assetName;
    private const string createNewPath = "Assets/Editor/AutoTileRulesets/";

    [MenuItem("Tools/AutoTileRulesetCreation")]
    public static void ShowWindow() {
        AutoTileRulesetCreationWindow wnd = GetWindow<AutoTileRulesetCreationWindow>(nameof(AutoTileRulesetCreationWindow));
        wnd.minSize = new(300, 150);
    }

    private void OnGUI() {
        spriteAtlas = EditorGUILayout.ObjectField("Sprite Atlas", spriteAtlas, typeof(Texture2D), true) as Texture2D;
        EditorGUILayout.Space();
        sourceRuleset = EditorGUILayout.ObjectField("Source Ruleset", sourceRuleset, typeof(WaveFunctionCollapseTileRuleset), true) as WaveFunctionCollapseTileRuleset;
        
        EditorGUILayout.Space();
        assetName = EditorGUILayout.TextField("Asset Name", assetName,  EditorStyles.textField);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Destination: {GetNewAssetPath()}", EditorStyles.helpBox);
        
        if (GUILayout.Button("Create New")) {
            if (sourceRuleset == null) return;
            
            WaveFunctionCollapseTileRuleset newRuleset = new() {
                baseMapRuleset = new(),
                superMapRuleset = new(),
                idToGuid = new(sourceRuleset.idToGuid),
                emptyStateIndex = sourceRuleset.emptyStateIndex,
                generationMetaString = $"Generated from {sourceRuleset.name} :: {DateTime.Now}",
            };

            newRuleset.baseMapRuleset.rules = new TileRule[sourceRuleset.baseMapRuleset.rules.Length];
            newRuleset.superMapRuleset.rules = new TileRule[sourceRuleset.superMapRuleset.rules.Length];
            
            sourceRuleset.baseMapRuleset.rules.CopyTo(newRuleset.baseMapRuleset.rules, 0);
            sourceRuleset.superMapRuleset.rules.CopyTo(newRuleset.superMapRuleset.rules, 0);
            RemapTileAssetGUIDs(newRuleset.idToGuid);

            AssetDatabase.CreateAsset(newRuleset, GetNewAssetPath());
        }
    }
    
    private void RemapTileAssetGUIDs(List<string> newIdToGui) {
        List<Sprite> spritesFromAtlas = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(spriteAtlas)).OfType<Sprite>().ToList();
        spritesFromAtlas = spritesFromAtlas.OrderBy(x => GetSuffixNumberInAssetName(x.name)).ToList();
        
        for (int i = 0; i < sourceRuleset.idToGuid.Count; i++) {
            string guiString = sourceRuleset.idToGuid[i];
            if (!GUID.TryParse(guiString, out GUID sourceTileGuid)) continue;

            TileBase sourceTileAsset = sourceTileGuid.LoadAsset<TileBase>();
            int sourceNumberSuffix = GetSuffixNumberInAssetName(sourceTileAsset.name);

            if (sourceNumberSuffix < 0) continue;
            
            // We need to map the sprite name to the tile asset of the same name
            string spriteName = spritesFromAtlas[sourceNumberSuffix].name;
            GUID[] guids = AssetDatabase.FindAssetGUIDs(spriteName);
            GUID correspondingTileGuid = guids.FirstOrDefault(x => AssetDatabase.GetMainAssetTypeFromGUID(x) == typeof(Tile));
            if (correspondingTileGuid != default) {
                newIdToGui[i] = correspondingTileGuid.ToString();
            }
        }
    }

    private int GetSuffixNumberInAssetName(string str) {
        return int.Parse(str.Split('_')[^1]);
    }

    private string GetNewAssetPath() {
       return $"{createNewPath}{assetName}.asset"; 
    }

}