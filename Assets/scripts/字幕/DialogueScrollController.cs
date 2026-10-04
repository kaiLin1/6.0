using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogueScrollController : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;

    private Coroutine pendingLayout;
    private bool followAfterLayout;

    internal bool ShouldFollowBottom => !isActiveAndEnabled ||
        (pendingLayout != null ? followAfterLayout :
            scrollRect.content.rect.height <= scrollRect.viewport.rect.height ||
            scrollRect.verticalNormalizedPosition <= 0.01f);

    private void OnEnable()
    {
        ContentChanged(true);
    }

    private void OnDisable()
    {
        if (pendingLayout != null)
            StopCoroutine(pendingLayout);
        pendingLayout = null;
    }

    internal void ContentChanged(bool followBottom)
    {
        // Closed panels lay out and show the latest entry when they next open.
        if (!isActiveAndEnabled) return;

        followAfterLayout = followBottom;
        if (pendingLayout != null)
            StopCoroutine(pendingLayout);
        pendingLayout = StartCoroutine(RefreshAfterLayout());
    }

    private IEnumerator RefreshAfterLayout()
    {
        // The history panel pauses game time, so do not use a scaled wait.
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
        Canvas.ForceUpdateCanvases();
        if (followAfterLayout)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 0f;
        }
        // Top-anchored content grows downwards, preserving an existing reading offset.
        pendingLayout = null;
    }
}
