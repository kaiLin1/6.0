using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoDropdown : MonoBehaviour
{
    public enum ListType { 分辨率, 全屏窗口化 }
    [Header("这个列表是哪种")]
    [SerializeField] private ListType listType = ListType.分辨率;

    [Header("点开/收起的按钮")]
    [SerializeField] private Button openButton;

    [Header("按钮上显示文字的 TMP（选了之后会显示当前选项）")]
    [SerializeField] private TMP_Text openButtonLabel;

    [Header("弹出的面板（先勾掉激活）")]
    [SerializeField] private GameObject panel;

    [Header("面板里的按钮模板（勾掉激活）")]
    [SerializeField] private Button buttonTemplate;

    [Header("只有【分辨率】模式要拖：内容根节点")]
    [SerializeField] private RectTransform content;

    [Header("基准分辨率")]
    [SerializeField] private float baseWidth  = 1920f;
    [SerializeField] private float baseHeight = 1080f;

    // ⭐ 分辨率档位，想改就改这里
    private readonly (int w, int h)[] resolutions =
    {
        (1280, 720), (1366, 768), (1600, 900),
        (1920, 1080), (2560, 1440), (3840, 2160),
    };

    private int currentRes;
    private bool fullscreen;

    void Start()
    {
        // 恢复上次的选择
        currentRes = PlayerPrefs.GetInt("ResolutionIndex", 3);
        fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        // 收起 + 点按钮开合
        panel.SetActive(false);
        openButton.onClick.AddListener(Toggle);

        // 按类型生成按钮
        if (listType == ListType.分辨率)
        {
            for (int i = 0; i < resolutions.Length; i++)
            {
                int index = i;
                string label = $"{resolutions[i].w} × {resolutions[i].h}";
                CreateButton(label, () =>
                {
                    currentRes = index;
                    ApplyResolution();
                    SelectAndClose(label);   // ⭐ 按钮显示选项 + 收起
                });
            }

            openButtonLabel.text = $"{resolutions[currentRes].w} × {resolutions[currentRes].h}";
        }
        else // 全屏窗口化
        {
            CreateButton("全屏", () =>
            {
                fullscreen = true;
                ApplyResolution();
                SelectAndClose("全屏");
            });
            CreateButton("窗口化", () =>
            {
                fullscreen = false;
                ApplyResolution();
                SelectAndClose("窗口化");
            });

            openButtonLabel.text = fullscreen ? "全屏" : "窗口化";
        }
    }

    void Toggle()
    {
        bool show = !panel.activeSelf;
        panel.SetActive(show);
        if (show) panel.transform.SetAsLastSibling(); // 打开时盖在最上面
    }

    // ⭐ 选中：按钮文字换掉 + 面板收起
    void SelectAndClose(string label)
    {
        openButtonLabel.text = label;
        panel.SetActive(false);
    }

    void CreateButton(string label, UnityEngine.Events.UnityAction onClick)
    {
        buttonTemplate.gameObject.SetActive(false);
        Button btn = Instantiate(buttonTemplate, panel.transform);
        btn.gameObject.SetActive(true);
        btn.GetComponentInChildren<TMP_Text>().text = label;
        btn.onClick.AddListener(onClick);
    }

    void ApplyResolution()
    {
        currentRes = Mathf.Clamp(currentRes, 0, resolutions.Length - 1);
        var r = resolutions[currentRes];

        Screen.SetResolution(r.w, r.h, fullscreen);

        PlayerPrefs.SetInt("ResolutionIndex", currentRes);
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);

        if (content != null)
            StartCoroutine(ReapplyNextFrame());
    }

    IEnumerator ReapplyNextFrame()
    {
        yield return null;
        float scale = Mathf.Min(Screen.width / baseWidth, Screen.height / baseHeight);
        content.sizeDelta  = new Vector2(baseWidth, baseHeight);
        content.localScale = new Vector3(scale, scale, 1f);
    }
}