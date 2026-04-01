using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// 敌人死亡后头顶出现的 x2 广告奖励按钮（World Space）。
/// 由 EnemyComponent.Die() 按概率生成，点击看广告后额外发放等额死亡金币。
///
/// 注意：不使用 Button.onClick / EventSystem，
/// 改为 Update 手动检测触点位置，避免被摇杆 Canvas 拦截事件。
/// </summary>
public class EnemyDoubleRewardUI : MonoBehaviour
{
    [Header("UI 元素")]
    [SerializeField] private Button adButton;
    [SerializeField] private Text rewardText;

    [Header("参数")]
    [Tooltip("未点击时多少秒后自动消失")]
    [SerializeField] private float autoDestroyDelay = 5f;
    [Tooltip("高于生成位置的 Y 偏移")]
    [SerializeField] private float yOffset = 1.8f;

    private float          _reward;
    private Camera         _camera;
    private Canvas         _canvas;
    private RectTransform  _buttonRect;
    private bool           _clicked;       // 防止连续触发

    // ── 静态清场（复活/重开时调用）────────────────────────
    public static void ClearAll()
    {
        foreach (var ui in FindObjectsOfType<EnemyDoubleRewardUI>())
            Destroy(ui.gameObject);
    }

    // ── 初始化（由 EnemyComponent 调用）────────────────────
    public void Init(float reward, Vector3 spawnWorldPos)
    {
        _reward = reward;
        _camera = Camera.main;

        _canvas = GetComponentInChildren<Canvas>(true)
               ?? GetComponentInParent<Canvas>();

        if (_canvas != null)
        {
            _canvas.renderMode  = RenderMode.WorldSpace;
            _canvas.worldCamera = _camera;

            if (_canvas.GetComponent<GraphicRaycaster>() == null)
                _canvas.gameObject.AddComponent<GraphicRaycaster>();

            _canvas.transform.position   = spawnWorldPos + Vector3.up * yOffset;
            _canvas.transform.localScale = Vector3.zero;
            _canvas.transform.DOScale(Vector3.one, 0.28f)
                              .SetEase(Ease.OutBack)
                              .SetUpdate(true);
        }
        else
        {
            Debug.LogError("[EnemyDoubleRewardUI] 预制体中找不到 Canvas，请检查预制体结构！");
        }

        if (adButton != null)
        {
            _buttonRect = adButton.GetComponent<RectTransform>();
            // 不用 onClick，改为 Update 手动检测，避免被摇杆 Canvas 拦截
        }
        else
        {
            Debug.LogError("[EnemyDoubleRewardUI] adButton 未绑定，请检查预制体 Inspector！");
        }

        if (rewardText != null)
            rewardText.text = "x2";

        Destroy(gameObject, autoDestroyDelay);
    }

    private void LateUpdate()
    {
        if (_camera != null && _canvas != null)
            _canvas.transform.forward = _camera.transform.forward;
    }

    private void Update()
    {
        if (_clicked || _buttonRect == null || _camera == null) return;

        // ── 获取触点屏幕坐标（同时支持移动端触摸和 PC 鼠标）──
        Vector2 screenPos;
        bool    touched;

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            touched   = touch.phase == TouchPhase.Began;
            screenPos = touch.position;
        }
        else
        {
            touched   = Input.GetMouseButtonDown(0);
            screenPos = Input.mousePosition;
        }

        if (!touched) return;

        // ── 判断触点是否落在按钮 RectTransform 内 ──────────
        // 传入 worldCamera 可正确处理 World Space Canvas 的 3D 投影
        if (RectTransformUtility.RectangleContainsScreenPoint(_buttonRect, screenPos, _camera))
            OnClick();
    }

    private void OnClick()
    {
        _clicked              = true;
        adButton.interactable = false;

        AdManager.Instance.ShowRewarded(
            AdManager.Scenes.EnemyDoubleReward,
            onRewarded: () =>
            {
                _db.Money.Value += _reward;
                Debug.Log($"EnemyDoubleRewardUI: x2 +{_reward}，当前金币={_db.Money.Value}");
                Destroy(gameObject);
            },
            onFailed: err =>
            {
                _clicked              = false;
                adButton.interactable = true;
                Debug.LogWarning($"EnemyDoubleRewardUI: 广告失败 - {err}");
            });
    }
}
