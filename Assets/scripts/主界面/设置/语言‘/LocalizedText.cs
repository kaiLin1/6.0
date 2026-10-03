using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Header("模式一：填 key（从总控字典取）")]
    [SerializeField] private string key;

    [Header("模式二：key 留空 → 用本地翻译（顺序对应语言列表）")]
    [SerializeField] private LocalizedString local;

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

        text.text = !string.IsNullOrEmpty(key)
            ? LanguageManager.Instance.Get(key)
            : local.Get(LanguageManager.Instance.Current);

        var font = LanguageManager.Instance.CurrentFont;
        if (font != null)
            text.font = font;
    }
}