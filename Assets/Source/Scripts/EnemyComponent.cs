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
    /// </summary>
    internal virtual void DieOnWin()
    {
        if (isDead)
            return;

        isDead = true;
        _enemySpawn.EnemyDied();

        if (navMesh != null && navMesh.isActiveAndEnabled)
            navMesh.enabled = false;

        if (capsCollider != null)
            capsCollider.enabled = false;

        Destroy(gameObject);
    }
    internal void DespawnEnemy()
    {
        _enemySpawn.EnemyDied();

        isDead = true;

        Destroy(gameObject);
    }
}