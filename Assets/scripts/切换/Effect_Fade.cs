using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Effect_Appear;

public class Effect_Fade : MonoBehaviour
{
    [Header("淡出设置")]
    public float clock = 3f;                    // 淡出时长（秒），0 会默认为 3
    public Effect effect = Effect.Directly;     // Directly=立即 Delayed=延迟
    public float delayedClock;                  // Delayed 模式的等待秒数
    public RaycastMode raycastMode = RaycastMode.ConditionalFalse;
    public float threshold_transparent;         // 透明度低于此值后关 raycast（0 默认 0.05）
    public FinalMode finalMode = FinalMode.Keep;// Close=淡完后隐藏物体 Keep=保持透明
    public InitMode initMode = InitMode.Auto;   // Auto=播放时把透明度重置为 1
    public GameObject obj;                      // 要淡出的目标，不填就是本物体

    [Header("⭐ 层级模式")]
    [Tooltip("勾上 = 淡出整个层级下所有图片/文字；不勾 = 只淡出 obj 一个")]
    public bool affectHierarchy = false;

    [Header("⭐ 排除列表（层级模式下，拖进来的物体及其子物体不受影响）")]
    public List<GameObject> exclusions = new List<GameObject>();

    [Header("播放控制")]
    public bool playOnStart = false;            // 默认 false：开场不自动播放
    public bool playOnce = true;

    // ================= 内部状态 =================
    float transparent = 1f;
    float startAlpha = 1f;
    float timer;
    int state;          // ConditionalFalse 用：低于阈值后关 raycast
    int wait_state;     // 0=等待延迟 1=可以开始 2=已开始
    float wait_timer;
    bool fading;
    float times;

    // ⭐ 层级模式的淡出目标（每次开始淡出时重新收集）
    class Target
    {
        public Graphic graphic;             // UI 图形（Image / TMP / RawImage）
        public SpriteRenderer sprite;       // 场景精灵（单物体模式用）
        public float startAlpha;
    }
    readonly List<Target> targets = new List<Target>();

    public enum RaycastMode { ConditionalFalse, False, Active }
    public enum FinalMode { Keep, Close }
    public enum Effect { Directly, Delayed }
    public enum InitMode { Manual, Auto }


    void OnEnable()
    {
        if (playOnce && times > 0) return;
        times++;

        if (obj == null) obj = gameObject;

        if (effect == Effect.Directly)
            wait_state = 1;

        // Auto 只在 Fade 自己要自动播放时才重置透明度
        if (initMode == InitMode.Auto && playOnStart)
            SetAlpha(1f);

        if (clock < 0) transparent = 1;
        else if (clock == 0) clock = 3;

        if (playOnStart && wait_state == 1)
            StartFade();
    }


    // ✅ ⭐ 核心：外部触发入口（拼图 / 按钮 调这个）
    public void TriggerEffect()
    {
        Debug.Log("Fade 触发成功");

        if (obj == null) obj = gameObject;

        bool wasActive = obj.activeSelf;

        playOnStart = true;
        timer = 0;
        wait_timer = 0;
        fading = false;

        wait_state = (effect == Effect.Delayed) ? 0 : 1;

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (!obj.activeSelf) obj.SetActive(true);

        // 触发前是隐藏的 → 唤醒后立刻压成透明，绝不闪全亮
        if (!wasActive)
            SetAlpha(0f);

        if (wait_state == 1 && playOnStart)
            StartFade();
    }


    void Update()
    {
        // 延迟模式
        if (playOnStart && clock > 0 && effect == Effect.Delayed)
        {
            if (wait_state == 0)
            {
                wait_timer += Time.deltaTime;
                if (wait_timer > delayedClock)
                    wait_state = 1;
            }
            else if (wait_state == 1)
            {
                wait_state = 2;
                StartFade();
            }
        }

        // 正在淡出
        if (fading && clock > 0)
        {
            timer += Time.deltaTime;
            float t01 = Mathf.Clamp01(timer / clock);
            transparent = Mathf.SmoothStep(1f, 0f, t01);

            foreach (var tg in targets)
            {
                float a = Mathf.SmoothStep(tg.startAlpha, 0f, t01);
                ApplyAlpha(tg, a);
            }

            // ConditionalFalse：透明度低于阈值后关掉 raycast
            if (state == 1 && transparent <= threshold_transparent)
            {
                SetRaycast(false);
                state = 0;
            }

            // Close：完全透明后隐藏物体
            if (finalMode == FinalMode.Close && transparent < 0.01f)
                obj.SetActive(false);

            if (timer >= clock)
                fading = false;
        }
    }


    // ========================================
    // ⭐ 开始淡出：收集目标 + 记录各自透明度
    // ========================================
    void StartFade()
    {
        targets.Clear();

        if (affectHierarchy)
        {
            // ⭐ 层级模式：收集下面所有图形，排除列表里的整棵跳过
            foreach (var g in obj.GetComponentsInChildren<Graphic>(true))
            {
                if (g == null) continue;
                if (IsExcluded(g.transform)) continue;

                targets.Add(new Target
                {
                    graphic = g,
                    sprite = null,
                    startAlpha = g.color.a
                });
            }
        }
        else
        {
            // 单物体模式（含场景 SpriteRenderer）
            if (obj.TryGetComponent(out Graphic g1))
                targets.Add(new Target { graphic = g1, sprite = null, startAlpha = g1.color.a });
            else if (obj.TryGetComponent(out SpriteRenderer sr))
                targets.Add(new Target { graphic = null, sprite = sr, startAlpha = sr.color.a });
        }

        startAlpha = GetAlpha();

        if (threshold_transparent <= 0)
            threshold_transparent = 0.05f;

        if (raycastMode == RaycastMode.False)
            SetRaycast(false);
        else if (raycastMode == RaycastMode.ConditionalFalse)
        {
            SetRaycast(true);
            state = 1;
        }
        else
            SetRaycast(true);

        timer = 0;
        fading = true;
    }


    bool IsExcluded(Transform t)
    {
        foreach (var ex in exclusions)
        {
            if (ex == null) continue;
            if (t == ex.transform || t.IsChildOf(ex.transform))
                return true;
        }
        return false;
    }


    // ========================================
    // 工具
    // ========================================
    void ApplyAlpha(Target tg, float a)
    {
        if (tg.graphic != null)
        {
            Color c = tg.graphic.color; c.a = a; tg.graphic.color = c;
        }
        else if (tg.sprite != null)
        {
            Color c = tg.sprite.color; c.a = a; tg.sprite.color = c;
        }
    }


    void SetAlpha(float a)
    {
        if (obj == null) return;

        if (obj.TryGetComponent(out Image img))
        { Color c = img.color; c.a = a; img.color = c; }
        else if (obj.TryGetComponent(out SpriteRenderer sr))
        { Color c = sr.color; c.a = a; sr.color = c; }
        else if (obj.TryGetComponent(out TextMeshProUGUI tmp))
        { Color c = tmp.color; c.a = a; tmp.color = c; }
    }


    float GetAlpha()
    {
        if (obj == null) return 1f;

        if (obj.TryGetComponent(out Image img))
            return img.color.a;
        if (obj.TryGetComponent(out SpriteRenderer sr))
            return sr.color.a;
        if (obj.TryGetComponent(out TextMeshProUGUI tmp))
            return tmp.color.a;

        return 1f;
    }


    void SetRaycast(bool on)
    {
        if (affectHierarchy)
        {
            foreach (var tg in targets)
                if (tg.graphic != null)
                    tg.graphic.raycastTarget = on;
            return;
        }

        if (obj == null) return;
        if (obj.TryGetComponent(out Image img))
            img.raycastTarget = on;
        else if (obj.TryGetComponent(out TextMeshProUGUI tmp))
            tmp.raycastTarget = on;
    }
}