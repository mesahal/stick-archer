using UnityEngine;
using System.Collections;

/// <summary>
/// Manages different arena environments that change each round.
/// </summary>
public class EnvironmentManager : MonoBehaviour
{
    [Header("Arena Prefabs")]
    public GameObject[] arenaPrefabs;
    
    [Header("Spawn Points")]
    public Transform player1Spawn;
    public Transform player2Spawn;
    
    [Header("Transition")]
    public float transitionDuration = 0.5f;
    
    private GameObject currentArena;
    private int currentArenaIndex = -1;
    
    void Start()
    {
        // Load initial arena if none exists
        if (currentArena == null && arenaPrefabs.Length > 0)
        {
            LoadRandomEnvironment();
        }
    }
    
    /// <summary>
    /// Load a random environment different from current.
    /// </summary>
    public void LoadRandomEnvironment()
    {
        if (arenaPrefabs.Length == 0) return;
        
        int newIndex;
        do
        {
            newIndex = Random.Range(0, arenaPrefabs.Length);
        } while (newIndex == currentArenaIndex && arenaPrefabs.Length > 1);
        
        LoadEnvironment(newIndex);
    }
    
    /// <summary>
    /// Load specific environment by index.
    /// </summary>
    public void LoadEnvironment(int index)
    {
        if (index < 0 || index >= arenaPrefabs.Length) return;
        
        StartCoroutine(DoEnvironmentTransition(index));
    }
    
    IEnumerator DoEnvironmentTransition(int newIndex)
    {
        // Fade out
        yield return FadeScreen(0f, 1f, transitionDuration * 0.5f);
        
        // Destroy old arena
        if (currentArena != null)
        {
            Destroy(currentArena);
        }
        
        // Spawn new arena
        currentArena = Instantiate(arenaPrefabs[newIndex], Vector3.zero, Quaternion.identity);
        currentArenaIndex = newIndex;
        
        // Update spawn points
        FindSpawnPoints();
        
        // Fade in
        yield return FadeScreen(1f, 0f, transitionDuration * 0.5f);
    }
    
    void FindSpawnPoints()
    {
        // Look for spawn points in new arena
        var p1 = GameObject.Find("Player1Spawn");
        var p2 = GameObject.Find("Player2Spawn");
        
        if (p1 != null) player1Spawn = p1.transform;
        if (p2 != null) player2Spawn = p2.transform;
    }
    
    IEnumerator FadeScreen(float fromAlpha, float toAlpha, float duration)
    {
        // Create fade overlay if needed
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null) yield break;
        
        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(canvas.transform, false);
        
        var rt = fadeObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        
        var image = fadeObj.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0, 0, 0, fromAlpha);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Color c = image.color;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            image.color = c;
            yield return null;
        }
        
        Destroy(fadeObj);
    }
    
    /// <summary>
    /// Get the current arena name.
    /// </summary>
    public string GetCurrentArenaName()
    {
        if (currentArenaIndex >= 0 && currentArenaIndex < arenaPrefabs.Length)
        {
            return arenaPrefabs[currentArenaIndex].name;
        }
        return "Unknown";
    }
}
