using System;
using D2D.Utilities;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeButtonUI : MonoBehaviour
{
    [SerializeField] private Image upgradeIcon;
    [SerializeField] private Text upgradeText;
    [SerializeField] private Button upgradeButton;

    [Header("广告角标（可选）")]
    [Tooltip("挂在按钮上的广告图标 GameObject，需要看广告时显示")]
    [SerializeField] private GameObject adBadge;

    public Button UpgradeButton => upgradeButton;

    private void OnEnable()
    {
        UpgradeButton.onClick.AddListener(() => transform.DOPunchScale(Vector3.one * .2f, .4f, 1, 1).SetRelative());
    }

    public void InitButtonUI(Sprite icon, string text)
    {
        upgradeIcon.sprite = icon;
        upgradeText.text = text;
    }

    /// <summary>
    /// 控制广告角标的显示。isAdGated=true 时显示广告图标，点击此按钮需要看广告。
    /// </summary>
    public void SetAdGated(bool isAdGated)
    {
        if (adBadge != null)
            adBadge.SetActive(isAdGated);
    }
}