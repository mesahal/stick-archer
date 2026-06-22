#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared helpers used by all UI screen builder scripts.
/// </summary>
public static class UIBuilderHelpers
{
    // ── Asset paths ─────────────────────────────────────────────────────────
    public const string BtnPrefabPath      = "Assets/Art/UI/Prefabs/Btn_Primary.prefab";
    public const string Rounded16Path      = "Assets/Art/UI/Shapes/rounded_16.png";
    public const string Rounded24Path      = "Assets/Art/UI/Shapes/rounded_24.png";
    public const string Rounded32Path      = "Assets/Art/UI/Shapes/rounded_32.png";
    public const string PillPath           = "Assets/Art/UI/Shapes/pill_128.png";
    public const string PillBarPath        = "Assets/Art/UI/Shapes/pill_bar.png";
    public const string Circle128Path      = "Assets/Art/UI/Shapes/circle_128.png";
    public const string GradPrimaryPath    = "Assets/Art/UI/Gradients/btn_primary.png";
    public const string GradSuccessPath    = "Assets/Art/UI/Gradients/btn_success.png";
    public const string GradDangerPath     = "Assets/Art/UI/Gradients/btn_danger.png";
    public const string GradGoldPath       = "Assets/Art/UI/Gradients/btn_gold.png";
    public const string GradWarningPath    = "Assets/Art/UI/Gradients/btn_warning.png";
    public const string GradPanelBgPath    = "Assets/Art/UI/Gradients/panel_bg.png";
    public const string GradPanelDarkPath  = "Assets/Art/UI/Gradients/panel_dark.png";

    // ── Design tokens ────────────────────────────────────────────────────────
    public static readonly Color BgDark       = Hex("#141A29");
    public static readonly Color BgPanel      = Hex("#1F2438");
    public static readonly Color BgPanelDeep  = Hex("#0F1421");
    public static readonly Color Primary      = Hex("#268CF2");
    public static readonly Color Success      = Hex("#33B859");
    public static readonly Color Danger       = Hex("#F23F3F");
    public static readonly Color Warning      = Hex("#F28C1A");
    public static readonly Color Gold         = Hex("#FFD933");
    public static readonly Color White        = Color.white;
    public static readonly Color TextMid      = new Color(1,1,1,0.75f);
    public static readonly Color TextDim      = new Color(1,1,1,0.50f);
    public static readonly Color TextHint     = new Color(1,1,1,0.40f);

    // ── Color helpers ────────────────────────────────────────────────────────
    public static Color Hex(string html, float alpha = 1f)
    {
        ColorUtility.TryParseHtmlString(html, out Color c);
        c.a = alpha;
        return c;
    }

    public static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    // ── Sprite loading ───────────────────────────────────────────────────────
    public static Sprite Spr(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogWarning($"UIBuilder: sprite not found at '{path}'");
        return s;
    }

    // ── GameObject factory ───────────────────────────────────────────────────
    public static GameObject CreateGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    // ── RectTransform presets ────────────────────────────────────────────────
    public static RectTransform RT(GameObject go) =>
        go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();

    public static RectTransform Stretch(GameObject go)
    {
        var rt = RT(go);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static RectTransform Center(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt = RT(go);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform TopStretch(GameObject go, float height, float padL = 0, float padR = 0)
    {
        var rt = RT(go);
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(padL, -height);
        rt.offsetMax = new Vector2(-padR, 0);
        return rt;
    }

    public static RectTransform BottomCenter(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt = RT(go);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform TopLeft(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt = RT(go);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform TopRight(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt = RT(go);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static RectTransform TopCenter(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt = RT(go);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    // ── Image factory ────────────────────────────────────────────────────────
    public static Image AddImage(GameObject go, Sprite sprite, Color color,
        Image.Type type = Image.Type.Simple, bool raycast = false)
    {
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.type = type;
        img.raycastTarget = raycast;
        return img;
    }

    public static Image CardImage(GameObject go, Sprite sprite = null, Color? color = null)
    {
        var spr = sprite ?? Spr(Rounded32Path);
        return AddImage(go, spr, color ?? BgPanel, Image.Type.Sliced, false);
    }

    // ── TMP factory ──────────────────────────────────────────────────────────
    public static TextMeshProUGUI AddTMP(GameObject go, string text, float size,
        FontStyles style, TextAlignmentOptions align, Color color,
        float charSpacing = 0, bool raycast = false)
    {
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = color;
        tmp.characterSpacing = charSpacing;
        tmp.raycastTarget = raycast;
        return tmp;
    }

    // ── Button prefab instantiation ──────────────────────────────────────────
    public static Button InstantiateBtn(Transform parent, string goName,
        Vector2 pos, Vector2 size,
        string label, string gradientPath, string iconPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BtnPrefabPath);
        if (prefab == null) { Debug.LogError("Btn_Primary prefab not found"); return null; }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = goName;
        go.transform.SetParent(parent, false);
        Center(go, pos, size);

        var fill  = go.transform.Find("Fill")?.GetComponent<Image>();
        var lbl   = go.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        var icon  = go.transform.Find("Icon")?.GetComponent<Image>();

        if (fill  != null) fill.sprite = Spr(gradientPath);
        if (lbl   != null) lbl.text   = label;
        if (icon != null)
        {
            bool hasIcon = iconPath != null;
            icon.gameObject.SetActive(hasIcon);
            icon.enabled = hasIcon;
            if (hasIcon)
            {
                icon.sprite = Spr(iconPath);
                icon.preserveAspect = true;
            }
        }

        return go.GetComponent<Button>();
    }

    // ── Outline (hollow) button ──────────────────────────────────────────────
    public static Button CreateOutlineBtn(Transform parent, string goName,
        Vector2 pos, Vector2 size,
        string label, Color strokeColor, string iconPath = null)
    {
        var go = CreateGO(goName, parent);
        Center(go, pos, size);

        // Border pill
        var borderImg = AddImage(go, Spr(PillPath), WithAlpha(strokeColor, 0f), Image.Type.Sliced, false);
        // Outer outline via outline-image workaround: use an Image with just stroke color + low alpha fill
        borderImg.color = WithAlpha(strokeColor, 0.25f);
        borderImg.type = Image.Type.Sliced;
        borderImg.raycastTarget = true;

        // Label
        var lblGO = CreateGO("Label", go.transform);
        Stretch(lblGO);
        var tmp = AddTMP(lblGO, label, 30f, FontStyles.Bold, TextAlignmentOptions.Center,
            strokeColor, 0, false);

        // Icon (optional)
        if (iconPath != null)
        {
            var iconGO = CreateGO("Icon", go.transform);
            Center(iconGO, new Vector2(-size.x * 0.3f, 0), new Vector2(40, 40));
            var img = AddImage(iconGO, Spr(iconPath), strokeColor, Image.Type.Simple, false);
            img.preserveAspect = true;
        }

        // Button component
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = borderImg;

        // ButtonAnimator
        go.AddComponent<ButtonAnimator>();

        return btn;
    }

    // ── Simple icon button (circle bg + icon) ────────────────────────────────
    public static Button CreateIconBtn(Transform parent, string goName,
        Vector2 pos, Vector2 size, string iconPath, Color iconColor)
    {
        var go = CreateGO(goName, parent);
        Center(go, pos, size);

        // Background circle
        var bg = AddImage(go, Spr(Circle128Path), WithAlpha(White, 0.08f), Image.Type.Simple, false);
        go.AddComponent<ButtonAnimator>();

        // Icon
        var iconGO = CreateGO("Icon", go.transform);
        Center(iconGO, Vector2.zero, size * 0.55f);
        var iconImg = AddImage(iconGO, Spr(iconPath), iconColor, Image.Type.Simple, false);
        iconImg.preserveAspect = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        return btn;
    }

    // ── Dim overlay ──────────────────────────────────────────────────────────
    public static Image CreateDim(Transform parent, float alpha = 0.7f)
    {
        var go = CreateGO("Dim", parent);
        Stretch(go);
        return AddImage(go, null, new Color(0, 0, 0, alpha), Image.Type.Simple, true);
    }

    // ── TopAccent bar ────────────────────────────────────────────────────────
    public static void CreateTopAccent(Transform parent, Color color, float height = 6f, float pad = 24f)
    {
        var go = CreateGO("TopAccent", parent);
        TopStretch(go, height, pad, pad);
        AddImage(go, null, color, Image.Type.Simple, false);
    }

    // ── Clear children ───────────────────────────────────────────────────────
    public static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    // ── Find or create child GO ──────────────────────────────────────────────
    public static GameObject FindOrCreate(string name, Transform parent)
    {
        var t = parent.Find(name);
        if (t != null) return t.gameObject;
        return CreateGO(name, parent);
    }

    // ── Health/stat bar ──────────────────────────────────────────────────────
    public static void CreateStatBar(Transform parent, string goName,
        Vector2 pos, Vector2 size, string label, Color fillColor, float fillPct)
    {
        var barGO = CreateGO(goName, parent);
        Center(barGO, pos, size);

        // Track background
        var track = CreateGO("Track", barGO.transform);
        Stretch(track);
        AddImage(track, Spr(PillBarPath), WithAlpha(White, 0.1f), Image.Type.Sliced, false);

        // Fill
        var fill = CreateGO("Fill", barGO.transform);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0, 0); fillRT.anchorMax = new Vector2(fillPct, 1);
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;
        AddImage(fill, Spr(PillBarPath), fillColor, Image.Type.Sliced, false);

        // Label
        var lblGO = CreateGO("Label", barGO.transform);
        Stretch(lblGO);
        AddTMP(lblGO, label, 18f, FontStyles.Bold, TextAlignmentOptions.Left, TextMid);
    }
}
#endif
