using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedSubtitle : MonoBehaviour
{
    [Header("字幕内容（顺序对应语言列表，第 0 项是默认语言）")]
    [SerializeField] private LocalizedString line;

    [Header("当前语言没翻译时是否隐藏整条字幕")]
    [SerializeField] private bool hideIfEmpty = true;

    private TMP_Text text;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.OnLanguageChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.OnLanguageChanged -= Refresh;
    }

    void Refresh()
    {
        if (LanguageManager.Instance == null || text == null) return;

        string content = line.Get(LanguageManager.Instance.Current);

        if (string.IsNullOrEmpty(content) && hideIfEmpty)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        text.text = content;

        var font = LanguageManager.Instance.CurrentFont;
        if (font != null)
            text.font = font;
    }
}