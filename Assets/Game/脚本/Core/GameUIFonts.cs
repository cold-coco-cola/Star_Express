using UnityEngine;

/// <summary>
/// 游戏 UI 统一字体。所有文本应使用此字体以保证视觉一致。
/// </summary>
public static class GameUIFonts
{
    private static Font _defaultFont;

    public static Font Default
    {
        get
        {
            if (_defaultFont == null)
                _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _defaultFont;
        }
    }
}
