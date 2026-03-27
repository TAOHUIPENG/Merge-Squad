using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text priceText;
    [SerializeField] private Text levelText;

    public Button Button => button;
    public Text PriceText => priceText;
    public Text LevelText => levelText;
    
    private void OnEnable()
    {
        Button.onClick.AddListener(() => transform.DOPunchScale(Vector3.one * .2f, .4f, 1, 1).SetRelative());
    }
}