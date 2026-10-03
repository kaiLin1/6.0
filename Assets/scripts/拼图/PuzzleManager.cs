using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PuzzleManager : MonoBehaviour
{
    [Header("拼图列表（这一组的）")]
    public List<PuzzlePiece> pieces = new List<PuzzlePiece>();

    [Header("完成事件")]
    public UnityEvent onPuzzleComplete;

    private int placedCount = 0;

    void OnEnable()
    {
        placedCount = 0;
    }

    // 👉 拼图完成时调用
    public void NotifyPiecePlaced(PuzzlePiece piece)
    {
        if (piece == null || piece.target == null)
        {
            Debug.LogWarning("❌ 拼图或目标为空");
            return;
        }

        // ⭐ 每个 piece 都计数（允许一个 target 多个 piece）
        placedCount++;

        Debug.Log($"[{name}] 完成进度：{placedCount}/{pieces.Count}");

        if (placedCount >= pieces.Count)
        {
            Debug.Log($"🎉 拼图完成：{name}");
            onPuzzleComplete?.Invoke();
        }
    }
}