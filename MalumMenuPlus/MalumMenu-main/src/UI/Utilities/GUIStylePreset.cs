using UnityEngine;

namespace MalumMenu;

public static class GUIStylePreset
{
    private static GUIStyle _separator;
    private static GUIStyle _darkSeparator;
    private static GUIStyle _normalButton;
    private static GUIStyle _normalToggle;
    private static GUIStyle _tabButton;
    private static GUIStyle _tabTitle;
    private static GUIStyle _tabSubtitle;
    private static GUIStyle _hint;

    public static GUIStyle Separator
    {
        get
        {
            if (_separator == null)
            {
                _separator = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = Texture2D.whiteTexture },
                    margin = new RectOffset { top = 4, bottom = 4 },
                    padding = new RectOffset(),
                    border = new RectOffset()
                };
            }

            return _separator;
        }
    }

    public static GUIStyle DarkSeparator
    {
        get
        {
            if (_darkSeparator == null)
            {
                _darkSeparator = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = Texture2D.grayTexture },
                    margin = new RectOffset { top = 4, bottom = 4 },
                    padding = new RectOffset(),
                    border = new RectOffset()
                };
            }

            return _darkSeparator;
        }
    }

    public static GUIStyle NormalButton
    {
        get
        {
            if (_normalButton == null)
            {
                _normalButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 13
                };
            }

            return _normalButton;
        }
    }

    public static GUIStyle NormalToggle
    {
        get
        {
            if (_normalToggle == null)
            {
                _normalToggle = new GUIStyle(GUI.skin.toggle)
                {
                    fontSize = 13
                };
            }

            return _normalToggle;
        }
    }

    public static GUIStyle TabButton
    {
        get
        {
            if (_tabButton == null)
            {
                _tabButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                };
            }

            return _tabButton;
        }
    }

    public static GUIStyle TabTitle
    {
        get
        {
            if (_tabTitle == null)
            {
                _tabTitle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                };
            }

            return _tabTitle;
        }
    }

    public static GUIStyle TabSubtitle
    {
        get
        {
            if (_tabSubtitle == null)
            {
                _tabSubtitle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                };
            }

            return _tabSubtitle;
        }
    }

    // Small greyed, word-wrapping caption for hints under a control (e.g. keybind notes)
    public static GUIStyle Hint
    {
        get
        {
            if (_hint == null)
            {
                _hint = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };
            }

            return _hint;
        }
    }

    // Base font sizes (at 1.0 scale), used by the Menu Scale slider
    public const int BaseSkinFont = 15;
    public const int BaseTabButtonFont = 14;
    public const int BaseTabTitleFont = 20;
    public const int BaseTabSubtitleFont = 16;
    public const int BaseNormalFont = 13;

    // Rescales the cached tab styles' font sizes (driven each frame from MenuUI.InitStyles)
    public static void ApplyFontScale(float scale)
    {
        TabButton.fontSize = Mathf.RoundToInt(BaseTabButtonFont * scale);
        TabTitle.fontSize = Mathf.RoundToInt(BaseTabTitleFont * scale);
        TabSubtitle.fontSize = Mathf.RoundToInt(BaseTabSubtitleFont * scale);
        NormalButton.fontSize = Mathf.RoundToInt(BaseNormalFont * scale);
        NormalToggle.fontSize = Mathf.RoundToInt(BaseNormalFont * scale);
    }
}
