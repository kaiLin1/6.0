using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SubNodeChoose : MonoBehaviour, IPointerEnterHandler
{
    [Header("上移")]
    public float lift = 10f;
    [Header("放大")]
    public float scaleTarget = 1.18f;
    [Header("摆动")]
    public float wobbleAngle = 7f;
    public float wobbleDuration = 1.1f;
    [Header("过渡")]
    public float enterDuration = 0.22f;
    public float exitDuration = 0.25f;
    [Header("光圈")]
    public Color glowColor = new Color(1f, 0.86f, 0.45f, 0.9f);
    public Vector2 glowDistance = new Vector2(1.5f, -1.5f);
    [Header("抗闪烁")]
    public float alphaThreshold = 0.1f;
    public float exitGrace = 0.2f;
    public float gracePadding = 12f;
    [Header("其他")]
    public bool hoverEnabled = true;

    RectTransform rt;
    Vector2 homePos;
    Vector3 homeScale;
    int homeSiblingIndex;
    readonly List<Outline> glows = new List<Outline>();
    Coroutine animRoutine;
    Coroutine wobbleRoutine;
    bool hovered;
    bool inited;
    float leaveTimer;

    public bool IsHovered => hovered;

    void Awake()
    {
        Init();
    }

    void Update()
    {
        if (!inited || !hovered) return;

        // 核心规则：指针正下方压着的图标是谁？
        SubNodeChoose top = GetNodeUnderPointer(GetMousePos());

        if (top == this)
        {
            leaveTimer = 0f;               // 正压着我，安心悬停
            return;
        }

        // 没压着我：缓冲圈内给点宽限，圈外立刻走
        leaveTimer += Time.deltaTime;
        float limit = IsPointerNear(GetMousePos()) ? exitGrace : 0.02f;
        if (leaveTimer >= limit) DoExit();
    }

    // 自动适配新旧输入系统（Unity 6 默认新版 Input System）
    Vector2 GetMousePos()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    // 用 UI 射线检测找出指针位置最上层的 SubNodeChoose
    SubNodeChoose GetNodeUnderPointer(Vector2 screenPos)
    {
        if (EventSystem.current == null) return null;
        PointerEventData ped = new PointerEventData(EventSystem.current) { position = screenPos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        foreach (RaycastResult r in results)
        {
            SubNodeChoose node = r.gameObject.GetComponentInParent<SubNodeChoose>();
            if (node != null) return node;
        }
        return null;
    }

    public void Init()
    {
        if (inited) return;
        rt = (RectTransform)transform;
        homePos = rt.anchoredPosition;
        homeScale = rt.localScale;
        homeSiblingIndex = rt.GetSiblingIndex();

        foreach (Image img in GetComponentsInChildren<Image>())
            img.alphaHitTestMinimumThreshold = alphaThreshold;

        foreach (Graphic g in GetComponentsInChildren<Graphic>())
        {
            Outline o = g.gameObject.AddComponent<Outline>();
            o.effectColor = glowColor;
            o.effectDistance = glowDistance;
            o.enabled = false;
            glows.Add(o);
        }
        inited = true;
        Debug.Log("[悬停] 加载成功: " + name);   // 验证新版在跑，稳定后可删
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!inited || !hoverEnabled || hovered) return;
        hovered = true;
        leaveTimer = 0f;

        rt.SetAsLastSibling();
        SetGlow(true);
        if (animRoutine != null) StopCoroutine(animRoutine);
        if (wobbleRoutine != null) StopCoroutine(wobbleRoutine);
        animRoutine = StartCoroutine(EnterAnim());
    }

    bool IsPointerNear(Vector2 screenPos)
    {
        Camera cam = null; // Screen Space - Overlay 模式
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, cam, out Vector2 lp))
            return Mathf.Abs(lp.x) < rt.rect.width / 2f + gracePadding
                && Mathf.Abs(lp.y) < rt.rect.height / 2f + gracePadding;
        return false;
    }

    void DoExit()
    {
        if (!hovered) return;
        hovered = false;
        leaveTimer = 0f;
        SetGlow(false);
        if (animRoutine != null) StopCoroutine(animRoutine);
        if (wobbleRoutine != null) StopCoroutine(wobbleRoutine);
        animRoutine = StartCoroutine(ExitAnim());
    }

    IEnumerator EnterAnim()
    {
        Vector2 p0 = rt.anchoredPosition;
        Vector3 s0 = rt.localScale;
        for (float t = 0f; t < enterDuration; t += Time.deltaTime)
        {
            float p = t / enterDuration;
            rt.anchoredPosition = Vector2.Lerp(p0, homePos + Vector2.up * lift, p);
            rt.localScale = Vector3.Lerp(s0, homeScale * scaleTarget, p);
            yield return null;
        }
        rt.anchoredPosition = homePos + Vector2.up * lift;
        rt.localScale = homeScale * scaleTarget;
        wobbleRoutine = StartCoroutine(Wobble());
    }

    IEnumerator Wobble()
    {
        float half = wobbleDuration / 2f;
        while (hovered)
        {
            yield return RotateTo(wobbleAngle, half);
            if (!hovered) break;
            yield return RotateTo(-wobbleAngle, half);
        }
    }

    IEnumerator RotateTo(float target, float duration)
    {
        float start = rt.localEulerAngles.z;
        if (start > 180f) start -= 360f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float z = Mathf.Lerp(start, target, t / duration);
            rt.localRotation = Quaternion.Euler(0f, 0f, z);
            yield return null;
        }
        rt.localRotation = Quaternion.Euler(0f, 0f, target);
    }

    IEnumerator ExitAnim()
    {
        Vector2 p0 = rt.anchoredPosition;
        Vector3 s0 = rt.localScale;
        float z0 = rt.localEulerAngles.z;
        if (z0 > 180f) z0 -= 360f;

        for (float t = 0f; t < exitDuration; t += Time.deltaTime)
        {
            float p = t / exitDuration;
            rt.anchoredPosition = Vector2.Lerp(p0, homePos, p);
            rt.localScale = Vector3.Lerp(s0, homeScale, p);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(z0, 0f, p));
            yield return null;
        }
        rt.anchoredPosition = homePos;
        rt.localScale = homeScale;
        rt.localRotation = Quaternion.identity;
        rt.SetSiblingIndex(homeSiblingIndex);
    }

    void SetGlow(bool on)
    {
        foreach (Outline o in glows) o.enabled = on;
    }
}