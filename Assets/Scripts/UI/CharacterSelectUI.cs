using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Character selection screen.
/// Displays two character cards (Adventurer / Soldier) with stats.
/// Selecting a card highlights it with a gold border.
///
/// SETUP: Build the character select panel in the Unity Editor:
///   1. Create a panel under the MainMenu Canvas
///   2. Add two card areas with character art, name, subtitle, and stat bars
///   3. Add a Confirm button and a Back button
///   4. Drag-assign the references in the Inspector below
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    public static CharacterSelectUI Instance;

    [Header("Panels")]
    [Tooltip("Root panel for the character select screen (toggled on/off)")]
    public GameObject characterSelectPanel;

    [Header("Character Cards")]
    [Tooltip("Clickable card for Adventurer character")]
    public Button adventurerCard;
    [Tooltip("Clickable card for Soldier character")]
    public Button soldierCard;

    [Header("Card Borders (Image components on the card backgrounds)")]
    [Tooltip("Image component that serves as Adventurer card's border")]
    public Image adventurerBorder;
    [Tooltip("Image component that serves as Soldier card's border")]
    public Image soldierBorder;

    [Header("Selection Badges")]
    [Tooltip("Gold check badge shown on the selected card")]
    public GameObject adventurerCheckBadge;
    [Tooltip("Gold check badge shown on the selected card")]
    public GameObject soldierCheckBadge;

    [Header("Card Content — faded when not selected")]
    [Tooltip("CanvasGroup on adventurer card content for opacity control")]
    public CanvasGroup adventurerContent;
    [Tooltip("CanvasGroup on soldier card content for opacity control")]
    public CanvasGroup soldierContent;

    [Header("Confirm & Navigate")]
    public Button confirmButton;
    public Button backButton;

    [Header("Audio")]
    [Tooltip("Play a sound when switching selection")]
    public bool playSwitchSound = true;

    // ── State ──────────────────────────────────────────
    private int selectedIndex = 0; // 0 = Adventurer, 1 = Soldier

    public static int SelectedCharacter
    {
        get => PlayerPrefs.GetInt("SelectedCharacter", 0);
        set { PlayerPrefs.SetInt("SelectedCharacter", value); PlayerPrefs.Save(); }
    }

    // ── Lifecycle ──────────────────────────────────────

    void Awake()
    {
        Instance = this;

        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);

        ApplyCurrentCharacterArt();
    }

    void Start()
    {
        selectedIndex = SelectedCharacter;

        if (adventurerCard != null)
            adventurerCard.onClick.AddListener(() => SelectCharacter(0));
        if (soldierCard != null)
            soldierCard.onClick.AddListener(() => SelectCharacter(1));
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);

        UpdateVisuals();
    }

    void ApplyCurrentCharacterArt()
    {
        SetCardArt(adventurerCard, "Characters/Player1/archer_idle");
        SetCardArt(soldierCard, "Characters/Player2/archer_idle");
        HideCardDetails(adventurerCard);
        HideCardDetails(soldierCard);
    }

    void SetCardArt(Button card, string resourcePath)
    {
        if (card == null) return;

        Transform art = card.transform.Find("Content/Art");
        if (art == null) return;

        Image image = art.GetComponent<Image>();
        if (image == null) return;

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null) return;

        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
    }

    void HideCardDetails(Button card)
    {
        if (card == null) return;

        SetCardChildActive(card, "Content/Tagline", false);
        SetCardChildActive(card, "Content/StatSpeed", false);
        SetCardChildActive(card, "Content/StatPower", false);
    }

    void SetCardChildActive(Button card, string path, bool active)
    {
        Transform child = card.transform.Find(path);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    // ── Selection ──────────────────────────────────────

    void SelectCharacter(int index)
    {
        if (index == selectedIndex) return;

        selectedIndex = index;
        UpdateVisuals();

        // Play a UI sound if available
        // (Add a PlayUIClick() method to AudioManager if you want click sounds)
    }

    void UpdateVisuals()
    {
        bool isAdventurer = selectedIndex == 0;

        // Border colors: gold when selected, faint white when not
        Color activeColor   = UIDesignSystem.Gold;
        Color inactiveColor = new Color(1f, 1f, 1f, 0.08f);

        if (adventurerBorder != null)
        {
            adventurerBorder.color = isAdventurer ? activeColor : inactiveColor;
            // Selected border is thicker — use outline or separate Image
        }
        if (soldierBorder != null)
        {
            soldierBorder.color = isAdventurer ? inactiveColor : activeColor;
        }

        // Check badges
        if (adventurerCheckBadge != null)
            adventurerCheckBadge.SetActive(isAdventurer);
        if (soldierCheckBadge != null)
            soldierCheckBadge.SetActive(!isAdventurer);

        // Opacity on content
        if (adventurerContent != null)
            adventurerContent.alpha = isAdventurer ? 1f : 0.85f;
        if (soldierContent != null)
            soldierContent.alpha = isAdventurer ? 0.85f : 1f;
    }

    // ── Actions ────────────────────────────────────────

    void OnConfirm()
    {
        SelectedCharacter = selectedIndex;
        PlayerPrefs.Save();
        Debug.Log($"[CharacterSelect] Confirmed: {(selectedIndex == 0 ? "Adventurer" : "Soldier")}");

        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);

        // Proceed to game — either go to lobby or start practice
        if (GameMode.IsPractice)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameArena");
        }
        else
        {
            // For online, connect via NetworkManager
            NetworkManager.Instance?.ConnectAndPlay();
        }
    }

    void OnBack()
    {
        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);
    }

    // ── Public API ─────────────────────────────────────

    public void Show()
    {
        selectedIndex = SelectedCharacter;
        ApplyCurrentCharacterArt();

        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(true);

        UpdateVisuals();
    }

    public void ShowForMode(GameMode.Mode mode)
    {
        GameMode.Current = mode;
        Show();
    }
}
