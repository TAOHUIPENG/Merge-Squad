using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    [SerializeField] private Button homeButton;

    [Header("场景")]
    [Tooltip("点击 Home 后加载的场景名，留空则重新加载当前场景")]
    [SerializeField] private string menuSceneName = "";

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
            rewardText.text = $"+{earnedReward:0}";
    }

    private void OnDoubleReward()
    {
        AdManager.Instance.ShowRewarded(
            AdManager.Scenes.WinDouble,
            onRewarded: OnDoubleRewardAdComplete,
            onFailed:   err => Debug.LogWarning($"WinUI: 双倍奖励广告失败 - {err}"));
    }

    private void OnDoubleRewardAdComplete()
    {
        if (_db != null)
            _db.Money.Value += earnedReward;

        gameObject.SetActive(false);
    }

    private void OnGoHome()
    {
        _db.Money.Value += earnedReward;
        
        Time.timeScale = 1f;
        string target = string.IsNullOrEmpty(menuSceneName)
            ? SceneManager.GetActiveScene().name
            : menuSceneName;
        SceneManager.LoadScene(target);
    }
}
