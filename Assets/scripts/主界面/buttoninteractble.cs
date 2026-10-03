using UnityEngine;
using UnityEngine.UI;

public class ButtonHelper : MonoBehaviour
{
    public Button targetButton;   // 不填就自动找自己身上的 Button

    void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();
    }

    // ⭐ 无参数版本：专门给 UnityEvent 绑
    public void EnableButton()
    {
        if (targetButton != null)
            targetButton.interactable = true;
    }

    public void DisableButton()
    {
        if (targetButton != null)
            targetButton.interactable = false;
    }
}