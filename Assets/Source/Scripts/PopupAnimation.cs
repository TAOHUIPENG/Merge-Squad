using DG.Tweening;
using UnityEngine;

/// <summary>
/// 通用弹窗弹出动画工具类（基于 DOTween）
/// </summary>
public static class PopupAnimation
{
    /// 默认弹出时长（秒）
    public const float DefaultDuration = 0.28f;

    /// <summary>
    /// 播放弹窗弹出缩放动画。
    /// 使用 SetUpdate(true) 确保在 Time.timeScale = 0 时仍能正常播放（暂停弹窗场景适用）。
    /// </summary>
    /// <param name="target">要播放动画的 Transform（通常是弹窗面板根节点）</param>
    /// <param name="duration">动画时长，默认 0.28 秒</param>
    public static Tweener PlayOpen(Transform target, float duration = DefaultDuration)
    {
        target.localScale = Vector3.zero;
        return target.DOScale(Vector3.one, duration)
                     .SetEase(Ease.OutBack)
                     .SetUpdate(true); // 忽略 Time.timeScale，暂停时也能播放
    }
}

