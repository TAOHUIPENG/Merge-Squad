using D2D.Core;
using TTSDK;
using UnityEngine;
using UnityEngine.UI;
using static D2D.Utilities.CommonGameplayFacade;

/// <summary>
/// Menu主界面逻辑
/// </summary>
public class MenuUI : MonoBehaviour
{
    // ---- 原有升级按钮 ----
    [Header("升级按钮")]
    [SerializeField] private MenuUIButton1 fireRateIncreaseButton;
    [SerializeField] private MenuUIButton1 firePowerIncreaseButton;
    [SerializeField] private MenuUIButton1 freeCoinButton;
    
    [SerializeField] private GameObject fireRateUpgradeTxt;
    [SerializeField] private GameObject firePowerUpgradeTxt;
    [SerializeField] private GameObject fireRateUpgradeAds;
    [SerializeField] private GameObject firePowerUpgradeAds;
    
    
    [SerializeField] private Button continueButton;

    // ---- Menu功能按钮 ----
    [Header("功能按钮")]
    [SerializeField] private Button signInButton;           // 签到
    [SerializeField] private Button terrainSelectButton;    // 地形选择
    [SerializeField] private Button entranceRewardButton;   // 入口有奖
    [SerializeField] private Button addGiftButton;          // 添加有礼
    [SerializeField] private Button shareButton;            // 分享
    [SerializeField] private Button addStaminaButton;       // 加体力
    [SerializeField] private Button addCoinButton;          // 加金币
    [SerializeField] private Button addDesktopButton;       // 添加桌面

    // ---- 显示信息 ----
    [Header("信息显示")]
    [SerializeField] private Text staminaText;              // 体力文本
    [SerializeField] private Text coinText;                 // 金币文本
    [SerializeField] private Image userAvatar;              // 用户头像
    [SerializeField] private Text nicknameText;             // 昵称文本

    // ---- 关联面板（可选绑定） ----
    [Header("关联面板")]
    [SerializeField] private SignInUI signInUI;
    [SerializeField] private ShareUI shareUI;
    [SerializeField] private StaminaPopupUI staminaPopupUI;
    [SerializeField] private CoinPopupUI coinPopupUI;
    [SerializeField] private SidebarUI sidebarUI;
    [SerializeField] private AwardShortcutUI awardShortcutUI;


    private void Start()
    {
        // 原有按钮逻辑
        fireRateIncreaseButton.Button.onClick.AddListener(IncreaseFireRate);
        firePowerIncreaseButton.Button.onClick.AddListener(IncreasePowerUp);
        continueButton.onClick.AddListener(OnContinueClicked);

        // 功能按钮绑定
        if (signInButton != null)
            signInButton.onClick.AddListener(OnSignInClicked);

        if (terrainSelectButton != null)
            terrainSelectButton.onClick.AddListener(OnTerrainSelectClicked);

        if (entranceRewardButton != null)
            entranceRewardButton.onClick.AddListener(OnEntranceRewardClicked);

        if (addGiftButton != null)
            addGiftButton.onClick.AddListener(OnAddGiftClicked);

        if (shareButton != null)
            shareButton.onClick.AddListener(OnShareClicked);

        if (addStaminaButton != null)
            addStaminaButton.onClick.AddListener(OnAddStaminaClicked);
        
        addCoinButton?.onClick.AddListener(OnAddCoinClicked);

        freeCoinButton.Button.onClick.AddListener(OnFreeCoinClicked);

        addDesktopButton?.onClick.AddListener(OnAddDesktopClicked);

        UpdateStats();
        RefreshDisplay();

        // 侧边栏入口初始隐藏，等 SidebarManager.CheckScene 回调后再决定显示
        RefreshSidebarEntryVisibility();
        // 添加桌面按钮：已领取过奖励则隐藏
        RefreshDesktopButtonVisibility();
    }

    private void OnEnable()
    {
        RefreshDisplay();
    }

    // ---- 开始游戏 ----

    private void OnContinueClicked()
    {
        if (!StaminaManager.CanStartGame())
        {
            if (staminaPopupUI != null)
                ShowPanel(staminaPopupUI.gameObject);
            else
                Debug.LogWarning("MenuUI: 体力不足，且 staminaPopupUI 未绑定");
            return;
        }

        StaminaManager.ConsumeForGame();
        RefreshDisplay();
        _stateMachine.Push(new RunningState());
        UIGame.Instance?.Show();
    }

    /// <summary>由 StaminaPopupUI 等外部调用，同步刷新体力显示</summary>
    public void OnStaminaChanged() => RefreshDisplay();

    // ---- 功能按钮回调 ----

    private void OnSignInClicked()
    {
        if (signInUI != null)
            ShowPanel(signInUI.gameObject);
        else
            Debug.LogWarning("MenuUI: signInUI 未绑定");
    }

    private void OnTerrainSelectClicked()
    {
        Debug.Log("MenuUI: 地形选择 - 请绑定地形选择界面");
        // TODO: 打开地形选择面板
    }

    private void OnEntranceRewardClicked()
    {
        if (sidebarUI != null)
            ShowPanel(sidebarUI.gameObject);
        else
            Debug.LogWarning("MenuUI: sidebarUI 未绑定");
    }

    /// <summary>
    /// 根据 SidebarManager 状态控制入口有奖按钮显隐：
    ///   - 宿主不支持侧边栏 → 隐藏
    ///   - 奖励已领取 → 隐藏
    ///   - 其他情况 → 显示
    /// 由 SidebarManager.CheckScene 回调和 MarkRewardClaimed 后调用。
    /// </summary>
    public void RefreshSidebarEntryVisibility()
    {
        if (entranceRewardButton == null) return;

        bool supported = SidebarManager.Instance != null && SidebarManager.Instance.IsSidebarSupported;
        bool claimed   = SidebarManager.Instance != null && SidebarManager.Instance.IsRewardClaimed;
        entranceRewardButton.gameObject.SetActive(supported && !claimed);
    }

    /// <summary>
    /// 刷新"添加桌面"按钮显隐：奖励已领取则永久隐藏。
    /// 由 Start、AwardShortcutUI.MarkRewardClaimed 后调用。
    /// </summary>
    public void RefreshDesktopButtonVisibility()
    {
        if (addDesktopButton == null) return;
        bool claimed = AwardShortcutUI.Instance != null && AwardShortcutUI.Instance.IsRewardClaimed;
        addDesktopButton.gameObject.SetActive(!claimed);
    }

    /// <summary>
    /// 点击"添加桌面"：
    ///   1. 调用 TT.AddShortcut 弹出系统添加桌面提示
    ///   2. 在回调中用 TT.CheckShortcut 二次确认是否真正添加成功
    ///   3. 确认成功后弹出 AwardShortcutUI 奖励界面
    /// </summary>
    private void OnAddDesktopClicked()
    {
        TT.AddShortcut(bSuccess =>
        {
            Debug.Log("[MenuUI] OnCreateShortcut : " + bSuccess);

            // 无论系统回调成功与否，都用 CheckShortcut 做最终确认
            TT.CheckShortcut(exist =>
            {
                Debug.Log("[MenuUI] Shortcut exist: " + exist);
                if (exist)
                {
                    // 隐藏按钮
                    RefreshDesktopButtonVisibility();
                    // 弹出奖励界面
                    if (awardShortcutUI != null)
                        ShowPanel(awardShortcutUI.gameObject);
                    else
                        AwardShortcutUI.Instance?.Show();
                }
            });
        });
    }

    private void OnAddGiftClicked()
    {
        Debug.Log("MenuUI: 添加有礼 - 请绑定礼包界面");
        // TODO: 打开礼包面板
    }

    private void OnShareClicked()
    {
        if (shareUI != null)
            ShowPanel(shareUI.gameObject);
        else
            Debug.LogWarning("MenuUI: shareUI 未绑定");
    }

    private void OnAddStaminaClicked()
    {
        if (staminaPopupUI != null)
            ShowPanel(staminaPopupUI.gameObject);
        else
            Debug.LogWarning("MenuUI: staminaPopupUI 未绑定");
    }

    private void OnFreeCoinClicked()
    {
        AdManager.Instance.ShowRewarded(
            AdManager.Scenes.FreeCoin,
            onRewarded: () =>
            {
                int reward = CalcFreeCoinReward();
                _db.Money.Value += reward;
                Debug.Log($"[MenuUI] 免费金币广告看完，金币+{reward}，当前={_db.Money.Value}");
                UpdateStats();
                RefreshDisplay();
            },
            onFailed: err => Debug.LogWarning($"[MenuUI] 免费金币广告失败: {err}"));
    }

    private void OnAddCoinClicked()
    {
        if (coinPopupUI != null)
            ShowPanel(coinPopupUI.gameObject);
        else
            Debug.LogWarning("MenuUI: coinPopupUI 未绑定");
    }

    /// <summary>由 CoinPopupUI 等外部调用，同步刷新金币显示及按钮状态</summary>
    public void OnCoinChanged()
    {
        UpdateStats();   // 同步升级按钮的广告/金币图标状态
        RefreshDisplay(); // 同步顶部金币文本
    }

    /// <summary>
    /// 安全地显示一个弹窗面板。
    ///
    /// 【死循环根因】
    /// 若直接在 while 循环中调用 parent.SetActive(true) 来激活父节点，
    /// 且父节点上带有 GameStateMachineUser（如 UIGame），
    /// 则 SetActive(true) → OnEnable → BindCallbacks → On&lt;PauseState&gt;(OnGamePause)，
    /// 若当前状态已是 PauseState，GSM 会立刻触发 OnGamePause → SetActive(false)，
    /// 循环再次判断 !activeSelf → SetActive(true) → 无限死循环，编辑器卡死。
    ///
    /// 【正确方案】
    /// 不激活父节点，而是将弹窗移到根 Canvas 层级，
    /// 使其脱离受游戏状态控制的父节点层级，再执行 SetActive(true)。
    /// 根本建议：在场景中把所有弹窗直接放在 Canvas 的直接子节点下。
    /// </summary>
    private void ShowPanel(GameObject panel)
    {
        if (panel == null) return;

        // 若父节点链路中存在未激活节点，将面板移至根 Canvas 层级
        if (HasInactiveParentBeforeCanvas(panel.transform))
        {
            Canvas rootCanvas = GetRootCanvas();
            if (rootCanvas != null && panel.transform.parent != rootCanvas.transform)
            {
                Debug.LogWarning($"[MenuUI] '{panel.name}' 父节点未激活，已自动移至根 Canvas 层级。" +
                                 "建议在场景中将此面板直接放置于 Canvas 的直接子节点下，避免被其他界面的显隐逻辑影响。");
                panel.transform.SetParent(rootCanvas.transform, false);
            }
        }

        panel.SetActive(true);
    }

    /// <summary>
    /// 检查 t 向上到 Canvas 之间是否存在未激活的父节点
    /// </summary>
    private static bool HasInactiveParentBeforeCanvas(Transform t)
    {
        Transform p = t.parent;
        while (p != null && p.GetComponent<Canvas>() == null)
        {
            if (!p.gameObject.activeSelf) return true;
            p = p.parent;
        }
        return false;
    }

    /// <summary>
    /// 获取 MenuUI 所在的根 Canvas
    /// </summary>
    private Canvas GetRootCanvas()
    {
        // MenuUI 自身处于激活链路中，直接向上找即可
        Canvas c = GetComponentInParent<Canvas>();
        return c != null ? c : FindObjectOfType<Canvas>();
    }

    // ---- 信息刷新 ----

    private void RefreshDisplay()
    {
        // 金币
        if (coinText != null && _db != null)
            coinText.text = $"{_db.Money.Value:0}";

        // 体力（通过 StaminaManager 获取，自动结算离线回复）
        if (staminaText != null)
            staminaText.text = $"{StaminaManager.Get()}/{StaminaManager.MaxStamina}";

        // 昵称（默认值，接入SDK后替换）
        if (nicknameText != null && nicknameText.text == string.Empty)
            nicknameText.text = "玩家";
    }

    // ---- 原有升级逻辑（保持不变） ----

    private void IncreasePowerUp()
    {
        if (_db.PowerIncreaseLevel.Value >= _gameData.maxLevelUpgrade) return;

        bool isEnoughForPower = _db.Money.Value >= _gameData.PowerNextUpgradePrice;

        if (isEnoughForPower)
        {
            _db.PowerIncreaseLevel.Value++;
            _db.Money.Value -= _gameData.PowerUpgradePrice;
            UpdateStats();
            RefreshDisplay();
        }
        else
        {
            AdManager.Instance.ShowRewarded(AdManager.Scenes.IncreasePowerUp, () =>
            {
                _db.PowerIncreaseLevel.Value = Mathf.Min(
                    (int)_db.PowerIncreaseLevel.Value + 2, _gameData.maxLevelUpgrade);
                UpdateStats();
                RefreshDisplay();
            });
        }
   
    }

    private void IncreaseFireRate()
    {
        if (_db.FireRateDecreaseLevel.Value >= _gameData.maxLevelUpgrade) return;

        bool isEnoughForRate = _db.Money.Value >= _gameData.FireNextUpgradePrice;

        if (isEnoughForRate)
        {
            _db.FireRateDecreaseLevel.Value++;
            _db.Money.Value -= _gameData.FireUpgradePrice;
            UpdateStats();
            RefreshDisplay();
        }
        else
        {
            AdManager.Instance.ShowRewarded(AdManager.Scenes.IncreaseFireRate, () =>
            {
                _db.FireRateDecreaseLevel.Value = Mathf.Min(
                    (int)_db.FireRateDecreaseLevel.Value + 2, _gameData.maxLevelUpgrade);
                UpdateStats();
                RefreshDisplay();
            });
        }
      
    }

    private void CheckForDeactivatingButtons()
    {
        bool isEnoughForRate  = _db.Money.Value >= _gameData.FireNextUpgradePrice;
        bool isEnoughForPower = _db.Money.Value >= _gameData.PowerNextUpgradePrice;

        bool isRateMaxLevel  = _db.FireRateDecreaseLevel.Value >= _gameData.maxLevelUpgrade;
        bool isPowerMaxLevel = _db.PowerIncreaseLevel.Value    >= _gameData.maxLevelUpgrade;

        // 满级时广告图标和升级价格均隐藏
        fireRateUpgradeAds.SetActive(!isEnoughForRate  && !isRateMaxLevel);
        firePowerUpgradeAds.SetActive(!isEnoughForPower && !isPowerMaxLevel);
        fireRateUpgradeTxt.SetActive(isEnoughForRate   && !isRateMaxLevel);
        firePowerUpgradeTxt.SetActive(isEnoughForPower  && !isPowerMaxLevel);

        if (isRateMaxLevel)
            fireRateIncreaseButton.PriceText.text = "已满级";

        if (isPowerMaxLevel)
            firePowerIncreaseButton.PriceText.text = "已满级";
    }

    private void UpdateStats()
    {
        int maxIdx = _gameData.upgradesPercentByLevel.Length - 1;
        int powerIdx = Mathf.Clamp((int)_db.PowerIncreaseLevel.Value,    0, maxIdx);
        int fireIdx  = Mathf.Clamp((int)_db.FireRateDecreaseLevel.Value, 0, maxIdx);

        _db.PowerIncreasePercent.Value    = _gameData.upgradesPercentByLevel[powerIdx] / 100;
        _db.FireRateDecreasePercent.Value = _gameData.upgradesPercentByLevel[fireIdx]  / 100;

        firePowerIncreaseButton.LevelText.text = $"等级 {_db.PowerIncreaseLevel.Value}";
        fireRateIncreaseButton.LevelText.text = $"等级 {_db.FireRateDecreaseLevel.Value}";
        firePowerIncreaseButton.PriceText.text = $"{_gameData.PowerNextUpgradePrice}";
        fireRateIncreaseButton.PriceText.text = $"{_gameData.FireNextUpgradePrice} ";
        freeCoinButton.PriceText.text = $"+{CalcFreeCoinReward()}";
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(fireRateIncreaseButton.transform as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(firePowerIncreaseButton.transform as RectTransform);

        CheckForDeactivatingButtons();
    }

    /// <summary>
    /// 免费金币奖励金额：(射速下次升级价 + 火力下次升级价) × 1.5，向整十取整
    /// </summary>
    private int CalcFreeCoinReward()
    {
        float raw = (_gameData.FireNextUpgradePrice + _gameData.PowerNextUpgradePrice) * 1.5f;
        return Mathf.RoundToInt(raw / 10f) * 10;
    }
}

