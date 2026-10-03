using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DreamMaskController : MonoBehaviour
{
    [Header("摄像机")]
    public Camera targetCamera;

    [Header("黑框厚度")]
    public float topHeight = 150f;
    public float bottomHeight = 150f;
    public float sideWidth = 120f;

    [Header("黑框显示比例")]
    [Range(0, 1)]
    public float thickness = 1f;

    [Header("⭐ 加厚效果")]
    public float targetThickness = 1f;   // 触发后加厚到的目标值
    public float thickenTime = 2f;       // 加厚动画时长（秒）

    [Header("底部黑框位置")]
    public float bottomOffsetY = 250f;

    [Header("出现动画")]
    public float animationTime = 1.5f;

    [Header("向外扩散动画")]
    public float expandTime = 30f;
    public float expandDistance = 30f;

    [Header("颜色")]
    public Color maskColor = Color.black;

    // 四个黑框统一用数组管理  0上 1下 2左 3右
    RectTransform[] masks = new RectTransform[4];
    static readonly string[] maskNames = { "TopBlack", "BottomBlack", "LeftBlack", "RightBlack" };

    RectTransform canvasRect;
    float current = 0;
    bool isExpanding = false;
    Coroutine thickenCoroutine;


    void Awake()
    {
        canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();

        CreateMask();

        foreach (var m in masks)
            m.sizeDelta = Vector2.zero;

        current = 0;

        StartCoroutine(Change(1));
    }


    void CreateMask()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject obj = new GameObject(maskNames[i]);
            obj.transform.SetParent(transform);

            Image img = obj.AddComponent<Image>();
            img.color = maskColor;
            img.raycastTarget = false;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;

            masks[i] = rect;
        }
    }


    public void EnterDream()
    {
        StopAllCoroutines();
        StartCoroutine(Change(1));
    }


    public void ExitDream()
    {
        StopAllCoroutines();
        StartCoroutine(Change(0));
    }


    // 外部调用
    public void ExpandDisappear()
    {
        if (!isExpanding)
            StartCoroutine(ExpandMove());
    }


    // ========================================
    // ⭐ 外部触发：加厚到 Target Thickness
    //    （UnityEvent / 按钮绑这个，无参数）
    // ========================================
    public void StartThicken()
    {
        StartThickenTo(targetThickness);
    }


    // 代码里调用可传任意目标值
    public void StartThickenTo(float target)
    {
        if (thickenCoroutine != null)
            StopCoroutine(thickenCoroutine);

        thickenCoroutine = StartCoroutine(ThickenTo(target));
    }


    IEnumerator ThickenTo(float target)
    {
        float start = thickness;
        float timer = 0;

        while (timer < thickenTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, timer / thickenTime);

            thickness = Mathf.Lerp(start, target, t);

            // 用当前的显示值 current 重绘黑框
            SetMask(current);

            yield return null;
        }

        thickness = target;
        SetMask(current);

        thickenCoroutine = null;
    }


    IEnumerator ExpandMove()
    {
        isExpanding = true;

        float timer = 0;
        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;

        // 四个方向的起点和移动向量
        Vector3[] starts = new Vector3[4];
        Vector3[] moves = new Vector3[4];

        for (int i = 0; i < 4; i++)
        {
            starts[i] = masks[i].localPosition;

            moves[i] = i switch
            {
                0 => new Vector3(0, height * expandDistance, 0),   // 上
                1 => new Vector3(0, -height * expandDistance, 0),  // 下
                2 => new Vector3(-width * expandDistance, 0, 0),   // 左
                _ => new Vector3(width * expandDistance, 0, 0),    // 右
            };
        }

        while (timer < expandTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, timer / expandTime);

            for (int i = 0; i < 4; i++)
                masks[i].localPosition = Vector3.Lerp(starts[i], starts[i] + moves[i], t);

            yield return null;
        }

        foreach (var m in masks)
            m.gameObject.SetActive(false);
    }


    IEnumerator Change(float target)
    {
        float start = current;
        float timer = 0;

        while (timer < animationTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, timer / animationTime);

            current = Mathf.Lerp(start, target, t);
            SetMask(current);

            yield return null;
        }

        current = target;
        SetMask(current);
    }


    void SetMask(float value)
    {
        float width = canvasRect.rect.width;
        float height = canvasRect.rect.height;

        // 顺序：0上 1下 2左 3右
        Setup(masks[0],
            new Vector2(width, topHeight * thickness * value),
            new Vector3(0, height / 2 - topHeight * thickness * value / 2, 0));

        Setup(masks[1],
            new Vector2(width, bottomHeight * thickness * value),
            new Vector3(0, -height / 2 + bottomOffsetY + bottomHeight * thickness * value / 2, 0));

        Setup(masks[2],
            new Vector2(sideWidth * thickness * value, height),
            new Vector3(-width / 2 + sideWidth * thickness * value / 2, 0, 0));

        Setup(masks[3],
            new Vector2(sideWidth * thickness * value, height),
            new Vector3(width / 2 - sideWidth * thickness * value / 2, 0, 0));
    }


    void Setup(RectTransform rect, Vector2 size, Vector3 pos)
    {
        rect.sizeDelta = size;
        rect.localPosition = pos;
    }
}