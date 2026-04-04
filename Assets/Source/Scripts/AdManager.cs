using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 广告管理器（单例）
/// 已接入抖音小游戏 TTSDK 插页广告与激励视频广告。
///
/// 使用步骤：
///   1. 在 Inspector 中配置 _rewardedAdUnits（每个场景对应一个广告位 ID）以及 _interstitialAdId
///   2. 游戏启动时调用 AdManager.Instance.Init()
///   3. 需要展示激励广告时调用 ShowRewarded(sceneName, ...)，传入对应场景名称
///   4. 需要展示插页广告时调用 ShowInterstitial(...)
///
/// 注意：
///   - 激励广告每个广告位独立预加载，关闭后自动重载
///   - 插页广告实例只能展示一次，每次调用都会创建新实例
///   - 两个插页广告展示间隔不能少于 30s（SDK 限制）
/// </summary>
public class AdManager : MonoBehaviour
{
    // ── 场景名常量（所有调用方均应引用此处，避免拼写不一致）────────────────────
    public static class Scenes
    {
        public const string EnemyDoubleReward = "EnemyDoubleReward";
        public const string CoinPopup         = "CoinPopup";
        public const string FreeCoin          = "FreeCoin";
        public const string Revive            = "Revive";
        public const string FailDouble        = "FailDouble";
        public const string IncreasePowerUp   = "IncreasePowerUp";
        public const string IncreaseFireRate  = "IncreaseFireRate";
        public const string StaminaPopup      = "StaminaPopup";
        public const string WinDouble         = "WinDouble";
        // 升级界面（UpgradesHandle 中通过 SerializeField 可覆盖）
        public const string UpgradeAll        = "upgrade_all";
        public const string UpgradeSkill      = "upgrade_skill";
    }

    // ── 激励广告位配置 ────────────────────────────────────────────────────────
    [Serializable]
    public class RewardedAdUnit
    {
        [Tooltip("场景/位置标识，调用 ShowRewarded 时传入此名称")]
        public string sceneName;
        [Tooltip("抖音后台配置的激励广告位 ID")]
        public string adUnitId;
    }

    [Header("抖音激励广告位配置")]
    [SerializeField] private RewardedAdUnit[] _rewardedAdUnits;

    [Header("抖音插页广告位 ID")]
    [SerializeField] private string _interstitialAdId = "YOUR_INTERSTITIAL_AD_ID";

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

        SceneManager.sceneLoaded += OnSceneLoaded;

        TTSDK.TT.InitSDK((code,evn) =>
        {
            if (code == 0)
            {
                Debug.Log("[AdManager] TTSDK 初始化成功");
                Init();
            }
            else
            {
                Debug.LogWarning($"[AdManager] TTSDK 初始化失败 errCode={code}, event={evn}");
            }
        });
        
        // 启动时自动预加载所有广告位，无需外部调用 Init()
       // Init();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景切换后抖音 SDK 会销毁旧的广告实例（errCode 2005），需要重新创建。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 只有已经完成过初始化（有广告实例）才需要重建；首次初始化由 TTSDK.InitSDK 回调触发
        if (_rewardedAdMap.Count > 0)
        {
            Debug.Log($"[AdManager] 场景切换到 [{scene.name}]，重新加载所有激励广告位");
            // 清空旧的（已被 SDK 销毁的）实例，再重新创建
            _rewardedAdMap.Clear();
            Init();
        }
    }

    private void Start()
    {
        
    }

    // ── 内部状态 ─────────────────────────────────────────────────────────────

    // 每个 sceneName 对应一个预加载的激励广告实例
    private readonly Dictionary<string, TTSDK.TTRewardedVideoAd> _rewardedAdMap
        = new Dictionary<string, TTSDK.TTRewardedVideoAd>();

    // 每个 sceneName 对应当次展示的业务回调
    private readonly Dictionary<string, Action>         _pendingRewarded = new Dictionary<string, Action>();
    private readonly Dictionary<string, Action>         _pendingClosed   = new Dictionary<string, Action>();
    private readonly Dictionary<string, Action<string>> _pendingFailed   = new Dictionary<string, Action<string>>();

    // ── 初始化 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 初始化广告 SDK 并预加载所有已配置的激励广告位。在游戏启动时调用一次。
    /// </summary>
    public void Init()
    {
        Debug.Log("[AdManager] 初始化广告 SDK（TTSDK）");

        if (_rewardedAdUnits == null || _rewardedAdUnits.Length == 0)
        {
            Debug.LogWarning("[AdManager] 未配置任何激励广告位，请在 Inspector 中填写 _rewardedAdUnits");
            return;
        }

        foreach (var unit in _rewardedAdUnits)
        {
            if (string.IsNullOrEmpty(unit.sceneName) || string.IsNullOrEmpty(unit.adUnitId))
            {
                Debug.LogWarning("[AdManager] 激励广告位配置存在空项，已跳过");
                continue;
            }
            CreateAndLoadRewardedAd(unit.sceneName, unit.adUnitId);
        }
    }

    // ── 激励广告 ────────────────────────────────────────────────────────────

    private void CreateAndLoadRewardedAd(string sceneName, string adUnitId)
    {
        // 销毁旧实例（Destroy 后事件自然失效，无需手动取消订阅）
        if (_rewardedAdMap.TryGetValue(sceneName, out var oldAd) && oldAd != null)
        {
            oldAd.Destroy();
        }

        // 使用 TTSDK.TT 静态方法创建激励广告
        // 签名：(string videoAdId, Action<bool,int> closeCallback, Action<int,string> errCallback,
        //         bool multiton = false, string[] multitonRewardMsg = null,
        //         int multitonRewardTime = 0, bool progressTip = false)
        var ad = TTSDK.TT.CreateRewardedVideoAd(
            adUnitId,
            (isComplete, _) =>
            {
                if (isComplete)
                {
                    Debug.Log($"[AdManager] 激励广告[{sceneName}] —— 用户看完，发放奖励");
                    if (_pendingRewarded.TryGetValue(sceneName, out var onRewarded))
                        onRewarded?.Invoke();
                }
                else
                {
                    Debug.Log($"[AdManager] 激励广告[{sceneName}] —— 用户未看完，不发奖励");
                }

                if (_pendingClosed.TryGetValue(sceneName, out var onClosed))
                    onClosed?.Invoke();

                ClearPendingCallbacks(sceneName);

                // 广告关闭后立即预加载下一条
                CreateAndLoadRewardedAd(sceneName, adUnitId);
            },
            (errCode, errMsg) =>
            {
                Debug.LogWarning($"[AdManager] 激励广告[{sceneName}] 错误 errCode={errCode}, msg={errMsg}");
                if (_pendingFailed.TryGetValue(sceneName, out var onFailed))
                    onFailed?.Invoke($"errCode={errCode}: {errMsg}");

                ClearPendingCallbacks(sceneName);
            }
        );

        // 使用 lambda 订阅事件（自动转换为 SDK 自定义委托类型）
        ad.OnLoad  += () => Debug.Log($"[AdManager] 激励广告[{sceneName}] 加载完成，可展示");
        ad.OnError += (c, m) => Debug.LogWarning($"[AdManager] 激励广告[{sceneName}] 加载失败 errCode={c}, msg={m}");

        ad.Load();
        _rewardedAdMap[sceneName] = ad;
    }

    private void ClearPendingCallbacks(string sceneName)
    {
        _pendingRewarded.Remove(sceneName);
        _pendingClosed.Remove(sceneName);
        _pendingFailed.Remove(sceneName);
    }

    /// <summary>
    /// 展示指定场景的激励广告。
    /// <para>只有用户完整看完广告后才触发 <paramref name="onRewarded"/>。</para>
    /// </summary>
    /// <param name="sceneName">广告位场景名称，需与 Inspector 中配置的 sceneName 一致</param>
    /// <param name="onRewarded">用户看完广告后的奖励回调</param>
    /// <param name="onClosed">广告关闭回调（无论是否获得奖励）</param>
    /// <param name="onFailed">广告加载或展示失败回调，参数为错误描述</param>
    public void ShowRewarded(string sceneName, Action onRewarded, Action onClosed = null, Action<string> onFailed = null)
    {
        Debug.Log($"[AdManager] ShowRewarded scene={sceneName}");

#if UNITY_EDITOR
        // Editor 模式下直接回调成功，无需真实广告
        Debug.Log($"[AdManager] Editor 模式：模拟激励广告成功（scene={sceneName}）");
        onRewarded?.Invoke();
        onClosed?.Invoke();
        return;
#endif

        if (!_rewardedAdMap.TryGetValue(sceneName, out var ad) || ad == null)
        {
            Debug.LogWarning($"[AdManager] 场景[{sceneName}] 的激励广告实例不存在，请检查 Inspector 配置");
            onFailed?.Invoke($"场景[{sceneName}] 广告尚未就绪");
            return;
        }

        _pendingRewarded[sceneName] = onRewarded;
        _pendingClosed[sceneName]   = onClosed;
        _pendingFailed[sceneName]   = onFailed;
        ad.Show();
    }

    // ── 插页广告 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 展示插页广告。
    /// <para>注意：两次展示间隔不能少于 30s（SDK 限制）。</para>
    /// </summary>
    /// <param name="onClosed">广告关闭回调</param>
    /// <param name="onFailed">广告加载或展示失败回调，参数为错误描述</param>
    public void ShowInterstitial(Action onClosed = null, Action<string> onFailed = null)
    {
        Debug.Log("[AdManager] ShowInterstitial");

        // 插页广告实例只能展示一次，每次都需要创建新实例
        // 使用新版 param 接口（旧版 4-param 已废弃），通过事件设置回调
        var ad = TTSDK.TT.CreateInterstitialAd(
            new TTSDK.CreateInterstitialAdParam { InterstitialAdId = _interstitialAdId }
        );
        ad.OnError += (errCode, errMsg) =>
        {
            Debug.LogWarning($"[AdManager] 插页广告错误 errCode={errCode}, msg={errMsg}");
            onFailed?.Invoke($"errCode={errCode}: {errMsg}");
        };
        ad.OnClose += () =>
        {
            Debug.Log("[AdManager] 插页广告已关闭");
            onClosed?.Invoke();
        };
        ad.OnLoad += () =>
        {
            Debug.Log("[AdManager] 插页广告已加载，正在展示");
            ad.Show();
        };
        ad.Load();
    }
}
