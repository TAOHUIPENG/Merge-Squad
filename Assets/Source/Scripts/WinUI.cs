using D2D.Core;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 成功界面 - 获得奖励文本/看广告双倍奖励
/// </summary>
public class WinUI : GameStateMachineUser
{
    [Header("文本")]
    [SerializeField] private Text rewardText;             // 获得奖励文本

    [Header("按钮")]
    [SerializeField] private Button doubleRewardButton;   // 看广告双倍奖励

    private float earnedReward;

    protected override void OnGameWin()
    {
        earnedReward = _db != null ? _db.Money.Value : 0;
        RefreshUI();
        gameObject.SetActive(true);
    }

    private void Start()
    {
        if (doubleRewardButton != null)
            doubleRewardButton.onClick.AddListener(OnDoubleReward);

        gameObject.SetActive(false);
    }

    private void RefreshUI()
    {
        if (rewardText != null)
            rewardText.text = $"获得奖励: {earnedReward:0} 金币";
    }

    private void OnDoubleReward()
    {
        Debug.Log("WinUI: 看广告双倍奖励 - 请接入广告SDK");
        // TODO: 广告SDK播放，广告完成后调用 OnDoubleRewardAdComplete()
        // OnDoubleRewardAdComplete();
    }

    private void OnDoubleRewardAdComplete()
    {
        if (_db != null)
            _db.Money.Value += earnedReward;

        gameObject.SetActive(false);
    }
}

