using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class PortalController : MonoBehaviour
{
    [Header("Refs")]
    public DungeonManager dungeonManager;   // Asigná tu DungeonManager
    public Animator animator;               // Debe tener SOLO el clip activo en loop
    public Sprite lockedSprite;             // Sprite estático cuando está bloqueado

    [SerializeField] private bool isActive = false;

    private Collider2D col;
    private SpriteRenderer sr;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;

        sr = GetComponent<SpriteRenderer>();

        // Asegura estado inicial bloqueado
        SetActive(false);
    }

    /// <summary>
    /// Activa o bloquea el portal. Si está activo, reproduce la animación;
    /// si está bloqueado, muestra el sprite fijo.
    /// </summary>
    public void SetActive(bool value)
    {
        isActive = value;

        if (animator) animator.enabled = value;     // ON solo cuando está activo

        if (!value && sr && lockedSprite)
        {
            sr.sprite = lockedSprite;               // Muestra frame estático
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Player")) return;

        if (dungeonManager != null)
        {
            // Teletransporta al siguiente nivel (tu método actual)
            other.transform.position = dungeonManager.NextLevel();

            // Opcional: se apaga después de usarse
            // SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PortalController] Asigná el DungeonManager en el Inspector.");
        }
    }
}
