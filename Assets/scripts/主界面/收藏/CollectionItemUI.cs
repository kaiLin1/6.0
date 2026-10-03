using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CollectionItemUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public string itemID;
    public Sprite lockedSprite;
    public GameObject decorationPrefab;

    [Header("悬停反馈：变大的倍数和旋转角度")]
    public float hoverScale = 1.1f;
    public float hoverRotation = 5f;
    public float animTime = 0.1f;

    private Image img;
    private Sprite originalSprite;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private Coroutine animCoroutine;

    private void Awake()
    {
        img = GetComponent<Image>();
        if (img != null)
            originalSprite = img.sprite;

        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        if (img != null) Refresh();
    }

    public void Refresh()
    {
        bool unlocked = CollectionManager.GetInstance().IsUnlocked(itemID);
        if (img != null)
            img.sprite = unlocked ? originalSprite : lockedSprite;
    }

    // ===== 悬停反馈动画 =====

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 未解锁或编辑模式关 → 悬停没反应
        if (!CollectionEditMode.IsEditing) return;
        if (!CollectionManager.GetInstance().IsUnlocked(itemID)) return;

        PlayAnim(baseScale * hoverScale, baseRotation * Quaternion.Euler(0, 0, hoverRotation));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayAnim(baseScale, baseRotation);
    }

    private void PlayAnim(Vector3 targetScale, Quaternion targetRot)
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimRoutine(targetScale, targetRot));
    }

    private System.Collections.IEnumerator AnimRoutine(Vector3 targetScale, Quaternion targetRot)
    {
        Vector3 startScale = transform.localScale;
        Quaternion startRot = transform.localRotation;
        float t = 0f;

        while (t < animTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / animTime);
            k = 1f - (1f - k) * (1f - k); // 加速曲线，收回来的时候更利落

            transform.localScale = Vector3.Lerp(startScale, targetScale, k);
            transform.localRotation = Quaternion.Lerp(startRot, targetRot, k);
            yield return null;
        }

        transform.localScale = targetScale;
        transform.localRotation = targetRot;
    }

    // ===== 点击逻辑不变 =====

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CollectionEditMode.IsEditing) return;
        if (!CollectionManager.GetInstance().IsUnlocked(itemID)) return;

        if (decorationPrefab == null)
        {
            Debug.LogWarning("→ decorationPrefab 没拖预制体");
            return;
        }

        if (DecorationPlacer.Instance == null)
        {
            Debug.LogError("→ 场景里没有 DecorationPlacer！");
            return;
        }

        DecorationPlacer.Instance.Place(decorationPrefab);
    }
}