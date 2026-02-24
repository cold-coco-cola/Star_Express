#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Game.Scripts.UI.MainMenuBuilder))]
public class MainMenuBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("构建主菜单 UI"))
        {
            ((Game.Scripts.UI.MainMenuBuilder)target).BuildUI();
        }
    }

    [MenuItem("Star Express/构建主菜单 UI")]
    private static void BuildMainMenuFromMenu()
    {
        var builder = Object.FindObjectOfType<Game.Scripts.UI.MainMenuBuilder>();
        if (builder != null)
        {
            builder.BuildUI();
            Debug.Log("[Star Express] 主菜单 UI 已构建。");
        }
        else
        {
            Debug.LogWarning("[Star Express] 请先打开 StartMenu 场景并选中带 MainMenuBuilder 的对象。");
        }
    }
}
#endif
