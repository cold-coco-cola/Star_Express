#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>清理遗留的 Scenes 文件夹。项目规范：场景统一放在 Assets/Game/场景/，不再使用 Scenes 目录。</summary>
public static class ConsolidateScenesFolder
{
    [MenuItem("Star Express/清理空的 Scenes 文件夹")]
    public static void Run()
    {
        string scenesFolder = "Assets/Game/Scenes";
        if (!AssetDatabase.IsValidFolder("Assets/Game/Scenes"))
        {
            Debug.Log("[清理] Scenes 文件夹不存在，无需清理。");
            return;
        }
        AssetDatabase.Refresh();
        var guids = AssetDatabase.FindAssets("t:Object", new[] { scenesFolder });
        if (guids.Length == 0)
        {
            AssetDatabase.DeleteAsset(scenesFolder);
            AssetDatabase.Refresh();
            Debug.Log("[清理] 已删除空的 Scenes 文件夹。");
        }
        else
        {
            Debug.LogWarning($"[清理] Scenes 文件夹非空（{guids.Length} 个资源），请手动将场景移至 Assets/Game/场景/ 后再清理。");
        }
    }
}
#endif
