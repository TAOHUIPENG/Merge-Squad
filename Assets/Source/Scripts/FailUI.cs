using D2D.Core;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 失败界面 - 看广告复活/2倍奖励/当前金币文本
/// </summary>
public class FailUI : GameStateMachineUser
{
    [Header("按钮")]
    [SerializeField] private Button reviveAdButton;       // 看广告复活
    [SerializeField] private Button doubleRewardButton;   // 2倍奖励（看广告）

    [Header("文本")]
    [SerializeField] private Text currentCoinsText;       // 当前获得金币文本

    private float earnedCoins;

    protected override void OnGameLose()
    {
        earnedCoins = _db != null ? _db.Money.Value : 0;
        RefreshUI();
        gameObject.SetActive(true);
        // 游戏结束，隐藏游戏 HUD
        UIGame.Instance?.Hide();
    }

    private void Start()
    {
        if (reviveAdButton != null)
            reviveAdButton.onClick.AddListener(OnReviveAd);

        if (doubleRewardButton != null)
            doubleRewardButton.onClick.AddListener(OnDoubleReward);

        gameObject.SetActive(false);
    }

    private void RefreshUI()
    {
        if (currentCoinsText != null)
            currentCoinsText.text = $"获得金币: {earnedCoins:0}";
    }

    private void OnReviveAd()
    {
        Debug.Log("FailUI: 看广告复活 - 请接入广告SDK");
        // TODO: 广告SDK播放，广告完成后调用 OnReviveAdComplete()
        // OnReviveAdComplete();
    }

    private void OnReviveAdComplete()
    {
        gameObject.SetActive(false);
        // 复活玩家：推入RunningState
        _stateMachine.Push(new RunningState());
    }

    private void OnDoubleReward()
    {
        Debug.Log("FailUI: 看广告获得2倍奖励 - 请接入广告SDK");
        // TODO: 广告SDK播放，广告完成后调用 OnDoubleRewardAdComplete()
        // OnDoubleRewardAdComplete();
    }

    private void OnDoubleRewardAdComplete()
    {
        if (_db != null)
            _db.Money.Value += earnedCoins;

        gameObject.SetActive(false);
    }
}

