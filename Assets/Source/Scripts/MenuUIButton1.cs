using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIButton1 : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text priceText;
    [SerializeField] private Text levelText;

    public Button Button => button;
    public Text PriceText => priceText;
    public Text LevelText => levelText;
    
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        Button.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        Button.onClick.RemoveListener(OnClick);
        // 对象隐藏时终止动画并还原缩放，防止残留状态
        transform.DOKill();
        transform.localScale =originalScale;
    }

    private void OnClick()
    {
        // 先终止正在进行的动画并归位，再重新播放，防止连点时缩放累积漂移
        transform.DOKill();
        transform.localScale = originalScale;
        transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 1, 1);
    }
}