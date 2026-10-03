using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class ButtonGroupTrigger : MonoBehaviour
{
    [Header("需要完成的按钮")]
    public Button[] targetButtons;

    [Header("全部完成后触发")]
    public UnityEvent onAllButtonsTriggered;

    private HashSet<Button> clickedButtons = new HashSet<Button>();
    private bool finished = false;

    void Start()
    {
        foreach (Button btn in targetButtons)
        {
            if (btn == null) continue;

            btn.onClick.AddListener(() => OnButtonClicked(btn));
        }
    }

    void OnButtonClicked(Button btn)
    {
        if (finished) return;

        clickedButtons.Add(btn);

        if (clickedButtons.Count >= targetButtons.Length)
        {
            finished = true;
            onAllButtonsTriggered?.Invoke();
        }
    }

    public void ResetGroup()
    {
        clickedButtons.Clear();
        finished = false;
    }
}