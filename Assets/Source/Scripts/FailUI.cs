using D2D.Core;
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
        Debug.Log("FailUI: 看广告复活 - 请接入广告SDK");
        // TODO: 广告SDK完成回调后调用 OnReviveAdComplete()
        // 模拟广告完成：
        OnReviveAdComplete();
    }

    private void OnReviveAdComplete()
    {
        // 1. 清空场上所有敌人（减轻复活压力），但保留关卡计时
        _enemySpawn?.ReviveSpawner();

        // 2. 小队全员血量回满
        _squad?.FullHealSquad();

        // 3. 恢复 GameProgress 计时（isFinished → false）
        _gameProgress?.Revive();

        // 4. 关闭失败界面
        gameObject.SetActive(false);

        // 5. 重新推入 RunningState（LoseState → RunningState 合法）
        _stateMachine.Push(new RunningState());

        // 6. 恢复游戏 HUD
        UIGame.Instance?.Show();
    }

    private void OnDoubleReward()
    {
        Debug.Log("FailUI: 看广告获得2倍奖励 - 请接入广告SDK");
        // TODO: 广告完成后调用 OnDoubleRewardAdComplete()
    }

    private void OnDoubleRewardAdComplete()
    {
        if (_db != null)
            _db.Money.Value += earnedCoins;

        gameObject.SetActive(false);
    }
}
