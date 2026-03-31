using D2D.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 失败界面 - 看广告复活/2倍奖励/当前金币文本
/// 复活流程：清空敌人 → 小队满血 → 恢复计时 → 推入 RunningState
/// </summary>
public class FailUI : MonoBehaviour
{
    public static FailUI Instance { get; private set; }

    [Header("按钮")]
    [SerializeField] private Button reviveAdButton;
    [SerializeField] private Button doubleRewardButton;

    [Header("文本")]
    [SerializeField] private Text currentCoinsText;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    private float earnedCoins;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (reviveAdButton != null)
            reviveAdButton.onClick.AddListener(OnReviveAd);

        if (doubleRewardButton != null)
            doubleRewardButton.onClick.AddListener(OnDoubleReward);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 公开接口 ─────────────────────────────────────────

    public void Show()
    {
        earnedCoins = _db != null ? _db.Money.Value : 0;
        RefreshUI();
        UIGame.Instance?.Hide();
        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        // GameFinishWindowsSwitcher 会通过 _loseWindow.On(after: delay) 延迟激活本界面。
        // 若复活已完成（状态已切回 RunningState），立即关闭自身，防止延迟回调重新弹出失败界面。
        if (_stateMachine != null && _stateMachine.Last!=null&& !_stateMachine.Last.Is<LoseState>())
        {
            gameObject.SetActive(false);
            return;
        }

        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
    }

    // ── 内部逻辑 ─────────────────────────────────────────

    private void RefreshUI()
    {
        if (currentCoinsText != null)
            currentCoinsText.text = $"获得金币: {earnedCoins:0}";
    }

    private void OnReviveAd()
    {
        if (reviveAdButton != null)
            reviveAdButton.interactable = false;

        AdManager.Instance.ShowRewarded(
            onRewarded: OnReviveAdComplete,
            onClosed:   () => { if (reviveAdButton != null) reviveAdButton.interactable = true; },
            onFailed:   err =>
            {
                Debug.LogWarning($"FailUI: 复活广告失败 - {err}");
                if (reviveAdButton != null) reviveAdButton.interactable = true;
            });
    }

    private void OnReviveAdComplete()
    {
        // 1. 清空场上所有敌人（减轻复活压力），但保留关卡计时
        _enemySpawn?.ReviveSpawner();

        // 2. 重新实例化小队（成员在死亡时已从列表移除，FullHealSquad 无法作用于空列表）
        _squad?.ReviveSquad();

        // 3. 清除场景中残留的 x2 广告按钮
        EnemyDoubleRewardUI.ClearAll();

        // 4. 恢复 GameProgress 计时（isFinished → false）
        _gameProgress?.Revive();

        // 4. 杀掉弹窗动画 Tween，再关闭界面（防止 Tween 在 inactive 状态继续运行）
        (panelRoot != null ? panelRoot : transform).DOKill();
        gameObject.SetActive(false);

        // 5. 重新推入 RunningState（LoseState → RunningState 合法）
        _stateMachine.Push(new RunningState());

        // 6. 恢复游戏 HUD
        UIGame.Instance?.Show();

        LevelRestarter.Instance.player.gameObject.SetActive(true);

        // 7. 恢复按钮可交互状态（下次失败时可再次使用）
        if (reviveAdButton != null)
            reviveAdButton.interactable = true;
    }

    private void OnDoubleReward()
    {
        AdManager.Instance.ShowRewarded(
            onRewarded: OnDoubleRewardAdComplete,
            onFailed:   err => Debug.LogWarning($"FailUI: 双倍奖励广告失败 - {err}"));
    }

    private void OnDoubleRewardAdComplete()
    {
        if (_db != null)
            _db.Money.Value += earnedCoins;

        gameObject.SetActive(false);
    }
}
