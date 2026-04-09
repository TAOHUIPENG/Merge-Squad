using Animancer;
using D2D;
using D2D.Gameplay;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

using static D2D.Utilities.CommonGameplayFacade;

[RequireComponent(typeof(Health))]
public class SquadMember : Unit
{
    public NavMeshAgent navMesh;
    public SexyOverlap overLapObstacle;
    public SexyOverlap overLapEnemy;
    public MemberClass memberClass;
    public Animations animations;
    public Health health;
    public AnimancerComponent animancer;
    public Transform shootPoint;
    public int EvolveLevel;
    public SquadMember NextEvolution;

    [Header("Movement Settings")]
    public Vector3 targetVector;
    public float rotationLerp = 10f;
    public AnimationClip runForward;

    [HideInInspector]
    public EnemyComponent currentTarget;
    public SquadMember evolveTarget;

    [SerializeField] internal Color memberColor;

    [Header("双倍奖励广告")]
    [Tooltip("上场时头顶出现 x2 广告按钮的概率（0~100）；仅 showDoubleRewardOnSpawn 为 true 时生效")]
    [SerializeField] internal float doubleRewardChance = 30f;
    [Tooltip("x2 广告按钮的 World Space 预制体")]
    [SerializeField] internal GameObject doubleRewardUIPrefab;
    [Tooltip("勾选后在首次上场（生成到场上）时按概率显示 x2 按钮（仅用于 2 Neutral 等特定预设）")]
    [SerializeField] internal bool showDoubleRewardOnSpawn = false;

    private EnemyDoubleRewardUI _spawnedDoubleRewardUI;
    private bool _doubleRewardSpawnAttempted;

    /// <summary>当前是否存在待触发的 x2 广告 UI。</summary>
    public bool HasPendingDoubleReward => _spawnedDoubleRewardUI != null;

    private CharacterCanvas canvas;
    private Camera currentCamera;

    private Tween punchTween;

    internal float reloadTime = 0;
    internal float delayBeforeEvolve = 0;
    internal float lastShotSoundTime = 0;

    public bool IsEvolving;


    [Button("Debug Kill")]
    private void DebugKill()
    {
        health.ApplyDamage(null, health.CurrentPoints);
    }
    private void Awake()
    {
        navMesh = GetComponent<NavMeshAgent>();
        overLapObstacle = GetComponent<SexyOverlap>();
        health = GetComponent<Health>();

        if (canvas == null)
        {
            // includeInactive=true: 模板克隆时 CharacterCanvas 可能被 OnGameFinish 停用
            canvas = GetComponentInChildren<CharacterCanvas>(true);
            if (canvas != null)
                canvas.gameObject.SetActive(true); // 确保 UI 可见
        }

        targetVector = transform.forward * 10f + transform.up;

        if (canvas != null && canvas.HealthBar != null)
            canvas.HealthBar.SetHealth(health);

        // 所有角色（包括待吃的中立角色）在 Awake 时就设置进化文本，默认可见
        EnsureEvolutionTextVisible();
    }
    private void Update()
    {
        if (IsEvolving)
        {
            if (evolveTarget == null)
            {
                StopEvolving();

                return;
            }

            delayBeforeEvolve += Time.deltaTime;

            if (delayBeforeEvolve < _gameData.evolveDelay)
            {
                animancer.Play(animations.Idle);

                return;
            }

            var targetDistance = Vector3.Distance(evolveTarget.transform.position, transform.position);

            animancer.Play(runForward);
            navMesh.Move((evolveTarget.transform.position - transform.position) * Time.deltaTime * navMesh.speed * (1.7f / targetDistance));

            if (targetDistance < 0.5f)
            {
                _squad.EvolveMembers(this, evolveTarget);

                IsEvolving = false;
            }

            return;
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetVector - transform.position), Time.deltaTime * rotationLerp);
    }
    public virtual void Init()
    {
        runForward = animations.RunForward;

        if (canvas == null)
        {
            canvas = GetComponentInChildren<CharacterCanvas>(true);
            if (canvas != null)
                canvas.gameObject.SetActive(true);
        }

        animancer.Play(animations.Idle);

        // 显示进化文本，并激活 Canvas 内所有中间父节点（防止中间层默认关闭）
        if (!HasPendingDoubleReward)
            EnsureEvolutionTextVisible();
    }

    /// <summary>
    /// 激活 CharacterCanvas 内从根到 EvolutionText 的完整节点链，并设置文本内容。
    /// 有 x2 UI 时不调用此方法，以保持文本隐藏。
    /// </summary>
    public void EnsureEvolutionTextVisible()
    {
        if (canvas == null || canvas.EvolutionText == null) return;

        canvas.EvolutionText.text = $"{Mathf.Pow(2, EvolveLevel + 1)}";

        // 从 EvolutionText 向上遍历到 CharacterCanvas 根节点，激活途中所有未激活的父对象
        var t = canvas.EvolutionText.transform;
        while (t != null && t != canvas.transform)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            t = t.parent;
        }
        canvas.EvolutionText.gameObject.SetActive(true);
    }
    public virtual void Shoot(Transform target)
    {

    }
    public virtual void PunchScaleWithDelay(float delay)
    {
        punchTween.KillTo0();

        punchTween = transform.DOPunchScale(Vector3.one * _gameData.punchScale, _gameData.punchDuration, 0, 0).SetDelay(delay);
    }
    public virtual void SetIdleAnimation() 
    {
        animancer.Layers[0].Play(animations.Idle);
    }
    public void SetDanceAnimation(AnimationClip animation)
    {
        if (animation == null) return;
        animancer.Play(animation, 0.5f);
    }
    public void Evolve(SquadMember evolveTarget)
    {
        if (NextEvolution == null)
        {
            return;
        }

        animancer.Play(animations.RunForward);
        navMesh.ResetPath();
        IsEvolving = true;
        transform.LookAt(evolveTarget.transform.position);
        this.evolveTarget = evolveTarget;
    }
    private void StopEvolving()
    {
        animancer.Play(animations.Idle);
        IsEvolving = false;
    }

    /// <summary>
    /// 首次上场（生成到场上）时调用。
    /// 按概率在头顶生成 x2 广告视觉指示器，仅对 showDoubleRewardOnSpawn=true 的预设生效。
    /// 内置幂等保护，多次调用只执行一次。
    /// </summary>
    public void TryShowDoubleRewardUI()
    {
        if (!showDoubleRewardOnSpawn || doubleRewardUIPrefab == null) return;
        if (_doubleRewardSpawnAttempted) return;
        _doubleRewardSpawnAttempted = true;

        if (Random.Range(0f, 100f) >= doubleRewardChance) return;

        _spawnedDoubleRewardUI = Instantiate(doubleRewardUIPrefab)
            .GetComponent<EnemyDoubleRewardUI>();
        _spawnedDoubleRewardUI.Init(transform, onSelfDestroyed: () =>
        {
            // UI 被销毁（超时/Dismiss）时自动清空引用，防止 MissingReferenceException
            _spawnedDoubleRewardUI = null;
            health.Died -= DismissDoubleRewardUI;
            // x2 UI 消失后恢复显示进化文本
            SetEvolutionTextVisible(true);
        });

        health.Died += DismissDoubleRewardUI;

        // 有 x2 UI 时隐藏进化文本，避免与 x2 按钮重叠
        SetEvolutionTextVisible(false);
    }

    /// <summary>
    /// 玩家拾取时由 NeutralMember 调用：立即播放广告，命中概率后触发 onRewarded（x2 数量由调用方处理）。
    /// </summary>
    public void TriggerDoubleReward(System.Action onRewarded, System.Action<string> onFailed = null)
    {
        if (_spawnedDoubleRewardUI == null) return;  // Unity == 检查，已销毁也返回 null

        var ui = _spawnedDoubleRewardUI;
        _spawnedDoubleRewardUI = null;
        health.Died -= DismissDoubleRewardUI;

        if (ui != null)  // 双重保险：避免在清空后 ui 仍是已销毁引用
            ui.TriggerAd(onRewarded, onFailed);
    }

    /// <summary>成员死亡/离场时由外部或内部调用，关闭未触发的 x2 UI。</summary>
    public void DismissDoubleRewardUI()
    {
        if (_spawnedDoubleRewardUI != null)  // 使用 Unity == 检查，正确识别已销毁对象
            _spawnedDoubleRewardUI.Dismiss();
        _spawnedDoubleRewardUI = null;
        health.Died -= DismissDoubleRewardUI;
    }

    /// <summary>控制头顶进化文本的显隐，有 x2 UI 时隐藏以免重叠。</summary>
    private void SetEvolutionTextVisible(bool visible)
    {
        if (canvas == null || canvas.EvolutionText == null) return;
        if (visible)
            EnsureEvolutionTextVisible();
        else
            canvas.EvolutionText.gameObject.SetActive(false);
    }
}