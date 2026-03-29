using D2D.Core;
using D2D.Utilities;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Button continueButton;

    // ---- Menu功能按钮 ----
    [Header("功能按钮")]
    [SerializeField] private Button signInButton;           // 签到
    [SerializeField] private Button terrainSelectButton;    // 地形选择
    [SerializeField] private Button entranceRewardButton;   // 入口有奖
    [SerializeField] private Button addGiftButton;          // 添加有礼
    [SerializeField] private Button shareButton;            // 分享
    [SerializeField] private Button addStaminaButton;       // 加体力

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

    // 体力存储Key（与StaminaPopupUI保持一致）
    private const string StaminaKey = "Player_Stamina";
    private const int MaxStamina = 10;

    private void Start()
    {
        // 原有按钮逻辑
        fireRateIncreaseButton.Button.onClick.AddListener(IncreaseFireRate);
        firePowerIncreaseButton.Button.onClick.AddListener(IncreasePowerUp);
        continueButton.onClick.AddListener(() => _stateMachine.Push(new RunningState()));

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

        UpdateStats();
        RefreshDisplay();
    }

    // ---- 功能按钮回调 ----

    private void OnSignInClicked()
    {
        if (signInUI != null)
            signInUI.gameObject.SetActive(true);
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
        Debug.Log("MenuUI: 入口有奖 - 请绑定入口奖励界面");
        // TODO: 打开入口奖励面板
    }

    private void OnAddGiftClicked()
    {
        Debug.Log("MenuUI: 添加有礼 - 请绑定礼包界面");
        // TODO: 打开礼包面板
    }

    private void OnShareClicked()
    {
        if (shareUI != null)
            shareUI.gameObject.SetActive(true);
        else
            Debug.LogWarning("MenuUI: shareUI 未绑定");
    }

    private void OnAddStaminaClicked()
    {
        if (staminaPopupUI != null)
            staminaPopupUI.gameObject.SetActive(true);
        else
            Debug.LogWarning("MenuUI: staminaPopupUI 未绑定");
    }

    // ---- 信息刷新 ----

    private void RefreshDisplay()
    {
        // 金币
        if (coinText != null && _db != null)
            coinText.text = $"{_db.Money.Value:0}";

        // 体力
        if (staminaText != null)
        {
            int stamina = PlayerPrefs.GetInt(StaminaKey, MaxStamina);
            staminaText.text = $"{stamina}/{MaxStamina}";
        }

        // 昵称（默认值，接入SDK后替换）
        if (nicknameText != null && nicknameText.text == string.Empty)
            nicknameText.text = "玩家";
    }

    // ---- 原有升级逻辑（保持不变） ----

    private void IncreasePowerUp()
    {
        _db.PowerIncreaseLevel.Value++;
        _db.Money.Value -= _gameData.PowerUpgradePrice;
        UpdateStats();
        RefreshDisplay();
    }

    private void IncreaseFireRate()
    {
        _db.FireRateDecreaseLevel.Value++;
        _db.Money.Value -= _gameData.FireUpgradePrice;
        UpdateStats();
        RefreshDisplay();
    }

    private void CheckForDeactivatingButtons()
    {
        bool isEnoughForRate = _db.Money.Value >= _gameData.FireNextUpgradePrice;
        bool isEnoughForPower = _db.Money.Value >= _gameData.PowerNextUpgradePrice;

        fireRateIncreaseButton.Button.interactable = _gameData.upgradesPercentByLevel.Length <= _db.FireRateDecreaseLevel.Value ? false : isEnoughForRate;
        firePowerIncreaseButton.Button.interactable = _gameData.upgradesPercentByLevel.Length <= _db.FireRateDecreaseLevel.Value ? false : isEnoughForPower;

        if (_db.FireRateDecreaseLevel.Value >= _gameData.maxLevelUpgrade)
        {
            fireRateIncreaseButton.Button.interactable = false;
            fireRateIncreaseButton.PriceText.text = "MAX";
        }

        if (_db.PowerIncreaseLevel.Value >= _gameData.maxLevelUpgrade)
        {
            firePowerIncreaseButton.Button.interactable = false;
            firePowerIncreaseButton.PriceText.text = "MAX";
        }
    }

    private void UpdateStats()
    {
        _db.PowerIncreasePercent.Value = _gameData.upgradesPercentByLevel[(int)_db.PowerIncreaseLevel.Value] / 100;
        _db.FireRateDecreasePercent.Value = _gameData.upgradesPercentByLevel[(int)_db.FireRateDecreaseLevel.Value] / 100;

        firePowerIncreaseButton.LevelText.text = $"LEVEL {_db.PowerIncreaseLevel.Value}";
        fireRateIncreaseButton.LevelText.text = $"LEVEL {_db.FireRateDecreaseLevel.Value}";
        firePowerIncreaseButton.PriceText.text = $"{_gameData.PowerNextUpgradePrice} <sprite=0>";
        fireRateIncreaseButton.PriceText.text = $"{_gameData.FireNextUpgradePrice} <sprite=0>";

        CheckForDeactivatingButtons();
    }
}

