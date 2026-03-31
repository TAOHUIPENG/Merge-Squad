using System;
using UnityEngine;

/// <summary>
/// 广告管理器（单例）
/// 目前为纯接口存根，接入 SDK 后只需在对应方法内替换实现即可。
/// 
/// 使用示例：
///   // 初始化（在 App 启动时调用一次）
///   AdManager.Instance.Init();
///
///   // 展示插页广告
///   AdManager.Instance.ShowInterstitial(
///       onClosed: () => Debug.Log("插页关闭"),
///       onFailed: err => Debug.Log("插页失败: " + err));
///
///   // 展示激励广告（带奖励回调）
///   AdManager.Instance.ShowRewarded(
///       onRewarded: () => GiveReward(),
///       onClosed:   () => Debug.Log("激励关闭"),
///       onFailed:   err => Debug.Log("激励失败: " + err));
/// </summary>
public class AdManager : MonoBehaviour
{
    // ── 单例 ────────────────────────────────────────────────────────────────
    public static AdManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── 初始化 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 初始化广告 SDK。在游戏启动时调用一次。
    /// 接入 SDK 后在此处填写初始化代码（AppID、监听注册等）。
    /// </summary>
    public void Init()
    {
        Debug.Log("[AdManager] 初始化完成（当前为存根模式，未接入真实 SDK）");
        // TODO: 替换为真实 SDK 初始化，例如：
        // MaxSdk.InitializeSdk();
        // Advertisement.Initialize(gameId, testMode);
    }

    // ── 插页广告 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 展示插页广告。
    /// </summary>
    /// <param name="onClosed">广告正常关闭后回调（无论是否观看完毕）</param>
    /// <param name="onFailed">广告加载或展示失败回调，参数为错误描述</param>
    public void ShowInterstitial(Action onClosed = null, Action<string> onFailed = null)
    {
        Debug.Log("[AdManager] ShowInterstitial —— 存根调用（未接入真实 SDK）");

        // TODO: 替换为真实 SDK 展示逻辑，例如：
        // if (MaxSdk.IsInterstitialReady(adUnitId))
        //     MaxSdk.ShowInterstitial(adUnitId);
        // else
        //     onFailed?.Invoke("Interstitial not ready");

        // 存根：模拟广告立即关闭
        onClosed?.Invoke();
    }

    // ── 激励广告 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 展示激励广告。
    /// <para>只有用户真正看完广告后才会触发 <paramref name="onRewarded"/>，
    /// 中途跳过或失败只触发 <paramref name="onClosed"/> / <paramref name="onFailed"/>。</para>
    /// </summary>
    /// <param name="onRewarded">用户看完广告、发放奖励的回调</param>
    /// <param name="onClosed">广告关闭后回调（无论是否获得奖励）</param>
    /// <param name="onFailed">广告加载或展示失败回调，参数为错误描述</param>
    public void ShowRewarded(Action onRewarded, Action onClosed = null, Action<string> onFailed = null)
    {
        Debug.Log("[AdManager] ShowRewarded —— 存根调用（未接入真实 SDK）");

        // TODO: 替换为真实 SDK 展示逻辑，例如：
        // if (MaxSdk.IsRewardedAdReady(adUnitId))
        //     MaxSdk.ShowRewardedAd(adUnitId);
        // else
        //     onFailed?.Invoke("Rewarded not ready");

        // 存根：模拟用户看完广告并获得奖励
        onRewarded?.Invoke();
        onClosed?.Invoke();
    }
}

