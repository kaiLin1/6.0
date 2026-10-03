using UnityEngine;

public class CollectionEditMode : MonoBehaviour
{
    // CollectionItemUI 就是读这个静态属性来决定点不点的
    public static bool IsEditing { get; private set; } = false;

    // 编辑按钮的 OnClick 里调用
    public void ToggleEdit()
    {
        IsEditing = !IsEditing;
        Debug.Log(IsEditing ? "[编辑模式] 开：现在可以点击已解锁的收藏物"
                            : "[编辑模式] 关：点击无效");
    }
}