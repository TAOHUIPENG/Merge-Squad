using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 签到界面单个Item控制脚本
/// </summary>
public class SignInItem : MonoBehaviour
{
    [Header("背景")]
    [SerializeField] private GameObject bgClaimed;      // 已领取背景
    [SerializeField] private GameObject bgUnclaimed;    // 未领取背景
    [SerializeField] private GameObject bgLight;        // 当前可领取的光效背景

    [Header("金币图标")]
    [SerializeField] private GameObject iconClaimed;    // 已领取金币图标
    [SerializeField] private GameObject iconUnclaimed;  // 未领取金币图标

    [Header("文本")]
    [SerializeField] private Text rewardText;           // 奖励文本，如"+100"
    [SerializeField] private Text dayText;              // 天数文本，如"第1天"

    /// <summary>
    /// 设置Item状态
    /// </summary>
    /// <param name="day">第几天（1-8）</param>
    /// <param name="reward">奖励金币数量</param>
    /// <param name="isClaimed">是否已领取</param>
    /// <param name="isCurrent">是否为当前可领取的天</param>
    public void Setup(int day, int reward, bool isClaimed, bool isCurrent)
    {
        if (dayText != null)
            dayText.text = $"第{day}天";

        if (rewardText != null)
            rewardText.text = $"+{reward}";

        // 已领取 或 当前可领取 都显示已领取背景
        if (bgClaimed != null)
            bgClaimed.SetActive(isClaimed || isCurrent);

        if (bgUnclaimed != null)
            bgUnclaimed.SetActive(!isClaimed && !isCurrent);

        // 光效只在当前可领取且未领取时显示
        if (bgLight != null)
            bgLight.SetActive(isCurrent && !isClaimed);

        if (iconClaimed != null)
            iconClaimed.SetActive(isClaimed);

        if (iconUnclaimed != null)
            iconUnclaimed.SetActive(!isClaimed);
    }
}

