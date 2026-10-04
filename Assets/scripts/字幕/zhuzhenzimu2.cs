using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class DialogueTypewriter : MonoBehaviour
{
    [System.Serializable]
    public class DialoguePage
    {
        public TextMeshProUGUI target;
        [TextArea(3, 8)]
        public string content;
    }

    [System.Serializable]
    public class DialogueRound
    {
        [Header("备注（如：A的第三轮台词）")]
        public string note;

        [Header("这一轮的句子")]
        public List<DialoguePage> pages = new List<DialoguePage>();

        [Header("⭐ 本轮全部打完后触发（可拖物体进来）")]
        public UnityEvent onRoundFinished;
    }

    [Header("轮次列表（新词就往下加一轮）")]
    public List<DialogueRound> rounds = new List<DialogueRound>();

    [Header("打字速度")]
    public float characterInterval = 0.05f;

    [Header("开始延迟")]
    public float delay = 0f;

    [Header("打字音效")]
    public AudioClip typingClip;
    [Range(0f, 1f)] public float typingVolume = 1f;

    [Header("⭐ 历史记录文本（文字超过框自动可滚动）")]
    public TextMeshProUGUI historyText;

    [Header("⭐ 任意一轮打完时触发（通知对方）")]
    public UnityEvent onAnyRoundFinished;

    [Header("⭐ 没有更多轮次时触发（所有话说完了）")]
    public UnityEvent onAllRoundsFinished;

    private AudioSource audioSource;

    private int currentRound = 0;
    private int pageIndex = 0;
    private bool roundFinishedFired = false;
    private bool isTyping = false;

    // ⭐ 已经打过字的话，之后不再重复打
    private readonly HashSet<DialoguePage> typedPages = new HashSet<DialoguePage>();

    // ============================================================
    // ⭐ 以下全部是 static：两个对话组件共享同一份历史，谁也不覆盖谁
    // ============================================================
    private static readonly HashSet<DialoguePage> recordedPages = new HashSet<DialoguePage>();
    private static readonly List<string> historyEntries = new List<string>();

    private DialogueScrollController historyScroll;


    // 进入播放模式时清空共享记录（防止编辑器里残留上一次的）
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitStatics()
    {
        recordedPages.Clear();
        historyEntries.Clear();
    }


    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ClearAllText();

        if (historyText != null)
            historyScroll = historyText.GetComponentInParent<DialogueScrollController>(true);
    }


    public void PlayNextDialogue()
    {
        if (isTyping)
            return;

        if (rounds.Count == 0) return;
        if (currentRound >= rounds.Count) return;

        DialogueRound round = rounds[currentRound];

        // ⭐ 跳过已经打过的话，找下一句还没打过的
        while (pageIndex < round.pages.Count &&
               typedPages.Contains(round.pages[pageIndex]))
        {
            pageIndex++;
        }

        // ⭐ 本轮全部打过了（兜底触发，正常已由 TypeDialogue 自动触发过）
        if (pageIndex >= round.pages.Count)
        {
            if (!roundFinishedFired)
            {
                roundFinishedFired = true;
                round.onRoundFinished?.Invoke();
                onAnyRoundFinished?.Invoke();
            }
            return;
        }

        StartCoroutine(TypeDialogue(round, round.pages[pageIndex]));

        pageIndex++;
    }


    public void UnlockNextRound()
    {
        if (!roundFinishedFired)
            return;

        if (currentRound + 1 < rounds.Count)
        {
            currentRound++;
            pageIndex = 0;
            roundFinishedFired = false;
        }
        else
        {
            onAllRoundsFinished?.Invoke();
        }
    }


    IEnumerator TypeDialogue(DialogueRound round, DialoguePage page)
    {
        isTyping = true;

        if (page.target == null)
        {
            isTyping = false;
            yield break;
        }

        page.target.text = "";

        if (delay > 0)
            yield return new WaitForSeconds(delay);

        foreach (char c in page.content)
        {
            page.target.text += c;

            if (typingClip != null && c != ' ')
                audioSource.PlayOneShot(typingClip, typingVolume);

            yield return new WaitForSeconds(characterInterval);
        }

        isTyping = false;

        // ⭐ 标记这句话已经打过，以后不再重复打
        typedPages.Add(page);

        // ⭐ 这句话完整打完了 → 迁移到历史记录（只记一次）
        RecordHistory(round, page);

        // ⭐ 打完这句后检查：本轮没有还没打过的句子 = 这是最后一句
        //    → 立刻触发，不用再点一次按钮
        bool isLastPageOfRound = true;
        foreach (var p in round.pages)
        {
            if (!typedPages.Contains(p))
            {
                isLastPageOfRound = false;
                break;
            }
        }

        if (isLastPageOfRound && !roundFinishedFired)
        {
            roundFinishedFired = true;
            round.onRoundFinished?.Invoke();
            onAnyRoundFinished?.Invoke();
        }
    }


    // ========================================
    // ⭐ 已打完的文字放进历史文本
    //    格式：备注：内容     段落之间空一行
    // ========================================
    void RecordHistory(DialogueRound round, DialoguePage page)
    {
        if (historyText == null) return;

        // 已经迁移过的直接跳过，保证不重复
        if (recordedPages.Contains(page)) return;
        recordedPages.Add(page);

        string content = page.content.Trim();
        if (string.IsNullOrEmpty(content)) return;

        // 有备注就写 “备注：内容”
        string note = (round != null) ? round.note.Trim() : "";
        string entry = string.IsNullOrEmpty(note)
            ? content
            : note + "：" + content;

        historyEntries.Add(entry);

        RefreshHistory();
    }


    // ========================================
    // ⭐ 重建历史文本，通知滚动区域在布局完成后保持阅读位置
    // ========================================
    void RefreshHistory()
    {
        bool followBottom = historyScroll != null && historyScroll.ShouldFollowBottom;
        // 段落与段落之间空一行
        historyText.text = string.Join("\n\n", historyEntries);
        if (historyScroll != null)
            historyScroll.ContentChanged(followBottom);
    }


    public void ClearAllText()
    {
        foreach (DialogueRound round in rounds)
            foreach (DialoguePage page in round.pages)
                if (page.target != null)
                    page.target.text = "";
    }


    public void ResetAll()
    {
        currentRound = 0;
        pageIndex = 0;
        roundFinishedFired = false;
        isTyping = false;

        typedPages.Clear();             // 重置后所有句子恢复可打
        recordedPages.Clear();
        historyEntries.Clear();

        if (historyText != null)
        {
            historyText.text = "";
            historyText.firstVisibleCharacter = 0;
            historyText.maxVisibleLines = int.MaxValue;
            if (historyScroll != null)
                historyScroll.ContentChanged(true);
        }

        ClearAllText();
    }
}