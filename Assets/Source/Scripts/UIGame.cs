using D2D.Core;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏中UI界面 - 暂停按钮 / 退出游戏按钮
/// 游戏开始(RunningState)后显示，返回Menu(PauseState/WinState/LoseState)时隐藏
/// </summary>
public class UIGame : GameStateMachineUser
{
    [Header("按钮")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button exitButton;

    [Header("关联面板（可选）")]
    [SerializeField] private PauseUI pauseUI;
    [SerializeField] private ExitGameUI exitGameUI;

    private void Start()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        // 默认隐藏，等游戏开始后才显示
        gameObject.SetActive(false);
    }

    protected override void OnGameRun()
    {
        gameObject.SetActive(true);
    }

    protected override void OnGamePause()
    {
        // 返回Menu或暂停时隐藏
        gameObject.SetActive(false);
    }

    protected override void OnGameWin()
    {
        gameObject.SetActive(false);
    }

    protected override void OnGameLose()
    {
        gameObject.SetActive(false);
    }

    private void OnPauseClicked()
    {
        if (pauseUI != null)
        {
            pauseUI.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("UIGame: pauseUI 未绑定");
        }
    }

    private void OnExitClicked()
    {
        if (exitGameUI != null)
        {
            exitGameUI.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("UIGame: exitGameUI 未绑定");
        }
    }
}

