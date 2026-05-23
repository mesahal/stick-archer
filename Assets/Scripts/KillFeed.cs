using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Displays kill notifications at the top of screen.
/// Mobile-friendly: simple text, limited history, auto-fade.
/// </summary>
public class KillFeed : MonoBehaviour
{
    public static KillFeed Instance;
    
    [Header("UI References")]
    public RectTransform feedContainer;
    public GameObject killTextPrefab;
    
    [Header("Settings")]
    public int maxEntries = 3;
    public float entryLifetime = 2.5f;
    public float fadeTime = 0.5f;
    public float entrySpacing = 30f;
    public Vector2 startPosition = new Vector2(0, 180f);
    
    private Queue<GameObject> activeEntries = new Queue<GameObject>();
    private int entryCount = 0;
    
    void Awake()
    {
        Instance = this;
    }
    
    /// <summary>
    /// Show a kill in the feed.
    /// </summary>
    public void ShowKill(int killerPlayerIndex, int victimPlayerIndex)
    {
        string killer = killerPlayerIndex == 1 ? "Player 1" : "Player 2";
        string victim = victimPlayerIndex == 1 ? "Player 1" : "Player 2";
        string message = $"{killer} eliminated {victim}!";
        
        Color color = killerPlayerIndex == 1 
            ? new Color(0.2f, 0.5f, 1f) 
            : new Color(1f, 0.3f, 0.2f);
        
        AddEntry(message, color);
    }
    
    /// <summary>
    /// Show a custom message in the feed.
    /// </summary>
    public void ShowMessage(string message, Color color)
    {
        AddEntry(message, color);
    }
    
    void AddEntry(string message, Color color)
    {
        if (feedContainer == null) return;
        
        GameObject entry;
        TextMeshProUGUI tmp;
        
        if (killTextPrefab != null)
        {
            entry = Instantiate(killTextPrefab, feedContainer);
            tmp = entry.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            entry = new GameObject("KillFeedEntry");
            entry.transform.SetParent(feedContainer, false);
            tmp = entry.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
        }
        
        if (tmp != null)
        {
            tmp.text = message;
            tmp.color = color;
        }
        
        // Position from top
        RectTransform rt = entry.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = startPosition - Vector2.up * (entryCount * entrySpacing);
            rt.localScale = Vector3.one * 0.8f;
        }
        
        // Animate in
        StartCoroutine(AnimateEntry(rt));
        
        // Queue for cleanup
        activeEntries.Enqueue(entry);
        entryCount++;
        
        // Remove old entries
        if (activeEntries.Count > maxEntries)
        {
            GameObject old = activeEntries.Dequeue();
            if (old != null) Destroy(old);
            entryCount--;
        }
        
        // Auto-destroy after lifetime
        StartCoroutine(FadeAndDestroy(entry, entryLifetime));
    }
    
    IEnumerator AnimateEntry(RectTransform rt)
    {
        if (rt == null) yield break;
        
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one;
        Vector3 startScale = Vector3.one * 0.8f;
        
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;
            rt.localScale = Vector3.Lerp(startScale, targetScale, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        
        rt.localScale = targetScale;
    }
    
    IEnumerator FadeAndDestroy(GameObject entry, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (entry == null) yield break;
        
        TextMeshProUGUI tmp = entry.GetComponent<TextMeshProUGUI>();
        CanvasGroup cg = entry.GetComponent<CanvasGroup>();
        
        if (cg == null) cg = entry.AddComponent<CanvasGroup>();
        
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            if (cg != null)
                cg.alpha = 1f - (elapsed / fadeTime);
            yield return null;
        }
        
        if (activeEntries.Contains(entry))
        {
            // Rebuild queue without this entry
            Queue<GameObject> newQueue = new Queue<GameObject>();
            foreach (var e in activeEntries)
                if (e != entry && e != null) newQueue.Enqueue(e);
            activeEntries = newQueue;
            entryCount = activeEntries.Count;
        }
        
        Destroy(entry);
    }
}
