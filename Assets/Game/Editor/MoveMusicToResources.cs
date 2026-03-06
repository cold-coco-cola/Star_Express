#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 将 Assets/Game/音乐 移动到 Assets/Game/Resources/音乐，
/// 以便 Resources.Load 能加载背景音乐。
/// </summary>
public static class MoveMusicToResources
{
    [MenuItem("Star Express/移动音乐到 Resources")]
    public static void MoveMusicFolder()
    {
        const string src = "Assets/Game/音乐";
        const string dst = "Assets/Game/Resources/音乐";
        if (!AssetDatabase.IsValidFolder("Assets/Game/Resources"))
            AssetDatabase.CreateFolder("Assets/Game", "Resources");
        if (AssetDatabase.IsValidFolder(dst))
        {
            AssetDatabase.DeleteAsset(dst);
            AssetDatabase.Refresh();
        }
        string result = AssetDatabase.MoveAsset(src, dst);
        if (string.IsNullOrEmpty(result))
        {
            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[Star Express] 已将 音乐 文件夹移动到 Resources 下，背景音乐可正常加载。");
        }
        else
        {
            UnityEngine.Debug.LogWarning("[Star Express] 移动失败: " + result + "。请手动将 Assets/Game/音乐 移动到 Assets/Game/Resources/ 下。");
        }
    }
}
#endif
