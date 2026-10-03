using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class seeds : MonoBehaviour
{
    [System.Serializable]
    public class ChapterBox
    {
        public string chapterName = "Chapter 01";
        public Button button;              // 拖章节按钮
        public GameObject obj;             // 拖章节图片：Image 或包含 Image 的物体
    }

    [Header("章节设置")]
    public ChapterBox[] chapters = new ChapterBox[3];

    [Header("淡入淡出设置")]
    public float fadeInDuration = 1f;   // 淡入时间（秒）
    public float fadeOutDuration = 1f;  // 淡出时间（秒）

    [Header("其他")]
    public bool playOnStart;
    public UnityEvent onComplete;

    public int CurrentChapter => currentChapter;
    public bool IsSwitching => isSwitching;

    int currentChapter = -1;
    bool isSwitching;
    Coroutine currentRoutine;

    void Start()
    {
        // 初始状态：所有章节图片设为全透明且不可见，保证画面干干净净
        foreach (ChapterBox box in chapters)
            SetAlpha(box, 0f);

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
        if (isSwitching || index == currentChapter) return;

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SwitchChapter(index));
    }

    IEnumerator SwitchChapter(int targetIndex)
    {
        isSwitching = true;

        // 第一步：当前图片淡出（如果有点击过章节的话）
        if (currentChapter >= 0)
        {
            ChapterBox oldBox = chapters[currentChapter];
            if (oldBox.obj != null)
                yield return FadeObject(oldBox, 1f, 0f, fadeOutDuration);

            currentChapter = -1;
        }

        // 第二步：目标图片淡入
        ChapterBox newBox = chapters[targetIndex];
        if (newBox.obj != null)
        {
            yield return FadeObject(newBox, 0f, 1f, fadeInDuration);
            currentChapter = targetIndex;
        }

        isSwitching = false;
        currentRoutine = null;
        onComplete?.Invoke();
    }

    IEnumerator FadeObject(ChapterBox box, float startAlpha, float endAlpha, float fadeDuration)
    {
        // 立刻设成起始透明度，不闪帧
        SetAlpha(box, startAlpha);

        if (fadeDuration <= 0f)
        {
            SetAlpha(box, endAlpha);
            yield break;
        }

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float p = Mathf.Clamp01(timer / fadeDuration);
            SetAlpha(box, Mathf.Lerp(startAlpha, endAlpha, p));
            yield return null;
        }

        SetAlpha(box, endAlpha);
    }

    // 统一用 CanvasGroup 控制：一个组件淡整层，不管里面是几个 Image
    void SetAlpha(ChapterBox box, float alpha)
    {
        if (box.obj == null) return;

        CanvasGroup cg = box.obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = box.obj.AddComponent<CanvasGroup>();

        cg.alpha = alpha;
        cg.interactable = alpha >= 1f;      // 完全显示时才允许点击
        cg.blocksRaycasts = alpha >= 1f;    // 透明时不挡射线
    }
}