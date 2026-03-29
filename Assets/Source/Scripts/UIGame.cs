using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏中 HUD 界面（暂停 / 退出游戏按钮）。
/// 使用单例模式，提供 Show() / Hide() 接口。
/// 游戏开始时由调用方调用 Show()，返回主界面时调用 Hide()。
/// </summary>
public class UIGame : MonoBehaviour
{
    // ── 单例 ─────────────────────────────────────────────
    public static UIGame Instance { get; private set; }

    // ── Inspector 字段 ────────────────────────────────────
    [Header("按钮")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button exitButton;

    [Header("关联面板（可选）")]
    [SerializeField] private PauseUI pauseUI;
    [SerializeField] private ExitGameUI exitGameUI;

    // ── 生命周期 ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        // 默认隐藏，等游戏开始后由外部调用 Show()
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── 公开接口 ──────────────────────────────────────────

    /// <summary>游戏开始时调用，显示 HUD。</summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>返回主界面 / 游戏结束时调用，隐藏 HUD。</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ── 按钮回调 ──────────────────────────────────────────

    private void OnPauseClicked()
    {
        if (pauseUI != null)
            pauseUI.gameObject.SetActive(true);
        else
            Debug.LogWarning("UIGame: pauseUI 未绑定");
    }

    private void OnExitClicked()
    {
        if (exitGameUI != null)
            exitGameUI.gameObject.SetActive(true);
        else
            Debug.LogWarning("UIGame: exitGameUI 未绑定");
    }
}
