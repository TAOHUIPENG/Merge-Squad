using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 分享界面 - 关闭/好友/微信/QQ/微博
/// </summary>
public class ShareUI : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button shareFriendButton;
    [SerializeField] private Button shareWechatButton;
    [SerializeField] private Button shareQQButton;
    [SerializeField] private Button shareWeiboButton;

    [Header("动画")]
    [Tooltip("弹窗动画作用的面板根节点，留空则使用自身 Transform")]
    [SerializeField] private Transform panelRoot;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (shareFriendButton != null)
            shareFriendButton.onClick.AddListener(() => OnShare("好友"));

        if (shareWechatButton != null)
            shareWechatButton.onClick.AddListener(() => OnShare("微信"));

        if (shareQQButton != null)
            shareQQButton.onClick.AddListener(() => OnShare("QQ"));

        if (shareWeiboButton != null)
            shareWeiboButton.onClick.AddListener(() => OnShare("微博"));
    }

    private void OnEnable()
    {
        PopupAnimation.PlayOpen(panelRoot != null ? panelRoot : transform);
    }

    private void OnShare(string platform)
    {
        Debug.Log($"ShareUI: 分享到{platform} - 请接入对应平台分享SDK");
        // TODO: 接入各平台分享SDK
        // 分享成功后可给予奖励
    }
}
