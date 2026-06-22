using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Round transition UI - shows "ROUND X" between rounds.
/// Self-builds the full layout from designs/07_round_transition.svg:
/// dim overlay, gold "ROUND" label, big gold round number with dark outline,
/// "SCORE a — b" subtitle, "FIRST TO N WINS" line, and decorative side arrows.
/// Round colour states: rounds 1-2 green, 3-4 gold, match-point/final red.
/// </summary>
public class RoundTransition : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI roundText;     // big number (wired by GameUISetup)
    public TextMeshProUGUI arenaNameText;
    public CanvasGroup canvasGroup;

    [Header("Animation")]
    public float fadeInDuration = 0.3f;
    public float displayDuration = 1.4f;
    public float fadeOutDuration = 0.3f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Swipe Wipe")]
    [Tooltip("If enabled, a diagonal gold band sweeps across the screen as the round opens.")]
    public bool enableSwipeWipe = true;
    public float wipeDuration = 1.2f;

    // Built-on-demand extras
    Image _dimOverlay;
    TextMeshProUGUI _roundLabel;
    TextMeshProUGUI _scoreText;
    TextMeshProUGUI _subtitleText;
    TextMeshProUGUI _arrowLeft, _arrowRight;
    bool _built;

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        BuildExtras();
    }

    void BuildExtras()
    {
        if (_built) return;
        _built = true;

        Canvas parentCanvas = GetComponentInParent<Canvas>();

        // ── Dim overlay (full-screen, behind the transition content) ──
        if (parentCanvas != null && _dimOverlay == null)
        {
            var dimGO = new GameObject("RoundTransition_Dim", typeof(RectTransform));
            dimGO.transform.SetParent(parentCanvas.transform, false);
            dimGO.transform.SetAsFirstSibling();
            var drt = dimGO.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            _dimOverlay = dimGO.AddComponent<Image>();
            _dimOverlay.color = new Color(0f, 0f, 0f, 0f);
            _dimOverlay.raycastTarget = false;
        }

        // Restyle the big number (hero)
        if (roundText != null)
        {
            UIFontProvider.Apply(roundText, UIFontProvider.ExtraBold);
            roundText.fontSize = 220f;
            roundText.alignment = TextAlignmentOptions.Center;
            roundText.enableWordWrapping = false;
            roundText.raycastTarget = false;
            // An SDF outline fragments at this size; the menu title uses a drop shadow
            // with no outline (proven crisp), so match that.
            roundText.outlineWidth = 0f;
            UIFontProvider.ApplyTitleDropShadow(roundText);
            var nrt = roundText.rectTransform;
            nrt.anchorMin = new Vector2(0.5f, 0.5f);
            nrt.anchorMax = new Vector2(0.5f, 0.5f);
            nrt.sizeDelta = new Vector2(600, 360);
            nrt.anchoredPosition = new Vector2(0, 10);
        }

        // "ROUND" label above the number
        _roundLabel = MakeText("RoundLabel", "ROUND", 40f, UIFontProvider.ExtraBold,
            Color.white, new Vector2(0, 165), new Vector2(600, 60));
        if (_roundLabel != null) _roundLabel.characterSpacing = 18f;

        // Score subtitle below the number
        _scoreText = MakeText("ScoreSubtitle", "SCORE  0 — 0", 34f, UIFontProvider.Bold,
            Color.white, new Vector2(0, -170), new Vector2(700, 50));
        if (_scoreText != null) _scoreText.characterSpacing = 6f;

        // "FIRST TO N WINS"
        _subtitleText = MakeText("FirstToWins", "FIRST TO 5 WINS", 22f, UIFontProvider.Medium,
            new Color(1f, 1f, 1f, 0.65f), new Vector2(0, -215), new Vector2(700, 40));
        if (_subtitleText != null) _subtitleText.characterSpacing = 6f;

        // Decorative side arrows (guillemets render reliably in Inter; point inward)
        _arrowLeft = MakeText("ArrowLeft", "»", 64f, UIFontProvider.Bold,
            UIDesignSystem.Gold, new Vector2(-235, 12), new Vector2(120, 90));
        _arrowRight = MakeText("ArrowRight", "«", 64f, UIFontProvider.Bold,
            UIDesignSystem.Gold, new Vector2(235, 12), new Vector2(120, 90));
        if (_arrowLeft != null) { var c = _arrowLeft.color; c.a = 0.8f; _arrowLeft.color = c; }
        if (_arrowRight != null) { var c = _arrowRight.color; c.a = 0.8f; _arrowRight.color = c; }
    }

    void TintArrow(TextMeshProUGUI arrow, Color color)
    {
        if (arrow == null) return;
        arrow.color = new Color(color.r, color.g, color.b, 0.8f);
    }

    TextMeshProUGUI MakeText(string name, string text, float size, TMP_FontAsset font,
        Color color, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        UIFontProvider.Apply(tmp, font);
        return tmp;
    }

    /// <summary>Backwards-compatible entry point.</summary>
    public void ShowRound(int roundNumber, string arenaName = "")
        => ShowRound(roundNumber, -1, -1, 5, arenaName);

    /// <summary>Show the round transition with score context.</summary>
    public void ShowRound(int roundNumber, int p1Score, int p2Score, int scoreToWin, string arenaName = "")
    {
        StopAllCoroutines();
        StartCoroutine(DoRoundTransition(roundNumber, p1Score, p2Score, scoreToWin, arenaName));
    }

    Color RoundColor(int round, int p1, int p2, int scoreToWin)
    {
        bool matchPoint = scoreToWin > 0 && (p1 >= scoreToWin - 1 || p2 >= scoreToWin - 1);
        if (matchPoint) return UIDesignSystem.Danger;   // red final / match point
        if (round >= 3)  return UIDesignSystem.Gold;     // gold mid-match
        return UIDesignSystem.Success;                   // green early rounds
    }

    IEnumerator DoRoundTransition(int round, int p1, int p2, int scoreToWin, string arena)
    {
        BuildExtras();
        if (enableSwipeWipe) StartCoroutine(DoSwipeWipe());

        Color stateColor = RoundColor(round, p1, p2, scoreToWin);

        if (roundText != null)
        {
            roundText.text = round.ToString();
            // Gold rounds get the gradient; coloured states use a tinted gradient.
            roundText.enableVertexGradient = true;
            if (stateColor == UIDesignSystem.Gold)
            {
                roundText.colorGradient = new VertexGradient(
                    UIDesignSystem.GoldTitle, UIDesignSystem.GoldTitle,
                    UIDesignSystem.GoldStroke, UIDesignSystem.GoldStroke);
                roundText.color = Color.white;
            }
            else
            {
                roundText.enableVertexGradient = false;
                roundText.color = stateColor;
            }
        }
        if (_roundLabel != null) _roundLabel.gameObject.SetActive(true);
        TintArrow(_arrowLeft, stateColor);
        TintArrow(_arrowRight, stateColor);
        if (_arrowLeft != null) _arrowLeft.gameObject.SetActive(true);
        if (_arrowRight != null) _arrowRight.gameObject.SetActive(true);

        if (_scoreText != null)
        {
            if (p1 >= 0 && p2 >= 0)
            {
                _scoreText.gameObject.SetActive(true);
                _scoreText.text = $"SCORE  {p1} — {p2}";
            }
            else _scoreText.gameObject.SetActive(false);
        }
        if (_subtitleText != null)
        {
            _subtitleText.gameObject.SetActive(scoreToWin > 0);
            _subtitleText.text = $"FIRST TO {scoreToWin} WINS";
        }

        if (arenaNameText != null)
        {
            arenaNameText.text = string.IsNullOrEmpty(arena) ? "" : arena.ToUpper();
            arenaNameText.gameObject.SetActive(!string.IsNullOrEmpty(arena));
        }

        Transform textTransform = roundText != null ? roundText.transform : null;
        if (textTransform != null) textTransform.localScale = Vector3.zero;

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            canvasGroup.alpha = t;
            if (_dimOverlay != null) _dimOverlay.color = new Color(0, 0, 0, 0.45f * t);
            if (textTransform != null)
            {
                float scale = scaleCurve.Evaluate(t);
                textTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.15f, scale);
            }
            yield return null;
        }
        if (_dimOverlay != null) _dimOverlay.color = new Color(0, 0, 0, 0.45f);

        // Settle to 1.0
        if (textTransform != null) textTransform.localScale = Vector3.one;

        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            canvasGroup.alpha = 1f - t;
            if (_dimOverlay != null) _dimOverlay.color = new Color(0, 0, 0, 0.45f * (1f - t));
            if (textTransform != null) textTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        if (_dimOverlay != null) _dimOverlay.color = new Color(0, 0, 0, 0f);
    }

    /// <summary>
    /// Diagonal gold swipe wipe: a tilted full-screen band slides across the screen.
    /// </summary>
    IEnumerator DoSwipeWipe()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) yield break;

        var wipeName = "RoundTransition_Wipe";
        var existing = parentCanvas.transform.Find(wipeName);
        GameObject wipeGO = existing != null ? existing.gameObject : null;
        Image wipeImg;

        if (wipeGO == null)
        {
            wipeGO = new GameObject(wipeName);
            wipeGO.transform.SetParent(parentCanvas.transform, false);
            var rt = wipeGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.32f);
            rt.anchorMax = new Vector2(1f, 0.68f);
            rt.offsetMin = new Vector2(-400f, 0f);
            rt.offsetMax = new Vector2(400f, 0f);
            rt.localRotation = Quaternion.Euler(0f, 0f, 8f);
            wipeImg = wipeGO.AddComponent<Image>();
            wipeImg.raycastTarget = false;
            // Soft gold band: a vertical gold gradient sprite reads as a glowing sweep.
            wipeImg.sprite = UIArtProvider.TitleGold;
            wipeImg.type = Image.Type.Simple;
        }
        else
        {
            wipeImg = wipeGO.GetComponent<Image>();
        }

        // Gold band; if the gradient sprite is missing, fall back to a flat gold colour.
        wipeImg.color = wipeImg.sprite != null ? new Color(1f, 1f, 1f, 0.9f) : UIDesignSystem.Gold;
        wipeGO.transform.SetAsLastSibling();

        var wipeRT = wipeGO.GetComponent<RectTransform>();
        Vector2 start = new Vector2(-Screen.width * 1.6f, 0f);
        Vector2 mid = Vector2.zero;
        Vector2 end = new Vector2(Screen.width * 1.6f, 0f);

        wipeRT.anchoredPosition = start;
        wipeGO.SetActive(true);

        float t = 0f;
        float half = wipeDuration * 0.5f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            wipeRT.anchoredPosition = Vector2.LerpUnclamped(start, mid, 1f - Mathf.Pow(1f - k, 3f));
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / half);
            wipeRT.anchoredPosition = Vector2.LerpUnclamped(mid, end, k * k);
            yield return null;
        }

        wipeGO.SetActive(false);
    }

    /// <summary>Quick flash text (for events like "HEADSHOT!").</summary>
    public void FlashText(string message, Color? color = null)
    {
        StopAllCoroutines();
        StartCoroutine(DoFlash(message, color ?? Color.red));
    }

    IEnumerator DoFlash(string message, Color color)
    {
        // Hide round-only decoration during a generic flash.
        if (_roundLabel != null) _roundLabel.gameObject.SetActive(false);
        if (_scoreText != null) _scoreText.gameObject.SetActive(false);
        if (_subtitleText != null) _subtitleText.gameObject.SetActive(false);
        if (_arrowLeft != null) _arrowLeft.gameObject.SetActive(false);
        if (_arrowRight != null) _arrowRight.gameObject.SetActive(false);

        if (roundText != null)
        {
            roundText.enableVertexGradient = false;
            roundText.fontSize = 110f;
            roundText.text = message;
            roundText.color = color;
            roundText.transform.localScale = Vector3.one * 1.4f;
        }

        canvasGroup.alpha = 1f;
        yield return new WaitForSeconds(0.5f);

        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / 0.3f);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // Restore hero size and decorations for the next round display.
        if (roundText != null) roundText.fontSize = 240f;
        if (_roundLabel != null) _roundLabel.gameObject.SetActive(true);
        if (_arrowLeft != null) _arrowLeft.gameObject.SetActive(true);
        if (_arrowRight != null) _arrowRight.gameObject.SetActive(true);
    }
}
