using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetTutorialDebug : MonoBehaviour
{
    [ContextMenu("重置新手教程")]
    private void ResetTutorial()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 0);
        PlayerPrefs.Save();
        Debug.Log("新手教程已重置！重新进入第一关将再次显示教程。");
    }
}
