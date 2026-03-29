using D2D.Core;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 暂停界面 - 重新开始/继续/关闭
/// </summary>
public class PauseUI : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button closeButton;

    private void Start()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestart);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClose);
    }

    private void OnRestart()
    {
        gameObject.SetActive(false);
        _sceneLoader.ReloadCurrentScene();
    }

    private void OnContinue()
    {
        gameObject.SetActive(false);
        _stateMachine.Push(new RunningState());
    }

    private void OnClose()
    {
        gameObject.SetActive(false);
    }
}

