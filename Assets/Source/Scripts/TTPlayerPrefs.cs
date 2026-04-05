using UnityEngine;

/// <summary>
/// 跨平台 PlayerPrefs 封装。
///
/// 路由规则：
///   - 抖音 WebGL 真机环境 (UNITY_WEBGL &amp;&amp; !UNITY_EDITOR)
///       → TTSDK.TTStorage（持久化到抖音小游戏本地存储，重新进入游戏数据不丢失）
///   - Editor / 其他平台
///       → UnityEngine.PlayerPrefs（方便本地调试）
///
/// 用法：
///   将项目中所有 PlayerPrefs.XXX(…) 替换为 TTPlayerPrefs.XXX(…)，
///   API 签名与 UnityEngine.PlayerPrefs 完全一致，零学习成本。
///
/// 注意：
///   TTSDK.TTStorage 全部为同步接口，无需显式 Save()；
///   Save() 保留为空实现以兼容现有调用代码。
/// </summary>
public static class TTPlayerPrefs
{
    // ── Int ──────────────────────────────────────────────────────────────

    public static void SetInt(string key, int value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TTSDK.TTStorage.SetIntSync(key, value);
#else
        PlayerPrefs.SetInt(key, value);
#endif
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return TTSDK.TTStorage.GetIntSync(key, defaultValue);
#else
        return PlayerPrefs.GetInt(key, defaultValue);
#endif
    }

    // ── Float ────────────────────────────────────────────────────────────

    public static void SetFloat(string key, float value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TTSDK.TTStorage.SetFloatSync(key, value);
#else
        PlayerPrefs.SetFloat(key, value);
#endif
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return TTSDK.TTStorage.GetFloatSync(key, defaultValue);
#else
        return PlayerPrefs.GetFloat(key, defaultValue);
#endif
    }

    // ── String ───────────────────────────────────────────────────────────

    public static void SetString(string key, string value)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TTSDK.TTStorage.SetStringSync(key, value);
#else
        PlayerPrefs.SetString(key, value);
#endif
    }

    public static string GetString(string key, string defaultValue = "")
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return TTSDK.TTStorage.GetStringSync(key, defaultValue);
#else
        return PlayerPrefs.GetString(key, defaultValue);
#endif
    }

    // ── 通用 ─────────────────────────────────────────────────────────────

    public static bool HasKey(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return TTSDK.TTStorage.HasKeySync(key);
#else
        return PlayerPrefs.HasKey(key);
#endif
    }

    public static void DeleteKey(string key)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TTSDK.TTStorage.DeleteKeySync(key);
#else
        PlayerPrefs.DeleteKey(key);
#endif
    }

    public static void DeleteAll()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        TTSDK.TTStorage.DeleteAllSync();
#else
        PlayerPrefs.DeleteAll();
#endif
    }

    /// <summary>
    /// 兼容 PlayerPrefs.Save() 习惯调用。
    /// TTStorage 是同步写入，无需显式刷盘，此方法为空实现。
    /// </summary>
    public static void Save() { /* TTStorage 同步写入，不需要额外 flush */ }
}

