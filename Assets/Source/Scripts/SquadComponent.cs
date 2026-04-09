using Cinemachine;
using D2D.Core;
using D2D.Utilities;
using SRF;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static D2D.Utilities.CommonGameplayFacade;

public class SquadComponent : GameStateMachineUser
{
    [Header("Squad Settings")]
    [SerializeField] private List<SquadMember> squadMembers;
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _formationRadius = 1.5f;

    [Header("Camera Settings")]
    [SerializeField] private CinemachineTargetGroup cinemachineTargetGroup;

    private Joystick _joystick;
    private FormationComponent _formation;

    private SquadMember openSquadMember;

    private float temporaryFireRateIncrease;
    private float temporaryFirePowerIncrease;

    private float currentSpeed;

    // 记录初始 Formation 位置，Restart 时归位
    private Vector3 _initialFormationPosition;

    // Awake 时自动保存的初始成员模板（非激活副本），无需 Inspector 赋值
    private readonly List<GameObject> _memberTemplates = new();
    public SquadMember LastSquadMember => squadMembers[squadMembers.Count - 1];

    public Vector3 SpawnPosition => squadMembers.Count > 0 ? squadMembers[squadMembers.Count - 1].transform.position - Vector3.back : _formation.transform.position;

    public float TemporaryFireRateIncrease => temporaryFireRateIncrease / 100;
    public float TemporaryFirePowerIncrease => temporaryFirePowerIncrease / 100;

    private const float CinemachineMemberRadius = 5f;

    private void Awake()
    {
        _squad = this;

        _formation = FindObjectOfType<SquadFormation>().Get<FormationComponent>();
        cinemachineTargetGroup = FindObjectOfType<CinemachineTargetGroup>();

        // 记录初始位置，Restart 时归位
        _initialFormationPosition = _formation.transform.position;

        squadMembers = GetComponentsInChildren<SquadMember>().ToList();

        // 立即把初始成员克隆为非激活模板，作为 Restart 的蓝本（无需 Inspector 赋值）
        // GetComponentsInChildren 默认只返回激活对象，所以此后加入的非激活模板不会被误拾
        foreach (var member in squadMembers)
        {
            var template = Instantiate(member.gameObject, transform);
            template.SetActive(false);
            _memberTemplates.Add(template);
        }

        foreach (var member in squadMembers)
        {
            member.Init();
            member.animancer.Layers[0].Play(member.animations.Idle);
            member.health.Died += () => MemberDie(member);
            member.TryShowDoubleRewardUI();
        }

        _stateMachine.On<WinState>(SqaudIdle);
        var squadMembersCount = squadMembers.Count;

        _formation.RecreateFormation(Vector3.zero, squadMembersCount <= 4 ? _formationRadius - .3f : _formationRadius, squadMembersCount - 1);
        SetMembersToCinemachineGroup();

        _gameData.SetProjectilePrefab(_gameData.pistolProjectile);
        _gameData.SetSideProjectiles(0);

        currentSpeed = _speed;
    }
    private void Update()
    {
        if (!_stateMachine.Last.Is<RunningState>())
        {
            return;
        }

         Movement();
         Shoot();
    }
    #region States
    protected override void OnGameRun()
    {
        _joystick = FindObjectOfType<Joystick>();
    }
    protected override void OnGameWin()
    {
        base.OnGameWin();

        foreach (var member in squadMembers)
        {
            if (member.animations.DanceAnimations != null && member.animations.DanceAnimations.Length > 0)
            {
                var clip = member.animations.DanceAnimations.Random();
                if (clip != null)
                    member.SetDanceAnimation(clip);
            }
        }
    }
    #endregion

    #region Squad Members Control
    public void AddMember(SquadMember member)
    {
        if (member != null && !squadMembers.Contains(member))
        {
            member.Init();
            squadMembers.Add(member);

            var squadMembersCount = squadMembers.Count;
            _formation.RecreateFormation(squadMembers[0].transform.position, squadMembersCount <= 4 ? _formationRadius - .3f : _formationRadius, squadMembersCount - 1);
            member.health.Died += () => MemberDie(member);
            member.TryShowDoubleRewardUI();
            SetMembersToCinemachineGroup();
            PunchScaleSquad();
        }

        CheckForEvolves(member);
        OrderMembers();
    }
    private void CheckForEvolves(SquadMember newMember)
    {
        var exceptNewMemberList =  new List<SquadMember>(squadMembers);
        exceptNewMemberList.Remove(newMember);

        if (exceptNewMemberList.Count > 0 && exceptNewMemberList.Any(x => x.EvolveLevel == newMember.EvolveLevel))
        {
            newMember.Evolve(exceptNewMemberList.First(x => x.EvolveLevel == newMember.EvolveLevel));
        }
    }
    private void PunchScaleSquad()
    {
        for (int i = squadMembers.Count - 1; i >= 0; i--)
        {
            squadMembers[i].PunchScaleWithDelay(_gameData.punchDelay * i);
        }
    }

    private void SpawnLvlUpVFX()
    {
        var vfx = Instantiate(_gameData.levelUpVFX, squadMembers[0].transform.position + Vector3.up / 5f, Quaternion.LookRotation(Vector3.up));
        Destroy(vfx, 2f);
    }

    private void SpawnLvlUpVFXColored(Color color)
    {
        var vfx = Instantiate(_gameData.levelUpVFX, squadMembers[0].transform.position + Vector3.up / 5f, Quaternion.LookRotation(Vector3.up));

        var particleSystems = vfx.ChildrenGets<ParticleSystem>();

        Gradient grad = new Gradient();
        grad.SetKeys(new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });

        foreach (var system in particleSystems)
        {
            var co = system.colorOverLifetime;
            co.color = grad;
        }

        Destroy(vfx, 2f);
    }

    private void OrderMembers()
    {
        squadMembers = squadMembers.OrderByDescending(x => x.EvolveLevel).ToList();
    }
    public void EvolveMembers(SquadMember flyingMember, SquadMember staticMember = null)
    {
        if (flyingMember.NextEvolution == null)
        {
            return;
        }

        var flyingMemberPos = flyingMember.transform.position;
        Vector3 staticMemberPos = Vector3.zero;
        
        if (squadMembers.Contains(flyingMember))
        {
            squadMembers.Remove(flyingMember);
            Destroy(flyingMember.gameObject);
        }

        if (staticMember != null && squadMembers.Contains(staticMember))
        {
            staticMemberPos = staticMember.transform.position;

            squadMembers.Remove(staticMember);
            Destroy(staticMember.gameObject);
        }
        
        Vector3 newPos = staticMember == null ? flyingMemberPos : staticMemberPos;

        var newMember = Instantiate(flyingMember.NextEvolution.gameObject, 
            newPos,
            Quaternion.identity, _squad.transform).Get<SquadMember>();

        newMember.animancer.Play(newMember.animations.Idle);
        AddMember(newMember);

        var squadMembersCount = squadMembers.Count;

        _formation.RecreateFormation(Vector3.zero, squadMembersCount <= 4 ? _formationRadius - .3f : _formationRadius, squadMembersCount - 1);
        SetMembersToCinemachineGroup();
        OrderMembers();

        SpawnLvlUpVFXColored(flyingMember.NextEvolution.memberColor);
    }
    private void MemberDie(SquadMember member)
    {
        if (squadMembers.Contains(member))
        {
            squadMembers.Remove(member);

            if (squadMembers.Count <= 0)
            {
                _stateMachine.Push(new LoseState());
                FailUI.Instance?.Show();
                return;
            }

            var squadMembersCount = squadMembers.Count;
            _formation.RecreateFormation(Vector3.zero, squadMembersCount <= 4 ? _formationRadius - .3f : _formationRadius, squadMembersCount - 1);
            SetMembersToCinemachineGroup();
        }

        OrderMembers();
    }
    private void SetMembersToCinemachineGroup()
    {
        /*foreach (var oldTarget in cinemachineTargetGroup.m_Targets)
        {
            cinemachineTargetGroup.RemoveMember(oldTarget.target);
        }

        foreach (var member in squadMembers)
        {
            cinemachineTargetGroup.AddMember(member.transform, 1, CinemachineMemberRadius + squadMembers.Count / 2);
        }*/
    }
    private void SqaudIdle()
    {
        foreach (var member in squadMembers)
        {
            member.animancer.Layers[0].Play(member.animations.Idle);
        }
    }

    /// <summary>
    /// 复活专用：清空死亡成员 → Formation 归位 → 从模板重新实例化 → 连接死亡回调。
    /// 不重置临时升级数值，保留玩家本局内的强化。
    /// </summary>
    public void ReviveSquad()
    {
        // 销毁所有仍在场景中的活跃成员（包含正在执行 GrayFadeoutDeath 的对象）
        // GetComponentsInChildren 默认 includeInactive=false，不会误删非激活的模板
        foreach (var member in GetComponentsInChildren<SquadMember>())
            if (member != null) Destroy(member.gameObject);
        squadMembers.Clear();

        // Formation 归回初始位置
        _formation.transform.position = _initialFormationPosition;

        if (_memberTemplates.Count == 0)
        {
            Debug.LogWarning("[SquadComponent] 没有可用的成员模板，无法复活小队。");
            return;
        }

        // 从 Awake 时保存的非激活模板实例化新成员
        foreach (var template in _memberTemplates)
        {
            if (template == null) continue;

            var go = Instantiate(template, _initialFormationPosition, Quaternion.identity, transform);
            go.SetActive(true);

            var m = go.GetComponent<SquadMember>();
            m.Init();
            m.animancer.Layers[0].Play(m.animations.Idle);

            var captured = m;
            captured.health.Died += () => MemberDie(captured);
            squadMembers.Add(m);
        }

        // 重建 Formation
        int count = squadMembers.Count;
        _formation.RecreateFormation(Vector3.zero,
                                     count <= 4 ? _formationRadius - .3f : _formationRadius,
                                     count - 1);
        SetMembersToCinemachineGroup();
        OrderMembers();
    }

    /// <summary>
    /// 重置小队（不重载场景）：
    /// 销毁所有当前成员 → Formation 归位 → 从 Awake 时自动保存的模板重新实例化。
    /// </summary>
    public void ResetSquad()
    {
        // 销毁所有当前成员（包含进化后的高级成员）
        foreach (var member in squadMembers)
            if (member != null) Destroy(member.gameObject);
        squadMembers.Clear();

        // Formation 归回初始位置
        _formation.transform.position = _initialFormationPosition;

        if (_memberTemplates.Count == 0)
        {
            Debug.LogWarning("[SquadComponent] 没有可用的成员模板，无法还原小队。");
            return;
        }

        // 从 Awake 时保存的非激活模板实例化新成员
        foreach (var template in _memberTemplates)
        {
            if (template == null) continue;

            var go = Instantiate(template, _initialFormationPosition, Quaternion.identity, transform);
            go.SetActive(true);

            var m = go.GetComponent<SquadMember>();
            m.Init();
            m.animancer.Layers[0].Play(m.animations.Idle);

            var captured = m; // 捕获局部变量，避免 lambda 闭包引用最终值
            captured.health.Died += () => MemberDie(captured);
            squadMembers.Add(m);
        }

        // 重建 Formation
        int count = squadMembers.Count;
        _formation.RecreateFormation(Vector3.zero,
                                     count <= 4 ? _formationRadius - .3f : _formationRadius,
                                     count - 1);
        SetMembersToCinemachineGroup();
        OrderMembers();

        // 重置临时升级数值
        temporaryFireRateIncrease  = 0f;
        temporaryFirePowerIncrease = 0f;
        currentSpeed = _speed;
        _gameData.SetProjectilePrefab(_gameData.pistolProjectile);
        _gameData.SetSideProjectiles(0);
    }
    #endregion

    #region Upgrades
    public void IncreaseFireRate(float value)
    {
        temporaryFireRateIncrease += value;
        PunchScaleSquad();

        SpawnLvlUpVFX();
    }
    public void IncreaseSpeed()
    {
        currentSpeed *= _gameData.speedIncreasePercent;
        PunchScaleSquad();

        SpawnLvlUpVFX();
    }
    public void IncreaseFirePower(float value)
    {
        temporaryFirePowerIncrease += value;
        PunchScaleSquad();

        SpawnLvlUpVFX();
    }
    public void SetLaserProjectile()
    {
        _gameData.SetProjectilePrefab(_gameData.laserProjectile);

        SpawnLvlUpVFX();
    }
    public void HealSquad(float value)
    {
        foreach (var member in squadMembers)
        {
            member.health.Heal(member.health.MaxPoints * (value / 100));
        }

        SpawnLvlUpVFX();
    }

    /// <summary>
    /// 将所有小队成员的血量恢复至满（用于复活）
    /// </summary>
    public void FullHealSquad()
    {
        foreach (var member in squadMembers)
        {
            if (member != null && member.health != null)
                member.health.SetMaxPoints(member.health.MaxPoints, needRefill: true);
        }
    }
    public void DoubleEveryMember()
    {
        var squadMembersCopy = new List<SquadMember>(squadMembers);

        foreach (var member in squadMembersCopy)
        {
            EvolveMembers(member);
        }

        SpawnLvlUpVFX();
    }
    #endregion
    private void Shoot()
    {
        foreach (var member in squadMembers)
        {
            if (member.IsEvolving)
            {
                continue;
            }

            member.currentTarget = member.overLapEnemy.NearestTouchedOfType<EnemyComponent>(member.transform);

            if (member.currentTarget != null)
            {
                var newTargetPos = member.currentTarget.transform.position;
                newTargetPos.y = member.transform.position.y;
                member.targetVector = newTargetPos;

                member.Shoot(member.currentTarget.transform);
            }
        }
    }
    private void Movement()
    {
        var swift = new Vector3(_joystick.Horizontal, 0, _joystick.Vertical).normalized * currentSpeed;

        if (swift.magnitude < .1f)
        {
            int index = -1;

            for (int i = 0; i < squadMembers.Count; i++)
            {
                SquadMember member = squadMembers[i];

                index++;

                if (member.IsEvolving)
                {
                    continue;
                }

                if (member.navMesh == null || !member.navMesh.isActiveAndEnabled || !member.navMesh.isOnNavMesh)
                {
                    continue;
                }

                if (index == 0)
                {
                    member.navMesh.ResetPath();
                    member.SetIdleAnimation();
                    continue;
                }

                Transform targetPoint = index == 0 ? _formation.FormationPoints[index] : squadMembers[i - 1].transform;
                
                if (Vector3.Distance(targetPoint.position, member.navMesh.transform.position) > member.navMesh.stoppingDistance)
                {
                    member.animancer.Layers[0].Play(member.runForward, .4f).Speed = .8f;
                    member.navMesh.updateRotation = true;
                    member.navMesh.SetDestination(targetPoint.position);
                }
                else
                {
                    member.animancer.Layers[0].Play(member.animations.Idle, .4f);
                }
            }

            return;
        }

        int memberIndex = -1;

        for (int i = 0; i < squadMembers.Count; i++)
        {
            SquadMember member = squadMembers[i];
            memberIndex++;

            if (member.IsEvolving)
            {
                continue;
            }

            if (member.navMesh == null || !member.navMesh.isActiveAndEnabled || !member.navMesh.isOnNavMesh)
            {
                continue;
            }

            member.navMesh.isStopped = false;

            Transform targetPoint = memberIndex == 0 ? _formation.FormationPoints[memberIndex] : squadMembers[i - 1].transform;

            if (memberIndex == 0)
            {
                member.animancer.Layers[0].Play(member.runForward, .4f);

                member.navMesh.Move(swift * Time.deltaTime);
                member.navMesh.ResetPath();
                _formation.transform.position = member.transform.position;
            }
            else
            {
                member.navMesh.SetDestination(targetPoint.position);
                member.navMesh.updateRotation = false;

                member.navMesh.speed = currentSpeed;

                if (Vector3.Distance(targetPoint.position, member.navMesh.transform.position) <= member.navMesh.stoppingDistance)
                {
                    member.animancer.Layers[0].Play(member.animations.Idle, .4f);
                }
                else
                {
                    member.animancer.Layers[0].Play(member.runForward, .4f);
                }
            }
            
            if (member.currentTarget == null)
            {
                member.targetVector = member.transform.position + swift;
            }
        }
    }
    private void SetNotBlockedMember()
    {
        if (openSquadMember == null || openSquadMember.overLapObstacle.HasTouch)
        {
            openSquadMember = squadMembers.DefaultIfEmpty(squadMembers[0]).FirstOrDefault(x => !x.overLapObstacle.HasTouch);
        }
    }
}