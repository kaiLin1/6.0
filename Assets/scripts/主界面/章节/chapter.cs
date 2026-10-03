using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ChapterObjectController : MonoBehaviour
{
    [System.Serializable]
    public class ChapterBox
    {
        public string chapterName = "Chapter 01";
        public Button button;              // 拖章节按钮
        public GameObject obj;             // 拖场景里的箱子
        public Vector3 enterOffset;        // 进场相对偏移（如 0, 300, 0）
        public Vector3 exitOffset;         // 出场相对偏移（如 0, -300, 0）
        public bool useWorldPosition = true;
    }

    public enum MoveMode { Move, Jump }

    [Header("章节设置")]
    public ChapterBox[] chapters = new ChapterBox[3];

    [Header("移动设置")]
    public MoveMode moveMode = MoveMode.Move;
    public float speed = 1f;
    public float duration = 2f;
    public float jumpHeight = 2f;
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("其他")]
    public bool playOnStart;
    public UnityEvent onComplete;

    public int CurrentChapter => currentChapter;
    public bool IsMoving => isMoving;

    int currentChapter = -1;
    bool isMoving;
    Coroutine currentRoutine;

    void Start()
    {
        for (int i = 0; i < chapters.Length; i++)
        {
            int index = i;
            if (chapters[i].button != null)
                chapters[i].button.onClick.AddListener(() => PlayChapter(index));
        }

        if (playOnStart) PlayChapter(0);
    }

    public void PlayChapter(int index)
    {
        if (index < 0 || index >= chapters.Length) return;
        if (isMoving || index == currentChapter) return;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchChapter(index));
    }

    IEnumerator SwitchChapter(int targetIndex)
    {
        isMoving = true;

        // 第一步：旧箱子按偏移量移出（从当前位置开始）
        if (currentChapter >= 0)
        {
            ChapterBox oldBox = chapters[currentChapter];
            if (oldBox.obj != null)
                yield return MoveObject(oldBox, oldBox.exitOffset);

            currentChapter = -1;
        }

        // 第二步：新箱子从当前位置按偏移量进场
        ChapterBox newBox = chapters[targetIndex];
        if (newBox.obj != null)
        {
            yield return MoveObject(newBox, newBox.enterOffset);
            currentChapter = targetIndex;
        }

        isMoving = false;
        currentRoutine = null;
        onComplete?.Invoke();
    }

    // offset 为相对位移：目标 = 移动开始时的位置 + offset
    IEnumerator MoveObject(ChapterBox box, Vector3 offset)
    {
        Vector3 start = GetPosition(box);
        Vector3 target = start + offset;

        float finalDuration = speed > 0f ? duration / speed : 0f;
        if (finalDuration <= 0f)
        {
            SetPosition(box, target);
            yield break;
        }

        for (float timer = 0f; timer < finalDuration; timer += Time.deltaTime)
        {
            float p = speedCurve.Evaluate(Mathf.Clamp01(timer / finalDuration));
            Vector3 pos = Vector3.LerpUnclamped(start, target, p);

            if (moveMode == MoveMode.Jump)
                pos.y += 4f * jumpHeight * p * (1f - p);

            SetPosition(box, pos);
            yield return null;
        }

        SetPosition(box, target);
    }

    Vector3 GetPosition(ChapterBox box) =>
        box.useWorldPosition ? box.obj.transform.position : box.obj.transform.localPosition;

    void SetPosition(ChapterBox box, Vector3 pos)
    {
        if (box.useWorldPosition) box.obj.transform.position = pos;
        else box.obj.transform.localPosition = pos;
    }
}