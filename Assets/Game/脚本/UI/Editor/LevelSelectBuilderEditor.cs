#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Game.Scripts.UI.LevelSelectBuilder))]
public class LevelSelectBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("构建关卡选择 UI"))
        {
            ((Game.Scripts.UI.LevelSelectBuilder)target).BuildUI();
        }
    }

    [MenuItem("Star Express/构建关卡选择 UI")]
    private static void BuildFromMenu()
    {
        var builder = Object.FindObjectOfType<Game.Scripts.UI.LevelSelectBuilder>();
        if (builder != null)
        {
            builder.BuildUI();
            Debug.Log("[Star Express] 关卡选择 UI 已构建。");
        }
        else
        {
            Debug.LogWarning("[Star Express] 请先打开 LevelSelect 场景并选中带 LevelSelectBuilder 的对象。");
        }
    }
}
#endif
