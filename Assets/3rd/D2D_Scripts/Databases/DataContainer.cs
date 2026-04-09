using System;
using D2D.Common;
using UnityEngine;

namespace D2D.Databases
{
    /// <summary>
    /// 通用数据容器，使用 TTPlayerPrefs 持久化（抖音环境自动路由到 TTSDK.TTStorage）。
    /// 支持 int / float / string；其他类型通过 JSON 序列化后存为字符串。
    /// </summary>
    public class DataContainer<T> : TrackableValue<T>
    {
        public override T Value
        {
            get
            {
                if (_alwaysLoad)
                    return LoadFromPrefs();

                if (!_wasLoad)
                {
                    _value = LoadFromPrefs();
                    _wasLoad = true;
                }

                return _value;
            }
            set
            {
                base.Value = value;
                _value = value;

                if (_alwaysSave)
                    SaveToPrefs(value);
            }
        }

        private T _value;

        /// <summary>存储中是否存在该键</summary>
        public bool IsEmpty => !TTPlayerPrefs.HasKey(_key);

        private readonly string _key;
        private readonly T _defaultValue;
        private bool _wasLoad;
        private bool _alwaysLoad;
        private bool _alwaysSave;

        public DataContainer(string key, T defaultValue, bool alwaysLoad = true, bool alwaysSave = true)
            : base(defaultValue)
        {
            _key = key;
            _defaultValue = defaultValue;
            _alwaysLoad = alwaysLoad;
            _alwaysSave = alwaysSave;
        }

        public void Clear() => TTPlayerPrefs.DeleteKey(_key);

        public void Save() => SaveToPrefs(_value);

        public void Load() => _value = LoadFromPrefs();

        // ── 内部 PlayerPrefs 读写 ───────────────────────

        private void SaveToPrefs(T value)
        {
            if (typeof(T) == typeof(int))
                TTPlayerPrefs.SetInt(_key, Convert.ToInt32(value));
            else if (typeof(T) == typeof(float))
                TTPlayerPrefs.SetFloat(_key, Convert.ToSingle(value));
            else if (typeof(T) == typeof(string))
                TTPlayerPrefs.SetString(_key, value as string ?? "");
            else if (typeof(T) == typeof(bool))
                TTPlayerPrefs.SetInt(_key, Convert.ToBoolean(value) ? 1 : 0);
            else
                TTPlayerPrefs.SetString(_key, JsonUtility.ToJson(new JsonWrapper<T>(value)));

            TTPlayerPrefs.Save();
        }

        private T LoadFromPrefs()
        {
            if (!TTPlayerPrefs.HasKey(_key))
                return _defaultValue;

            try
            {
                if (typeof(T) == typeof(int))
                    return (T)(object)TTPlayerPrefs.GetInt(_key);
                if (typeof(T) == typeof(float))
                    return (T)(object)TTPlayerPrefs.GetFloat(_key);
                if (typeof(T) == typeof(string))
                    return (T)(object)TTPlayerPrefs.GetString(_key);
                if (typeof(T) == typeof(bool))
                    return (T)(object)(TTPlayerPrefs.GetInt(_key) != 0);

                // 其他复杂类型走 JSON 路径
                string json = TTPlayerPrefs.GetString(_key, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var wrapper = JsonUtility.FromJson<JsonWrapper<T>>(json);
                    if (wrapper != null) return wrapper.value;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DataContainer] 读取 '{_key}' 失败，返回默认值。原因：{e.Message}");
            }

            return _defaultValue;
        }

        // ── JSON 包装器（用于 int/float/string 以外的类型）──

        [Serializable]
        private class JsonWrapper<TVal>
        {
            public TVal value;
            public JsonWrapper(TVal v) => value = v;
        }
    }
}