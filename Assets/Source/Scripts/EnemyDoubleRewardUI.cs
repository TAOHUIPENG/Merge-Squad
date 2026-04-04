using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 中立单位生成时头顶出现的 x2 广告奖励视觉指示器（World Space）。
/// 玩家拾取带此 UI 的 SquadMember 时，由 NeutralMember 调用 TriggerAd() 播放广告。
/// 广告看完后稳定触发 onRewarded（x2 奖励）；广告失败则触发 onFailed（正常加入）。
/// x2 UI 出现的概率由 SquadMember.doubleRewardChance 控制，与此处无关。
/// </summary>
public class EnemyDoubleRewardUI : MonoBehaviour
{
    [Header("UI 元素")]
    [SerializeField] private Text rewardText;

    [Header("参数")]
    [Tooltip("高于跟随目标的 Y 偏移")]
    [SerializeField] private float yOffset = 1.8f;
    [Tooltip("开发测试用：广告加载失败时仍发放 x2 奖励（上线前务必关闭）")]
    [SerializeField] private bool simulateRewardOnAdFail = false;

    private Camera         _camera;
    private Canvas         _canvas;
    private Transform      _followTarget;
    private bool           _triggered;
    private Action         _onAdRewarded;
    private Action<string> _onAdFailed;
    private Action         _onSelfDestroyed;

    // ── 静态清场（复活/重开时调用）────────────────────────────────────────
    public static void ClearAll()
    {
        foreach (var ui in FindObjectsOfType<EnemyDoubleRewardUI>())
            Destroy(ui.gameObject);
    }

    // ── 初始化（由 SquadMember.TryShowDoubleRewardUI 调用）─────────────────
    public void Init(Transform followTarget, Action onSelfDestroyed = null)
    {
        _followTarget    = followTarget;
        _onSelfDestroyed = onSelfDestroyed;
        _camera          = Camera.main;

        _canvas = GetComponentInChildren<Canvas>(true)
               ?? GetComponentInParent<Canvas>();

        if (_canvas != null)
        {
            _canvas.renderMode  = RenderMode.WorldSpace;
            _canvas.worldCamera = _camera;

            _canvas.transform.position   = followTarget.position + Vector3.up * yOffset;
            _canvas.transform.localScale = Vector3.zero;
            _canvas.transform.DOScale(Vector3.one, 0.28f)
                              .SetEase(Ease.OutBack)
                              .SetUpdate(true);
        }
        else
        {
            Debug.LogError("[EnemyDoubleRewardUI] 预制体中找不到 Canvas，请检查预制体结构！");
        }

        if (rewardText != null)
            rewardText.text = "x2";

        // UI 只在被吃（TriggerAd）或小人离场（Dismiss）时消失，不设自动销毁
    }

    /// <summary>
    /// 玩家拾取该成员时由 NeutralMember 调用，立即播放广告。
    /// 广告看完 → 稳定调用 <paramref name="onRewarded"/>（x2 进化）。
    /// 广告失败/跳过 → 调用 <paramref name="onFailed"/>（正常加入队伍）。
    /// </summary>
    public void TriggerAd(Action onRewarded, Action<string> onFailed = null)
    {
        if (_triggered) return;
        _triggered    = true;
        _onAdRewarded = onRewarded;
        _onAdFailed   = onFailed;

        AdManager.Instance.ShowRewarded(
            AdManager.Scenes.EnemyDoubleReward,
            onRewarded: OnAdRewarded,
            onFailed:   OnAdFailed);
    }

    /// <summary>目标离场（超时/死亡）时由外部调用，广告未触发则直接销毁。</summary>
    public void Dismiss()
    {
        if (!_triggered)
            Destroy(gameObject);
        // 广告播放中则不打断，让回调自然结束
    }

    private void LateUpdate()
    {
        if (_followTarget == null)
        {
            if (!_triggered) Destroy(gameObject);
            return;
        }

        if (_canvas != null)
        {
            _canvas.transform.position = _followTarget.position + Vector3.up * yOffset;
            if (_camera != null)
                _canvas.transform.forward = _camera.transform.forward;
        }
    }

    private void OnAdRewarded()
    {
        // 看完广告稳定给 x2，不再二次随机——出现概率已由 doubleRewardChance 控制
        Debug.Log("[EnemyDoubleRewardUI] 广告看完，发放 x2 奖励");
        _onAdRewarded?.Invoke();
        Destroy(gameObject);
    }

    private void OnAdFailed(string err)
    {
        _triggered = false;
        Debug.LogError($"[EnemyDoubleRewardUI] ❌ 广告失败: {err}\n" +
                       "请检查 AdManager Inspector 中是否配置了 sceneName=EnemyDoubleReward 的广告位！");

        if (simulateRewardOnAdFail)
        {
            Debug.LogWarning("[EnemyDoubleRewardUI] simulateRewardOnAdFail=true，模拟广告成功（仅限测试）");
            _onAdRewarded?.Invoke();
        }
        else
        {
            _onAdFailed?.Invoke(err);
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 通知 SquadMember 清空引用，避免持有已销毁对象触发 MissingReferenceException
        _onSelfDestroyed?.Invoke();
    }
}
