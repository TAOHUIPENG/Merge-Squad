using System;
using System.Collections;
using System.Collections.Generic;
using TTSDK;
using TTSDK.UNBridgeLib.LitJson;
using UnityEngine;

/// <summary>
/// 侧边栏管理器（单例）
///
/// 职责：
///   1. 游戏启动时（Awake）立即注册 tt.onShow 监听，确保不错过任何热启动回调。
///   2. Start 时调用 tt.checkScene 判断当前宿主是否支持侧边栏，驱动入口显隐。
///   3. 记录最新 onShow 启动信息，供 SidebarUI 判断是否从侧边栏启动。
///   4. 封装 tt.navigateToScene，供 SidebarUI 调用跳转。
///   5. 用户从侧边栏返回时触发 <see cref="OnSidebarReturned"/> 事件，驱动奖励界面弹出。
///
/// 接入说明：
///   - 将此脚本挂载到游戏启动时最早激活的 GameObject（如 AdManager 同级）。
///   - SidebarRewardUI 订阅 SidebarManager.OnSidebarReturned 事件以自动弹出。
///   - MenuUI 通过 SidebarManager.IsSidebarSupported 控制入口按钮显隐。
/// </summary>
public class SidebarManager : MonoBehaviour
{
    // ── 场景常量 ──────────────────────────────────────────
    /// <summary>tt.checkScene 和 tt.navigateToScene 使用的场景名</summary>
    private const string SidebarSceneName  = "sidebar";
    /// <summary>tt.onShow 回调中 LaunchOption.Scene 的抖音侧边栏场景值（021）</summary>
    private const string SidebarSceneValue = "021";

    // ── 持久化 Key ────────────────────────────────────────
    private const string RewardClaimedKey = "Sidebar_RewardClaimed";

    /// <summary>轮询间隔（秒）：每隔此时间重新广播一次挂起的返回事件</summary>
    private const float RetryInterval = 0.5f;
    /// <summary>最长等待时间（秒）：超时后停止轮询，避免永久挂起</summary>
    private const float RetryTimeout  = 15f;

    // ── 单例 ─────────────────────────────────────────────
    public static SidebarManager Instance { get; private set; }

    // ── 公开事件 ──────────────────────────────────────────
    /// <summary>用户从侧边栏返回游戏时触发（tt.onShow scene == 侧边栏场景值）</summary>
    public static event Action OnSidebarReturned;

    // ── 状态 ─────────────────────────────────────────────
    /// <summary>当前宿主是否支持侧边栏（由 CheckScene 决定）</summary>
    public bool IsSidebarSupported { get; private set; }

    /// <summary>最新一次 tt.onShow 是否来自侧边栏</summary>
    public bool IsLaunchedFromSidebar { get; private set; }

    /// <summary>侧边栏奖励是否已领取（持久化，领取后入口永久隐藏）</summary>
    public bool IsRewardClaimed => TTPlayerPrefs.GetInt(RewardClaimedKey, 0) == 1;

    /// <summary>
    /// 从侧边栏返回事件已触发、但尚未被 SidebarRewardUI 消费的挂起标志。
    /// 用于处理"事件触发时 SidebarRewardUI 所在场景尚未加载"的竞态问题。
    /// 调用 <see cref="ConsumeReturnFlag"/> 消费后自动清除。
    /// </summary>
    public bool HasPendingReturn { get; private set; }

    /// <summary>正在运行的轮询协程引用，用于重复启动时先停旧的</summary>
    private Coroutine _retryCoroutine;

    // ── 生命周期 ──────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 【关键】必须在 Awake 最早注册 OnShow，防止热启动时错过回调
        RegisterOnShowListener();
    }

    private void Start()
    {
        // 检查当前宿主是否支持侧边栏（结果异步回调，驱动 MenuUI 显隐入口）
        CheckSceneSupport();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnregisterOnShowListener();
            Instance = null;
        }
    }

    // ── 公开接口 ──────────────────────────────────────────

    /// <summary>跳转到侧边栏（由 SidebarUI 进入按钮调用）</summary>
    public void NavigateToSidebar()
    {
//#if UNITY_WEBGL && !UNITY_EDITOR

    

        var data = new JsonData
        {
            ["scene"] = "sidebar",
        };
        TT.NavigateToScene(data, () =>
        {
            Debug.Log("navigate to scene success");
        }, () =>
        {
            Debug.Log("navigate to scene complete");
        }, (errCode,errMsg) =>
        {
            Debug.Log($"navigate to scene error, errCode:{errCode}, errMsg:{errMsg}");
        });
//#else
        // 编辑器/非 WebGL 平台：模拟从侧边栏返回（方便调试）
       // Debug.Log("[SidebarManager] Editor 模式：模拟侧边栏跳转并立即返回");
        SimulateSidebarReturn();
//#endif
    }

    // ── 私有方法 ──────────────────────────────────────────

    private void RegisterOnShowListener()
    {
//#if UNITY_WEBGL && !UNITY_EDITOR
        TTSDK.TT.GetAppLifeCycle().OnShow += HandleOnShow;
        Debug.Log("[SidebarManager] tt.onShow 监听已注册");
//#else
      //  Debug.Log("[SidebarManager] Editor 模式：跳过 tt.onShow 注册");
//#endif
    }

    private void UnregisterOnShowListener()
    {
//#if UNITY_WEBGL && !UNITY_EDITOR
        TTSDK.TT.GetAppLifeCycle().OnShow -= HandleOnShow;
//#endif
    }

    /// <summary>
    /// tt.onShow 回调：更新最新启动状态，判断是否来自侧边栏。
    /// 若来自侧边栏，触发 OnSidebarReturned 事件。
    /// </summary>
//#if UNITY_WEBGL && !UNITY_EDITOR
    private void HandleOnShow(Dictionary<string, object> param)
    {
        if (param.ContainsKey("location"))
        {
            IsLaunchedFromSidebar = param["location"]?.ToString() == "sidebar_card";
        }
        else
        {
            IsLaunchedFromSidebar = false;
        }

        Debug.Log($"[SidebarManager] tt.onShow scene={TTSDK.UNBridgeLib.LitJson.JsonMapper.ToJson(param)}, fromSidebar={IsLaunchedFromSidebar}");

        if (IsLaunchedFromSidebar)
        {
            Debug.Log("[SidebarManager] 从侧边栏返回，触发奖励流程");
            // 先置挂起标志，再触发事件：
            //   - 若 SidebarRewardUI 已加载 → 事件直接处理，ConsumeReturnFlag 会清除标志
            //   - 若 SidebarRewardUI 尚未加载 → 标志保留，轮询协程持续重试直到被消费
            HasPendingReturn = true;
            OnSidebarReturned?.Invoke();
            StartRetryPolling();
        }
    }
//#endif

    private void CheckSceneSupport()
    {
//#if UNITY_WEBGL && !UNITY_EDITOR
      
        TT.CheckScene(TTSideBar.SceneEnum.SideBar, isExist =>
        {
            Debug.Log("check scene success，");
            
            IsSidebarSupported = isExist;
            Debug.Log($"[SidebarManager] CheckScene 结果 isExist={isExist}");
            NotifySidebarSupportChanged();
            
        }, () =>
        {
            Debug.Log("check scene complete");
        }, (errCode, errMsg) =>
        {
            Debug.Log($"check scene error, errCode:{errCode}, errMsg:{errMsg}");
            IsSidebarSupported = false;
            Debug.LogWarning($"[SidebarManager] CheckScene 失败: {errMsg}，默认不展示入口");
            NotifySidebarSupportChanged();
        });
//#else
        // 编辑器/非 WebGL：默认展示入口，方便调试
        IsSidebarSupported = true;
        Debug.Log("[SidebarManager] Editor 模式：默认 IsSidebarSupported=true");
        NotifySidebarSupportChanged();
//#endif
    }

    /// <summary>通知 MenuUI 刷新入口按钮显隐状态</summary>
    private void NotifySidebarSupportChanged()
    {
        var menuUI = FindObjectOfType<MenuUI>();
        menuUI?.RefreshSidebarEntryVisibility();
    }

    /// <summary>编辑器调试用：模拟从侧边栏返回</summary>
    private void SimulateSidebarReturn()
    {
#if UNITY_EDITOR
        IsLaunchedFromSidebar = true;
        HasPendingReturn = true;
        OnSidebarReturned?.Invoke();
        StartRetryPolling();
#endif
    }

    /// <summary>
    /// 消费挂起的侧边栏返回标志（由 SidebarRewardUI 在处理奖励后调用）。
    /// 同时停止轮询协程，防止场景重载后重复触发奖励界面。
    /// </summary>
    public void ConsumeReturnFlag()
    {
        HasPendingReturn = false;
        if (_retryCoroutine != null)
        {
            StopCoroutine(_retryCoroutine);
            _retryCoroutine = null;
            Debug.Log("[SidebarManager] 轮询已停止，挂起标志已消费");
        }
    }

    /// <summary>
    /// 启动（或重启）轮询协程：每隔 <see cref="RetryInterval"/> 秒重新广播
    /// <see cref="OnSidebarReturned"/>，直到标志被消费或超过 <see cref="RetryTimeout"/>。
    /// </summary>
    private void StartRetryPolling()
    {
        if (_retryCoroutine != null)
            StopCoroutine(_retryCoroutine);
        _retryCoroutine = StartCoroutine(RetryPendingReturnCoroutine());
    }

    private IEnumerator RetryPendingReturnCoroutine()
    {
        float elapsed = 0f;
        Debug.Log("[SidebarManager] 开始轮询侧边栏返回事件（等待 SidebarRewardUI 就绪）");

        while (HasPendingReturn && elapsed < RetryTimeout)
        {
            yield return new WaitForSeconds(RetryInterval);
            elapsed += RetryInterval;

            if (!HasPendingReturn) break;   // 已在等待期间被消费

            Debug.Log($"[SidebarManager] 轮询重试广播 OnSidebarReturned（已等待 {elapsed:F1}s）");
            OnSidebarReturned?.Invoke();
        }

        if (HasPendingReturn)
        {
            // 超时：不再继续轮询，但保留标志供下次场景激活时检查
            Debug.LogWarning($"[SidebarManager] 轮询超时（{RetryTimeout}s），SidebarRewardUI 始终未响应");
        }

        _retryCoroutine = null;
    }

    // ── 奖励领取 ──────────────────────────────────────────

    /// <summary>
    /// 标记侧边栏奖励已领取并持久化。
    /// 调用后 IsRewardClaimed == true，入口按钮将被隐藏。
    /// </summary>
    public void MarkRewardClaimed()
    {
        TTPlayerPrefs.SetInt(RewardClaimedKey, 1);
        TTPlayerPrefs.Save();
        NotifySidebarSupportChanged();  // 通知 MenuUI 刷新（隐藏入口）
    }
}
