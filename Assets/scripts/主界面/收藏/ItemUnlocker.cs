using UnityEngine;

public class ItemUnlocker : MonoBehaviour
{
    public string itemID;

    public void UnlockByButton()
    {
        Debug.Log($"[按钮被点击] 我要解锁 '{itemID}'");
        CollectionManager.GetInstance().Unlock(itemID);
    }
}