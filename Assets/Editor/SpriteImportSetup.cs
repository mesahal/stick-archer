#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time setup: configures ALL art assets with correct import settings.
/// Kenney pixel platformer tiles = 18×18 px (PPU=18)
/// Kenney simplified platformer = 64×64 px (PPU=64)
/// Kenney background elements = variable (PPU=100)
/// Character sprites = ~50×80 px (PPU=32)
/// All pixel art uses Point filtering for crisp rendering.
/// </summary>
[InitializeOnLoad]
public static class SpriteImportSetup
{
    const string DoneKey = "SpriteImportSetup_v8";

    static SpriteImportSetup()
    {
        if (EditorPrefs.GetBool(DoneKey, false)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.delayCall += Run;
    }

    static void Run()
    {
        EditorApplication.delayCall -= Run;
        if (EditorApplication.isPlaying) return;

        Debug.Log("[SpriteImportSetup] Configuring sprite imports...");
        int count = 0;

        // Pixel platformer tiles (18×18)
        count += SetupFolder("Assets/Resources/Platforms", 18, FilterMode.Point);
        count += SetupFolder("Assets/Art/Platforms", 18, FilterMode.Point);

        // Simplified platformer tiles (64×64)
        count += SetupFolder("Assets/Resources/Platforms/building_wall.png", 64, FilterMode.Point);
        count += SetupFolder("Assets/Resources/Platforms/building_wall_alt.png", 64, FilterMode.Point);
        count += SetupFolder("Assets/Resources/Platforms/building_window.png", 64, FilterMode.Point);
        count += SetupFolder("Assets/Resources/Platforms/grass_top.png", 64, FilterMode.Point);
        count += SetupFolder("Assets/Resources/Platforms/dirt_fill.png", 64, FilterMode.Point);
        count += SetupFolder("Assets/Resources/Platforms/fence.png", 64, FilterMode.Point);

        // Background elements (variable, use Bilinear since they scale up)
        count += SetupFolder("Assets/Resources/Backgrounds", 100, FilterMode.Bilinear);
        count += SetupFolder("Assets/Art/Backgrounds", 100, FilterMode.Bilinear);

        // Character sprites (512 PPU for 1024px images = 2 world units; Bilinear for smooth scaling)
        count += SetupFolder("Assets/Art/Sprites/Player1_Adventurer", 512, FilterMode.Bilinear);
        count += SetupFolder("Assets/Art/Sprites/Player2_Soldier", 512, FilterMode.Bilinear);
        count += SetupFolder("Assets/Resources/Characters/Player1", 512, FilterMode.Bilinear);
        count += SetupFolder("Assets/Resources/Characters/Player2", 512, FilterMode.Bilinear);

        // Bird sprite (128 PPU for 137px Kenney parrot = ~1 world unit, Point for crisp look)
        count += SetupFolder("Assets/Resources/Sprites", 128, FilterMode.Point);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(DoneKey, true);

        Debug.Log($"[SpriteImportSetup] Configured {count} sprites.");
    }

    static int SetupFolder(string path, int ppu, FilterMode filter)
    {
        int count = 0;
        string[] guids;

        if (path.EndsWith(".png"))
        {
            // Single file
            guids = new[] { AssetDatabase.AssetPathToGUID(path) };
            if (string.IsNullOrEmpty(guids[0]))
            {
                SetupSingle(path, ppu, filter, ref count);
                return count;
            }
        }
        else
        {
            guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
        }

        foreach (var guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            SetupSingle(assetPath, ppu, filter, ref count);
        }
        return count;
    }

    static void SetupSingle(string assetPath, int ppu, FilterMode filter, ref int count)
    {
        if (string.IsNullOrEmpty(assetPath)) return;
        var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (imp == null) return;

        bool changed = false;

        if (imp.textureType != TextureImporterType.Sprite)
        { imp.textureType = TextureImporterType.Sprite; changed = true; }

        if (imp.spritePixelsPerUnit != ppu)
        { imp.spritePixelsPerUnit = ppu; changed = true; }

        if (imp.filterMode != filter)
        { imp.filterMode = filter; changed = true; }

        if (imp.textureCompression != TextureImporterCompression.Uncompressed)
        { imp.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }

        // Enable Read/Write for tiling
        if (!imp.isReadable)
        { imp.isReadable = true; changed = true; }

        if (changed)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            count++;
        }
    }
}
#endif
