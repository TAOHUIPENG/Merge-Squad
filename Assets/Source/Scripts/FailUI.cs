using D2D.Core;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 失败界面 - 看广告复活/2倍奖励/当前金币文本
/// 由外部在 Push(LoseState) 后直接调用 FailUI.Instance.Show()
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
        // OnEnable 触发动画
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
        // TODO: 广告完成后调用 OnReviveAdComplete()
    }

    private void OnReviveAdComplete()
    {
        gameObject.SetActive(false);
        _stateMachine.Push(new RunningState());
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
