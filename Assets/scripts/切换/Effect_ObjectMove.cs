using UnityEngine;
using UnityEngine.Events;

public class Effect_ObjectMove : MonoBehaviour
{
    [Header("移动设置")]
    public GameObject obj;                        // 要移动的物体（留空=本物体）
    public Vector3 targetPosition;                // 目标位置（直接填坐标）
    public bool useWorldPosition = true;          // true=世界坐标 false=相对本脚本的偏移
    public mode md = mode.move;
    public float speed = 1f;
    public float duration = 2f;
    public float jumpHeight = 2f;
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("触发设置")]
    public Trigger effect = Trigger.Directly;
    public float delayedClock = 0f;
    public bool playOnStart = false;

    [Header("⭐ 只触发一次（勾上=这局只能动一回；不勾=可反复触发）")]
    public bool triggerOnce = false;

    [Header("完成事件")]
    public UnityEvent onComplete;

    public enum mode { move, jump }
    public enum Trigger { Directly, Delayed }

    bool running;
    bool waiting;
    bool hasTriggered;      // 已经完整触发过（triggerOnce 用）
    float time;
    float waitTimer;
    Vector3 posA;
    Vector3 target;


    void Start()
    {
        if (!playOnStart) return;

        if (obj == null) obj = gameObject;

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

        if (!running || obj == null) return;

        time += Time.deltaTime;

        float t = Mathf.Clamp01(time / duration);
        float curveT = speedCurve.Evaluate(t);

        Vector3 flat = Vector3.Lerp(posA, target, curveT);

        if (md == mode.jump)
        {
            float arc = 4f * jumpHeight * curveT * (1f - curveT);
            obj.transform.position = flat + Vector3.up * arc;
        }
        else
        {
            obj.transform.position = flat;
        }

        if (time >= duration)
        {
            obj.transform.position = target;
            running = false;
            onComplete?.Invoke();
        }
    }


    // 给 Button、UnityEvent 调用
    public void TriggerEffect()
    {
        // 移动中再点：以当前位置为新起点重新算
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
        if (obj == null) obj = gameObject;

        // ⭐ 只触发一次的开关
        if (triggerOnce && hasTriggered) return;
        hasTriggered = true;

        // ⭐ 终点：世界坐标 or 相对本脚本位置的偏移
        target = useWorldPosition
            ? targetPosition
            : transform.position + targetPosition;

        // ⭐ speed → duration（每次按当前位置重算）
        if (speed > 0)
            duration = Vector3.Distance(obj.transform.position, target) / speed;

        posA = obj.transform.position;
        time = 0f;
        running = true;
        waiting = false;
    }
}