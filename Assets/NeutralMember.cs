using D2D;
using D2D.Utilities;
using DG.Tweening;
using System;
using UnityEngine;

using static D2D.Utilities.CommonGameplayFacade;

public class NeutralMember : Unit
{
    [SerializeField] private SexyOverlap neutralOverlap;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [Tooltip("x2 广告命中时额外生成的成员预制体（通常设为本预设自身）")]
    [SerializeField] private SquadMember memberPrefab;

    public Action OnDespawn;

    private Color activeColor;
    private Color deactivatedColor = Color.white;

    private void Awake()
    {
        activeColor = meshRenderer.material.color;

        meshRenderer.material.DOColor(deactivatedColor, 0);
    }

    private void Start()
    {
        // 在场上生成时，按概率显示 x2 广告指示器（等 SquadComponent 的 Awake 完成后执行）
        var member = Get<SquadMember>();
        member.TryShowDoubleRewardUI();

        // x2 UI 未生成时，确保进化文本可见（激活中间节点链）
        if (!member.HasPendingDoubleReward)
            member.EnsureEvolutionTextVisible();
    }

    private void Update()
    {
        if (neutralOverlap.HasTouch && neutralOverlap.Touched.gameObject != this)
        {
            AddNeutralToSquad();

            enabled = false;
            neutralOverlap.gameObject.SetActive(false);

            Despawn();
        }

        if (Vector3.Distance(transform.position, _formation.transform.position) > _gameData.neutralDespawnDistance)
        {
            Despawn();
            
            Destroy(gameObject);
        }
    }

    private void AddNeutralToSquad()
    {
        var member = Get<SquadMember>();

       // Debug.Log($"[NeutralMember] AddNeutralToSquad: HasPendingDoubleReward={member.HasPendingDoubleReward}");

        if (member.HasPendingDoubleReward)
        {
            var capturedMember = member;
            member.TriggerDoubleReward(
                onRewarded: () => JoinSquadAsEvolved(capturedMember),
                onFailed:   err =>
                {
                  //  Debug.LogWarning($"[NeutralMember] 广告失败/跳过，正常加入2: {err}");
                    JoinSquadNormal(capturedMember);
                }
            );
        }
        else
        {
           // Debug.Log("[NeutralMember] 无 x2 UI，直接加入2");
            _squad.AddMember(member);
        }

        gameObject.SetLayerRecursively(playerLayer.ToLayer());
        meshRenderer.material.DOColor(activeColor, .5f);
    }

    /// <summary>无广告/广告失败时：直接将原始成员（2）加入队伍。</summary>
    private void JoinSquadNormal(SquadMember member)
    {
       // Debug.Log("[NeutralMember] JoinSquadNormal: 加入2");
        if (member != null)
            _squad.AddMember(member);
    }

    /// <summary>
    /// 广告命中：在原位生成下一进化等级（4）后加入队伍，原始成员（2）销毁。
    /// </summary>
    private void JoinSquadAsEvolved(SquadMember member)
    {
      //  Debug.Log($"[NeutralMember] JoinSquadAsEvolved: member={member?.name ?? "NULL"}, NextEvolution={member?.NextEvolution?.name ?? "NULL"}");

        if (member == null)
        {
           // Debug.LogError("[NeutralMember] JoinSquadAsEvolved: member 已被销毁！");
            return;
        }

        if (member.NextEvolution == null)
        {
           // Debug.LogWarning("[NeutralMember] NextEvolution 未设置！请在 2 Neutral 预制体的 SquadMember 组件里赋值 NextEvolution。正常加入2。");
            _squad.AddMember(member);
            return;
        }

        var nextEvo = member.NextEvolution; // 在 Destroy 前保存引用
        var pos     = member.transform.position;
        var rot     = member.transform.rotation;

        Destroy(member.gameObject);

        var evolved = Instantiate(nextEvo.gameObject, pos, rot, _squad.transform)
            .GetComponent<SquadMember>();
        evolved.showDoubleRewardOnSpawn = false;

        _squad.AddMember(evolved);
       // Debug.Log($"[NeutralMember] ✅ x2 成功！加入 {nextEvo.name}（4）");
    }

    /// <summary>广告命中后额外生成一个同类成员并加入小队（实现 x2 数量）。</summary>
    private void SpawnExtraMember()
    {
        if (memberPrefab == null)
        {
           // Debug.LogWarning("[NeutralMember] memberPrefab 未赋值，无法执行 x2 生成");
            return;
        }

        var extraGO = Instantiate(memberPrefab.gameObject, _squad.SpawnPosition, Quaternion.identity, _squad.transform);
        var extra   = extraGO.GetComponent<SquadMember>();

        // 额外副本不再触发 x2 UI，防止无限循环
        extra.showDoubleRewardOnSpawn = false;

        _squad.AddMember(extra);
       // Debug.Log($"[NeutralMember] x2 命中！额外生成 {memberPrefab.name} 并加入小队");
    }

    private void Despawn()
    {
        // 离场时关闭未触发的 x2 UI
        Get<SquadMember>()?.DismissDoubleRewardUI();
        OnDespawn?.Invoke();
    }
}