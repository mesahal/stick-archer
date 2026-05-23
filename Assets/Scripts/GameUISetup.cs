using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Automatically creates all game UI elements at runtime.
/// Attach to Canvas in GameArena scene.
/// </summary>
public class GameUISetup : MonoBehaviour
{
    [Header("Health Bars")]
    public bool createHealthBars = true;
    public Vector2 healthBarSize = new Vector2(150, 20);
    public Vector3 p1HealthBarPos = new Vector3(-200, 180, 0);
    public Vector3 p2HealthBarPos = new Vector3(200, 180, 0);
    
    [Header("Round Display")]
    public bool createRoundDisplay = true;
    
    [Header("Headshot Feedback")]
    public bool createHeadshotFeedback = true;
    
    [Header("Wind Indicator")]
    public bool createWindIndicator = true;
    public Vector3 windIndicatorPos = new Vector3(0, 220, 0);
    
    private Canvas canvas;
    
    void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[GameUISetup] No Canvas found!");
            return;
        }
    }
    
    void Start()
    {
        // Health bars and wind indicator are now created by UIManager in the reference style.
        // Only create round display and headshot feedback here.
        if (createRoundDisplay) SetupRoundDisplay();
        if (createHeadshotFeedback) SetupHeadshotFeedback();
        
        // Update UIManager references
        UpdateUIManagerReferences();
    }
    
    void SetupHealthBars()
    {
        // Check if already exists
        if (GameObject.Find("HealthBar_P1") != null) return;
        
        // P1 Health Bar (left)
        GameObject p1Bar = CreateHealthBar("HealthBar_P1", p1HealthBarPos, new Color(0.2f, 0.5f, 1f));
        
        // P2 Health Bar (right)
        GameObject p2Bar = CreateHealthBar("HealthBar_P2", p2HealthBarPos, new Color(1f, 0.3f, 0.2f));
        
        Debug.Log("[GameUISetup] Health bars created");
    }
    
    GameObject CreateHealthBar(string name, Vector3 pos, Color color)
    {
        GameObject barObj = new GameObject(name);
        barObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = barObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = healthBarSize;
        
        // Background
        Image bg = barObj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // Fill (child)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barObj.transform, false);
        RectTransform fillRt = fillObj.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        
        Image fill = fillObj.AddComponent<Image>();
        fill.color = color;
        
        // HealthBarUI component
        HealthBarUI healthBar = barObj.AddComponent<HealthBarUI>();
        healthBar.healthBarFill = fill;
        healthBar.healthBarBackground = bg;
        healthBar.SetHealth(1f); // Full health
        
        return barObj;
    }
    
    void SetupRoundDisplay()
    {
        // Check if exists
        if (GameObject.Find("RoundTransition") != null) return;
        
        GameObject roundObj = new GameObject("RoundTransition");
        roundObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = roundObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600, 300);
        
        CanvasGroup cg = roundObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        
        // Round text
        GameObject textObj = new GameObject("RoundText");
        textObj.transform.SetParent(roundObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(0, 50);
        textRt.offsetMax = new Vector2(0, 0);
        
        TextMeshProUGUI roundText = textObj.AddComponent<TextMeshProUGUI>();
        roundText.text = "ROUND 1";
        roundText.fontSize = 72;
        roundText.fontStyle = FontStyles.Bold;
        roundText.alignment = TextAlignmentOptions.Center;
        roundText.color = Color.white;
        
        // Arena name text (smaller, below)
        GameObject arenaObj = new GameObject("ArenaName");
        arenaObj.transform.SetParent(roundObj.transform, false);
        RectTransform arenaRt = arenaObj.AddComponent<RectTransform>();
        arenaRt.anchorMin = Vector2.zero;
        arenaRt.anchorMax = new Vector2(1f, 0.5f);
        arenaRt.offsetMin = Vector2.zero;
        arenaRt.offsetMax = Vector2.zero;
        
        TextMeshProUGUI arenaText = arenaObj.AddComponent<TextMeshProUGUI>();
        arenaText.text = "";
        arenaText.fontSize = 36;
        arenaText.alignment = TextAlignmentOptions.Center;
        arenaText.color = new Color(1f, 0.8f, 0.2f);
        
        // RoundTransition component
        RoundTransition transition = roundObj.AddComponent<RoundTransition>();
        transition.roundText = roundText;
        transition.arenaNameText = arenaText;
        transition.canvasGroup = cg;
        
        Debug.Log("[GameUISetup] Round transition UI created");
    }
    
    void SetupHeadshotFeedback()
    {
        if (GameObject.Find("HeadshotFeedback") != null) return;
        
        GameObject hsObj = new GameObject("HeadshotFeedback");
        hsObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = hsObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600, 200);
        
        CanvasGroup cg = hsObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        
        // Text
        TextMeshProUGUI hsText = hsObj.AddComponent<TextMeshProUGUI>();
        hsText.text = "HEADSHOT!";
        hsText.fontSize = 96;
        hsText.fontStyle = FontStyles.Bold;
        hsText.alignment = TextAlignmentOptions.Center;
        hsText.color = new Color(1f, 0.2f, 0.1f);
        
        // Add outline/glow effect via material
        hsText.fontMaterial.EnableKeyword("GLOW_ON");
        
        // Component
        HeadshotFeedback feedback = hsObj.AddComponent<HeadshotFeedback>();
        feedback.headshotText = hsText;
        feedback.canvasGroup = cg;
        
        Debug.Log("[GameUISetup] Headshot feedback UI created");
    }
    
    void SetupWindIndicator()
    {
        if (GameObject.Find("WindIndicator") != null) return;
        
        GameObject windObj = new GameObject("WindIndicator");
        windObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rt = windObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = windIndicatorPos;
        rt.sizeDelta = new Vector2(100, 40);
        
        // Wind arrow (triangle pointing direction)
        GameObject arrowObj = new GameObject("WindArrow");
        arrowObj.transform.SetParent(windObj.transform, false);
        RectTransform arrowRt = arrowObj.AddComponent<RectTransform>();
        arrowRt.anchorMin = new Vector2(0, 0.5f);
        arrowRt.anchorMax = new Vector2(0, 0.5f);
        arrowRt.pivot = new Vector2(0.5f, 0.5f);
        arrowRt.sizeDelta = new Vector2(30, 30);
        arrowRt.anchoredPosition = new Vector2(-30, 0);
        
        Image arrowImg = arrowObj.AddComponent<Image>();
        arrowImg.color = Color.white;
        arrowImg.sprite = CreateTriangleSprite();
        
        // Wind text
        GameObject textObj = new GameObject("WindText");
        textObj.transform.SetParent(windObj.transform, false);
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(1, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.pivot = new Vector2(1, 0.5f);
        textRt.sizeDelta = new Vector2(70, 30);
        textRt.anchoredPosition = new Vector2(0, 0);
        
        TextMeshProUGUI windText = textObj.AddComponent<TextMeshProUGUI>();
        windText.text = "0.0";
        windText.fontSize = 20;
        windText.alignment = TextAlignmentOptions.Right;
        windText.color = Color.white;
        
        // WindSystem reference
        WindSystem windSys = windObj.AddComponent<WindSystem>();
        windSys.windArrow = arrowRt;
        windSys.windText = windText;
        
        Debug.Log("[GameUISetup] Wind indicator created");
    }
    
    Sprite CreateTriangleSprite()
    {
        // Create a simple triangle texture
        Texture2D tex = new Texture2D(32, 32);
        Color clear = new Color(0, 0, 0, 0);
        Color white = Color.white;
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // Simple triangle shape
                int width = (int)((y / 31f) * 16f);
                if (x >= 16 - width && x <= 16 + width)
                    tex.SetPixel(x, y, white);
                else
                    tex.SetPixel(x, y, clear);
            }
        }
        
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    void UpdateUIManagerReferences()
    {
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager == null) return;
        
        // Find and assign references
        var p1Score = GameObject.Find("P1ScoreText")?.GetComponent<TextMeshProUGUI>();
        var p2Score = GameObject.Find("P2ScoreText")?.GetComponent<TextMeshProUGUI>();
        
        if (p1Score != null) uiManager.player1ScoreText = p1Score;
        if (p2Score != null) uiManager.player2ScoreText = p2Score;
    }
}
