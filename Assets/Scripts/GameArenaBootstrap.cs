using UnityEngine;
using Photon.Pun;

/// <summary>
/// Placed once in the GameArena scene.
/// On start, nukes ALL pre-placed scene objects except essentials,
/// generates arena + background IMMEDIATELY (not deferred to next frame),
/// THEN spawns archers on the generated platforms.
/// </summary>
public class GameArenaBootstrap : MonoBehaviourPunCallbacks
{
    [Header("Practice Mode Prefabs")]
    public GameObject archerLocalPrefab;
    public GameObject arrowLocalPrefab;

    void Start()
    {
        // NUCLEAR CLEAN: destroy every non-essential scene object
        NukePrePlacedObjects();

        // Set camera
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.42f, 0.55f, 0.45f);
            Camera.main.orthographicSize = 5.5f;
        }

        // Auto-load prefabs from Resources if not assigned in inspector
        if (archerLocalPrefab == null)
            archerLocalPrefab = Resources.Load<GameObject>("ArcherLocal");
        if (arrowLocalPrefab == null)
            arrowLocalPrefab = Resources.Load<GameObject>("ArrowLocal");

        if (archerLocalPrefab == null)
            Debug.LogError("[Bootstrap] ArcherLocal prefab not found in Resources!");
        if (arrowLocalPrefab == null)
            Debug.LogError("[Bootstrap] ArrowLocal prefab not found in Resources!");

        // Generate arena IMMEDIATELY (not deferred to Start)
        GenerateArenaImmediate();

        // Setup visual effects
        SetupVisualEffects();
        SetupOtherSystems();
        
        if (GameMode.IsPractice)
        {
            SpawnPracticeArchers();
            SetupPracticeManager();
            UIManager.Instance?.ShowGameHUD();
            UIManager.Instance?.UpdateScore(0, 0);
        }
        else if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
        {
            NetworkManager.Instance?.SpawnLocalPlayer();
            UIManager.Instance?.ShowGameHUD();
        }
        else
        {
            Debug.LogWarning("[Bootstrap] Editor test mode – skipping network spawn.");
            UIManager.Instance?.ShowGameHUD();
        }

        // Ensure TouchControls exists for mobile input
        if (FindObjectOfType<TouchControls>() == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var tc = new GameObject("TouchControls");
                tc.transform.SetParent(canvas.transform, false);
                tc.AddComponent<TouchControls>();
            }
            else
            {
                var tc = new GameObject("TouchControls");
                tc.AddComponent<TouchControls>();
            }
            Debug.Log("[Bootstrap] Created TouchControls component.");
        }
    }

    /// <summary>
    /// Generate arena + background RIGHT NOW (not deferred to component Start).
    /// This ensures spawn points exist before SpawnPracticeArchers() runs.
    /// </summary>
    void GenerateArenaImmediate()
    {
        // Create ArenaGenerator and call GenerateArena() directly
        var arenaGen = new GameObject("ArenaGenerator");
        var gen = arenaGen.AddComponent<ArenaGenerator>();
        gen.generateOnStart = false; // we call it ourselves
        int arenaType = Random.Range(0, 6);
        gen.GenerateArena(arenaType); // ← IMMEDIATE, not deferred

        // Create background
        if (FindObjectOfType<ArenaBackground>() == null)
        {
            var bgObj = new GameObject("ArenaBackground");
            bgObj.AddComponent<ArenaBackground>();
        }
    }

    void NukePrePlacedObjects()
    {
        string[] keepNames = { "Main Camera", "Canvas", "EventSystem", "UIManager" };
        GameObject self = this.gameObject;

        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager
                     .GetActiveScene().GetRootGameObjects())
        {
            if (root == self) continue;

            bool keep = false;
            foreach (string name in keepNames)
                if (root.name == name) { keep = true; break; }
            if (keep) continue;

            if (root.GetComponent<Camera>() != null) continue;
            if (root.GetComponent<Canvas>() != null)
            {
                CleanCanvasChildren(root);
                continue;
            }
            if (root.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) continue;

            Destroy(root);
        }
    }

    void CleanCanvasChildren(GameObject canvas)
    {
        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            var child = canvas.transform.GetChild(i);
            string n = child.name;

            if (n == "GameHUDPanel" || n == "ResultPanel" || n == "OpponentLeftPanel")
            {
                for (int j = child.childCount - 1; j >= 0; j--)
                    Destroy(child.GetChild(j).gameObject);
                continue;
            }

            if (child.GetComponent<TouchControls>() != null) continue;
            Destroy(child.gameObject);
        }
    }

    void SetupPracticeManager()
    {
        if (FindObjectOfType<PracticeGameManager>() == null)
        {
            var go = new GameObject("PracticeGameManager");
            go.AddComponent<PracticeGameManager>();
        }
    }
    
    void SetupVisualEffects()
    {
        if (FindObjectOfType<VisualEffectsManager>() == null)
        {
            var vfxGo = new GameObject("VisualEffectsManager");
            var vfx = vfxGo.AddComponent<VisualEffectsManager>();
            vfx.enableCameraShake = true;
            vfx.enableTouchFeedback = true;
            vfx.enableKillFeed = true;
            vfx.enableAmbientEffects = false;
            vfx.enableParallax = false;
        }
    }
    
    void SetupOtherSystems()
    {
        if (FindObjectOfType<SetupWizard>() == null)
        {
            var wizard = new GameObject("SetupWizard");
            wizard.AddComponent<SetupWizard>();
        }
        
        var canvas = FindObjectOfType<Canvas>();
        if (canvas != null && canvas.GetComponent<GameUISetup>() == null)
            canvas.gameObject.AddComponent<GameUISetup>();
        
        if (FindObjectOfType<WindSystem>() == null)
        {
            var windObj = new GameObject("WindSystem");
            var wind = windObj.AddComponent<WindSystem>();
            wind.randomizeEachRound = true;
        }
    }

    void SpawnPracticeArchers()
    {
        if (archerLocalPrefab == null)
        {
            Debug.LogError("[Bootstrap] archerLocalPrefab not assigned!");
            return;
        }

        // Spawn points were ALREADY created by GenerateArenaImmediate()
        var p1GO = GameObject.Find("Player1Spawn");
        var p2GO = GameObject.Find("Player2Spawn");

        if (p1GO == null || p2GO == null)
            Debug.LogWarning("[Bootstrap] Spawn points not found! Using fallback positions.");

        Vector3 p1Pos = p1GO != null ? p1GO.transform.position : new Vector3(-5f, 0f, 0);
        Vector3 p2Pos = p2GO != null ? p2GO.transform.position : new Vector3( 5f, 0f, 0);

        Debug.Log($"[Bootstrap] Spawning P1 at {p1Pos}, P2 at {p2Pos}");

        // --- Player 1 (human) ---
        var p1Obj = Instantiate(archerLocalPrefab, p1Pos, Quaternion.identity);
        var p1Archer = p1Obj.GetComponent<ArcherLocal>();
        p1Archer.playerIndex        = 1;
        p1Archer.spawnPosition      = p1Pos;
        p1Archer.isPlayerControlled = true;
        p1Archer.arrowLocalPrefab   = arrowLocalPrefab; // always assign (even if null)

        // --- Player 2 (AI) ---
        var p2Obj = Instantiate(archerLocalPrefab, p2Pos, Quaternion.identity);
        var p2Archer = p2Obj.GetComponent<ArcherLocal>();
        p2Archer.playerIndex        = 2;
        p2Archer.spawnPosition      = p2Pos;
        p2Archer.isPlayerControlled = false;
        p2Archer.arrowLocalPrefab   = arrowLocalPrefab;

        // AI controller
        var ai = p2Obj.AddComponent<AIController>();
        ai.difficulty = (AIController.Difficulty)(int)GameMode.Difficulty;
        
        // Hit zones
        p1Obj.AddComponent<ArcherAutoSetup>().autoSetupOnStart = true;
        p2Obj.AddComponent<ArcherAutoSetup>().autoSetupOnStart = true;
    }
}
