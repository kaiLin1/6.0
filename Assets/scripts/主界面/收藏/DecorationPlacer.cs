using System.Collections.Generic;
using UnityEngine;

public class DecorationPlacer : MonoBehaviour
{
    public static DecorationPlacer Instance { get; private set; }

    [Header("装扮允许拖拽活动的区域（RectTransform）")]
    public RectTransform placeArea;

    [Header("区域里最多同时存在几个装扮（-1 表示不限数量）")]
    public int maxCount = 5;

    [Header("所有装扮预制体登记表（按顺序，存档靠它认预制体）")]
    public List<GameObject> prefabRegistry = new List<GameObject>();

    private const string SaveKey = "DecorationLayout";
    private readonly List<GameObject> spawned = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadLayout(); // 进场景时恢复上次摆好的装扮
    }

    // ===== 生成 / 删除 / 清空 =====

    public void Place(GameObject prefab)
    {
        if (prefab == null) return;
        if (maxCount >= 0 && spawned.Count >= maxCount)
        {
            Debug.Log($"[生成] 已达上限 {maxCount} 个");
            return;
        }

        int index = prefabRegistry.IndexOf(prefab);
        if (index < 0)
        {
            Debug.LogError($"[生成] {prefab.name} 不在 prefabRegistry 里！把它拖进登记表");
            return;
        }

        Spawn(prefab, index, Vector2.zero);
        SaveLayout();
    }

    private GameObject Spawn(GameObject prefab, int prefabIndex, Vector2 pos)
    {
        GameObject item = Instantiate(prefab, placeArea);
        RectTransform rt = item.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;

        DraggableItem drag = item.GetComponent<DraggableItem>();
        if (drag == null) drag = item.AddComponent<DraggableItem>();
        drag.Setup(placeArea, prefabIndex); // 多传一个：记住自己是哪个预制体

        spawned.Add(item);
        return item;
    }

    public void Remove(GameObject item)
    {
        if (item == null) return;
        spawned.Remove(item);
        Destroy(item);
        SaveLayout(); // 删除后也存档
        Debug.Log($"[生成] 已删除，剩余 {spawned.Count} 个");
    }

    public void ClearAll()
    {
        foreach (var go in spawned)
            if (go != null) Destroy(go);
        spawned.Clear();
        SaveLayout();
    }

    // ===== 存档 / 读档 =====

    [System.Serializable]
    private class DecorationData
    {
        public int prefabIndex;
        public float x;
        public float y;
    }

    [System.Serializable]
    private class DecorationSave
    {
        public List<DecorationData> items = new List<DecorationData>();
    }

    public void SaveLayout()
    {
        DecorationSave save = new DecorationSave();
        foreach (var go in spawned)
        {
            if (go == null) continue;
            var drag = go.GetComponent<DraggableItem>();
            var rt = go.GetComponent<RectTransform>();
            save.items.Add(new DecorationData
            {
                prefabIndex = drag != null ? drag.PrefabIndex : -1,
                x = rt.anchoredPosition.x,
                y = rt.anchoredPosition.y
            });
        }
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    private void LoadLayout()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return;

        DecorationSave save = JsonUtility.FromJson<DecorationSave>(PlayerPrefs.GetString(SaveKey));
        if (save == null) return;

        foreach (var data in save.items)
        {
            if (data.prefabIndex < 0 || data.prefabIndex >= prefabRegistry.Count) continue;
            var prefab = prefabRegistry[data.prefabIndex];
            if (prefab == null) continue;

            Spawn(prefab, data.prefabIndex, new Vector2(data.x, data.y));
        }
        Debug.Log($"[生成] 恢复了 {spawned.Count} 个装扮");
    }
}