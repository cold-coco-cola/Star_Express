#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>合并 Scenes 与 场景 文件夹：用 Scenes/StartMenu 替换 场景/StartMenu，然后删除 Scenes 文件夹。</summary>
public static class ConsolidateScenesFolder
{
    [MenuItem("Star Express/合并场景文件夹 (Scenes→场景)")]
    public static void Run()
    {
        string scenesPath = "Assets/Game/Scenes/StartMenu.unity";
        string targetPath = "Assets/Game/场景/StartMenu.unity";

        var scenesAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenesPath);
        if (scenesAsset == null)
        {
            Debug.Log("[合并] Scenes/StartMenu.unity 不存在。若 Scenes 为空则删除。");
            DeleteEmptyScenesFolder();
            return;
        }

        // 删除 场景/StartMenu，以便移动
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath) != null)
        {
            AssetDatabase.DeleteAsset(targetPath);
        }

        string err = AssetDatabase.MoveAsset(scenesPath, targetPath);
        if (!string.IsNullOrEmpty(err))
        {
            Debug.LogError("[合并] 移动失败: " + err);
            return;
        }

        DeleteEmptyScenesFolder();
        AssetDatabase.Refresh();
        Debug.Log("[合并] 完成。StartMenu 现位于 Assets/Game/场景/");
    }

    private static void DeleteEmptyScenesFolder()
    {
        string scenesFolder = "Assets/Game/Scenes";
        if (!AssetDatabase.IsValidFolder("Assets/Game/Scenes")) return;
        AssetDatabase.Refresh();
        var guids = AssetDatabase.FindAssets("t:Object", new[] { scenesFolder });
        if (guids.Length == 0)
        {
            AssetDatabase.DeleteAsset(scenesFolder);
            Debug.Log("[合并] 已删除 Scenes 文件夹。");
        }
    }
}
#endif
