using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color fillRectangleColor = new Color(0.2588f, 0.7843f, 0.9764f, 1f);
    [SerializeField] private Color backgroundRectangleColor = new Color(0.098f, 0.098f, 0.098f, 0.9f);
    [SerializeField] private TMP_Text dashText;
    [SerializeField] private string readyLabel = "Dash listo";
    [SerializeField] private string rechargingFormat = "Dash {0:0.0}s";
    [SerializeField, Range(0f, 0.1f)] private float emptyFillThreshold = 0.01f;

    static Texture2D solidTexture;
    static Sprite solidSprite;

    void Awake()
    {
        if (!player)
        {
#if UNITY_2022_1_OR_NEWER
            player = FindFirstObjectByType<PlayerController>();
#else
            player = FindObjectOfType<PlayerController>();
#endif
        }

        if (!backgroundImage)
        {
            backgroundImage = GetComponent<Image>();
        }

        ConfigureRectangleImages();
    }

    void Update()
    {
        if (!player || !fillImage || !dashText) return;

        float normalized = Mathf.Clamp01(player.DashCooldownNormalized);
        float fillValue = normalized <= emptyFillThreshold ? 0f : normalized;
        fillImage.fillAmount = fillValue;

        float remaining = player.DashCooldownRemaining;
        dashText.text = remaining > 0.05f
            ? string.Format(rechargingFormat, remaining)
            : readyLabel;
    }

    public void SetPlayer(PlayerController controller)
    {
        player = controller;
    }

    void ConfigureRectangleImages()
    {
        Sprite sprite = GetSolidSprite();

        if (fillImage)
        {
            if (fillImage.sprite == null)
            {
                fillImage.sprite = sprite;
            }
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.preserveAspect = false;
            fillImage.color = fillRectangleColor;
            fillImage.raycastTarget = false;
        }

        if (backgroundImage)
        {
            bool assignedRuntimeSprite = false;
            if (backgroundImage.sprite == null)
            {
                backgroundImage.sprite = sprite;
                assignedRuntimeSprite = true;
            }

            if (assignedRuntimeSprite)
            {
                backgroundImage.type = Image.Type.Simple;
                backgroundImage.preserveAspect = false;
                backgroundImage.color = backgroundRectangleColor;
                backgroundImage.raycastTarget = false;
            }
        }
    }

    Sprite GetSolidSprite()
    {
        if (solidSprite != null) return solidSprite;

        solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        solidTexture.SetPixel(0, 0, Color.white);
        solidTexture.Apply();
        solidTexture.name = "DashCooldownUI_RuntimeTexture";
        solidTexture.hideFlags = HideFlags.HideAndDontSave;

        solidSprite = Sprite.Create(solidTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        solidSprite.name = "DashCooldownUI_RuntimeSprite";
        solidSprite.hideFlags = HideFlags.HideAndDontSave;

        return solidSprite;
    }
}
