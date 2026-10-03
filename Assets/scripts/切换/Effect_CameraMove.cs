using UnityEngine;
using UnityEngine.Events;

public class Effect_CameraMove : MonoBehaviour
{
    [Header("⭐ 相机要移动多少（相对当前位置的偏移）")]
    public Vector2 moveOffset;

    [Header("移动设置")]
    public mode md = mode.move;
    public float speed = 5f;                        // 移动速度（0 则用 duration）
    public float duration = 2f;                     // 固定时长（speed=0 时生效）
    public float jumpHeight = 2f;                   // jump 模式的抛物线高度
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("触发设置")]
    public Trigger effect = Trigger.Directly;
    public float delayedClock = 0f;
    public bool playOnStart = false;

    [Header("⭐ 只触发一次（勾上后这局只能动一回，防误触）")]
    public bool triggerOnce = false;

    [Header("完成事件")]
    public UnityEvent onComplete;

    public enum mode { move, jump }
    public enum Trigger { Directly, Delayed }

    Camera cam;
    bool running;       // 正在移动中
    bool hasTriggered;  // 已经完整触发过一次（triggerOnce 用）
    bool waiting;
    float time;
    float waitTimer;
    Vector3 posA;
    Vector3 target;


    void Awake()
    {
        cam = Camera.main;
    }


    void Start()
    {
        if (!playOnStart) return;

        if (effect == Trigger.Directly)
            Effect();
        else
            waiting = true;
    }


    void Update()
    {
        if (waiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= delayedClock)
            {
                waiting = false;
                Effect();
            }
        }

        if (!running || cam == null) return;

        time += Time.deltaTime;

        float t = Mathf.Clamp01(time / duration);
        float curveT = speedCurve.Evaluate(t);

        Vector3 flat = Vector3.Lerp(posA, target, curveT);

        if (md == mode.jump)
        {
            float arc = 4f * jumpHeight * curveT * (1f - curveT);
            cam.transform.position = flat + Vector3.up * arc;
        }
        else
        {
            cam.transform.position = flat;
        }

        if (time >= duration)
        {
            cam.transform.position = target;
            running = false;
            onComplete?.Invoke();
        }
    }


    // 给 Button、UnityEvent 调用
    public void TriggerEffect()
    {
        // ⭐ 移动中：忽略这次点击
        if (running || waiting) return;

        // ⭐ 只触发一次的开关
        if (triggerOnce && hasTriggered) return;

        if (effect == Trigger.Directly)
        {
            Effect();
        }
        else
        {
            waitTimer = 0f;
            waiting = true;
        }
    }


    public void Effect()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("没有找到 Main Camera");
                return;
            }
        }

        hasTriggered = true;

        // ⭐ 终点 = 当前位置 + 偏移
        target = cam.transform.position + new Vector3(moveOffset.x, moveOffset.y, 0f);

        // ⭐ speed → duration
        if (speed > 0)
            duration = Vector3.Distance(cam.transform.position, target) / speed;

        posA = cam.transform.position;
        time = 0f;
        running = true;
    }
}