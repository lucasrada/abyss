using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Escala")]
    public float hoverScale = 1.15f;
    public float clickScale = 0.9f;
    public float speed = 8f;

    [Header("SFX")]
    public AudioClip hoverClip;
    public AudioClip clickClip;
    [Range(0f,1f)] public float sfxVolume = 1f;

    private Vector3 initialScale;
    private Vector3 targetScale;
    private bool isHovered;
    private float clickFeedbackDuration = 0.08f;
    private float clickTimer;

    void Awake()
    {
        initialScale = transform.localScale;
        targetScale = initialScale;
    }

    void Update()
    {
        // Lerp de escala
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * speed);

        // Rebote post click
        if (clickTimer > 0f)
        {
            clickTimer -= Time.unscaledDeltaTime;
            if (clickTimer <= 0f)
            {
                targetScale = isHovered ? initialScale * hoverScale : initialScale;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = initialScale * hoverScale;
        PlayUISfx(hoverClip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = initialScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        targetScale = initialScale * clickScale;
        clickTimer = clickFeedbackDuration;
        PlayUISfx(clickClip);
    }

    private void PlayUISfx(AudioClip clip)
    {
        if (!clip) return;
        if (UISfxPlayer.Instance == null)
        {
            Debug.LogWarning("[ButtonHoverScaler] No hay UISfxPlayer en la escena.");
            return;
        }
        UISfxPlayer.Instance.PlayOneShotSafe(clip, sfxVolume);
    }
}
