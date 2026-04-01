using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 金币不足弹窗 - 关闭/看广告获取金币
/// 每次看广告奖励 1000 金币
/// </summary>
public class CoinPopupUI : MonoBehaviour
{
    // ── 常量 ─────────────────────────────────────────────
    public const int AdCoinReward = 1000;

    // ── 单例（方便其他 UI 直接访问）──────────────────────
    public static CoinPopupUI Instance { get; private set; }

    [Header("按钮")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button watchAdButton;

    [Header("金币文本（可选）")]
    [SerializeField] private Text coinText;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAd);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
        RefreshCoinText();
    }

    private void RefreshCoinText()
    {
        if (coinText != null && _db != null)
            coinText.text = $"{_db.Money.Value:0}";
    }

    private void OnWatchAd()
    {
        AdManager.Instance.ShowRewarded(
            AdManager.Scenes.CoinPopup,
            onRewarded: OnAdComplete,
            onFailed:   err => Debug.LogWarning($"CoinPopupUI: 激励广告失败 - {err}"));
    }

    private void OnAdComplete()
    {
        _db.Money.Value += AdCoinReward;
        Debug.Log($"CoinPopupUI: 金币+{AdCoinReward}，当前金币={_db.Money.Value}");
        RefreshCoinText();

        // 通知 MenuUI 同步刷新金币显示
        var menuUI = FindObjectOfType<MenuUI>();
        menuUI?.OnCoinChanged();

        gameObject.SetActive(false);
    }
}

