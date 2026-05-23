using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime setup wizard that checks for common issues and auto-fixes where possible.
/// Runs automatically when game starts.
/// </summary>
public class SetupWizard : MonoBehaviour
{
    [Header("Auto-Run")]
    public bool runOnStart = true;
    public bool showDebugInfo = true;
    
    [Header("Checks")]
    public bool checkTags = true;
    public bool checkLayers = true;
    public bool checkPrefabs = true;
    public bool checkUI = true;
    
    void Start()
    {
        if (runOnStart)
            RunSetupCheck();
    }
    
    public void RunSetupCheck()
    {
        Debug.Log("╔════════════════════════════════════════════════════════╗");
        Debug.Log("║     STICK ARCHERS BATTLE - SETUP WIZARD               ║");
        Debug.Log("╚════════════════════════════════════════════════════════╝");
        
        int issuesFound = 0;
        int autoFixed = 0;
        
        // Check 1: Canvas exists
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[SetupWizard] ❌ No Canvas found in scene! UI will not work.");
            Debug.Log("[SetupWizard]   → Create: GameObject → UI → Canvas");
            issuesFound++;
        }
        else if (showDebugInfo)
        {
            Debug.Log("[SetupWizard] ✓ Canvas found");
        }
        
        // Check 2: GameArenaBootstrap
        var bootstrap = FindObjectOfType<GameArenaBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogWarning("[SetupWizard] ⚠ No GameArenaBootstrap found. Auto-creating...");
            GameObject bootObj = new GameObject("GameArenaBootstrap");
            bootObj.AddComponent<GameArenaBootstrap>();
            autoFixed++;
        }
        else if (showDebugInfo)
        {
            Debug.Log("[SetupWizard] ✓ GameArenaBootstrap found");
            
            // Check prefab assignments
            if (bootstrap.archerLocalPrefab == null)
            {
                Debug.LogWarning("[SetupWizard] ⚠ GameArenaBootstrap.archerLocalPrefab not assigned!");
                Debug.Log("[SetupWizard]   → Run: Tools → Validate Prefabs");
                issuesFound++;
            }
            
            if (bootstrap.arrowLocalPrefab == null)
            {
                Debug.LogWarning("[SetupWizard] ⚠ GameArenaBootstrap.arrowLocalPrefab not assigned!");
                Debug.Log("[SetupWizard]   → Run: Tools → Validate Prefabs");
                issuesFound++;
            }
        }
        
        // Check 3: MainMenuController (if in menu scene)
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            var mainMenu = FindObjectOfType<MainMenuController>();
            if (mainMenu == null)
            {
                Debug.LogWarning("[SetupWizard] ⚠ No MainMenuController found in MainMenu scene!");
                issuesFound++;
            }
            else if (showDebugInfo)
            {
                Debug.Log("[SetupWizard] ✓ MainMenuController found");
            }
        }
        
        // Check 4: AudioManager
        if (FindObjectOfType<AudioManager>() == null)
        {
            Debug.LogWarning("[SetupWizard] ⚠ No AudioManager found. Auto-creating...");
            GameObject audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();
            autoFixed++;
        }
        else if (showDebugInfo)
        {
            Debug.Log("[SetupWizard] ✓ AudioManager found");
        }
        
        // Check 5: UIManager
        if (FindObjectOfType<UIManager>() == null)
        {
            Debug.LogWarning("[SetupWizard] ⚠ No UIManager found. Auto-creating...");
            GameObject uiObj = new GameObject("UIManager");
            uiObj.AddComponent<UIManager>();
            autoFixed++;
        }
        else if (showDebugInfo)
        {
            Debug.Log("[SetupWizard] ✓ UIManager found");
        }
        
        // Check 6: NetworkManager (for online mode)
        if (FindObjectOfType<NetworkManager>() == null && !GameMode.IsPractice)
        {
            Debug.LogWarning("[SetupWizard] ⚠ No NetworkManager found (needed for online mode)");
            issuesFound++;
        }
        
        // Check 7: Physics 2D settings
        if (Physics2D.gravity.y > -5f)
        {
            Debug.LogWarning("[SetupWizard] ⚠ Gravity is weak! Setting to -12...");
            Physics2D.gravity = new Vector2(0, -12f);
            autoFixed++;
        }
        
        // Summary
        Debug.Log("╔════════════════════════════════════════════════════════╗");
        if (issuesFound == 0 && autoFixed == 0)
        {
            Debug.Log("║  ✓ SETUP COMPLETE - All systems ready!              ║");
        }
        else
        {
            Debug.Log($"║  ⚠ SETUP CHECK COMPLETE                              ║");
            Debug.Log($"║     Issues found: {issuesFound}  |  Auto-fixed: {autoFixed}               ║");
            if (issuesFound > 0)
            {
                Debug.Log($"║                                                      ║");
                Debug.Log($"║  Run in Unity: Tools → Check Stick Archers Setup   ║");
            }
        }
        Debug.Log("╚════════════════════════════════════════════════════════╝");
    }
    
    void OnGUI()
    {
        // Draw setup button in editor
#if UNITY_EDITOR
        if (GUILayout.Button("Run Setup Check"))
        {
            RunSetupCheck();
        }
#endif
    }
}
