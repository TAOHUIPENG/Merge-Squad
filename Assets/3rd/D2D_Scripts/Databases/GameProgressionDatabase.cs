using System;
using System.Collections.Generic;
using D2D.Common;
using D2D.Utilities;
using D2D.Core;
using D2D.Gameplay;
using NaughtyAttributes;
using UnityEngine;
using static D2D.Utilities.SettingsFacade;
using static D2D.Utilities.CommonLazyFacade;
using static D2D.Utilities.CommonGameplayFacade;

namespace D2D.Databases
{
    public class GameProgressionDatabase : GameStateMachineUser, ILazy
    {
        #region For analytics

        public readonly DataContainer<int> Reloads =
            new DataContainer<int>("ReloadsCount", 0);

        public readonly DataContainer<int> CompletedLevelsPerSession =
            new DataContainer<int>("CompletedLevelsPerSession", 0);

        #endregion

        // Stickman Squad Specified Vars
        public readonly DataContainer<float> PowerIncreasePercent =
            new DataContainer<float>("PowerIncreasePercent", 0);

        public readonly DataContainer<float> FireRateDecreasePercent =
            new DataContainer<float>("FireRateDecreasePercent", 0);

        public readonly DataContainer<float> PowerIncreaseLevel =
            new DataContainer<float>("PowerIncreaseLevel", 0);

        public readonly DataContainer<float> FireRateDecreaseLevel =
            new DataContainer<float>("FireRateDecreaseLevel", 0);

        public readonly DataContainer<string> UnlockableItem =
            new DataContainer<string>("UnlockableItem", "");

        public readonly DataContainer<float> UnlockableItemProgress =
            new DataContainer<float>("UnlockableItemProgress", 0f);

        public readonly DataContainer<string> LastUnlockedMember =
            new DataContainer<string>("LastUnlockedMember", "");

        // ── UnlockedMembers（List<string>，PlayerPrefs + JSON）──────────

        public List<string> UnlockedMembers
        {
            get
            {
                if (_UnlockedMembers != null)
                    return _UnlockedMembers;

                if (TTPlayerPrefs.HasKey(UnlockedMembersKey))
                {
                    var wrapper = JsonUtility.FromJson<StringListWrapper>(
                        TTPlayerPrefs.GetString(UnlockedMembersKey));
                    _UnlockedMembers = wrapper?.list ?? new List<string>();
                }
                else
                {
                    _UnlockedMembers = new List<string>();
                    SaveMembers();
                }

                return _UnlockedMembers;
            }
        }

        private List<string> _UnlockedMembers;

        public void SaveMembers()
        {
            var wrapper = new StringListWrapper { list = _UnlockedMembers ?? new List<string>() };
            TTPlayerPrefs.SetString(UnlockedMembersKey, JsonUtility.ToJson(wrapper));
            TTPlayerPrefs.Save();
        }

        [Serializable]
        private class StringListWrapper
        {
            public List<string> list = new List<string>();
        }

        private const string UnlockedMembersKey = "UnlockedMembers";

        // ── Common Vars ──────────────────────────────────────────────────

        public readonly DataContainer<int> PassedLevels =
            new DataContainer<int>("PassedLevels", 0);

        public readonly TrackableValue<float> Money =
            new TrackableValue<float>(value: 0, firstGet: () => TTPlayerPrefs.GetInt("Money"));

        public readonly DataContainer<int> LastSceneNumber =
            new DataContainer<int>("LastSceneNumber", 1);

        /// <summary>玩家昵称，首次生成后永久存档（格式：用户XXXX）</summary>
        public readonly DataContainer<string> Nickname =
            new DataContainer<string>("Nickname", "");

        public float TimeOfSceneLoad { get; private set; }

        public float TimeElapsedFromSceneLoad => Time.time - TimeOfSceneLoad;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ResetLevelsPerSession()
        {
            LazySugar.FindLazy<GameProgressionDatabase>().CompletedLevelsPerSession.Value = 0;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Money.Changed += SaveMoney;
            TimeOfSceneLoad = Time.time;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Money.Changed -= SaveMoney;
        }

        private void SaveMoney(float val)
        {
            TTPlayerPrefs.SetInt("Money", Money.Value.Round());
            TTPlayerPrefs.Save();
        }

        private void OnApplicationQuit()
        {
            SaveMoney(Money.Value.Round());
        }

        private void Start()
        {
            var level = this.Find<Level>();
            if (level != null)
                LastSceneNumber.Value = level.SceneNumber;
        }

        protected override void OnGameWin()
        {
            CompletedLevelsPerSession.Value++;
            PassedLevels.Value++;
        }

        /// <summary>重置所有游戏进度数据</summary>
        public static void Clear()
        {
            TTPlayerPrefs.SetInt("ReloadsCount", 0);
            TTPlayerPrefs.SetInt("CompletedLevelsPerSession", 0);
            TTPlayerPrefs.SetInt("PassedLevels", 0);
            TTPlayerPrefs.SetInt("LastSceneNumber", 1);
            TTPlayerPrefs.SetFloat("PowerIncreasePercent", 0f);
            TTPlayerPrefs.SetFloat("FireRateDecreasePercent", 0f);
            TTPlayerPrefs.SetFloat("PowerIncreaseLevel", 0f);
            TTPlayerPrefs.SetFloat("FireRateDecreaseLevel", 0f);
            TTPlayerPrefs.SetFloat("UnlockableItemProgress", 0f);
            TTPlayerPrefs.SetString("UnlockableItem", "");
            TTPlayerPrefs.SetString("LastUnlockedMember", "");
            TTPlayerPrefs.SetString("UnlockedMembers", "");
            TTPlayerPrefs.SetInt("Money", 0);
            TTPlayerPrefs.SetString("Nickname", "");
            TTPlayerPrefs.Save();
        }
    }
}