using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 退出游戏确认界面 - 取消/确认
/// </summary>
public class ExitGameUI : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    private void Start()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
    }

    private void OnConfirm()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

