using System.Collections.Generic;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

    private HashSet<string> unlockedItems = new HashSet<string>();
    private const string SaveKey = "UnlockedItems";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    /// <summary>任何地方调用都保证拿到管理器，没有就自动创建一个</summary>
    public static CollectionManager GetInstance()
    {
        if (Instance == null)
        {
            var go = new GameObject("CollectionManager(Auto)");
            Instance = go.AddComponent<CollectionManager>();
            DontDestroyOnLoad(go);
            Debug.LogWarning("CollectionManager 不存在，已自动创建。建议还是在游玩界面放一个。");
        }
        return Instance;
    }

    public void Unlock(string itemID)
    {
        Debug.Log($"[解锁请求] ID = '{itemID}'");
        if (unlockedItems.Add(itemID))
        {
            Save();
            Debug.Log($"[解锁成功] {itemID}，当前已解锁: {string.Join(",", unlockedItems)}");
        }
        else
        {
            Debug.LogWarning($"[跳过] {itemID} 之前已经解锁过了");
        }
    }

    public bool IsUnlocked(string itemID)
    {
        return unlockedItems.Contains(itemID);
    }

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, string.Join(",", unlockedItems));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        unlockedItems.Clear();
        if (PlayerPrefs.HasKey(SaveKey))
        {
            string data = PlayerPrefs.GetString(SaveKey);
            Debug.Log($"[读取存档] {data}");
            if (!string.IsNullOrEmpty(data))
                foreach (string id in data.Split(','))
                    unlockedItems.Add(id);
        }
    }

    public void ClearSave()
    {
        unlockedItems.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        Debug.Log("存档已清空");
    }
}