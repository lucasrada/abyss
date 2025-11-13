using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Health bar slider")]
    public Slider healthBarSlider;
    [Tooltip("Health text (optional)")]
    public Text healthText;
    [Tooltip("Player controller reference")]
    public PlayerController playerController;

    [Header("Visual Settings")]
    [Tooltip("Color when health is full")]
    public Color fullHealthColor = new Color(0.2f, 1f, 0.2f);
    [Tooltip("Color when health is medium")]
    public Color mediumHealthColor = new Color(1f, 0.8f, 0f);
    [Tooltip("Color when health is low")]
    public Color lowHealthColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Health threshold for low health color (0-1)")]
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.25f;
    [Tooltip("Health threshold for medium health color (0-1)")]
    [Range(0f, 1f)]
    public float mediumHealthThreshold = 0.5f;

    [Header("Animation Settings")]
    [Tooltip("Smooth transition speed for health changes")]
    public float smoothSpeed = 5f;
    [Tooltip("Enable pulsing effect on low health")]
    public bool pulseOnLowHealth = true;
    [Tooltip("Pulse speed multiplier")]
    public float pulseSpeed = 2f;
    [Tooltip("Pulse intensity (0-1)")]
    [Range(0f, 1f)]
    public float pulseIntensity = 0.3f;
    [Tooltip("Normalized difference before snapping to the exact value to avoid stray ticks")]
    [Range(0f, 0.1f)]
    public float snapThreshold = 0.002f;

    private Image healthBarFill;
    private float targetHealthValue;
    private float displayHealthValue;

    void Start()
    {
        if (playerController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }
        }

        if (healthBarSlider != null)
        {
            healthBarFill = healthBarSlider.fillRect.GetComponent<Image>();

            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = 100;
            healthBarSlider.interactable = false;
        }

        if (playerController != null)
        {
            playerController.OnHealthChanged += UpdateHealthDisplay;
            playerController.OnPlayerDeath += OnPlayerDeath;

            targetHealthValue = playerController.CurrentHealth;
            displayHealthValue = targetHealthValue;
            UpdateHealthDisplay(playerController.CurrentHealth);
        }
        else
        {
            Debug.LogWarning("PlayerController not found for HealthUI!");
        }
    }

    void Update()
    {
        if (healthBarSlider != null && playerController != null)
        {
            displayHealthValue = Mathf.Lerp(displayHealthValue, targetHealthValue, Time.deltaTime * smoothSpeed);
            float maxHealth = Mathf.Max(1f, playerController.MaxHealth);
            float normalizedDiff = Mathf.Abs(displayHealthValue - targetHealthValue) / maxHealth;
            if (normalizedDiff <= Mathf.Max(0.0001f, snapThreshold))
            {
                displayHealthValue = targetHealthValue;
            }
            healthBarSlider.value = displayHealthValue;

            float healthPercentage = displayHealthValue / maxHealth;

            if (healthBarFill != null)
            {
                Color targetColor = GetHealthColor(healthPercentage);

                if (pulseOnLowHealth && healthPercentage <= lowHealthThreshold)
                {
                    float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                    float pulseFactor = 1f - (pulse * pulseIntensity);
                    targetColor = Color.Lerp(targetColor, Color.white, pulse * pulseIntensity);
                }

                healthBarFill.color = targetColor;
            }
        }
    }

    void UpdateHealthDisplay(int currentHealth)
    {
        if (playerController == null) return;

        targetHealthValue = currentHealth;

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = playerController.MaxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {playerController.MaxHealth}";
        }
    }

    Color GetHealthColor(float healthPercentage)
    {
        if (healthPercentage <= lowHealthThreshold)
        {
            return Color.Lerp(lowHealthColor, mediumHealthColor,
                healthPercentage / lowHealthThreshold);
        }
        else if (healthPercentage <= mediumHealthThreshold)
        {
            float t = (healthPercentage - lowHealthThreshold) / (mediumHealthThreshold - lowHealthThreshold);
            return Color.Lerp(mediumHealthColor, fullHealthColor, t);
        }
        else
        {
            float t = (healthPercentage - mediumHealthThreshold) / (1f - mediumHealthThreshold);
            return Color.Lerp(fullHealthColor, fullHealthColor, t);
        }
    }

    void OnPlayerDeath()
    {
        targetHealthValue = 0;
        UpdateHealthDisplay(0);
    }

    void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.OnHealthChanged -= UpdateHealthDisplay;
            playerController.OnPlayerDeath -= OnPlayerDeath;
        }
    }

}
