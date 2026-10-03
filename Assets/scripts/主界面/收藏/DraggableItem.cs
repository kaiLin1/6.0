using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public int PrefabIndex { get; private set; } // 生成器告诉我：我是登记表里第几个预制体

    private RectTransform area;
    private RectTransform self;
    private float lastClickTime = -1f;
    private const float doubleClickInterval = 0.3f;

    public void Setup(RectTransform moveArea, int prefabIndex)
    {
        area = moveArea;
        self = GetComponent<RectTransform>();
        PrefabIndex = prefabIndex;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CollectionEditMode.IsEditing) return;
        self.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CollectionEditMode.IsEditing) return;

        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area, eventData.position, eventData.pressEventCamera, out local);

        Vector2 half = self.rect.size * 0.5f;
        local.x = Mathf.Clamp(local.x, area.rect.xMin + half.x, area.rect.xMax - half.x);
        local.y = Mathf.Clamp(local.y, area.rect.yMin + half.y, area.rect.yMax - half.y);

        self.anchoredPosition = local;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 拖完自动存档，这样切场景位置也在
        if (CollectionEditMode.IsEditing && DecorationPlacer.Instance != null)
            DecorationPlacer.Instance.SaveLayout();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CollectionEditMode.IsEditing) return;

        if (Time.unscaledTime - lastClickTime <= doubleClickInterval)
        {
            DecorationPlacer.Instance.Remove(gameObject);
            lastClickTime = -1f;
        }
        else
        {
            lastClickTime = Time.unscaledTime;
        }
    }
}