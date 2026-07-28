using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Renames every sliced sprite in a texture's sprite sheet to "<rootAssetName>_<index>",
/// where rootAssetName is the texture's own file name (no extension).
///
/// e.g. texture "swamptileset.png" sliced into "sulfurtileset_0", "sulfurtileset_1", ...
/// becomes "swamptileset_0", "swamptileset_1", ...
///
/// Usage: select one or more sliced textures in the Project window, then
/// Assets > Sprite Tools > Rename Sliced Sprites To Root Name.
/// </summary>
public static class RenameSlicedSpritesToRoot
{
    [MenuItem("Assets/Sprite Tools/Rename Sliced Sprites To Root Name")]
    private static void RenameSelected()
    {
        Object[] selection = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        if (selection.Length == 0)
        {
            Debug.LogWarning("RenameSlicedSpritesToRoot: no textures selected.");
            return;
        }

        int texturesTouched = 0;
        int spritesRenamed = 0;

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (Object obj in selection)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
                    continue;

                SpriteMetaData[] sheet = importer.spritesheet;
                if (sheet == null || sheet.Length == 0)
                    continue;

                string rootName = System.IO.Path.GetFileNameWithoutExtension(path);
                bool changed = false;

                for (int i = 0; i < sheet.Length; i++)
                {
                    string suffix = GetTrailingSuffix(sheet[i].name, i);
                    string newName = rootName + "_" + suffix;

                    if (sheet[i].name != newName)
                    {
                        sheet[i].name = newName;
                        changed = true;
                        spritesRenamed++;
                    }
                }

                if (changed)
                {
                    importer.spritesheet = sheet;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    texturesTouched++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log($"RenameSlicedSpritesToRoot: renamed {spritesRenamed} sprite(s) across {texturesTouched} texture(s).");
    }

    [MenuItem("Assets/Sprite Tools/Rename Sliced Sprites To Root Name", true)]
    private static bool ValidateRenameSelected()
    {
        return Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets).Length > 0;
    }

    // Pulls the trailing "_<digits>" off a sprite name, e.g. "sulfurtileset_12" -> "12".
    // Falls back to the sprite's sheet index if no numeric suffix is present, so the
    // tool still works on sprites that were never in a "name_number" format.
    private static string GetTrailingSuffix(string spriteName, int fallbackIndex)
    {
        if (string.IsNullOrEmpty(spriteName))
            return fallbackIndex.ToString();

        int lastUnderscore = spriteName.LastIndexOf('_');
        if (lastUnderscore < 0 || lastUnderscore == spriteName.Length - 1)
            return fallbackIndex.ToString();

        for (int i = lastUnderscore + 1; i < spriteName.Length; i++)
        {
            if (!char.IsDigit(spriteName[i]))
                return fallbackIndex.ToString();
        }

        return spriteName.Substring(lastUnderscore + 1);
    }
}
