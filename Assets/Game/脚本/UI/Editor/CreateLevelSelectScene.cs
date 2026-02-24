#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CreateLevelSelectScene
{
    [MenuItem("Star Express/创建关卡选择场景")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Object.FindObjectOfType<Camera>();
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.08f, 1f);
        }

        var manager = new GameObject("LevelSelectManager");
        manager.AddComponent<Game.Scripts.UI.LevelSelectController>();

        var path = "Assets/Game/场景/LevelSelect.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[Star Express] 关卡选择场景已创建: {path}");
    }

    [MenuItem("Star Express/当前场景添加 LevelSelectManager")]
    public static void AddManagerToCurrentScene()
    {
        if (Object.FindObjectOfType<Game.Scripts.UI.LevelSelectController>() != null)
        {
            Debug.Log("[Star Express] 场景中已有 LevelSelectManager。");
            return;
        }
        var go = new GameObject("LevelSelectManager");
        go.AddComponent<Game.Scripts.UI.LevelSelectController>();
        Undo.RegisterCreatedObjectUndo(go, "Add LevelSelectManager");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Star Express] 已添加 LevelSelectManager，请保存场景。");
    }
}
#endif
