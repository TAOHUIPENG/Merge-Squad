using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

using static D2D.Utilities.CommonGameplayFacade;

public class SoundSwitcher : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Button audioButton;
    [SerializeField] private Image cross;

    private const string MasterVolume = "MasterVolume";
    private const float Mute = -80f;

    private void Awake()
    {
        audioButton.onClick.AddListener(SwitchSound);
        UpdateCross();
    }
    private void Start()
    {
        float masterValue = TTPlayerPrefs.GetFloat(MasterVolume, 0);
        audioMixer.SetFloat(MasterVolume, masterValue);
    }

    private void SwitchSound()
    {
        float value = TTPlayerPrefs.GetFloat(MasterVolume, 0);

        TTPlayerPrefs.SetFloat(MasterVolume, value == 0 ? Mute : 0);

        audioMixer.SetFloat(MasterVolume, TTPlayerPrefs.GetFloat(MasterVolume, 0));

        UpdateCross();
    }
    private void UpdateCross()
    {
        cross.gameObject.SetActive(TTPlayerPrefs.GetFloat(MasterVolume, 0) == Mute);
    }
}