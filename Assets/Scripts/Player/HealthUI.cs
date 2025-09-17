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
    public Color fullHealthColor = Color.green;
    [Tooltip("Color when health is low")]
    public Color lowHealthColor = Color.red;
    [Tooltip("Health threshold for low health color")]
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;

    // Reference to the fill image for color changes
    private Image healthBarFill;

    void Start()
    {
        // Find player controller if not assigned
        if (playerController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }
        }

        // Get the fill image from the slider
        if (healthBarSlider != null)
        {
            healthBarFill = healthBarSlider.fillRect.GetComponent<Image>();
            
            // Set up the slider
            healthBarSlider.minValue = 0;
            healthBarSlider.maxValue = 100; // Or use playerController.MaxHealth
            healthBarSlider.interactable = false; // Players shouldn't drag it
        }

        // Subscribe to health changes
        if (playerController != null)
        {
            playerController.OnHealthChanged += UpdateHealthDisplay;
            playerController.OnPlayerDeath += OnPlayerDeath;
            
            // Initialize display
            UpdateHealthDisplay(playerController.CurrentHealth);
        }
        else
        {
            Debug.LogWarning("PlayerController not found for HealthUI!");
        }
    }

    void UpdateHealthDisplay(int currentHealth)
    {
        if (playerController == null) return;

        // Update slider value
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
            healthBarSlider.maxValue = playerController.MaxHealth;
        }

        // Update color if we have the fill image
        if (healthBarFill != null)
        {
            float healthPercentage = (float)currentHealth / playerController.MaxHealth;
            
            // Update color based on health percentage
            Color targetColor = Color.Lerp(lowHealthColor, fullHealthColor, 
                                         healthPercentage / lowHealthThreshold);
            healthBarFill.color = healthPercentage <= lowHealthThreshold ? targetColor : fullHealthColor;
        }

        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {playerController.MaxHealth}";
        }
    }

    void OnPlayerDeath()
    {
        // You can add death UI handling here
        Debug.Log("Player has died!");
        
        // Update the display to show zero health
        UpdateHealthDisplay(0);
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (playerController != null)
        {
            playerController.OnHealthChanged -= UpdateHealthDisplay;
            playerController.OnPlayerDeath -= OnPlayerDeath;
        }
    }
}