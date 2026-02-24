using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;

namespace Game.Scripts.UI
{
    [CreateAssetMenu(fileName = "MainMenuStyle", menuName = "配置/主菜单样式")]
    public class MainMenuStyle : ScriptableObject
    {
        [Header("Assets")]
        public Font mainFont;
        [Tooltip("背景视频（粒子星空），留空时 Builder 会尝试自动加载")]
        public VideoClip backgroundVideo;
        public Sprite backgroundSprite;
        public Sprite planetSprite;
        public Sprite logoSprite;
        public Sprite buttonHighlightSprite; // 选中按钮时的图标（如箭头）

        [Header("Colors")]
        public Color backgroundColor = new Color(0.02f, 0.02f, 0.08f, 1f); // 深邃太空蓝
        public Color textColor = new Color(0.95f, 0.92f, 0.88f, 1f); // 柔和暖白
        public Color highlightColor = new Color(1f, 0.65f, 0.25f, 1f); // 暖橙高亮
        public Color buttonBgColor = new Color(0.08f, 0.1f, 0.18f, 0.85f); // 按钮背景

        [Header("Settings")]
        public float buttonHeight = 56f;
        public float buttonSpacing = 24f;
        public int fontSize = 32;
    }
}
