#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Unity Editor menu: Tools → Stick Archers → Create Prefabs
/// Builds Archer.prefab and Arrow.prefab in Assets/Resources/.
/// Run ONCE after the scene is set up.
/// </summary>
public static class PrefabCreatorHelper
{
    const string ResourcesPath = "Assets/Resources";

    [MenuItem("Tools/Stick Archers/Create Prefabs")]
    static void CreateAllPrefabs()
    {
        EnsureFolder(ResourcesPath);
        CreateArcherPrefab();
        CreateArrowPrefab();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Prefabs Created",
            "Archer.prefab and Arrow.prefab created in Assets/Resources/.\n\n" +
            "Next steps:\n" +
            "1. Assign archer sprites to the SpriteRenderer on each prefab\n" +
            "2. Assign Arrow spawn point child to Archer.arrowSpawnPoint\n" +
            "3. Add an Animator Controller to Archer (idle/charge/fire/ragdoll states)\n" +
            "4. Assign prefabs to NetworkManager.archerPrefab / arrowPrefab fields", "OK");
    }

    // ---------------------------------------------------------------
    //  ARCHER PREFAB
    // ---------------------------------------------------------------
    static void CreateArcherPrefab()
    {
        string path = ResourcesPath + "/Archer.prefab";

        // Root
        GameObject root = new GameObject("Archer");

        // Sprite body
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 1;

        // Physics
        var rb = root.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 2f;

        // Collider (capsule-like box)
        var col = root.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 1.2f);
        col.offset = new Vector2(0f, 0f);

        // Animator
        root.AddComponent<Animator>();

        // Photon
        var pv = root.AddComponent<Photon.Pun.PhotonView>();
        var ptv = root.AddComponent<Photon.Pun.PhotonTransformView>();
        pv.ObservedComponents = new System.Collections.Generic.List<Component>
            { ptv };
        ptv.m_SynchronizePosition = true;
        ptv.m_SynchronizeRotation = false;
        ptv.m_SynchronizeScale = false;

        // Archer script
        var archerScript = root.AddComponent<Archer>();

        // Arrow spawn point child (end of bow)
        GameObject spawnPt = new GameObject("ArrowSpawnPoint");
        spawnPt.transform.SetParent(root.transform, false);
        spawnPt.transform.localPosition = new Vector3(0.6f, 0.3f, 0f);
        archerScript.arrowSpawnPoint = spawnPt.transform;

        // Save as prefab
        bool success;
        PrefabUtility.SaveAsPrefabAsset(root, path, out success);
        Object.DestroyImmediate(root);

        if (success)
            Debug.Log("[PrefabCreatorHelper] Archer.prefab created at " + path);
        else
            Debug.LogError("[PrefabCreatorHelper] Failed to create Archer.prefab");
    }

    // ---------------------------------------------------------------
    //  ARROW PREFAB
    // ---------------------------------------------------------------
    static void CreateArrowPrefab()
    {
        string path = ResourcesPath + "/Arrow.prefab";

        GameObject root = new GameObject("Arrow");

        // Sprite
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;

        // Physics — no gravity scale override; Arrow script adds impulse force
        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Trigger collider (thin capsule for arrow shaft)
        var col = root.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.5f, 0.1f);
        col.direction = CapsuleDirection2D.Horizontal;
        col.isTrigger = true;

        // Photon
        var pv = root.AddComponent<Photon.Pun.PhotonView>();
        // Arrow syncs via RPCs only — no transform view needed

        // Arrow script
        root.AddComponent<Arrow>();

        bool success;
        PrefabUtility.SaveAsPrefabAsset(root, path, out success);
        Object.DestroyImmediate(root);

        if (success)
            Debug.Log("[PrefabCreatorHelper] Arrow.prefab created at " + path);
        else
            Debug.LogError("[PrefabCreatorHelper] Failed to create Arrow.prefab");
    }

    // ---------------------------------------------------------------
    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string folder = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
