using UnityEngine;
using UnityEngine.UI;

using static D2D.Utilities.CommonGameplayFacade;

public class PowerText : MonoBehaviour
{
    private Text textPro;

    private void Awake()
    {
        textPro = GetComponent<Text>();

        //UpdatePowerText(0);

        //_gameProgress.OnLevelUp += UpdatePowerText;
    }
    private void UpdatePowerText(int level)
    {
        textPro.text = $"LVL {level}";
    }
}