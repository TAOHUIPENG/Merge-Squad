using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 退出游戏确认界面 - 取消/确认
/// 打开时暂停游戏（timeScale=0），取消时恢复（timeScale=1）
/// </summary>
public class ExitGameUI : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    private void Start()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
    }

    private void OnEnable()
    {
        // 暂停游戏
        Time.timeScale = 0f;
        // 弹窗弹出动画
        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
    }

    // ── 按钮回调 ─────────────────────────────────────────

    private void OnCancel()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        UIGame.Instance?.Show();
    }

    private void OnConfirm()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
