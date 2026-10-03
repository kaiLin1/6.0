using UnityEngine;
using TMPro;
using System.Collections;

public class PauseController : MonoBehaviour
{
    [Header("暂停界面")]
    public GameObject pausePanel;

    [Header("文字条")]
    public RectTransform textBar;
    public TextMeshProUGUI text;

    [Header("文字内容")]
    [TextArea]
    public string message;

    [Header("文字条滑动")]
    public float slideDistance = 300f;
    public float slideTime = 0.8f;

    bool paused = false;

    Vector2 textEndPosition;
    Coroutine textBarCoroutine;


    void Start()
    {
        // 开始时关闭暂停界面
        if (pausePanel != null)
            pausePanel.SetActive(false);


        // 记录文字条正常位置
        if (textBar != null)
        {
            textEndPosition = textBar.anchoredPosition;

            // 开始时放到正常位置的上方
            textBar.anchoredPosition =
                textEndPosition +
                new Vector2(0, slideDistance);
        }


        // 设置文字
        if (text != null)
        {
            text.text = message;
        }
    }


    public void TogglePause()
    {
        if (paused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }


    // =========================
    // 开启暂停
    // =========================

    public void Pause()
    {
        if (paused)
            return;

        paused = true;


        // 打开暂停界面
        if (pausePanel != null)
            pausePanel.SetActive(true);


        // 暂停游戏
        Time.timeScale = 0;


        // 让文字条从上面滑下来
        StartTextBarAnimation(true);
    }


    // =========================
    // 关闭暂停
    // =========================

    public void Resume()
    {
        if (!paused)
            return;

        paused = false;


        // 让文字条先滑回去
        StartTextBarAnimation(false);


        // 恢复游戏
        Time.timeScale = 1;


        // 等文字条滑回去以后再关闭整个面板
        StartCoroutine(HidePanelAfterSlide());
    }


    // =========================
    // 暂停按钮调用
    // =========================

    public void ClickPause()
    {
        Pause();
    }


    // =========================
    // 一键切换按钮调用
    // =========================

    public void ClickTogglePause()
    {
        TogglePause();
    }


    // =========================
    // 开始文字条动画
    // =========================

    void StartTextBarAnimation(bool show)
    {
        if (textBar == null)
            return;


        if (textBarCoroutine != null)
        {
            StopCoroutine(textBarCoroutine);
        }


        textBarCoroutine =
            StartCoroutine(
                MoveTextBar(show)
            );
    }


    // =========================
    // 文字条移动
    // =========================

    IEnumerator MoveTextBar(bool show)
    {
        Vector2 startPosition;
        Vector2 targetPosition;


        if (show)
        {
            // 从上面开始
            startPosition =
                textEndPosition +
                new Vector2(0, slideDistance);

            // 滑到正常位置
            targetPosition =
                textEndPosition;
        }
        else
        {
            // 从正常位置开始
            startPosition =
                textBar.anchoredPosition;

            // 滑回上面
            targetPosition =
                textEndPosition +
                new Vector2(0, slideDistance);
        }


        textBar.anchoredPosition =
            startPosition;


        float timer = 0;


        while (timer < slideTime)
        {
            timer += Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer / slideTime
                );


            // 平滑动画
            t =
                Mathf.SmoothStep(
                    0,
                    1,
                    t
                );


            textBar.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );


            yield return null;
        }


        textBar.anchoredPosition =
            targetPosition;


        textBarCoroutine = null;
    }


    // =========================
    // 等文字条滑完再关闭Panel
    // =========================

    IEnumerator HidePanelAfterSlide()
    {
        yield return new WaitForSecondsRealtime(slideTime);


        if (!paused && pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }
}