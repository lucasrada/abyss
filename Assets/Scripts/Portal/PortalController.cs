using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class PortalController : MonoBehaviour
{
    [Header("Refs")]
    public DungeonManager dungeonManager;   // Asign� tu DungeonManager
    public Animator animator;               // Debe tener SOLO el clip activo en loop
    public Sprite lockedSprite;             // Sprite est�tico cuando est� bloqueado

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
    /// Activa o bloquea el portal. Si est� activo, reproduce la animaci�n;
    /// si est� bloqueado, muestra el sprite fijo.
    /// </summary>
    public void SetActive(bool value)
    {
        isActive = value;

        if (animator) animator.enabled = value;     // ON solo cuando est� activo

        if (!value && sr && lockedSprite)
        {
            sr.sprite = lockedSprite;               // Muestra frame est�tico
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Player")) return;

        if (dungeonManager != null)
        {
            // Teletransporta al siguiente nivel (tu m�todo actual)
            other.transform.position = dungeonManager.NextLevel();

            // Opcional: se apaga despu�s de usarse
            // SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PortalController] Asign� el DungeonManager en el Inspector.");
        }
    }
}
