using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 侧边栏界面 - 由 MenuUI 的"入口有奖"按钮打开。
///
/// 打开时根据 SidebarManager.IsLaunchedFromSidebar 自动切换按钮状态：
///   - 来自侧边栏 → 主按钮显示「领取奖励」，点击打开 SidebarRewardUI
///   - 非侧边栏启动 → 主按钮显示「进入侧边栏目」，点击调用 tt.navigateToScene 跳转
/// </summary>
public class SidebarUI : MonoBehaviour
{
    // ── 单例 ─────────────────────────────────────────────
    public static SidebarUI Instance { get; private set; }

    [Header("按钮")]
    [SerializeField] private Button enterSidebarButton;   // 进入侧边栏目 / 领取奖励（状态切换）
    [SerializeField] private Button closeButton;

    [Header("按钮文本（可选，状态切换用）")]
    [Tooltip("enterSidebarButton 上的 Text 组件，用于切换「进入侧边栏目」/「领取奖励」文案")]
    [SerializeField] private Text enterButtonText;

    [Header("关联面板")]
    [SerializeField] private SidebarRewardUI sidebarRewardUI;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    // ── 文案常量 ──────────────────────────────────────────
    private const string TextEnter = "进入侧边栏目";
    private const string TextClaim = "领取奖励";

    // ── 生命周期 ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (enterSidebarButton != null)
            enterSidebarButton.onClick.AddListener(OnEnterSidebarClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        // 奖励已领取则不应再开启此界面（安全保护）
        if (SidebarManager.Instance != null && SidebarManager.Instance.IsRewardClaimed)
        {
            gameObject.SetActive(false);
            return;
        }

        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
        RefreshButtonState();
    }

    // ── 按钮回调 ──────────────────────────────────────────

    private void OnEnterSidebarClicked()
    {
        bool fromSidebar = SidebarManager.Instance != null && SidebarManager.Instance.IsLaunchedFromSidebar;

        if (fromSidebar)
        {
            // 用户已从侧边栏返回 → 关闭侧边栏界面，打开奖励界面
            gameObject.SetActive(false);
            OpenRewardPanel();
        }
        else
        {
            // 用户尚未访问侧边栏 → 跳转第三方侧边栏
            gameObject.SetActive(false);
            SidebarManager.Instance?.NavigateToSidebar();
        }
    }

    private void OnClose()
    {
        gameObject.SetActive(false);
    }

    // ── 私有方法 ──────────────────────────────────────────

    /// <summary>根据是否来自侧边栏，切换主按钮文案。</summary>
    private void RefreshButtonState()
    {
       /* if (enterButtonText == null) return;

        bool fromSidebar = SidebarManager.Instance != null && SidebarManager.Instance.IsLaunchedFromSidebar;
        enterButtonText.text = fromSidebar ? TextClaim : TextEnter;*/
    }

    private void OpenRewardPanel()
    {
        if (sidebarRewardUI != null)
            sidebarRewardUI.gameObject.SetActive(true);
        else
            Debug.LogWarning("SidebarUI: sidebarRewardUI 未绑定，无法弹出奖励界面");
    }
}
