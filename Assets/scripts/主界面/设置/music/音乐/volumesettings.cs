using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider voiceSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private string masterParam = "MasterVol";
    [SerializeField] private string bgmParam    = "BGMVol";
    [SerializeField] private string voiceParam  = "VoiceVol";
    [SerializeField] private string sfxParam    = "SFXVol";

    void Start()
    {
        masterSlider.value = PlayerPrefs.GetFloat(masterParam, 1f);
        bgmSlider.value    = PlayerPrefs.GetFloat(bgmParam, 1f);
        voiceSlider.value  = PlayerPrefs.GetFloat(voiceParam, 1f);
        sfxSlider.value    = PlayerPrefs.GetFloat(sfxParam, 1f);

        masterSlider.onValueChanged.AddListener(v => SetVolume(masterParam, v));
        bgmSlider.onValueChanged.AddListener(v => SetVolume(bgmParam, v));
        voiceSlider.onValueChanged.AddListener(v => SetVolume(voiceParam, v));
        sfxSlider.onValueChanged.AddListener(v => SetVolume(sfxParam, v));

        SetVolume(masterParam, masterSlider.value);
        SetVolume(bgmParam, bgmSlider.value);
        SetVolume(voiceParam, voiceSlider.value);
        SetVolume(sfxParam, sfxSlider.value);
    }

    void SetVolume(string param, float value)
    {
        float dB = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        mixer.SetFloat(param, dB);
        PlayerPrefs.SetFloat(param, value);
    }
}