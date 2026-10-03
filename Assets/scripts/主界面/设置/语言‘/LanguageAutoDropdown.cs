using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanguageAutoDropdown : MonoBehaviour
{
    [Header("点开/收起的按钮")]
    [SerializeField] private Button openButton;

    [Header("按钮上显示文字的 TMP")]
    [SerializeField] private TMP_Text openButtonLabel;

    [Header("弹出的面板（先勾掉激活）")]
    [SerializeField] private GameObject panel;

    [Header("面板里的按钮模板（勾掉激活）")]
    [SerializeField] private Button buttonTemplate;

    void Start()
    {
        panel.SetActive(false);
        openButton.onClick.AddListener(Toggle);

        // 从总控自动读出所有语言，生成一排按钮
        buttonTemplate.gameObject.SetActive(false);
        int count = LanguageManager.Instance.Count;
        for (int i = 0; i < count; i++)
        {
            int index = i;
            string langName = LanguageManager.Instance.GetLanguageName(i);
            CreateButton(langName, index, () => Select(index, langName));
        }

        UpdateLabel();
        LanguageManager.Instance.OnLanguageChanged += UpdateLabel;
    }

    void OnDestroy()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.OnLanguageChanged -= UpdateLabel;
    }

    void Toggle()
    {
        bool show = !panel.activeSelf;
        panel.SetActive(show);
        if (show) panel.transform.SetAsLastSibling();
    }

    void Select(int index, string langName)
    {
        LanguageManager.Instance.SetLanguage(index);
        openButtonLabel.text = langName;

        // ⭐ 主按钮的文字也换成当前语言对应的字体
        var font = LanguageManager.Instance.GetFont(index);
        if (font != null)
            openButtonLabel.font = font;

        panel.SetActive(false);
    }

    void UpdateLabel()
    {
        int cur = LanguageManager.Instance.Current;
        openButtonLabel.text = LanguageManager.Instance.GetLanguageName(cur);

        var font = LanguageManager.Instance.GetFont(cur);
        if (font != null)
            openButtonLabel.font = font;
    }

    // ⭐ 生成选项按钮：文字 = 语言名，字体 = 该语言自己的字体
    void CreateButton(string label, int langIndex, UnityEngine.Events.UnityAction onClick)
    {
        Button btn = Instantiate(buttonTemplate, panel.transform);
        btn.gameObject.SetActive(true);

        TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
        txt.text = label;

        var font = LanguageManager.Instance.GetFont(langIndex);
        if (font != null)
            txt.font = font;

        btn.onClick.AddListener(onClick);
    }
}