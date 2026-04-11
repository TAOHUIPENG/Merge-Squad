using D2D.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    [SerializeField] private Button homeButton;

    [Header("场景")]
    [Tooltip("点击 Home 后加载的场景名，留空则重新加载当前场景")]
    [SerializeField] private string menuSceneName = "";

    [Header("文本")]
    [SerializeField] private Text currentCoinsText;
    [Tooltip("双倍奖励按钮下方文本，显示正常奖励 × 2")]
    [SerializeField] private Text doubleRewardText;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    private float earnedCoins;
    private float _levelStartMoney;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _levelStartMoney = _db != null ? _db.Money.Value : 0;

        if (reviveAdButton != null)
            reviveAdButton.onClick.AddListener(OnReviveAd);

        if (doubleRewardButton != null)
            doubleRewardButton.onClick.AddListener(OnDoubleReward);

        if (homeButton != null)
            homeButton.onClick.AddListener(OnGoHome);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 公开接口 ─────────────────────────────────────────

    public void Show()
    {
        earnedCoins = _db != null ? _db.Money.Value - _levelStartMoney : 0;
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
            currentCoinsText.text = $"+{earnedCoins:0}";
        if (doubleRewardText != null)
            doubleRewardText.text = $"+{earnedCoins * 2:0}";
    }

    private void OnReviveAd()
    {
        if (reviveAdButton != null)
            reviveAdButton.interactable = false;

        AdManager.Instance.ShowRewarded(
            AdManager.Scenes.Revive,
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
            AdManager.Scenes.FailDouble,
            onRewarded: OnDoubleRewardAdComplete,
            onFailed:   err => Debug.LogWarning($"FailUI: 双倍奖励广告失败 - {err}"));
    }

    private void OnDoubleRewardAdComplete()
    {
        if (_db != null)
            _db.Money.Value += earnedCoins;

        GoToMenu();
    }

    private void OnGoHome()
    {
        // 金币在敌人死亡时已实时写入 _db.Money，无需再次累加
        GoToMenu();
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        string target = string.IsNullOrEmpty(menuSceneName)
            ? SceneManager.GetActiveScene().name
            : menuSceneName;
        SceneManager.LoadScene(target);
    }
}
