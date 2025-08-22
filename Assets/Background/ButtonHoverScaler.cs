using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
[RequireComponent(typeof(AudioSource))]
public class ButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Escala cuando el mouse está encima")]
    public float hoverScale = 1.15f;
    [Header("Escala cuando se hace click")]
    public float clickScale = 0.9f;
    [Header("Velocidad de interpolación")]
    public float speed = 8f;
    [Header("Sonido al pasar el mouse")]
    public AudioClip hoverClip;
    [Header("Sonido al hacer clic")]
    public AudioClip clickClip;
    [Header("Duración del efecto click (segundos)")]
    public float clickFeedbackDuration = 0.08f;

    private Vector3 initialScale;
    private Vector3 targetScale;
    private bool isHovered = false;
    private bool isClicking = false;
    [SerializeField]  AudioSource audioSource;
    private float clickTimer = 0f;

    void Start()
    {
        initialScale = transform.localScale;
        targetScale = initialScale;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // Si está en feedback de click, usamos esa escala
        if (isClicking)
        {
            targetScale = initialScale * clickScale;
            clickTimer -= Time.unscaledDeltaTime; // Unscaled por si hay pausas/timeScale 0
            if (clickTimer <= 0)
            {
                isClicking = false;
                // Volver a escala hover o normal
                targetScale = isHovered ? initialScale * hoverScale : initialScale;
            }
        }
        else
        {
            targetScale = isHovered ? initialScale * hoverScale : initialScale;
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (hoverClip) audioSource.PlayOneShot(hoverClip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Inicia el feedback visual
        isClicking = true;
        clickTimer = clickFeedbackDuration;
        if (clickClip) audioSource.PlayOneShot(clickClip);
    }
}
