using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PuzzleTarget : MonoBehaviour
{
    [Header("检测")]
    public float hoverDistance = 1.5f;

    [Header("🔧 遮挡判定开关")]
    public bool enableOcclusionCheck = false;

    private bool isHovering = false;

    [Header("⭐ 已放入的拼图列表")]
    public List<PuzzlePiece> placedPieces = new List<PuzzlePiece>();

    [Header("最大可放数量（0=无限）")]
    public int maxPieces = 0;

    [Header("事件")]
    public UnityEvent onPlaced;

    [Header("🔄 可选：缩小嵌入替换")]
    public bool useReplaceMode = false;
    public RectTransform replacePoint;
    public Vector3 replaceScale = Vector3.one;
    public float replaceSpeed = 8f;

    public void CheckHover(Vector3 pieceWorldPos)
    {
        if (!gameObject.activeInHierarchy)
        {
            isHovering = false;
            return;
        }

        Image img = GetComponent<Image>();
        if (img != null && img.color.a <= 0.01f)
        {
            isHovering = false;
            return;
        }

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null && cg.alpha <= 0.01f)
        {
            isHovering = false;
            return;
        }

        // ⭐ 如果达到上限，不能再接收
        if (maxPieces > 0 && placedPieces.Count >= maxPieces)
        {
            isHovering = false;
            return;
        }

        if (enableOcclusionCheck)
        {
            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = screenPos;

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);

            bool blocked = false;
            foreach (var r in results)
            {
                if (r.gameObject == gameObject) break;
                if (r.gameObject.GetComponent<PuzzlePiece>() != null) continue;
                if (r.gameObject.GetComponent<Graphic>() != null)
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked)
            {
                isHovering = false;
                return;
            }
        }

        float dist = Vector3.Distance(transform.position, pieceWorldPos);
        isHovering = dist < hoverDistance;
    }

    public bool IsHovering()
    {
        return isHovering;
    }

    public void OnPlaced(PuzzlePiece piece)
    {
        if (piece == null) return;

        // ⭐ 防止重复放同一个 piece
        if (placedPieces.Contains(piece))
            return;

        // ⭐ 数量限制
        if (maxPieces > 0 && placedPieces.Count >= maxPieces)
            return;

        placedPieces.Add(piece);

        Debug.Log("🎯 拼图放置成功：" + name + " 当前数量：" + placedPieces.Count);

        onPlaced?.Invoke();

        if (useReplaceMode && replacePoint != null)
        {
            piece.StartCoroutine(ReplaceToTarget(piece));
        }
    }

    IEnumerator ReplaceToTarget(PuzzlePiece piece)
    {
        RectTransform pieceRect = piece.GetComponent<RectTransform>();

        Vector2 startPos = pieceRect.anchoredPosition;
        Vector2 endPos = pieceRect.parent.InverseTransformPoint(replacePoint.position);

        Vector3 startScale = piece.transform.localScale;
        Vector3 endScale = replaceScale;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * replaceSpeed;

            pieceRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            piece.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        pieceRect.anchoredPosition = endPos;
        piece.transform.localScale = endScale;

        piece.transform.SetParent(replacePoint);
        piece.transform.SetAsLastSibling();
    }
}