using D2D.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        // 暂停游戏
        Time.timeScale = 0f;
        // 弹窗弹出动画
        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
    }

    // ── 按钮回调 ─────────────────────────────────────────

    private void OnRestart()
    {
        ResumeTime();
        gameObject.SetActive(false);
        // 直接重新加载场景（不经过 SceneLoader 的过渡/loading 界面）
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnContinue()
    {
        ResumeTime();
        gameObject.SetActive(false);
        // 游戏已在 RunningState，仅恢复 timeScale 即可继续
        // _stateMachine.Push(new RunningState()) 在已是 RunningState 时无效，故省略
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
