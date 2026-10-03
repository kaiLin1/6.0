using System.Collections.Generic;

[System.Serializable]
public class LocalizedString
{
    // 顺序对应 LanguageManager 的语言列表：0=第0项, 1=第1项…
    // 某项留空 = 自动回落到第 0 项
    public List<string> texts = new List<string>();

    public string Get(int languageIndex)
    {
        if (texts.Count == 0) return "";
        if (languageIndex < texts.Count && !string.IsNullOrEmpty(texts[languageIndex]))
            return texts[languageIndex];
        return texts[0];
    }
}