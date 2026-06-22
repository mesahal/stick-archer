using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StickArcher.Progression;

namespace StickArcher.UI
{
    /// <summary>
    /// Top-left profile header: level + XP bar + coins.
    /// Baked into MainMenu by VisualOverhaul_v12; falls back to self-build if missing.
    /// Layout matches designs/01_main_menu.svg (440×104 pill at 40,40).
    /// </summary>
    public class ProfileBadge : MonoBehaviour
    {
        TextMeshProUGUI _levelText;
        TextMeshProUGUI _coinsText;
        RectTransform _xpFill;
        const float BadgeWidth = 440f;
        const float BadgeHeight = 104f;
        const float LevelX = 28f;
        const float LevelY = 8f;
        const float BarX = 28f;
        const float BarY = 62f;
        const float XpBarWidth = 205f;
        const float CoinRightPad = 24f;

        float _xpBarWidth = XpBarWidth;

        static readonly Color PillBg = new Color(0.102f, 0.122f, 0.200f, 0.34f);

        void Start()
        {
            if (!TryWireExisting())
                Build();

            var pill = transform.Find("ProfilePill");
            if (pill != null)
                NormalizeLayout(pill);

            Refresh(ProfileManager.Instance?.Profile);
            if (ProfileManager.Instance != null)
                ProfileManager.Instance.OnProfileChanged += Refresh;
        }

        void OnDestroy()
        {
            if (ProfileManager.Instance != null)
                ProfileManager.Instance.OnProfileChanged -= Refresh;
        }

        void Refresh(PlayerProfile p)
        {
            if (p == null) return;
            if (_levelText != null) _levelText.text = $"LV {p.level}";
            if (_coinsText != null) _coinsText.text = $"{p.coins:N0}";
            if (_xpFill != null)
            {
                int needed = ProfileManager.Instance != null ? ProfileManager.Instance.XpForNextLevel() : 1;
                float frac = needed > 0 ? Mathf.Clamp01((float)p.xp / needed) : 0f;
                float w = _xpBarWidth * frac;
                _xpFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(w, 0f));
            }
        }

        bool TryWireExisting()
        {
            var pill = transform.Find("ProfilePill");
            if (pill == null) return false;

            _levelText = pill.Find("Level")?.GetComponent<TextMeshProUGUI>();
            _coinsText = pill.Find("CoinsBlock/CoinsColumn/Coins")?.GetComponent<TextMeshProUGUI>();
            if (_coinsText == null)
                _coinsText = pill.Find("Coins")?.GetComponent<TextMeshProUGUI>();
            _xpFill = pill.Find("XpBarBG/XpFill")?.GetComponent<RectTransform>();
            var barRT = pill.Find("XpBarBG")?.GetComponent<RectTransform>();
            if (barRT != null) _xpBarWidth = barRT.sizeDelta.x;
            return _levelText != null && _coinsText != null && _xpFill != null;
        }

        void NormalizeLayout(Transform pill)
        {
            RectTransform pillRT = pill.GetComponent<RectTransform>();
            if (pillRT != null)
            {
                pillRT.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);
            }

            if (_levelText != null)
            {
                RectTransform levelRT = _levelText.rectTransform;
                levelRT.anchorMin = levelRT.anchorMax = new Vector2(0f, 1f);
                levelRT.pivot = new Vector2(0f, 1f);
                levelRT.anchoredPosition = new Vector2(LevelX, -LevelY);
                levelRT.sizeDelta = new Vector2(170f, 44f);
                _levelText.alignment = TextAlignmentOptions.Left;
                _levelText.fontSize = 36f;
                _levelText.characterSpacing = 2f;
                _levelText.color = UIDesignSystem.Gold;
            }

            Transform bar = pill.Find("XpBarBG");
            RectTransform barRT = bar != null ? bar.GetComponent<RectTransform>() : null;
            if (barRT != null)
            {
                _xpBarWidth = XpBarWidth;
                barRT.anchorMin = barRT.anchorMax = new Vector2(0f, 1f);
                barRT.pivot = new Vector2(0f, 0.5f);
                barRT.anchoredPosition = new Vector2(BarX, -BarY);
                barRT.sizeDelta = new Vector2(_xpBarWidth, 12f);
            }

            if (_xpFill != null)
            {
                _xpFill.anchorMin = new Vector2(0f, 0f);
                _xpFill.anchorMax = new Vector2(0f, 1f);
                _xpFill.pivot = new Vector2(0f, 0.5f);
                _xpFill.anchoredPosition = Vector2.zero;
                _xpFill.sizeDelta = new Vector2(_xpFill.sizeDelta.x, 0f);
            }

            Transform coinBlock = pill.Find("CoinsBlock");
            RectTransform blockRT = coinBlock != null ? coinBlock.GetComponent<RectTransform>() : null;
            if (blockRT != null)
            {
                blockRT.anchorMin = blockRT.anchorMax = new Vector2(1f, 0.5f);
                blockRT.pivot = new Vector2(1f, 0.5f);
                blockRT.anchoredPosition = new Vector2(-CoinRightPad, -1f);
                blockRT.sizeDelta = new Vector2(150f, 54f);
            }

            var row = coinBlock != null ? coinBlock.GetComponent<HorizontalLayoutGroup>() : null;
            if (row != null)
            {
                row.childAlignment = TextAnchor.MiddleRight;
                row.spacing = 12f;
                row.childControlWidth = false;
                row.childControlHeight = false;
                row.childForceExpandWidth = false;
                row.childForceExpandHeight = false;
            }

            Transform coinIcon = coinBlock != null ? coinBlock.Find("CoinIcon") : pill.Find("CoinIcon");
            RectTransform coinRT = coinIcon != null ? coinIcon.GetComponent<RectTransform>() : null;
            if (coinRT != null)
            {
                coinRT.sizeDelta = new Vector2(22f, 22f);
                var coinLE = coinIcon.GetComponent<LayoutElement>() ?? coinIcon.gameObject.AddComponent<LayoutElement>();
                coinLE.preferredWidth = 22f;
                coinLE.preferredHeight = 22f;
            }

            Transform col = pill.Find("CoinsBlock/CoinsColumn");
            RectTransform colRT = col != null ? col.GetComponent<RectTransform>() : null;
            if (colRT != null)
            {
                colRT.sizeDelta = new Vector2(110f, 54f);
                var colLE = col.GetComponent<LayoutElement>() ?? col.gameObject.AddComponent<LayoutElement>();
                colLE.preferredWidth = 110f;
                colLE.preferredHeight = 54f;
            }

            var vcol = col != null ? col.GetComponent<VerticalLayoutGroup>() : null;
            if (vcol != null)
            {
                vcol.childAlignment = TextAnchor.UpperRight;
                vcol.spacing = -2f;
                vcol.childControlWidth = false;
                vcol.childControlHeight = false;
                vcol.childForceExpandWidth = false;
                vcol.childForceExpandHeight = false;
            }

            if (_coinsText != null)
            {
                RectTransform coinsRT = _coinsText.rectTransform;
                coinsRT.sizeDelta = new Vector2(110f, 32f);
                _coinsText.alignment = TextAlignmentOptions.Right;
                _coinsText.fontSize = 30f;
                _coinsText.color = Color.white;
                // Keep large coin counts (e.g. "12,500") on a single line.
                _coinsText.enableWordWrapping = false;
                _coinsText.overflowMode = TextOverflowModes.Overflow;
            }

            Transform coinsLabelTransform = col != null ? col.Find("CoinsLabel") : pill.Find("CoinsLabel");
            TextMeshProUGUI coinsLabel = coinsLabelTransform != null
                ? coinsLabelTransform.GetComponent<TextMeshProUGUI>()
                : null;
            if (coinsLabel != null)
            {
                RectTransform labelRT = coinsLabel.rectTransform;
                labelRT.sizeDelta = new Vector2(110f, 22f);
                coinsLabel.enableWordWrapping = false;
                coinsLabel.overflowMode = TextOverflowModes.Overflow;
                coinsLabel.alignment = TextAlignmentOptions.Right;
                coinsLabel.fontSize = 15f;
                coinsLabel.characterSpacing = 4f;
                coinsLabel.color = new Color(1f, 1f, 1f, 0.55f);
            }
        }

        void Build()
        {
            var rootRT = transform as RectTransform;
            if (rootRT != null)
            {
                rootRT.anchorMin = rootRT.anchorMax = new Vector2(0f, 1f);
                rootRT.pivot = new Vector2(0f, 1f);
                rootRT.anchoredPosition = Vector2.zero;
                rootRT.sizeDelta = Vector2.zero;
            }

            var pill = AddImage(transform, "ProfilePill", PillBg);
            ApplySprite(pill, UIArtProvider.Rounded32, Image.Type.Sliced);
            SetFromTopLeft(pill.rectTransform, 40f, 40f, new Vector2(BadgeWidth, BadgeHeight));

            var border = AddImage(pill.transform, "Border", new Color(1f, 1f, 1f, 0.08f));
            Stretch(border.rectTransform);

            const float barWidth = XpBarWidth;
            _xpBarWidth = barWidth;

            _levelText = AddTextFromTopLeft(pill.transform, "Level", LevelX, LevelY, new Vector2(170f, 44f), new Vector2(0f, 1f));
            _levelText.alignment = TextAlignmentOptions.Left;
            _levelText.fontSize = 36f;
            UIFontProvider.Apply(_levelText, UIFontProvider.ExtraBold);
            _levelText.characterSpacing = 2f;
            _levelText.color = UIDesignSystem.Gold;

            var barBg = AddImage(pill.transform, "XpBarBG", new Color(1f, 1f, 1f, 0.14f));
            ApplySprite(barBg, UIArtProvider.PillBar, Image.Type.Sliced);
            barBg.pixelsPerUnitMultiplier = 1f;
            var barRT = barBg.rectTransform;
            barRT.anchorMin = barRT.anchorMax = new Vector2(0f, 1f);
            barRT.pivot = new Vector2(0f, 0.5f);
            barRT.anchoredPosition = new Vector2(BarX, -BarY);
            barRT.sizeDelta = new Vector2(barWidth, 12f);

            var fill = AddImage(barBg.transform, "XpFill", UIDesignSystem.Gold);
            ApplySprite(fill, UIArtProvider.PillBar, Image.Type.Sliced);
            fill.pixelsPerUnitMultiplier = 1f;
            _xpFill = fill.rectTransform;
            _xpFill.anchorMin = Vector2.zero;
            _xpFill.anchorMax = new Vector2(0f, 1f);
            _xpFill.pivot = new Vector2(0f, 0.5f);
            _xpFill.anchoredPosition = Vector2.zero;
            _xpFill.sizeDelta = new Vector2(0f, 0f);

            var coinBlock = new GameObject("CoinsBlock");
            coinBlock.transform.SetParent(pill.transform, false);
            var blockRT = coinBlock.AddComponent<RectTransform>();
            blockRT.anchorMin = blockRT.anchorMax = new Vector2(1f, 0.5f);
            blockRT.pivot = new Vector2(1f, 0.5f);
            blockRT.anchoredPosition = new Vector2(-CoinRightPad, -1f);
            blockRT.sizeDelta = new Vector2(150f, 54f);

            var row = coinBlock.AddComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleRight;
            row.spacing = 12f;
            row.childControlWidth = false;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var coinIcon = AddImage(coinBlock.transform, "CoinIcon", UIDesignSystem.Gold);
            ApplySprite(coinIcon, UIArtProvider.Circle128, Image.Type.Simple);
            var coinRT = coinIcon.rectTransform;
            coinRT.sizeDelta = new Vector2(22f, 22f);
            var coinLE = coinIcon.gameObject.AddComponent<LayoutElement>();
            coinLE.preferredWidth = 22f;
            coinLE.preferredHeight = 22f;

            var colGo = new GameObject("CoinsColumn");
            colGo.transform.SetParent(coinBlock.transform, false);
            var colRT = colGo.AddComponent<RectTransform>();
            colRT.sizeDelta = new Vector2(78f, 54f);
            var colLE = colGo.AddComponent<LayoutElement>();
            colLE.preferredWidth = 78f;
            colLE.preferredHeight = 54f;
            var vcol = colGo.AddComponent<VerticalLayoutGroup>();
            vcol.childAlignment = TextAnchor.UpperRight;
            vcol.spacing = -2f;
            vcol.childControlWidth = false;
            vcol.childControlHeight = false;
            vcol.childForceExpandWidth = false;
            vcol.childForceExpandHeight = false;

            _coinsText = AddLayoutText(colGo.transform, "Coins", 78f, 32f);
            _coinsText.alignment = TextAlignmentOptions.Right;
            _coinsText.fontSize = 30f;
            UIFontProvider.Apply(_coinsText, UIFontProvider.ExtraBold);
            _coinsText.color = Color.white;

            var coinsLabel = AddLayoutText(colGo.transform, "CoinsLabel", 78f, 22f);
            coinsLabel.alignment = TextAlignmentOptions.Right;
            coinsLabel.text = "COINS";
            coinsLabel.fontSize = 15f;
            UIFontProvider.Apply(coinsLabel, UIFontProvider.Bold);
            coinsLabel.characterSpacing = 4f;
            coinsLabel.color = new Color(1f, 1f, 1f, 0.55f);
        }

        static void ApplySprite(Image img, Sprite sprite, Image.Type type)
        {
            if (sprite == null) return;
            img.sprite = sprite;
            img.type = type;
        }

        static Image AddImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static TextMeshProUGUI AddLayoutText(Transform parent, string name, float width, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.raycastTarget = false;
            return t;
        }

        static TextMeshProUGUI AddTextFromTopLeft(Transform parent, string name,
            float xFromLeft, float yFromTop, Vector2 size, Vector2 pivot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(xFromLeft, -yFromTop);
            rt.sizeDelta = size;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.raycastTarget = false;
            return t;
        }

        static void SetFromTopLeft(RectTransform rt, float xFromLeft, float yFromTop, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(xFromLeft, -yFromTop);
            rt.sizeDelta = size;
        }

        static void SetFromTopLeftCentered(RectTransform rt, float xCenter, float yCenter, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xCenter, -yCenter);
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
