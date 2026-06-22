using UnityEngine;
using Photon.Pun;
using StickArcher.Analytics;

/// <summary>
/// Placed once in the GameArena scene.
/// On start, generates arena + background, then spawns archers on the platforms.
///
/// NOTE: No longer nukes pre-placed scene objects. UI is now built in the
/// Unity Editor and must NOT be destroyed at runtime.
/// </summary>
public class GameArenaBootstrap : MonoBehaviourPunCallbacks
{
    [Header("Practice Mode Prefabs")]
    public GameObject archerLocalPrefab;
    public GameObject arrowLocalPrefab;

    // Deterministic camera framing for the arena (centered on the origin). The arena
    // is built around (0,0); this guarantees the archers are always in view regardless
    // of any stale/authored camera transform in the scene.
    static readonly Vector3 ArenaCameraPosition = new Vector3(0f, 0f, -10f);
    const float ArenaCameraSize = 5.5f;

    /// <summary>
    /// Keep the game simulating even when the window/editor isn't focused. Without this,
    /// Unity pauses play mode the moment it loses OS focus — so while the player waits
    /// idle between rounds the match appears to "hang" until they click again. Runs once
    /// at startup, before any scene loads, so it covers every scene and builds too.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnableRunInBackground()
    {
        Application.runInBackground = true;
    }

    void Awake()
    {
        // Belt-and-braces: ensure it's on even if this scene is entered directly.
        Application.runInBackground = true;
        // Frame the camera before CameraShaker (or anything else) caches its position.
        FrameCamera();
    }

    void FrameCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.orthographicSize = ArenaCameraSize;
        cam.transform.position = ArenaCameraPosition;
    }

    void Start()
    {
#if UNITY_EDITOR
        // When starting GameArena directly from the editor (not via menu), force Practice mode.
        if (!Photon.Pun.PhotonNetwork.IsConnected)
            GameMode.Current = GameMode.Mode.Practice;
#endif
        // Re-assert camera framing (covers cameras spawned after Awake).
        FrameCamera();

        // Auto-load prefabs from Resources if not assigned in inspector
        if (archerLocalPrefab == null)
            archerLocalPrefab = Resources.Load<GameObject>("ArcherLocal");
        if (arrowLocalPrefab == null)
            arrowLocalPrefab = Resources.Load<GameObject>("ArrowLocal");

        if (archerLocalPrefab == null)
            Debug.LogError("[Bootstrap] ArcherLocal prefab not found in Resources!");
        if (arrowLocalPrefab == null)
            Debug.LogError("[Bootstrap] ArrowLocal prefab not found in Resources!");

        // Setup visual effects and other systems
        SetupVisualEffects();
        SetupOtherSystems();

        // Build arena geometry (platforms, ground, spawn points) and background
        GenerateArenaImmediate();

        // Funnel: a match begins when the arena scene is entered for a known mode.
        Analytics.MatchStarted(
            GameMode.Current.ToString().ToLower(),
            GameMode.Difficulty.ToString().ToLower(),
            CharacterSelectUI.SelectedCharacter);

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

    void GenerateArenaImmediate()
    {
        // Remove the old pre-placed brick buildings saved in the GameArena scene
        // (Building_Left / Building_Right). The design uses simple wooden platforms
        // built below, so these no longer belong.
        foreach (string n in new[] { "Building_Left", "Building_Right" })
        {
            var old = GameObject.Find(n);
            if (old != null) Destroy(old);
        }

        // Build background (sky gradient, mountains, clouds, ground color)
        if (FindObjectOfType<ArenaBackground>() == null)
        {
            var go = new GameObject("ArenaBackground");
            go.AddComponent<ArenaBackground>();
        }

        // Build platforms, ground tiles, and Player1Spawn/Player2Spawn points
        var arenaGen = FindObjectOfType<ArenaGenerator>();
        if (arenaGen == null)
        {
            var go = new GameObject("ArenaGenerator");
            arenaGen = go.AddComponent<ArenaGenerator>();
        }
        // generateOnStart is false by default; call directly so spawn points exist
        // before SpawnPracticeArchers() runs
        if (!GameMode.IsPractice && PhotonNetwork.InRoom)
        {
            int type = 0;
            int seed = 0;
            bool hasSeed = false;
            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            if (props != null)
            {
                if (props.ContainsKey("_at")) type = (int)props["_at"];
                if (props.ContainsKey("_as"))
                {
                    seed = (int)props["_as"];
                    hasSeed = true;
                }
            }

            if (!hasSeed)
            {
                seed = 0;
                Debug.LogWarning("[Bootstrap] Online arena seed missing; using deterministic fallback.");
            }

            arenaGen.GenerateArena(type, seed);
        }
        else
        {
            arenaGen.GenerateArena(0);
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
        int selectedCharacter = CharacterSelectUI.SelectedCharacter;
        int opponentCharacter = selectedCharacter == 0 ? 1 : 0;

        // --- Player 1 (human) ---
        var p1Obj = Instantiate(archerLocalPrefab, p1Pos, Quaternion.identity);
        p1Obj.transform.position    = SpawnAlignment.AlignFeetTo(p1Obj, p1Pos);
        var p1Archer = p1Obj.GetComponent<ArcherLocal>();
        p1Archer.playerIndex        = 1;
        p1Archer.selectedCharacterIndex = selectedCharacter;
        p1Archer.spawnPosition      = p1Pos;
        p1Archer.isPlayerControlled = true;
        p1Archer.arrowLocalPrefab   = arrowLocalPrefab; // always assign (even if null)

        // --- Player 2 (AI) ---
        var p2Obj = Instantiate(archerLocalPrefab, p2Pos, Quaternion.identity);
        p2Obj.transform.position    = SpawnAlignment.AlignFeetTo(p2Obj, p2Pos);
        var p2Archer = p2Obj.GetComponent<ArcherLocal>();
        p2Archer.playerIndex        = 2;
        p2Archer.selectedCharacterIndex = opponentCharacter;
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
