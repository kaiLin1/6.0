using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 圆形阵列收散器（独立版）
/// ------------------------------------------------------------
/// 用法：
/// 1. 建一个容器 UI 物体（RectTransform 拉大），把要围成圈的小物体
///    作为它的子物体摆进去（每个成员自己要带 Image/Text 等图形）。
/// 2. 容器上挂本脚本。
/// 3. Members 留空 = 自动收集所有带图形的子物体；
///    也可以手动指定，排除不想进圈的（比如中间的花/按钮）。
/// 4. 按钮 OnClick 绑 CircleScatter → Toggle()。
///    代码里也能调 Gather()（收拢）/ Scatter()（散开）。
/// ------------------------------------------------------------
/// 效果：成员围成圆阵、各自上下浮动、悬停放大；
///       Toggle 时在"围拢 ↔ 向外散开"之间平滑切换（带惯性）。
/// </summary>
public class CircleScatter : MonoBehaviour
{
    [Header("成员（留空 = 自动收集带图形的子物体）")]
    public List<RectTransform> members = new List<RectTransform>();

    [Header("圆阵")]
    public float radius = 260f;          // 围拢时的半径
    public float scatterDistance = 300f; // 散开时向外多远的距离
    public float lerpSpeed = 9f;         // 收/散惯性速度

    [Header("漂浮（相位自动错开）")]
    public bool enableFloat = true;
    public float floatAmp = 7f;
    public float floatSpeed = 1.2f;

    [Header("悬停放大")]
    public bool enableHoverScale = true;
    public float hoverScale = 1.14f;

    // ==================== 运行时 ====================
    class M
    {
        public RectTransform rt;
        public CanvasGroup cg;
        public CircleMember ctrl;
        public Vector2 dir;      // 圆阵方向
        public float phase;      // 漂浮相位
        public float hover = 1f; // 悬停缩放
    }

    readonly List<M> list = new List<M>();
    float progress = 1f;   // 1=围拢 0=散开
    float target = 1f;


    void Start()
    {
        Init();
    }


    /// <summary>收集成员（运行时加了新成员可重新调用）</summary>
    public void Init()
    {
        foreach (var m in list)
            if (m.cg != null) Destroy(m.cg);
        list.Clear();

        List<RectTransform> rts = members;
        if (rts == null || rts.Count == 0)
        {
            rts = new List<RectTransform>();
            foreach (Transform child in transform)
            {
                var rt = child as RectTransform;
                if (rt != null && rt.GetComponent<Graphic>() != null)
                    rts.Add(rt);
            }
        }

        int n = rts.Count;
        if (n == 0)
        {
            Debug.LogWarning("CircleScatter：没有找到任何成员");
            return;
        }

        for (int i = 0; i < n; i++)
        {
            float angle = Mathf.PI * 2f / n * i - Mathf.PI / 2f;
            var rt = rts[i];

            var m = new M
            {
                rt = rt,
                dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                phase = i * 0.7f
            };

            m.cg = rt.GetComponent<CanvasGroup>();
            if (m.cg == null) m.cg = rt.gameObject.AddComponent<CanvasGroup>();

            m.ctrl = rt.GetComponent<CircleMember>();
            if (m.ctrl == null) m.ctrl = rt.gameObject.AddComponent<CircleMember>();

            list.Add(m);
        }
    }


    // ============================================================
    //  ⭐ 按钮绑这些
    // ============================================================
    public void Toggle()  { target = target > 0.5f ? 0f : 1f; }
    public void Gather()  { target = 1f; }
    public void Scatter() { target = 0f; }


    void Update()
    {
        if (list.Count == 0) return;

        // 惯性平滑
        progress += (target - progress) * lerpSpeed * Time.deltaTime;
        if (Mathf.Abs(target - progress) < 0.001f) progress = target;

        float dist = radius + scatterDistance * (1f - progress);
        float baseScale = 0.35f + 0.65f * progress;
        float t = Time.time;

        foreach (var m in list)
        {
            // 漂浮
            float floatY = enableFloat ? Mathf.Sin(t * floatSpeed + m.phase) * floatAmp : 0f;

            m.rt.anchoredPosition = m.dir * dist + new Vector2(0, floatY);
            m.cg.alpha = progress;

            // 悬停放大
            float wantHover = (enableHoverScale && m.ctrl.hover) ? hoverScale : 1f;
            m.hover = Mathf.Lerp(m.hover, wantHover, 12f * Time.deltaTime);
            m.rt.localScale = Vector3.one * baseScale * m.hover;
        }
    }


    // 悬停检测小组件（自动挂在每个成员上）
    class CircleMember : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool hover;
        public void OnPointerEnter(PointerEventData e) { hover = true; }
        public void OnPointerExit(PointerEventData e) { hover = false; }
    }
}