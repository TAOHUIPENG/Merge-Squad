using D2D.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 暂停界面 - 重新开始/继续/关闭
/// 打开时暂停游戏（timeScale=0），关闭时恢复（timeScale=1）
/// </summary>
public class PauseUI : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button closeButton;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    [Header("体力不足弹窗")]
    [SerializeField] private StaminaPopupUI staminaPopupUI;

    private void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestart);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
    }

    private void OnEnable()
    {
        Time.timeScale = 0f;
        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
    }

    // ── 按钮回调 ─────────────────────────────────────────

    private void OnRestart()
    {
        if (!StaminaManager.CanStartGame())
        {
            // 体力不足：保持暂停界面，在其上方弹出体力补充弹窗
            StaminaPopupUI popup = staminaPopupUI != null
                ? staminaPopupUI
                : StaminaPopupUI.Instance;

            if (popup != null)
                popup.gameObject.SetActive(true);
            else
                Debug.LogWarning("PauseUI: 体力不足，且 staminaPopupUI 未绑定");
            return;
        }

        StaminaManager.ConsumeForGame();
        ResumeTime();
        gameObject.SetActive(false);
        // 不重新加载场景，通过 LevelRestarter 原地重置关卡
        LevelRestarter.Instance?.Restart();
    }

    private void OnContinue()
    {
        ResumeTime();
        gameObject.SetActive(false);
        UIGame.Instance?.Show();
    }

    private void OnClose()
    {
        ResumeTime();
        gameObject.SetActive(false);
        UIGame.Instance?.Show();
    }

    // ── 工具方法 ─────────────────────────────────────────

    private static void ResumeTime() => Time.timeScale = 1f;
}
