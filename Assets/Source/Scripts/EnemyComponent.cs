using Animancer;
using D2D;
using D2D.Core;
using D2D.Gameplay;
using D2D.Utilities;
using UnityEngine;
using UnityEngine.AI;
using static D2D.Utilities.CommonGameplayFacade;

[RequireComponent(typeof(OnTriggerEnterComponent))]
public class EnemyComponent : Unit, IHittable
{
    public OnTriggerEnterComponent triggerEnterComponent;
    public Health health;

    [SerializeField] internal float speed = 10f;
    [SerializeField] internal float deathReward = 1f;
    [SerializeField] internal int refreshNavMeshFrames = 4;
    [SerializeField] internal Animations animations;

    [Header("Combat")]
    [SerializeField] internal float attackRate = 1f;
    [SerializeField] internal float damage = 1f;
    [SerializeField] internal SexyOverlap overlap;

    [Header("After Death")]
    [SerializeField] internal GameObject powerUpPrefab;

    internal float attackTimer;

    internal NavMeshAgent navMesh;
    internal CharacterCanvas canvas;
    internal AnimancerComponent animancer;
    internal CapsuleCollider capsCollider;

    internal bool isDead = false;

    internal virtual void Awake()
    {
        triggerEnterComponent = GetComponent<OnTriggerEnterComponent>();
        health = GetComponent<Health>();
        navMesh = GetComponent<NavMeshAgent>();
        capsCollider = GetComponent<CapsuleCollider>();
        animancer = GetComponentInChildren<AnimancerComponent>();
        canvas = GetComponentInChildren<CharacterCanvas>();

        animancer.Play(animations.UnarmedRun);

        if (overlap == null)
        {
            overlap = GetComponentInChildren<SexyOverlap>();
        }

        navMesh.speed = speed;

        health.Died += Die;

        _stateMachine.On<WinState>(DieOnWin);
    }
    public virtual void Update()
    {
        if (isDead)
        {
            return;
        }

        if (Time.frameCount % refreshNavMeshFrames == 0)
        {
            navMesh.SetDestination(_formation.transform.position);

            if (Vector3.Distance(transform.position, _formation.transform.position) > _gameData.enemyDespawnDistance)
            {
                DespawnEnemy();
            }
        }

        if (overlap.HasTouch && attackTimer <= Time.time)
        {
            var closestPlayer = overlap.NearestTouchedOfType<SquadMember>(transform);
            
            if (closestPlayer != null)
            {
                closestPlayer.health.ApplyDamage(gameObject, damage);
            }

            attackTimer = Time.time + attackRate;
        }
    }
    public void SetSpeed(float multiplier)
    {
        navMesh.speed = speed * multiplier;
    }
    public void SetDamage(float multiplier)
    {
        damage *= multiplier;
    }

    public void SetHealth(float multiplier)
    {
        var newHealth = (int) (health.MaxPoints * multiplier);
        canvas.HealthBar.SetHealth(health);
        health.SetMaxPoints(newHealth, true);
    }
    public void GetHit(float damage)
    {
        if (isDead)
        {
            return;
        }

        health.ApplyDamage(gameObject, damage);
    }
    internal virtual void Die()
    {
        if (isDead)
            return;

        if (powerUpPrefab != null)
        {
            var powerUp = Instantiate(powerUpPrefab, transform.position, Quaternion.identity).Get<XPPoint>();
            powerUp.Init(transform.position, _formation.transform.position);
        }

        _enemySpawn.EnemyDied();

        _db.Money.Value += deathReward;

        isDead = true;

        if (navMesh != null && navMesh.isActiveAndEnabled && navMesh.isOnNavMesh)
        {
            navMesh.isStopped = true;
            navMesh.ResetPath();
            navMesh.velocity = Vector3.zero;
        }
        navMesh.enabled = false;

        canvas.HealthBar.gameObject.SetActive(false);

        overlap.enabled = false;

        capsCollider.enabled = false;

        animancer.Animator.applyRootMotion = true;
        animancer.Play(animations.Death);

        DHaptic.HapticLight();

        Destroy(gameObject, 3f);
    }

    /// <summary>
    /// 游戏胜利时调用的轻量版死亡：跳过动画和掉落物，直接销毁对象，
    /// 避免 Animancer 在后续帧的回调中触发 WebGL IL2CPP 的 WASM null 函数指针崩溃。
    ///
    /// 已死亡敌人（isDead=true）处于死亡动画中，挂有 Destroy(gameObject, 3f) 的延迟销毁。
    /// 若不立即销毁，Animancer 会在后续帧继续更新其 PlayableGraph，
    /// 动画结束时通过 IL2CPP 虚函数表（invoke_vii/invoke_iiii）调用空指针，
    /// 在 WebGL 中产生 "null function or function signature mismatch" 崩溃。
    /// 对已死亡敌人调用 Destroy(gameObject) 可提前覆盖延迟销毁，立即停止 Animancer 更新。
    /// </summary>
    internal virtual void DieOnWin()
    {
        if (isDead)
        {
            // 已死亡但仍在播放死亡动画的敌人：停止 Animancer 并立即销毁，
            // 防止 Playables 虚调用在 WebGL 引发 WASM null 函数指针崩溃。
            if (animancer != null)
                animancer.Stop();
            Destroy(gameObject);
            return;
        }

        isDead = true;
        _enemySpawn.EnemyDied();

        if (navMesh != null && navMesh.isActiveAndEnabled)
            navMesh.enabled = false;

        if (capsCollider != null)
            capsCollider.enabled = false;

        if (animancer != null)
            animancer.Stop();
        Destroy(gameObject);
    }
    internal void DespawnEnemy()
    {
        _enemySpawn.EnemyDied();

        isDead = true;

        Destroy(gameObject);
    }
}