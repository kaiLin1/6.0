using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 绘本角色图鉴（Unity 版）v2 —— 修复版
/// ------------------------------------------------------------
/// 用法：
/// 1. Canvas 下建空 UI 物体拉大（建议 900×750 以上），挂本脚本。
/// 2. Inspector 填角色列表、仓库道具；场景里有 EventSystem。
/// 3. Play 后自动生成圆阵、中心花、小屋弹窗、仓库。
/// ------------------------------------------------------------
/// v2 修复：头像/图标圆形裁剪、文字被背景盖住、仓库溢出、
///          名字压头像、图标尺寸错误等。
/// </summary>
public class CharacterCodex : MonoBehaviour
{
    // ==================== 数据 ====================
    [Serializable]
    public class CodexItem
    {
        public string itemName;
        [TextArea(2, 5)] public string desc;
        public Sprite img;          // 留空 = 显示名字占位
    }

    [Serializable]
    public class PlacedItem
    {
        public CodexItem item;
        public Vector2 pos;         // 小屋内的位置，0~100（左上为 0,0）
    }

    [Serializable]
    public class Character
    {
        public string charName;
        public string tag;
        public Sprite avatar;       // 留空 = 显示"头像"占位
        [TextArea(2, 6)] public string desc;
        public Sprite room;         // 小屋场景图，留空 = 占位色块
        public List<PlacedItem> items = new List<PlacedItem>();
        [HideInInspector] public List<PlacedItem> placed = new List<PlacedItem>();
    }

    // ==================== Inspector ====================
    [Header("角色圆阵")]
    public List<Character> characters = new List<Character>();
    public float radius = 260f;
    public float scatterDistance = 300f;
    public float orbSize = 150f;
    public float lerpSpeed = 9f;

    [Header("仓库（固定格子数）")]
    public List<CodexItem> warehouse = new List<CodexItem>();
    public int warehouseSlots = 15;

    [Header("⭐ 中文字体（必须拖一个中文字体资源，否则全是方框）")]
    public TMP_FontAsset chineseFont;

    [Header("⭐ 中心花图标（❀ 显示不了就换成 * 或你自己画的图）")]
    public string flowerIcon = "❀";

    [Header("⭐ 自定义圆形底图（留空 = 程序生成圆点）")]
    public Sprite circleArt;

    [Header("配色（对应网页绘本风）")]
    public Color paperColor   = new Color(1f, 0.992f, 0.957f);
    public Color redColor     = new Color(0.71f, 0.29f, 0.23f);
    public Color redSoftColor = new Color(0.79f, 0.42f, 0.35f);
    public Color yellowColor  = new Color(0.95f, 0.79f, 0.30f);

    // ==================== 运行时 ====================
    [Serializable] class SavedPin { public int itemIndex; public Vector2 pos; }
    [Serializable] class SavedPinList { public List<SavedPin> pins = new List<SavedPin>(); }

    class Orb
    {
        public RectTransform rt;
        public CanvasGroup canvasGroup;
        public CodexOrb ctrl;
        public Vector2 dir;
        public float phase;
        public float hoverScale = 1f;
    }

    Canvas canvas;
    readonly List<Orb> orbs = new List<Orb>();

    float progress = 1f;
    float target = 1f;

    GameObject popup;
    RectTransform roomRt;
    TMP_Text popupTitle, popupTag, popupDesc;
    Image roomImage;
    Transform pinRoot, whRoot;
    TMP_Text flowerHint;
    Character current;
    CodexItem dragItem;
    GameObject ghost;


    // ============================================================
    //  生成圆形贴图
    // ============================================================
    static Sprite circleSprite;
    static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        float c = (s - 1) / 2f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                tex.SetPixel(x, y, d <= c - 1 ? Color.white : Color.clear);
            }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        circleSprite.name = "CodexCircle";
        return circleSprite;
    }

    Sprite CircleArt() => circleArt != null ? circleArt : GetCircleSprite();


    // ============================================================
    //  构建
    // ============================================================
    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("CharacterCodex 必须放在 Canvas 下"); return; }

        // ⭐ 没填角色就先生成 6 个占位，填了自己的就忽略这段
        if (characters == null) characters = new List<Character>();
        if (characters.Count == 0)
        {
            Debug.Log("CharacterCodex：Characters 为空，已生成 6 个占位角色。");
            for (int i = 1; i <= 6; i++)
            {
                characters.Add(new Character
                {
                    charName = "角色" + i,
                    tag = "占位",
                    avatar = null,
                    room = null,
                    desc = "在 Inspector 的 Characters 里替换成你的角色。",
                    items = new List<PlacedItem>()
                });
            }
        }
        if (warehouse == null) warehouse = new List<CodexItem>();

        BuildFlower();
        BuildOrbs();
        BuildPopup();
    }


    void BuildFlower()
    {
        var go = CreateUI("CenterFlower", transform);
        var rt = go.rectTransform();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(190, 190);

        var bg = go.AddComponent<Image>();
        bg.sprite = CircleArt();
        bg.color = new Color(1f, 1f, 1f, 0.25f);

        var btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        var icon = CreateText(go.transform, flowerIcon, 64, TextAlignmentOptions.Center, redSoftColor);
        icon.raycastTarget = false;
        icon.rectTransform().anchorMin = icon.rectTransform().anchorMax = new Vector2(0.5f, 0.62f);
        icon.rectTransform().pivot = new Vector2(0.5f, 0.5f);

        flowerHint = CreateText(go.transform, "点我散开", 15, TextAlignmentOptions.Center, redSoftColor);
        flowerHint.raycastTarget = false;
        flowerHint.rectTransform().anchorMin = flowerHint.rectTransform().anchorMax = new Vector2(0.5f, 0.3f);
        flowerHint.rectTransform().pivot = new Vector2(0.5f, 0.5f);

        btn.onClick.AddListener(() =>
        {
            Debug.Log("🌸 中心花被点击，target = " + target);
            target = target > 0.5f ? 0f : 1f;
            flowerHint.text = target > 0.5f ? "点我散开" : "点我围拢";
        });
    }


    void BuildOrbs()
    {
        int n = characters.Count;
        if (n == 0) return;

        for (int i = 0; i < n; i++)
        {
            var c = characters[i];
            float angle = Mathf.PI * 2f / n * i - Mathf.PI / 2f;
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            var go = CreateUI("Orb_" + c.charName, transform);
            var rt = go.rectTransform();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(orbSize, orbSize);

            var cg = go.AddComponent<CanvasGroup>();

            // ⭐ 透明点击层：点击脚本在这个物体上，必须有图形接住射线
            var hit = go.AddComponent<Image>();
            hit.sprite = CircleArt();
            hit.color = new Color(1f, 1f, 1f, 0.01f);

            // 外圈红边
            var ring = CreateUI("Ring", go.transform);
            ring.rectTransform().Stretch();
            var ringImg = ring.AddComponent<Image>();
            ringImg.sprite = CircleArt();
            ringImg.color = redSoftColor;
            ringImg.raycastTarget = false;

            // 内圈纸色（比外圈小一圈）
            var face = CreateUI("Face", go.transform);
            face.rectTransform().Stretch(Vector2.zero, Vector2.one, Vector2.one * 4f);
            var faceImg = face.AddComponent<Image>();
            faceImg.sprite = CircleArt();
            faceImg.color = paperColor;
            faceImg.raycastTarget = false;

            // 头像（圆形裁剪：Mask）
            if (c.avatar != null)
            {
                var av = CreateUI("Avatar", face.transform);
                av.rectTransform().Stretch(Vector2.zero, Vector2.one, Vector2.one * 10f);
                var avImg = av.AddComponent<Image>();
                avImg.sprite = CircleArt();
                avImg.color = Color.white;
                var avMask = av.AddComponent<Mask>();
                avMask.showMaskGraphic = false;

                var pic = CreateUI("Pic", av.transform);
                pic.rectTransform().Stretch();
                var picImg = pic.AddComponent<Image>();
                picImg.sprite = c.avatar;
                picImg.preserveAspect = true;
                picImg.color = Color.white;
                picImg.raycastTarget = false;
            }
            else
            {
                var ph = CreateText(face.transform, "头像", 16, TextAlignmentOptions.Center, redSoftColor);
                ph.raycastTarget = false;
                ph.rectTransform().Stretch();
            }

            // 名字 / 标签（放在圆的下部，不压头像）
            var nameT = CreateText(go.transform, c.charName, 18, TextAlignmentOptions.Center, redColor);
            nameT.raycastTarget = false;
            nameT.rectTransform().anchorMin = nameT.rectTransform().anchorMax =
                nameT.rectTransform().pivot = new Vector2(0.5f, 0.5f);
            nameT.rectTransform().anchoredPosition = new Vector2(0, -50f);

            var tagT = CreateText(go.transform, c.tag, 12, TextAlignmentOptions.Center, redSoftColor);
            tagT.raycastTarget = false;
            tagT.rectTransform().anchorMin = tagT.rectTransform().anchorMax =
                tagT.rectTransform().pivot = new Vector2(0.5f, 0.5f);
            tagT.rectTransform().anchoredPosition = new Vector2(0, -67f);

            var ctrl = go.AddComponent<CodexOrb>();
            int index = i;
            ctrl.onClick = () => OpenRoom(characters[index]);

            orbs.Add(new Orb
            {
                rt = rt,
                canvasGroup = cg,
                ctrl = ctrl,
                dir = dir,
                phase = i * 0.7f
            });
        }
    }


    void BuildPopup()
    {
        // 遮罩
        popup = CreateUI("RoomPopup", transform);
        var maskRt = popup.rectTransform();
        maskRt.Stretch();
        var maskImg = popup.AddComponent<Image>();
        maskImg.color = new Color(0.35f, 0.2f, 0.16f, 0.4f);
        var maskBtn = popup.AddComponent<Button>();
        maskBtn.transition = Selectable.Transition.None;
        maskBtn.onClick.AddListener(CloseRoom);

        // 窗口
        var win = CreateUI("Window", popup.transform);
        var winRt = win.rectTransform();
        winRt.anchorMin = winRt.anchorMax = winRt.pivot = new Vector2(0.5f, 0.5f);
        winRt.sizeDelta = new Vector2(900, 720);
        var winImg = win.AddComponent<Image>();
        winImg.color = paperColor;

        // 标题
        popupTitle = CreateText(win.transform, "", 32, TextAlignmentOptions.Left, redColor);
        popupTitle.raycastTarget = false;
        popupTitle.rectTransform().SetTopLeft(30, -22, 500, 50);

        // 标签（背景是单独一层，不会盖住字）
        var tagBg = CreateUI("TagBg", win.transform);
        tagBg.rectTransform().SetTopLeft(30, -74, 130, 30);
        var tagBgImg = tagBg.AddComponent<Image>();
        tagBgImg.color = yellowColor;
        tagBgImg.raycastTarget = false;

        popupTag = CreateText(tagBg.transform, "", 15, TextAlignmentOptions.Center, redColor);
        popupTag.raycastTarget = false;
        popupTag.rectTransform().Stretch();

        popupDesc = CreateText(win.transform, "", 16, TextAlignmentOptions.TopLeft, redSoftColor);
        popupDesc.raycastTarget = false;
        popupDesc.rectTransform().SetTopLeft(30, -112, 840, 56);

        // 小屋场景
        var room = CreateUI("Room", win.transform);
        roomRt = room.rectTransform();
        roomRt.SetTopLeft(30, -180, 840, 340);
        roomImage = room.AddComponent<Image>();
        roomImage.color = paperColor;
        var roomBorder = room.AddComponent<Outline>();
        roomBorder.effectColor = redSoftColor;
        roomBorder.effectDistance = new Vector2(2, -2);

        var roomPh = CreateText(room.transform, "小屋场景占位", 16, TextAlignmentOptions.Center, redSoftColor);
        roomPh.raycastTarget = false;
        roomPh.rectTransform().Stretch();

        pinRoot = new GameObject("Pins", typeof(RectTransform)).transform;
        pinRoot.SetParent(roomRt, false);
        pinRoot.rectTransform().Stretch();

        // 仓库
        var whTitle = CreateText(win.transform, "仓 库 · 按住拖进小屋装饰", 15, TextAlignmentOptions.Left, redSoftColor);
        whTitle.raycastTarget = false;
        whTitle.rectTransform().SetTopLeft(30, -530, 400, 30);

        var wh = CreateUI("Warehouse", win.transform);
        var whRt = wh.rectTransform();
        whRt.SetTopLeft(30, -562, 840, 66);
        var hlg = wh.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = hlg.childControlHeight = false;
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
        whRoot = whRt;

        BuildWarehouseSlots();

        // 关闭按钮
        var closeBg = CreateUI("CloseBg", win.transform);
        closeBg.rectTransform().SetTopRight(-16, -12, 44, 44);
        var closeImg = closeBg.AddComponent<Image>();
        closeImg.sprite = CircleArt();
        closeImg.color = new Color(1f, 1f, 1f, 0.35f);
        var closeBtn = closeBg.AddComponent<Button>();
        closeBtn.transition = Selectable.Transition.None;
        closeBtn.onClick.AddListener(CloseRoom);

        var closeT = CreateText(closeBg.transform, "×", 24, TextAlignmentOptions.Center, redColor);
        closeT.raycastTarget = false;
        closeT.rectTransform().Stretch();

        popup.SetActive(false);
    }


    void BuildWarehouseSlots()
    {
        for (int i = 0; i < warehouseSlots; i++)
        {
            CodexItem item = i < warehouse.Count ? warehouse[i] : null;

            var slot = CreateUI("Slot_" + i, whRoot);
            slot.rectTransform().sizeDelta = new Vector2(48, 48);

            var img = slot.AddComponent<Image>();
            img.sprite = CircleArt();
            img.color = (item != null && item.img != null) ? Color.white : paperColor;

            if (item != null && item.img != null)
            {
                // 圆形裁剪
                var maskGo = CreateUI("Mask", slot.transform);
                maskGo.rectTransform().Stretch();
                var mImg = maskGo.AddComponent<Image>();
                mImg.sprite = CircleArt();
                mImg.color = Color.white;
                var m = maskGo.AddComponent<Mask>();
                m.showMaskGraphic = false;

                var pic = CreateUI("Pic", maskGo.transform);
                pic.rectTransform().Stretch();
                var picImg = pic.AddComponent<Image>();
                picImg.sprite = item.img;
                picImg.preserveAspect = true;
                picImg.color = Color.white;
                picImg.raycastTarget = false;
            }
            else
            {
                var t = CreateText(slot.transform, item != null ? item.itemName : "空", 11,
                    TextAlignmentOptions.Center, redSoftColor);
                t.raycastTarget = false;
                t.rectTransform().Stretch();
            }

            if (item == null) continue;

            var drag = slot.AddComponent<CodexDrag>();
            drag.onDown = e => BeginGhostDrag(item, e);
            drag.onDrag = MoveGhostDrag;
            drag.onUp = EndGhostDrag;
        }
    }


    // ============================================================
    //  圆阵动画
    // ============================================================
    void Update()
    {
        progress += (target - progress) * lerpSpeed * Time.deltaTime;
        if (Mathf.Abs(target - progress) < 0.001f) progress = target;

        float t = Time.time;
        float dist = radius + scatterDistance * (1f - progress);
        float baseScale = 0.35f + 0.65f * progress;

        foreach (var orb in orbs)
        {
            float floatY = Mathf.Sin(t * 1.2f + orb.phase) * 7f;

            orb.rt.anchoredPosition = orb.dir * dist + new Vector2(0, floatY);
            orb.canvasGroup.alpha = progress;

            orb.hoverScale = Mathf.Lerp(orb.hoverScale, orb.ctrl.hover ? 1.14f : 1f, 12f * Time.deltaTime);
            orb.rt.localScale = Vector3.one * baseScale * orb.hoverScale;
        }
    }


    // ============================================================
    //  小屋弹窗 + 道具
    // ============================================================
    void OpenRoom(Character c)
    {
        current = c;
        if (c.placed == null) c.placed = new List<PlacedItem>();
        LoadPins(c);

        popupTitle.text = c.charName + " 的小屋";
        popupTag.text = c.tag;
        popupDesc.text = c.desc;

        if (c.room != null)
        {
            roomImage.sprite = c.room;
            roomImage.preserveAspect = false;
            roomImage.color = Color.white;
        }
        else
        {
            roomImage.sprite = null;
            roomImage.color = paperColor;
        }

        foreach (Transform child in pinRoot) Destroy(child.gameObject);
        foreach (var it in c.items) CreatePin(it);
        foreach (var it in c.placed) CreatePin(it);

        popup.SetActive(true);
    }


    void CloseRoom() { popup.SetActive(false); }


    void CreatePin(PlacedItem rec)
    {
        var pin = CreateUI("Pin_" + rec.item.itemName, pinRoot);
        var rt = pin.rectTransform();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(74, 74);
        SetPinPos(rt, rec.pos);

        // ⭐ 透明点击层（拖拽/点击脚本在这个物体上）
        var hit = pin.AddComponent<Image>();
        hit.sprite = CircleArt();
        hit.color = new Color(1f, 1f, 1f, 0.01f);

        var dot = CreateUI("Dot", pin.transform);
        dot.rectTransform().sizeDelta = new Vector2(56, 56);
        dot.rectTransform().pivot = new Vector2(0.5f, 0.5f);
        dot.rectTransform().anchoredPosition = new Vector2(0, 8);
        var dotImg = dot.AddComponent<Image>();
        dotImg.sprite = CircleArt();
        dotImg.color = paperColor;
        dotImg.raycastTarget = false;

        if (rec.item.img != null)
        {
            var maskGo = CreateUI("Mask", dot.transform);
            maskGo.rectTransform().Stretch();
            var mImg = maskGo.AddComponent<Image>();
            mImg.sprite = CircleArt();
            mImg.color = Color.white;
            var m = maskGo.AddComponent<Mask>();
            m.showMaskGraphic = false;

            var pic = CreateUI("Pic", maskGo.transform);
            pic.rectTransform().Stretch();
            var picImg = pic.AddComponent<Image>();
            picImg.sprite = rec.item.img;
            picImg.preserveAspect = true;
            picImg.color = Color.white;
            picImg.raycastTarget = false;
        }
        else
        {
            var t = CreateText(dot.transform, rec.item.itemName, 9, TextAlignmentOptions.Center, redSoftColor);
            t.raycastTarget = false;
            t.rectTransform().Stretch();
        }

        var nameT = CreateText(pin.transform, rec.item.itemName, 12, TextAlignmentOptions.Center, redColor);
        nameT.raycastTarget = false;
        nameT.rectTransform().pivot = new Vector2(0.5f, 1f);
        nameT.rectTransform().anchoredPosition = new Vector2(0, -24f);

        // 介绍气泡
        var tip = CreateUI("Tip", pin.transform);
        var tipRt = tip.rectTransform();
        tipRt.pivot = new Vector2(0.5f, 0f);
        tipRt.anchoredPosition = new Vector2(0, 46f);
        tipRt.sizeDelta = new Vector2(210, 84f);
        var tipImg = tip.AddComponent<Image>();
        tipImg.color = paperColor;
        tipImg.raycastTarget = false;
        var tipText = CreateText(tip.transform,
            "<b>" + rec.item.itemName + "</b>\n" + rec.item.desc,
            13, TextAlignmentOptions.TopLeft, redSoftColor);
        tipText.raycastTarget = false;
        tipText.rectTransform().Stretch(Vector2.zero, Vector2.one, new Vector2(10f, 8f));
        tip.SetActive(false);

        // 拖拽 + 点击
        var drag = pin.AddComponent<CodexDrag>();
        Vector2 downPos = default;
        bool moved = false;

        drag.onDown = e =>
        {
            downPos = e.position;
            moved = false;
            pin.transform.SetAsLastSibling();
        };
        drag.onDrag = e =>
        {
            if (!moved && Vector2.Distance(e.position, downPos) > 8f)
            {
                moved = true;
                tip.SetActive(false);
            }
            if (moved && ScreenToRoom(e.position, out Vector2 pct))
                SetPinPos(rt, pct);
        };
        drag.onUp = e =>
        {
            if (moved)
            {
                rec.pos = GetPinPos(rt);
                SavePins(current);
            }
            else
            {
                tip.SetActive(!tip.activeSelf);
            }
        };
    }


    // ============================================================
    //  仓库拖拽
    // ============================================================
    void BeginGhostDrag(CodexItem item, PointerEventData e)
    {
        if (current == null || !popup.activeSelf) return;
        dragItem = item;

        ghost = CreateUI("Ghost", canvas.transform);
        var rt = ghost.rectTransform();
        rt.sizeDelta = new Vector2(48, 48);
        rt.SetAsLastSibling();
        var img = ghost.AddComponent<Image>();
        img.sprite = CircleArt();
        img.color = yellowColor;

        if (item.img != null)
        {
            var maskGo = CreateUI("Mask", ghost.transform);
            maskGo.rectTransform().Stretch();
            var mImg = maskGo.AddComponent<Image>();
            mImg.sprite = CircleArt();
            mImg.color = Color.white;
            var m = maskGo.AddComponent<Mask>();
            m.showMaskGraphic = false;

            var pic = CreateUI("Pic", maskGo.transform);
            pic.rectTransform().Stretch();
            var picImg = pic.AddComponent<Image>();
            picImg.sprite = item.img;
            picImg.preserveAspect = true;
            picImg.color = Color.white;
            picImg.raycastTarget = false;
        }

        MoveGhostDrag(e);
    }


    void MoveGhostDrag(PointerEventData e)
    {
        if (ghost == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform, e.position, e.pressEventCamera,
            out Vector2 local);
        ghost.rectTransform().anchoredPosition = local;
    }


    void EndGhostDrag(PointerEventData e)
    {
        if (ghost == null) { dragItem = null; return; }

        if (popup.activeSelf && current != null && dragItem != null &&
            ScreenToRoom(e.position, out Vector2 pct))
        {
            var rec = new PlacedItem { item = dragItem, pos = pct };
            current.placed.Add(rec);
            CreatePin(rec);
            SavePins(current);
        }

        Destroy(ghost);
        ghost = null;
        dragItem = null;
    }


    // ============================================================
    //  存档
    // ============================================================
    void SavePins(Character c)
    {
        var list = new SavedPinList();
        foreach (var p in c.placed)
        {
            int idx = warehouse.IndexOf(p.item);
            if (idx >= 0) list.pins.Add(new SavedPin { itemIndex = idx, pos = p.pos });
        }
        PlayerPrefs.SetString("Codex_" + characters.IndexOf(c), JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    void LoadPins(Character c)
    {
        string key = "Codex_" + characters.IndexOf(c);
        if (!PlayerPrefs.HasKey(key)) return;

        var list = JsonUtility.FromJson<SavedPinList>(PlayerPrefs.GetString(key));
        c.placed = new List<PlacedItem>();
        if (list == null || list.pins == null) return;

        foreach (var sp in list.pins)
            if (sp.itemIndex >= 0 && sp.itemIndex < warehouse.Count)
                c.placed.Add(new PlacedItem { item = warehouse[sp.itemIndex], pos = sp.pos });
    }


    // ============================================================
    //  坐标工具
    // ============================================================
    bool ScreenToRoom(Vector2 screenPos, out Vector2 pct)
    {
        pct = default;
        if (!RectTransformUtility.RectangleContainsScreenPoint(roomRt, screenPos, null))
            return false;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(roomRt, screenPos, null, out Vector2 local);
        Rect r = roomRt.rect;
        pct = new Vector2(
            Mathf.Clamp((local.x + r.width / 2f) / r.width * 100f, 4f, 96f),
            Mathf.Clamp((r.height / 2f - local.y) / r.height * 100f, 8f, 92f));
        return true;
    }

    void SetPinPos(RectTransform rt, Vector2 pct)
    {
        Rect r = roomRt.rect;
        rt.anchoredPosition = new Vector2(
            (pct.x - 50f) / 100f * r.width,
            (50f - pct.y) / 100f * r.height);
    }

    Vector2 GetPinPos(RectTransform rt)
    {
        Rect r = roomRt.rect;
        return new Vector2(
            rt.anchoredPosition.x / r.width * 100f + 50f,
            50f - rt.anchoredPosition.y / r.height * 100f);
    }


    // ============================================================
    //  UI 生成小工具
    // ============================================================
    static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    TMP_Text CreateText(Transform parent, string content, float size,
        TextAlignmentOptions align, Color color)
    {
        var go = CreateUI("Text", parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        if (chineseFont != null) t.font = chineseFont;   // ⭐ 全局用中文字体
        return t;
    }


    // ============================================================
    //  小组件
    // ============================================================
    class CodexOrb : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public bool hover;
        public Action onClick;
        public void OnPointerEnter(PointerEventData e) { hover = true; }
        public void OnPointerExit(PointerEventData e) { hover = false; }
        public void OnPointerClick(PointerEventData e) { onClick?.Invoke(); }
    }

    class CodexDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<PointerEventData> onDown, onDrag, onUp;
        public void OnPointerDown(PointerEventData e) { onDown?.Invoke(e); }
        public void OnDrag(PointerEventData e) { onDrag?.Invoke(e); }
        public void OnPointerUp(PointerEventData e) { onUp?.Invoke(e); }
    }
}


// RectTransform 快捷设置
public static class CodexRectExt
{
    public static RectTransform rectTransform(this GameObject go) => (RectTransform)go.transform;
    public static RectTransform rectTransform(this Transform t) => (RectTransform)t;
    public static RectTransform rectTransform(this Component c) => (RectTransform)c.transform;

    public static void Stretch(this RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // padding 为正 = 向内缩
    public static void Stretch(this RectTransform rt, Vector2 min, Vector2 max, Vector2 padding)
    {
        rt.anchorMin = min; rt.anchorMax = max;
        rt.offsetMin = padding; rt.offsetMax = -padding;
    }

    public static void SetTopLeft(this RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    public static void SetTopRight(this RectTransform rt, float x, float y, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}