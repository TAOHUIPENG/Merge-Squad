using UnityEngine;
using UnityEngine.UI;
using D2D;

using static D2D.Utilities.CommonGameplayFacade;
using MoreMountains.NiceVibrations;

public class VibrationSwitcher : MonoBehaviour
{
    [SerializeField] private Button vibrationButton;
    [SerializeField] private Image cross;

    private const string Vibration = "Vibration";

    private void Awake()
    {
        vibrationButton.onClick.AddListener(SwitchVibration);
        UpdateCross();
    }
    private void Start()
    {
        bool isActive = TTPlayerPrefs.GetFloat(Vibration, 0) == 1;
        MMVibrationManager.SetHapticsActive(isActive);
    }

    private void SwitchVibration()
    {
        float value = TTPlayerPrefs.GetFloat(Vibration, 0);

        TTPlayerPrefs.SetFloat(Vibration, value == 0 ? 1 : 0);

        bool isActive = TTPlayerPrefs.GetFloat(Vibration, 0) == 1;
        MMVibrationManager.SetHapticsActive(isActive);

        UpdateCross();
    }
    private void UpdateCross()
    {
        cross.gameObject.SetActive(TTPlayerPrefs.GetFloat(Vibration, 0) == 1);
    }
}