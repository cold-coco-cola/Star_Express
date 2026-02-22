using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 菜单：给场景中的 GameManager 挂上 Line Manager 组件（搜不到时用此方式）。
/// </summary>
public static class EnsureLineManagerOnGameManager
{
    [MenuItem("Star Express/给 GameManager 挂上 Line Manager")]
    public static void AddLineManagerToGameManager()
    {
        var gm = UnityEngine.Object.FindObjectOfType<GameManager>();
        if (gm == null)
        {
            Debug.LogWarning("[Star Express] 当前场景中未找到 GameManager，请先打开游戏场景。");
            return;
        }

        var lineManagerType = GetMonoBehaviourTypeByName("LineManager");
        if (lineManagerType == null)
        {
            Debug.LogError("[Star Express] 未找到 LineManager 类型，请确认 Assets/Game/脚本/Core/LineManager.cs 无编译错误。");
            return;
        }

        var existing = gm.GetComponent(lineManagerType);
        if (existing != null)
        {
            Debug.Log("[Star Express] GameManager 上已有 Line Manager 组件。");
            Selection.activeGameObject = gm.gameObject;
            return;
        }

        Undo.AddComponent(gm.gameObject, lineManagerType);
        EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
        Debug.Log("[Star Express] 已给 GameManager 挂上 Line Manager，请 Ctrl+S 保存场景。");
        Selection.activeGameObject = gm.gameObject;
    }

    private static Type GetMonoBehaviourTypeByName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var t in asm.GetTypes())
                {
                    if (t.Name == typeName && typeof(MonoBehaviour).IsAssignableFrom(t))
                        return t;
                }
            }
            catch (ReflectionTypeLoadException) { }
        }
        return null;
    }
}
