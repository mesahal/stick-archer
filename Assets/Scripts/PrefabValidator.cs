using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Validates and creates placeholder prefabs if missing.
/// Run via menu: Tools → Validate Prefabs
/// </summary>
#if UNITY_EDITOR
public class PrefabValidator : MonoBehaviour
{
    const string ARCHER_PREFAB_PATH = "Assets/Prefabs/ArcherLocal.prefab";
    const string ARROW_PREFAB_PATH = "Assets/Prefabs/ArrowLocal.prefab";
    
    [MenuItem("Tools/Validate Prefabs")]
    static void ValidateAllPrefabs()
    {
        Debug.Log("[PrefabValidator] Starting validation...");
        
        // Check ArcherLocal prefab
        GameObject archerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARCHER_PREFAB_PATH);
        if (archerPrefab == null)
        {
            Debug.LogWarning("[PrefabValidator] ArcherLocal.prefab not found at " + ARCHER_PREFAB_PATH);
            Debug.Log("[PrefabValidator] Creating placeholder prefab...");
            CreatePlaceholderArcherPrefab();
        }
        else
        {
            Debug.Log("[PrefabValidator] ✓ ArcherLocal.prefab found");
        }
        
        // Check ArrowLocal prefab
        GameObject arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARROW_PREFAB_PATH);
        if (arrowPrefab == null)
        {
            Debug.LogWarning("[PrefabValidator] ArrowLocal.prefab not found at " + ARROW_PREFAB_PATH);
            Debug.Log("[PrefabValidator] Creating placeholder prefab...");
            CreatePlaceholderArrowPrefab();
        }
        else
        {
            Debug.Log("[PrefabValidator] ✓ ArrowLocal.prefab found");
        }
        
        // Assign to GameArenaBootstrap
        AssignPrefabsToBootstrap();
        
        Debug.Log("[PrefabValidator] Validation complete!");
    }
    
    [MenuItem("Tools/Assign Prefabs to Bootstrap")]
    static void AssignPrefabsToBootstrap()
    {
        // Find GameArenaBootstrap in scene
        var bootstrap = FindObjectOfType<GameArenaBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogError("[PrefabValidator] No GameArenaBootstrap found in current scene!");
            return;
        }
        
        SerializedObject so = new SerializedObject(bootstrap);
        
        // Assign archer prefab
        GameObject archerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARCHER_PREFAB_PATH);
        if (archerPrefab != null && bootstrap.archerLocalPrefab == null)
        {
            so.FindProperty("archerLocalPrefab").objectReferenceValue = archerPrefab;
            Debug.Log("[PrefabValidator] Assigned archerLocalPrefab to GameArenaBootstrap");
        }
        
        // Assign arrow prefab
        GameObject arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ARROW_PREFAB_PATH);
        if (arrowPrefab != null && bootstrap.arrowLocalPrefab == null)
        {
            so.FindProperty("arrowLocalPrefab").objectReferenceValue = arrowPrefab;
            Debug.Log("[PrefabValidator] Assigned arrowLocalPrefab to GameArenaBootstrap");
        }
        
        so.ApplyModifiedProperties();
    }
    
    static void CreatePlaceholderArcherPrefab()
    {
        // Create directory if needed
        if (!Directory.Exists("Assets/Prefabs"))
            Directory.CreateDirectory("Assets/Prefabs");
        
        // Create basic archer GameObject
        GameObject archer = new GameObject("ArcherLocal");
        
        // Add required components
        archer.AddComponent<SpriteRenderer>();
        var rb = archer.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        var col = archer.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 1.5f);
        
        // Add scripts
        archer.AddComponent<ArcherLocal>();
        
        // Create prefab
        PrefabUtility.SaveAsPrefabAsset(archer, ARCHER_PREFAB_PATH);
        DestroyImmediate(archer);
        
        Debug.Log($"[PrefabValidator] Created placeholder: {ARCHER_PREFAB_PATH}");
        Debug.LogWarning("[PrefabValidator] ⚠ Placeholder prefab created. You should replace it with your actual archer art!");
    }
    
    static void CreatePlaceholderArrowPrefab()
    {
        if (!Directory.Exists("Assets/Prefabs"))
            Directory.CreateDirectory("Assets/Prefabs");
        
        GameObject arrow = new GameObject("ArrowLocal");
        
        // Add components
        arrow.AddComponent<SpriteRenderer>();
        var rb = arrow.AddComponent<Rigidbody2D>();
        var col = arrow.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 0.15f);
        col.isTrigger = true;
        
        // Add scripts
        arrow.AddComponent<ArrowLocal>();
        arrow.AddComponent<ArrowTrail>();
        arrow.AddComponent<ArrowStuck>();
        
        // Create prefab
        PrefabUtility.SaveAsPrefabAsset(arrow, ARROW_PREFAB_PATH);
        DestroyImmediate(arrow);
        
        Debug.Log($"[PrefabValidator] Created placeholder: {ARROW_PREFAB_PATH}");
        Debug.LogWarning("[PrefabValidator] ⚠ Placeholder prefab created. You should replace it with your actual arrow art!");
    }
    
    [MenuItem("Tools/Setup Android Build")]
    static void SetupAndroidBuild()
    {
        // Set orientation
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        Debug.Log("[PrefabValidator] Android build settings applied:");
        Debug.Log("  - Orientation: Landscape Left");
        Debug.Log("  - To build: File \u2192 Build Settings \u2192 Build");
        Debug.LogWarning("[PrefabValidator] Note: You must still add scenes to Build Settings manually!");
    }
}
#endif
