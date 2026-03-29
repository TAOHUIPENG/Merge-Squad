using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 成功界面 - 获得奖励文本/看广告双倍奖励
/// 由外部在 Push(WinState) 后直接调用 WinUI.Instance.Show()
/// </summary>
public class WinUI : MonoBehaviour
{
    public static WinUI Instance { get; private set; }

    [Header("文本")]
    [SerializeField] private Text rewardText;

    [Header("按钮")]
    [SerializeField] private Button doubleRewardButton;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    private float earnedReward;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
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
        earnedReward = _db != null ? _db.Money.Value : 0;
        RefreshUI();
        UIGame.Instance?.Hide();
        // 通关奖励 +2 体力
        StaminaManager.Add(StaminaManager.WinReward);
        gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
    }

    // ── 内部逻辑 ─────────────────────────────────────────

    private void RefreshUI()
    {
        if (rewardText != null)
            rewardText.text = $"获得奖励: {earnedReward:0} 金币";
    }

    private void OnDoubleReward()
    {
        Debug.Log("WinUI: 看广告双倍奖励 - 请接入广告SDK");
        // TODO: 广告完成后调用 OnDoubleRewardAdComplete()
    }

    private void OnDoubleRewardAdComplete()
    {
        if (_db != null)
            _db.Money.Value += earnedReward;

        gameObject.SetActive(false);
    }
}
