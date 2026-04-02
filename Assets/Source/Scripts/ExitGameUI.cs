using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 退出当前关卡确认界面 - 取消（继续游戏）/ 确认（返回主界面）
/// 打开时暂停游戏（timeScale=0），取消时恢复（timeScale=1）
/// </summary>
public class ExitGameUI : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    [Header("主界面场景")]
    [Tooltip("确认退出后要加载的场景名，留空则重新加载当前场景（单场景工程适用）")]
    [SerializeField] private string menuSceneName = "";

    private void Start()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
        
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

    private void OnCancel()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        UIGame.Instance?.Show();
    }

    private void OnConfirm()
    {
        Time.timeScale = 1f;

        // 加载指定场景；未填写则重新加载当前场景（回到菜单 PauseState）
        string target = string.IsNullOrEmpty(menuSceneName)
            ? SceneManager.GetActiveScene().name
            : menuSceneName;

        SceneManager.LoadScene(target);
    }
    
    private void OnClose()
    {
        ResumeTime();
        gameObject.SetActive(false);
        UIGame.Instance?.Show();
    }
    
    private static void ResumeTime() => Time.timeScale = 1f;
}
