using TTSDK;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 添加桌面奖励界面 - 用户成功将游戏添加到桌面后弹出。
///
/// 触发路径：
///   MenuUI.OnAddDesktopClicked → TT.AddShortcut 成功 → TT.CheckShortcut 确认 → Show()
///
/// 奖励规则：
///   - 只能领取一次（PlayerPrefs 持久化）
///   - 领取后通知 MenuUI 隐藏"添加桌面"按钮
/// </summary>
public class AwardShortcutUI : MonoBehaviour
{
    // ── 持久化 Key ────────────────────────────────────────
    private const string RewardClaimedKey = "Shortcut_RewardClaimed";

    // ── 单例 ─────────────────────────────────────────────
    public static AwardShortcutUI Instance { get; private set; }

    // ── 公开状态 ──────────────────────────────────────────
    /// <summary>桌面奖励是否已领取（持久化）</summary>
    public bool IsRewardClaimed => PlayerPrefs.GetInt(RewardClaimedKey, 0) == 1;

    [Header("按钮")]
    [SerializeField] private Button claimButton;
    [SerializeField] private Button closeButton;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    // ── 生命周期 ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaim);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
    }

    // ── 公开接口 ──────────────────────────────────────────

    /// <summary>
    /// 显示奖励界面（由 MenuUI 在确认添加桌面成功后调用）。
    /// 若奖励已领取，则静默忽略。
    /// </summary>
    public void Show()
    {
        if (IsRewardClaimed)
        {
            Debug.Log("[AwardShortcutUI] 桌面奖励已领取，跳过弹出");
            return;
        }
        gameObject.SetActive(true);
    }

    // ── 按钮回调 ──────────────────────────────────────────

    private void OnClaim()
    {
        _db.Money.Value += 1000;
        Debug.Log($"[AwardShortcutUI] 领取桌面奖励，金币+1000，当前={_db.Money.Value}");

        // 刷新 MenuUI 金币显示
        FindObjectOfType<MenuUI>()?.OnCoinChanged();

        // 标记已领取并通知 MenuUI 隐藏"添加桌面"按钮
        MarkRewardClaimed();

        gameObject.SetActive(false);
    }

    private void OnClose()
    {
        gameObject.SetActive(false);
    }

    /// <summary>标记奖励已领取并持久化，然后通知 MenuUI 刷新按钮显隐。</summary>
    private void MarkRewardClaimed()
    {
        PlayerPrefs.SetInt(RewardClaimedKey, 1);
        PlayerPrefs.Save();
        FindObjectOfType<MenuUI>()?.RefreshDesktopButtonVisibility();
    }
}

