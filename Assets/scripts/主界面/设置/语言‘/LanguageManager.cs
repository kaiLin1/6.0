using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    [Header("⭐ 语言列表：顺序就是编号，第 0 项 = 默认/回落语言")]
    [SerializeField] private List<string> languageNames = new List<string>
    {
        "中文", "English", "日本語", "한국어", "Français"
    };

    [Header("每种语言对应的字体（顺序和上面一致，可留空）")]
    [SerializeField] private List<TMP_FontAsset> fonts = new List<TMP_FontAsset>();

    public int Current { get; private set; }
    public int Count => languageNames.Count;
    public System.Action OnLanguageChanged;

    // 当前语言该用的字体（没配就返回 null，文字用原字体）
    public TMP_FontAsset CurrentFont =>
        Current < fonts.Count ? fonts[Current] : null;

    // key → 各语言文本
    private readonly Dictionary<string, List<string>> table = new Dictionary<string, List<string>>();

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Current = PlayerPrefs.GetInt("Language", 0);
        Current = Mathf.Clamp(Current, 0, languageNames.Count - 1);
        BuildTable();
    }

        // ⭐ 按语言编号取字体（生成语言按钮时给每个按钮用）
    public TMP_FontAsset GetFont(int index) =>
        index >= 0 && index < fonts.Count ? fonts[index] : null;

    // ⭐ 静态 UI 文本：Add(key, 中文, 英文, 日文, 韩文, 法文)
    //    后面几种不填也行，会自动回落到中文
    void BuildTable()
    {
        Add("start_game", "开始游戏", "Start Game", "ゲーム開始", "게임 시작", "Jouer");
        Add("settings",   "设置",     "Settings",   "設定",       "설정",      "Paramètres");
        Add("collection", "收藏",     "Collection", "コレクション","컬렉션",    "Collection");
        Add("chapters",   "章节",     "Chapters",   "章",         "챕터",      "Chapitres");
        Add("back",       "返回",     "Back",       "戻る",       "뒤로",      "Retour");
        Add("master_vol", "总音量",   "Master",     "マスター",    "마스터",     "Principal");
        Add("bgm_vol",    "音乐",     "Music",      "音楽",       "음악",      "Musique");
        Add("voice_vol",  "旁白",     "Voice",      "ボイス",      "보이스",     "Voix");
        Add("sfx_vol",    "音效",     "SFX",        "効果音",      "효과음",     "Effets");
        Add("language",   "语言",     "Language",   "言語",       "언어",      "Langue");
        // …以后加 UI 文本在这里加一行
    }

    void Add(string key, params string[] translations)
    {
        table[key] = new List<string>(translations);
    }

    public string Get(string key)
    {
        if (table.TryGetValue(key, out List<string> texts))
        {
            if (Current < texts.Count && !string.IsNullOrEmpty(texts[Current]))
                return texts[Current];
            if (texts.Count > 0)
                return texts[0]; // 缺当前语言 → 回落默认语言
        }
        return key;
    }

    public string GetLanguageName(int index) => languageNames[index];

    public void SetLanguage(int index)
    {
        index = Mathf.Clamp(index, 0, languageNames.Count - 1);
        if (Current == index) return;

        Current = index;
        PlayerPrefs.SetInt("Language", index);
        OnLanguageChanged?.Invoke();
    }
}