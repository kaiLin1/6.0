using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class PuzzlePiece : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("功能开关")]
    public bool enableDrag = true;
    public bool enableHover = true;
    public bool enableClickScale = true;

    [Header("拖拽解锁")]
    public bool dragEnabledAtStart = true;

    [Header("拼图目标")]
    public PuzzleTarget target;
    public float snapSpeed = 10f;

    [Header("⭐ 所属拼图管理器")]
    public PuzzleManager manager;

    [Header("缩放")]
    public float hoverScale = 1.05f;
    public float clickScale = 1.1f;
    public float scaleSpeed = 10f;

    [Header("层级控制")]
    public bool changeLayerOnSnap = true;
    public int layerOffset = 1;

    [Header("完成后设为子物体")]
    public bool setAsChildOnSnap = false;

    [Header("完成后删除")]
    public bool destroyAfterSnap = false;
    public float destroyDelay = 0f;

    public System.Action<PuzzlePiece> onPlaced;

    private RectTransform rectTransform;
    private Canvas canvas;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private bool isPointerOver = false;
    private bool isDragging = false;
    private bool isClicking = false;
    private bool isLocked = false;

    private bool hasSnapped = false;

    // 是否允许交互（拖拽、点击、悬停）
    private bool canDrag;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        canDrag = dragEnabledAtStart;
    }

    void Update()
    {
        if (isLocked) return;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canDrag || isLocked) return;

        isPointerOver = true;

        if (enableHover && !isClicking)
            targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!canDrag || isLocked) return;

        isPointerOver = false;

        if (!isDragging)
            targetScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!canDrag || isLocked) return;

        isClicking = true;

        if (enableClickScale)
            targetScale = originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!canDrag || isLocked) return;

        isClicking = false;

        if (enableHover && isPointerOver)
            targetScale = originalScale * hoverScale;
        else
            targetScale = originalScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!enableDrag || !canDrag || isLocked) return;

        isDragging = true;
    }
    public void OnDrag(PointerEventData eventData)
    { 
        if (!enableDrag || !canDrag || isLocked) return; 
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor; 
        if (target != null) { target.CheckHover(transform.position); 
        } 
    }
    public void OnDragPlus(PointerEventData eventData)
    {
        if (!enableDrag || !canDrag || isLocked) return;

        DragByCanvasMode(eventData);
        //rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;

        if (target != null)
        {
            target.CheckHover(transform.position);
        }
    }
    private void DragByCanvasMode(PointerEventData eventData)
    {
        if (canvas == null)
            return;

        switch (canvas.renderMode)
        {
            case RenderMode.ScreenSpaceOverlay:
                DragScreenSpaceOverlay(eventData);
                break;

            case RenderMode.ScreenSpaceCamera:
                DragScreenSpaceCamera(eventData);
                break;

            case RenderMode.WorldSpace:
                DragWorldSpace(eventData);
                break;
        }
    }

    private void DragScreenSpaceOverlay(PointerEventData eventData)
    {
        rectTransform.anchoredPosition +=
            eventData.delta / canvas.scaleFactor;
    }

    private void DragScreenSpaceCamera(PointerEventData eventData)
    {
        rectTransform.anchoredPosition +=
            eventData.delta / canvas.scaleFactor;
    }

    private void DragWorldSpace(PointerEventData eventData)
    {
        Camera cam = eventData.pressEventCamera;

        Vector3 worldPoint;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform,
            eventData.position,
            cam,
            out worldPoint))
        {
            rectTransform.position = worldPoint;
        }
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!enableDrag || !canDrag || isLocked) return;

        isDragging = false;

        if (target != null && target.IsHovering())
        {
            StartCoroutine(SnapToTarget());
        }
        else
        {
            targetScale = isPointerOver ? originalScale * hoverScale : originalScale;
        }
    }

    IEnumerator SnapToTarget()
    {
        if (hasSnapped) yield break;
        hasSnapped = true;

        Vector2 start = rectTransform.anchoredPosition;
        Vector2 end = rectTransform.parent.InverseTransformPoint(target.transform.position);

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * snapSpeed;
            rectTransform.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        rectTransform.anchoredPosition = end;

        if (setAsChildOnSnap && target != null)
        {
            transform.SetParent(target.transform);
            rectTransform.anchoredPosition = Vector2.zero;
        }

        if (changeLayerOnSnap && target != null)
        {
            int targetIndex = target.transform.GetSiblingIndex();

            int newIndex = Mathf.Clamp(
                targetIndex + layerOffset,
                0,
                transform.parent.childCount - 1
            );

            transform.SetSiblingIndex(newIndex);
        }

        isLocked = true;
        targetScale = transform.localScale;

        var img = GetComponent<UnityEngine.UI.Image>();

        if (img != null)
            img.raycastTarget = false;

        if (target != null)
            target.OnPlaced(this);

        if (manager != null)
            manager.NotifyPiecePlaced(this);

        onPlaced?.Invoke(this);

        if (destroyAfterSnap)
        {
            yield return new WaitForSeconds(destroyDelay);
            Destroy(gameObject);
        }
    }

    // ===== 外部控制 =====

    public void EnableDrag()
    {
        canDrag = true;
    }

    public void DisableDrag()
    {
        canDrag = false;

        isPointerOver = false;
        isDragging = false;
        isClicking = false;

        targetScale = originalScale;
    }

    public void SetDragEnabled(bool value)
    {
        canDrag = value;

        if (!value)
        {
            isPointerOver = false;
            isDragging = false;
            isClicking = false;

            targetScale = originalScale;
        }
    }
}