using D2D;
using D2D.Utilities;
using UnityEngine;

using static D2D.Utilities.CommonGameplayFacade;

public class NeutralsSpawn : Unit
{
    [SerializeField] private int maxNeutralsOnScreen = 8;
    [SerializeField] private float spawnCooldown = .8f;
    [SerializeField] private NeutralMember neutralMember;

    [Tooltip("新角色与已有角色的最小生成距离")]
    [SerializeField] private float minSpawnDistance = 4.5f;
    [Tooltip("找不到合适位置时的最大重试次数")]
    [SerializeField] private int maxSpawnAttempts = 10;

    private Camera currentCamera;

    private int currentAmount;

    private float timer;

    private void Awake()
    {
        currentCamera = Camera.main;
    }

    private void Update()
    {
        if (maxNeutralsOnScreen > currentAmount)
        {
            if (Time.time <= timer)
            {
                return;
            }

            SpawnNeutral();

            timer = Time.time + spawnCooldown;
        }
    }
    private void DespawnNeutral()
    {
        currentAmount--;
    }
    private void SpawnNeutral()
    {
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            float xPos;
            float yPos;
            float sign = Mathf.Sign(Random.Range(-1, 2));

            if (Random.Range(0, 100) > 50)
            {
                xPos = 0.5f + 0.6f * sign;
                yPos = Random.Range(0, 100) / 100f;
            }
            else
            {
                xPos = Random.Range(0, 100) / 100f;
                yPos = 0.5f + 0.6f * sign;
            }

            Vector3 direction = Camera.main.ViewportToWorldPoint(new Vector3(xPos, yPos, -10));
            Ray ray = new Ray(currentCamera.transform.position, currentCamera.transform.position - direction);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _gameData.GroundLayer))
                continue;

            // 检查与场上所有待吃角色的距离
            if (IsTooCloseToOthers(hit.point))
                continue;

            NeutralMember neutralMemberComp = Instantiate(neutralMember.gameObject, hit.point, Quaternion.identity).Get<NeutralMember>();
            neutralMemberComp.OnDespawn += DespawnNeutral;

            var memberComp = neutralMemberComp.Get<SquadMember>();
            memberComp.Init();

            currentAmount++;
            return;
        }
        // 超出重试次数，本次跳过，等下次 Update 再尝试
    }

    private bool IsTooCloseToOthers(Vector3 candidatePos)
    {
        foreach (var existing in FindObjectsOfType<NeutralMember>())
        {
            if (Vector3.Distance(candidatePos, existing.transform.position) < minSpawnDistance)
                return true;
        }
        return false;
    }
}