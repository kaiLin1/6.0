using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OneClickButton : MonoBehaviour
{
    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        Debug.Log("点击一次，关闭 Button Interactable");

        button.interactable = false;
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }
}