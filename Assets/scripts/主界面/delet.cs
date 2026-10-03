using UnityEngine;

public class ClearSaveButton : MonoBehaviour
{
    // ===== 按钮的 OnClick 里调用这个方法 =====
    public void ClearAllSave()
    {
        // 清空所有 PlayerPrefs：收藏解锁记录 + 装扮布局 + 其他所有存档
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        Debug.Log("[存档] 已全部清空！");

        // 清空后刷新收藏界面：所有图片变回灰色
        var items = FindObjectsByType<CollectionItemUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var item in items)
            item.Refresh();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // 编辑器调试快捷键：按 F1 一键清空（打包后自动剔除，玩家按了无效）
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.f1Key.wasPressedThisFrame)
        {
            ClearAllSave();
        }
    }
#endif
}