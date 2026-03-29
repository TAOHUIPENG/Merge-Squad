using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 分享界面 - 关闭/好友/微信/QQ/微博
/// 成功分享奖励：(射速下次升级价 + 伤害下次升级价)，整十取整
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
        // TODO: 接入各平台分享SDK后，在回调中调用 OnShareSuccess(platform)
        // 模拟分享成功：
        OnShareSuccess(platform);
    }

    private void OnShareSuccess(string platform)
    {
        int reward = CalcShareReward();
        if (_db != null)
            _db.Money.Value += reward;

        Debug.Log($"ShareUI: 分享到{platform}成功，奖励金币 +{reward}");
    }

    /// <summary>
    /// 分享奖励：(射速下次升级价 + 伤害下次升级价)，整十取整
    /// </summary>
    private int CalcShareReward()
    {
        float raw = _gameData.FireNextUpgradePrice + _gameData.PowerNextUpgradePrice;
        return Mathf.RoundToInt(raw / 10f) * 10;
    }
}
