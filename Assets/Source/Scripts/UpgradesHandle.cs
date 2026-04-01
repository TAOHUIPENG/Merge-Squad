using D2D;
using D2D.Core;
using D2D.Utilities;
using NaughtyAttributes;
using SRF;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static D2D.Utilities.CommonGameplayFacade;

public class UpgradesHandle : Unit
{
    /// Game With Members
    [SerializeField] private MemberUpgrades[] rareMemberUpgrades;
    [SerializeField] private MemberUpgrades[] mediumMemberUpgrades;
    [SerializeField] private MemberUpgrades[] commonMemberUpgrades;
    [SerializeField] private MemberUpgrades baseUpgrade;

    /// Game With Skills
    [SerializeField] private Upgrades[] allUpgrades;

    [SerializeField] private float rareChance = 10f;
    [SerializeField] private float mediumChance = 30f;

    [SerializeField] private StatsUpgrades[] statsUpgrades;

    [Header("广告升级")]
    [Tooltip("指定哪个升级项需要看广告才能获得，留空则全部不需要（将 Double Skill asset 拖入此处）")]
    [SerializeField] private Upgrades adGatedUpgrade;

    [Tooltip("「全都要」按钮对应的激励广告位场景名，需与 AdManager Inspector 中配置的 sceneName 一致")]
    [SerializeField] private string rewardedSceneUpgradeAll = AdManager.Scenes.UpgradeAll;
    [Tooltip("广告门控升级项对应的激励广告位场景名，需与 AdManager Inspector 中配置的 sceneName 一致")]
    [SerializeField] private string rewardedSceneAdGated = AdManager.Scenes.UpgradeSkill;

    [Header("调试升级")]
    [SerializeField] private bool isDebug = false;
    [SerializeField, ShowIf("isDebug")] private MemberUpgrades[] debugUpgrades;

    private bool createdStatsUpgrade = false;

    private UpgradeUI upgradeUI;

    private List<MemberUpgrades> m_AvailableMemberUpgrades = new();
    private List<MemberUpgrades> m_AllMembers = new();

    // 记录本轮展示的全部升级项，供「全都要」功能使用
    private readonly List<Upgrades> _currentUpgrades = new();

    private void Awake()
    {
        upgradeUI = Find<UpgradeUI>();

        _upgradesHandle = this;

        _gameProgress.OnLevelUp += OnLevelUp;

        if (_db.UnlockedMembers.IsNullOrEmpty())
        {
            foreach (var member in commonMemberUpgrades)
            {
                _db.UnlockedMembers.Add(member.name);

                _db.SaveMembers();
            }
        }

        m_AllMembers = rareMemberUpgrades.Concat(commonMemberUpgrades).Concat(mediumMemberUpgrades).ToList();

        m_AvailableMemberUpgrades = m_AllMembers;

        /// Upgrade Block Visuals
        //_upgradesBlock.CreateItemsUI(allUpgrades);

        /*if (isDebug)
        {
            m_AvailableMemberUpgrades = new(debugUpgrades);

            return;
        }*/

        /*
        foreach (var item in _db.UnlockedMembers)
        {
            m_AvailableMemberUpgrades.Add(m_AllMembers.First(x => x.name == item));
        }*/
    }

    private void OnLevelUp(int level)
    {
        if (_stateMachine.Last.Is<LoseState>() || _stateMachine.Last.Is<WinState>())
        {
            return;
        }

        ShowUpgradeUI();
    }
    private void ShowUpgradeUI()
    {
        upgradeUI.ShowUI();

        var buttons = upgradeUI.GetButtons();
        
        GameWithSkills(buttons);

        // 绑定「全都要」按钮：看广告后3项升级全部生效
        if (upgradeUI.GetAllButton != null)
        {
            upgradeUI.GetAllButton.onClick.RemoveAllListeners();
            upgradeUI.GetAllButton.onClick.AddListener(() =>
                AdManager.Instance.ShowRewarded(rewardedSceneUpgradeAll, () => UpgradeAll()));
        }
        
        // GameWithMembers(buttons);
    }
    private void GameWithSkills(UpgradeButtonUI[] buttons)
    {
        List<Upgrades> usedUpgrade = new List<Upgrades>();
        _currentUpgrades.Clear();

        for (int i = 0; i < buttons.Length; i++)
        {
            var index = i;

            buttons[index].UpgradeButton.onClick.RemoveAllListeners();

            var upgrade = allUpgrades.Except(usedUpgrade).ToArray().Random();
            var capturedUpgrade = upgrade;

            _currentUpgrades.Add(upgrade);

            bool isAdGated = adGatedUpgrade != null && upgrade == adGatedUpgrade;
            buttons[index].SetAdGated(isAdGated);

            if (isAdGated)
            {
                buttons[index].UpgradeButton.onClick.AddListener(() =>
                    AdManager.Instance.ShowRewarded(rewardedSceneAdGated, () => Upgrade(capturedUpgrade)));
            }
            else
            {
                buttons[index].UpgradeButton.onClick.AddListener(() => Upgrade(capturedUpgrade));
            }

            buttons[index].InitButtonUI(upgrade.Icon, upgrade.UpgradeText);

            usedUpgrade.Add(upgrade);
        }
    }
    private void GameWithMembers(UpgradeButtonUI[] buttons)
    {
        createdStatsUpgrade = false;
        List<MemberUpgrades> usedMemberUpgrade = new List<MemberUpgrades>();

        for (int i = 0; i < buttons.Length; i++)
        {
            var index = i;

            buttons[index].UpgradeButton.onClick.RemoveAllListeners();

            if (!createdStatsUpgrade)
            {
                var statsUpgrade = statsUpgrades.Random();

                buttons[index].UpgradeButton.onClick.AddListener(() => Upgrade(statsUpgrade));
                buttons[index].InitButtonUI(statsUpgrade.Icon, statsUpgrade.UpgradeText);

                createdStatsUpgrade = true;

                continue;
            }

            MemberUpgrades memberUpgrade;
            List<MemberUpgrades> availableMemberUpgrades = GetRandomAvailableMembers();

            if (availableMemberUpgrades.Count > 1)
            {
                memberUpgrade = availableMemberUpgrades.Except(usedMemberUpgrade).ToArray().Random();
            }
            else
            {
                memberUpgrade = availableMemberUpgrades.ToArray().Random();
            }

            buttons[index].UpgradeButton.onClick.AddListener(() => Upgrade(memberUpgrade));
            buttons[index].InitButtonUI(memberUpgrade.Icon, memberUpgrade.UpgradeText);

            usedMemberUpgrade.Add(memberUpgrade);
        }
    }

    private List<MemberUpgrades> GetRandomAvailableMembers()
    {
        MembersComparer membersComparer = new MembersComparer();
        List<MemberUpgrades> availableMemberUpgrades = new();

        if (Random.Range(0, 100) < rareChance)
        {
            availableMemberUpgrades = rareMemberUpgrades.Intersect(m_AvailableMemberUpgrades, membersComparer).ToList();

            if (availableMemberUpgrades.Count > 0)
            {
                return availableMemberUpgrades;
            }
        }

        if (availableMemberUpgrades.Count == 0 && Random.Range(0, 100) < mediumChance)
        {
            availableMemberUpgrades = mediumMemberUpgrades.Intersect(m_AvailableMemberUpgrades, membersComparer).ToList();

            if (availableMemberUpgrades.Count > 0)
            {
                return availableMemberUpgrades;
            }
        }

        if (availableMemberUpgrades.Count == 0)
        {
            availableMemberUpgrades = commonMemberUpgrades.Intersect(m_AvailableMemberUpgrades, membersComparer).ToList();
        }


        if (availableMemberUpgrades.Count == 0)
        {
            availableMemberUpgrades = new List<MemberUpgrades>() 
            { baseUpgrade };
        }

        return availableMemberUpgrades;
    }

    private void Upgrade(Upgrades upgrades)
    {
        ApplyUpgrade(upgrades);
        _audioManager.PlayOneShot(_gameData.spawnClip, 0.4f);
        upgradeUI.HideUI();
        //_upgradesBlock.UnlockUpgrade(upgrades);
    }

    /// <summary>看广告后将本轮全部3个升级项一次性生效</summary>
    private void UpgradeAll()
    {
        foreach (var upgrade in _currentUpgrades)
            ApplyUpgrade(upgrade);

        _audioManager.PlayOneShot(_gameData.spawnClip, 0.4f);
        upgradeUI.HideUI();
    }

    /// <summary>执行单条升级的核心逻辑（不包含音效和关闭UI）</summary>
    private void ApplyUpgrade(Upgrades upgrades)
    {
        switch (upgrades.GetUpgradeType())
        {
            case UpgradesType.Member:

                UpgradeMember(upgrades as MemberUpgrades);
                break;

            case UpgradesType.Stats:

                UpgradeStats(upgrades as StatsUpgrades);
                break;

            case UpgradesType.Skill:

                var upgrade = upgrades as SkillUpgrades;
                upgrade.Activate();

                break;
        }
    }
    private void UpgradeStats(StatsUpgrades upgrade)
    {
        switch (upgrade.StatsUpgradesType)
        {
            case StatsUpgradesType.AttackPower:

                _squad.IncreaseFirePower(upgrade.IncreasePercent);
                break;

            case StatsUpgradesType.AttackRate:

                _squad.IncreaseFireRate(upgrade.IncreasePercent);
                break;

            case StatsUpgradesType.Heal:

                _squad.HealSquad(upgrade.IncreasePercent);
                break;
        }
    }

    private void UpgradeMember(MemberUpgrades upgrade)
    {
        var newMember = Instantiate(upgrade.MemberPrefab, _squad.SpawnPosition, Quaternion.identity, _squad.transform).GetComponent<SquadMember>();
        _squad.AddMember(newMember);
    }
}

public class MembersComparer : IEqualityComparer<MemberUpgrades>
{
    public bool Equals(MemberUpgrades x, MemberUpgrades y)
    {
        return x.UpgradeText == y.UpgradeText;
    }

    public int GetHashCode(MemberUpgrades obj)
    {
        //Check whether the object is null
        if (ReferenceEquals(obj, null)) return 0;

        return obj.name.GetHashCode();
    }
}