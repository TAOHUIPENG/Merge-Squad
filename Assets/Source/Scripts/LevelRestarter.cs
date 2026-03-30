using D2D.Core;
using UnityEngine;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 关卡重新开始（不重新加载场景）。
/// 协调 EnemySpawn / SquadComponent / GameProgress 各自的 Reset，
/// 全程保持在 RunningState，不触发任何 UI 状态切换。
/// </summary>
public class LevelRestarter : MonoBehaviour
{
    public GameObject player;
    public static LevelRestarter Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── 公开接口 ────────────────────────────────────────────────

    /// <summary>
    /// 重新开始当前关卡（不重载场景）。
    /// 调用顺序：敌人清场 → 小队还原 → 进度归零 → UI 修正
    /// </summary>
    public void Restart()
    {
        Time.timeScale = 1f;

        // 1. 清除所有场上敌人，重置生成器
        _enemySpawn?.ResetSpawner();

        // 2. 销毁当前小队成员，从预制体重新生成
        _squad?.ResetSquad();

        // 3. 重置关卡计时、等级、XP 等进度数据
        _gameProgress?.ResetProgress();

        // 4. 确保游戏 HUD 可见（若之前被 FailUI/WinUI 隐藏过）
        UIGame.Instance?.Show();

        Debug.Log("[LevelRestarter] 关卡已重新开始");
    }
}

