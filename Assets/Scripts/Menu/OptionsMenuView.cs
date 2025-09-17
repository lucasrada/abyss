using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuView : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private Slider sliderMaster, sliderMusic, sliderSFX;

    [Range(0,1)] public float defaultMaster = 0.8f;
    [Range(0,1)] public float defaultMusic  = 0.8f;
    [Range(0,1)] public float defaultSFX    = 0.8f;

    const string KEY_MASTER="opt_master", KEY_MUSIC="opt_music", KEY_SFX="opt_sfx";
    bool _binding;

    void Awake()
    {
        if (!audioManager) Debug.LogError("[OptionsMenuView] Asigná AudioManager.");
        Bind(sliderMaster, OnMasterChanged, "[OptionsMenuView] Asigná Slider Master.");
        Bind(sliderMusic , OnMusicChanged , "[OptionsMenuView] Asigná Slider Music.");
        Bind(sliderSFX   , OnSFXChanged   , "[OptionsMenuView] Asigná Slider SFX.");
    }

    void OnEnable()
    {
        _binding = true;
        SetWithoutNotify(sliderMaster, PlayerPrefs.HasKey(KEY_MASTER)?PlayerPrefs.GetFloat(KEY_MASTER):defaultMaster);
        SetWithoutNotify(sliderMusic , PlayerPrefs.HasKey(KEY_MUSIC )?PlayerPrefs.GetFloat(KEY_MUSIC ):defaultMusic );
        SetWithoutNotify(sliderSFX   , PlayerPrefs.HasKey(KEY_SFX   )?PlayerPrefs.GetFloat(KEY_SFX   ):defaultSFX   );
        ApplyAll();
        _binding = false;
    }

    void Bind(Slider s, UnityEngine.Events.UnityAction<float> cb, string err)
    {
        if (!s){ Debug.LogError(err); return; }
        s.minValue=0; s.maxValue=1; s.wholeNumbers=false;
        s.onValueChanged.RemoveAllListeners();
        s.onValueChanged.AddListener(cb);
    }

    void SetWithoutNotify(Slider s, float v){ if(s) s.SetValueWithoutNotify(Mathf.Clamp01(v)); }

    void ApplyAll()
    {
        if(!audioManager) return;
        if(sliderMaster) audioManager.SetMaster(sliderMaster.value);
        if(sliderMusic ) audioManager.SetMusic (sliderMusic.value);
        if(sliderSFX   ) audioManager.SetSFX   (sliderSFX.value);
    }

    void OnMasterChanged(float v){ if(_binding||!audioManager) return; audioManager.SetMaster(v); PlayerPrefs.SetFloat(KEY_MASTER,v); PlayerPrefs.Save(); }
    void OnMusicChanged (float v){ if(_binding||!audioManager) return; audioManager.SetMusic (v); PlayerPrefs.SetFloat(KEY_MUSIC ,v); PlayerPrefs.Save(); }
    void OnSFXChanged   (float v){ if(_binding||!audioManager) return; audioManager.SetSFX   (v); PlayerPrefs.SetFloat(KEY_SFX   ,v); PlayerPrefs.Save(); }
}
