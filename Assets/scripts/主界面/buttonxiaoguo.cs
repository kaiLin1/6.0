using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 按钮悬停/按下反馈（平滑缩放 + 可选旋转）
/// ------------------------------------------------------------
/// 用法：挂在 Button（或任何带图形的 UI）所在物体上，无需绑定。
/// 效果：
///   鼠标移入 → 平滑放大 + 可选倾斜/旋转
///   按下     → 再缩一点
///   移出/松开 → 平滑还原
/// 与 Button 本身的颜色过渡不冲突。
/// </summary>
public class ButtonHoverFX : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("缩放")]
    public float hoverScale = 1.12f;    // 悬停放大到多少倍
    public float pressScale = 0.94f;    // 按下时相对悬停再缩多少
    public float lerpSpeed = 12f;       // 平滑速度（越大越跟手）

    [Header("旋转")]
    public bool enableRotate = false;   // 总开关
    public RotateMode rotateMode = RotateMode.Tilt;

    [Tooltip("倾斜模式：悬停时倾斜的角度（度），移出还原")]
    public float tiltAngle = -5f;

    [Tooltip("旋转模式：悬停时每秒钟转的度数")]
    public float spinSpeed = 90f;

    public enum RotateMode { Tilt, Spin }


    // ==================== 运行时 ====================
    Vector3 baseScale = Vector3.one;
    float targetScale = 1f;
    float currentScale = 1f;

    float targetRotZ = 0f;
    float currentRotZ = 0f;
    float spinRotZ = 0f;

    bool hovering;
    bool pressing;


    void Start()
    {
        baseScale = transform.localScale;
        currentScale = 1f;
    }


    void Update()
    {
        // ---- 缩放目标 ----
        targetScale = hovering ? (pressing ? pressScale : hoverScale) : 1f;
        currentScale = Mathf.Lerp(currentScale, targetScale, lerpSpeed * Time.deltaTime);
        transform.localScale = baseScale * currentScale;

        // ---- 旋转目标 ----
        if (enableRotate)
        {
            if (rotateMode == RotateMode.Tilt)
            {
                targetRotZ = hovering ? tiltAngle : 0f;
            }
            else // Spin：悬停时持续转，移出停在原位
            {
                if (hovering) spinRotZ += spinSpeed * Time.deltaTime;
                targetRotZ = spinRotZ;
            }
        }
        else
        {
            targetRotZ = 0f;
        }

        currentRotZ = Mathf.LerpAngle(currentRotZ, targetRotZ, lerpSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(0, 0, currentRotZ);
    }


    // ============================================================
    //  事件（无需手动绑定，脚本自动接收）
    // ============================================================
    public void OnPointerEnter(PointerEventData e) { hovering = true; }
    public void OnPointerExit(PointerEventData e)  { hovering = false; pressing = false; }
    public void OnPointerDown(PointerEventData e)  { pressing = true; }
    public void OnPointerUp(PointerEventData e)    { pressing = false; }
}