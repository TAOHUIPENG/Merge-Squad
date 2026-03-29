using System;
using D2D.Common;
using UnityEngine;

namespace D2D.Databases
{
    /// <summary>
    /// 通用数据容器，使用 PlayerPrefs 持久化。
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

        /// <summary>PlayerPrefs 中是否存在该键</summary>
        public bool IsEmpty => !PlayerPrefs.HasKey(_key);

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

        public void Clear() => PlayerPrefs.DeleteKey(_key);

        public void Save() => SaveToPrefs(_value);

        public void Load() => _value = LoadFromPrefs();

        // ── 内部 PlayerPrefs 读写 ───────────────────────

        private void SaveToPrefs(T value)
        {
            if (typeof(T) == typeof(int))
                PlayerPrefs.SetInt(_key, Convert.ToInt32(value));
            else if (typeof(T) == typeof(float))
                PlayerPrefs.SetFloat(_key, Convert.ToSingle(value));
            else if (typeof(T) == typeof(string))
                PlayerPrefs.SetString(_key, value as string ?? "");
            else
                PlayerPrefs.SetString(_key, JsonUtility.ToJson(new JsonWrapper<T>(value)));

            PlayerPrefs.Save();
        }

        private T LoadFromPrefs()
        {
            if (!PlayerPrefs.HasKey(_key))
                return _defaultValue;

            try
            {
                if (typeof(T) == typeof(int))
                    return (T)(object)PlayerPrefs.GetInt(_key);
                if (typeof(T) == typeof(float))
                    return (T)(object)PlayerPrefs.GetFloat(_key);
                if (typeof(T) == typeof(string))
                    return (T)(object)PlayerPrefs.GetString(_key);

                // 其他复杂类型走 JSON 路径
                string json = PlayerPrefs.GetString(_key, "");
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