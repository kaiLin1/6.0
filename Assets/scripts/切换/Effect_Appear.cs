using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Effect_Appear : MonoBehaviour
{
    public enum Effect { Directly, Delayed }
    public enum FinalMode { Keep, Close }
    public enum RaycastMode { False, ConditionalActive, Active }
    public enum InitMode { Auto, Manual }

    [Header("目标物体（留空 = 自己）")]
    public GameObject obj;

    [Header("淡入时间（秒，<=0 瞬显，0 默认按3秒）")]
    public float clock;

    [Header("触发方式")]
    public Effect effect = Effect.Directly;
    public float delayedClock;

    [Header("完成后模式")]
    public FinalMode finalMode = FinalMode.Keep;
    public RaycastMode raycastMode = RaycastMode.ConditionalActive;
    public float threshold_transparent;

    [Header("初始化模式")]
    public InitMode initMode = InitMode.Auto;

    [Header("⭐ 层级模式")]
    [Tooltip("勾上 = 淡入整个层级下所有图片/文字；不勾 = 只淡入 obj 一个")]
    public bool affectHierarchy = false;

    [Header("⭐ 排除列表（层级模式下，拖进来的物体及其子物体不受影响）")]
    public List<GameObject> exclusions = new List<GameObject>();

    [Header("播放设置")]
    public bool playOnStart = false;    // 默认 false：只有 TriggerEffect 能启动
    public bool playOnce = true;

    [Header("全部完成事件")]
    public UnityEvent onAllComplete;

    float times;
    int waitState;
    float waitTimer;
    float timer;
    int raycastState;
    Coroutine fadeCoroutine;

    bool triggering;

    // ⭐ 淡入目标（每次开始时重新收集）
    readonly List<Graphic> targets = new List<Graphic>();


    // ============================================================
    //  收集目标
    // ============================================================
    void CollectTargets()
    {
        targets.Clear();

        if (affectHierarchy)
        {
            foreach (var g in obj.GetComponentsInChildren<Graphic>(true))
            {
                if (g == null) continue;
                if (IsExcluded(g.transform)) continue;
                targets.Add(g);
            }
        }
        else
        {
            var g = obj.GetComponent<Graphic>();
            if (g != null) targets.Add(g);
        }
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


    void OnEnable()
    {
        if (obj == null) obj = gameObject;

        if (effect == Effect.Directly)
            waitState = 1;

        if (clock == 0) clock = 3;

        // Auto：先把透明度归 0（层级模式 = 整组一起藏好）
        if (initMode == InitMode.Auto)
        {
            CollectTargets();
            SetAlphaAll(0f);
        }

        // 被 TriggerEffect 唤醒的：只初始化，绝不自动播放
        if (triggering) return;

        if (playOnce && times > 0) return;

        if (waitState == 1 && playOnStart)
            StartFade();
    }


    void Update()
    {
        if (waitState != 0) return;
        if (clock <= 0 || effect != Effect.Delayed) return;

        waitTimer += Time.deltaTime;

        if (waitTimer >= delayedClock)
        {
            waitTimer = 0;
            waitState = 1;
            StartFade();
        }
    }


    // ========================================
    // ⭐ 按钮调用：手动触发（唯一的启动入口）
    // ========================================

    public void TriggerEffect()
    {
        if (playOnce && times > 0)
        {
            Debug.Log("Appear 已触发过，playOnce 阻止重复");
            return;
        }

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (obj == null)
            obj = gameObject;

        triggering = true;

        if (!obj.activeSelf)
            obj.SetActive(true);

        triggering = false;

        if (clock == 0) clock = 3;

        if (effect == Effect.Delayed)
        {
            waitState = 0;
            waitTimer = 0;
        }
        else
        {
            StartFade();
        }
    }


    // ========================================
    // 开始淡入
    // ========================================

    void StartFade()
    {
        times++;
        timer = 0;
        raycastState = 0;

        CollectTargets();

        if (threshold_transparent <= 0)
            threshold_transparent = 0.95f;

        // 瞬显
        if (clock <= 0)
        {
            SetAlphaAll(1f);
            SetRaycastAll(true);

            if (finalMode == FinalMode.Close && obj != null)
                obj.SetActive(false);

            onAllComplete?.Invoke();
            return;
        }

        SetAlphaAll(0f);

        if (raycastMode == RaycastMode.False)
        {
            SetRaycastAll(false);
        }
        else if (raycastMode == RaycastMode.ConditionalActive)
        {
            SetRaycastAll(false);
            raycastState = 1;
        }
        else
        {
            SetRaycastAll(true);
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeRoutine());
    }


    IEnumerator FadeRoutine()
    {
        while (timer < clock)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / clock);
            float alpha = Mathf.SmoothStep(0f, 1f, t);

            SetAlphaAll(alpha);

            if (raycastState == 1 && alpha >= threshold_transparent)
            {
                SetRaycastAll(true);
                raycastState = 0;
            }

            if (finalMode == FinalMode.Close && alpha > 0.99f && obj != null)
            {
                obj.SetActive(false);
            }

            yield return null;
        }

        fadeCoroutine = null;
        onAllComplete?.Invoke();
    }


    // ========================================
    // 工具方法
    // ========================================

    void SetAlphaAll(float a)
    {
        if (targets.Count > 0)
        {
            foreach (var g in targets)
            {
                if (g == null) continue;
                Color c = g.color; c.a = a; g.color = c;
            }
            return;
        }

        // 兜底：SpriteRenderer（非 UI）
        if (obj == null) return;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color; c.a = a; sr.color = c;
        }
    }


    void SetRaycastAll(bool value)
    {
        foreach (var g in targets)
            if (g != null)
                g.raycastTarget = value;
    }
}