using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 体力不足弹窗 - 关闭/看广告获取体力
/// </summary>
public class StaminaPopupUI : MonoBehaviour
{
    [Header("按钮")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button watchAdButton;

    [Header("每次广告获得体力")]
    [SerializeField] private int staminaRewardPerAd = 3;

    // 体力存储Key
    private const string StaminaKey = "Player_Stamina";
    private const int MaxStamina = 10;

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAd);
    }

    private void OnWatchAd()
    {
        // TODO: 接入广告SDK，广告播放完成后调用 OnAdComplete()
        Debug.Log("StaminaPopupUI: 请接入广告SDK后在此处播放广告");
        // 模拟广告完成：
        OnAdComplete();
    }

    private void OnAdComplete()
    {
        int current = PlayerPrefs.GetInt(StaminaKey, MaxStamina);
        current = Mathf.Min(current + staminaRewardPerAd, MaxStamina);
        PlayerPrefs.SetInt(StaminaKey, current);
        PlayerPrefs.Save();

        Debug.Log($"StaminaPopupUI: 体力+{staminaRewardPerAd}，当前体力={current}");
        gameObject.SetActive(false);
    }
}

