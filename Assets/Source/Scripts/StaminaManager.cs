using System;
using UnityEngine;

/// <summary>
/// 体力系统管理器（静态工具类）
/// - 最大体力：25
/// - 每局消耗：5
/// - 自动回复：每分钟 +1（基于真实时间）
/// - 关卡通关奖励：+2
/// - 看广告奖励：+10
/// </summary>
public static class StaminaManager
{
    // ── 常量 ──────────────────────────────────────────────
    public const int MaxStamina       = 25;
    public const int GameCost         = 5;
    public const int WinReward        = 2;
    public const int AdReward         = 10;
    public const int RecoveryPerMin   = 1;

    private const string StaminaKey       = "Player_Stamina";
    private const string LastRecoveryKey  = "Stamina_LastRecoveryTicks";

    // ── 公开接口 ──────────────────────────────────────────

    /// <summary>获取当前体力（自动先结算离线回复）</summary>
    public static int Get()
    {
        ApplyOfflineRecovery();
        return PlayerPrefs.GetInt(StaminaKey, MaxStamina);
    }

    /// <summary>直接设置体力（会 Clamp 到 [0, Max]）</summary>
    public static void Set(int value)
    {
        PlayerPrefs.SetInt(StaminaKey, Mathf.Clamp(value, 0, MaxStamina));
        PlayerPrefs.Save();
    }

    /// <summary>增加体力</summary>
    public static void Add(int amount) => Set(Get() + amount);

    /// <summary>体力是否足够开始一局游戏</summary>
    public static bool CanStartGame() => Get() >= GameCost;

    /// <summary>开始游戏时扣除体力，并记录回复基准时间</summary>
    public static void ConsumeForGame()
    {
        Set(Get() - GameCost);
        SaveRecoveryTimestamp();
    }

    // ── 离线回复计算 ──────────────────────────────────────

    private static void ApplyOfflineRecovery()
    {
        string tickStr = PlayerPrefs.GetString(LastRecoveryKey, "");

        // 首次使用：记录当前时间并返回
        if (string.IsNullOrEmpty(tickStr))
        {
            SaveRecoveryTimestamp();
            return;
        }

        if (!long.TryParse(tickStr, out long ticks)) return;

        DateTime lastTime     = new DateTime(ticks, DateTimeKind.Utc);
        double   minutesPassed = (DateTime.UtcNow - lastTime).TotalMinutes;
        int      recovered     = (int)(minutesPassed * RecoveryPerMin);

        if (recovered <= 0) return;

        int current    = PlayerPrefs.GetInt(StaminaKey, MaxStamina);
        int newStamina = Mathf.Min(current + recovered, MaxStamina);
        PlayerPrefs.SetInt(StaminaKey, newStamina);

        // 基准时间只向前推「实际回复的分钟数」，保留余数
        DateTime newBase = lastTime.AddMinutes(recovered);
        PlayerPrefs.SetString(LastRecoveryKey, newBase.Ticks.ToString());
        PlayerPrefs.Save();
    }

    private static void SaveRecoveryTimestamp()
    {
        PlayerPrefs.SetString(LastRecoveryKey, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }
}

