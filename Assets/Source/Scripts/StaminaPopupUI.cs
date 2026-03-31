using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 体力不足弹窗 - 关闭/看广告获取体力
/// </summary>
public class StaminaPopupUI : MonoBehaviour
{
    // ── 单例（方便其他 UI 直接访问）──────────────────────
    public static StaminaPopupUI Instance { get; private set; }

    [Header("按钮")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button watchAdButton;

    [Header("体力文本（可选）")]
    [SerializeField] private Text staminaText;

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
        RefreshStaminaText();
    }

    private void RefreshStaminaText()
    {
        if (staminaText != null)
            staminaText.text = $"{StaminaManager.Get()}/{StaminaManager.MaxStamina}";
    }

    private void OnWatchAd()
    {
        AdManager.Instance.ShowRewarded(
            onRewarded: OnAdComplete,
            onFailed:   err => Debug.LogWarning($"StaminaPopupUI: 激励广告失败 - {err}"));
    }

    private void OnAdComplete()
    {
        StaminaManager.Add(StaminaManager.AdReward);
        Debug.Log($"StaminaPopupUI: 体力+{StaminaManager.AdReward}，当前体力={StaminaManager.Get()}");
        RefreshStaminaText();

        // 通知 MenuUI 同步刷新体力显示
        var menuUI = FindObjectOfType<MenuUI>();
        menuUI?.OnStaminaChanged();

        gameObject.SetActive(false);
    }
}
