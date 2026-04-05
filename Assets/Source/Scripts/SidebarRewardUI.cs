using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;
/// <summary>
/// 侧边栏奖励界面 - 用户从侧边栏返回后弹出。
///
/// 触发路径：
///   1. SidebarManager.OnSidebarReturned 事件 → 自动激活（用户热启动从侧边栏返回）
///   2. SidebarUI.OnEnterSidebarClicked 主动打开（用户已从侧边栏返回后再次点击入口）
/// </summary>
public class SidebarRewardUI : MonoBehaviour
{
    // ── 单例 ─────────────────────────────────────────────
    public static SidebarRewardUI Instance { get; private set; }

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

        // 【关键】即使 gameObject 处于 inactive 状态，C# 委托仍然有效，
        // 所以必须在 Awake 里订阅，而非 Start——因为 inactive 对象的 Start 不会执行。
        SidebarManager.OnSidebarReturned += OnSidebarReturned;

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
        SidebarManager.OnSidebarReturned -= OnSidebarReturned;

        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
    }

    // ── 事件处理 ──────────────────────────────────────────

    /// <summary>SidebarManager 通知从侧边栏返回 → 仅在未领取时激活奖励界面。</summary>
    private void OnSidebarReturned()
    {
        // 消费挂起标志，防止场景重载后再次触发
        SidebarManager.Instance?.ConsumeReturnFlag();

        if (SidebarManager.Instance != null && SidebarManager.Instance.IsRewardClaimed)
        {
            Debug.Log("[SidebarRewardUI] 奖励已领取，跳过弹出");
            return;
        }
        gameObject.SetActive(true);
    }

    // ── 按钮回调 ──────────────────────────────────────────

    private void OnClaim()
    {
        _db.Money.Value += 1000;
        Debug.Log($"[SidebarRewardUI] 领取侧边栏奖励，金币+1000，当前={_db.Money.Value}");

        // 刷新 MenuUI 金币显示
        FindObjectOfType<MenuUI>()?.OnCoinChanged();

        // 标记已领取（持久化），并通知 MenuUI 隐藏入口按钮
        SidebarManager.Instance?.MarkRewardClaimed();

        gameObject.SetActive(false);
    }

    private void OnClose()
    {
        gameObject.SetActive(false);
    }
}
