using System.Collections;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);
}

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Color baseColor = Color.white;
    private Color defaultColor = Color.white;
    private float currentHealth;
    private Coroutine flashRoutine;
    private bool isDying;

    public Color DefaultColor => defaultColor;

    private void Awake()
    {
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        CacheDefaultColor();
    }

    private void OnEnable()
    {
        maxHealth = maxHealth > 0f ? maxHealth : 1f;
        flashDuration = Mathf.Max(0.01f, flashDuration);

        spriteRenderer ??= GetComponent<SpriteRenderer>();
        CacheDefaultColor();
        baseColor = defaultColor;
        currentHealth = maxHealth;
        isDying = false;
        ResetVisualState();
    }

    public void SetDefaultColor(Color color)
    {
        defaultColor = color;
        SetBaseColor(color);
    }

    public void SetBaseColor(Color color)
    {
        baseColor = color;

        if (!isDying && flashRoutine == null && spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDying)
        {
            return;
        }

        float appliedDamage = Mathf.Max(0f, damage);
        if (appliedDamage <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - appliedDamage);
        if (currentHealth <= 0f)
        {
            BeginDeathSequence();
            return;
        }

        StartFlash(destroyAfterFlash: false);
    }

    private void BeginDeathSequence()
    {
        isDying = true;

        if (TryGetComponent(out EnemyAI enemyAI))
        {
            enemyAI.enabled = false;
        }

        if (TryGetComponent(out Rigidbody2D rigidbody2d))
        {
            rigidbody2d.linearVelocity = Vector2.zero;
            rigidbody2d.simulated = false;
        }

        foreach (Collider2D hitbox in GetComponents<Collider2D>())
        {
            hitbox.enabled = false;
        }

        StartFlash(destroyAfterFlash: true);
    }

    private void StartFlash(bool destroyAfterFlash)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine(destroyAfterFlash));
    }

    private IEnumerator FlashRoutine(bool destroyAfterFlash)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }

        yield return new WaitForSeconds(flashDuration);

        if (destroyAfterFlash)
        {
            Destroy(gameObject);
            yield break;
        }

        ResetVisualState();
        flashRoutine = null;
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        baseColor = defaultColor;
        ResetVisualState();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(0.01f, maxHealth);
        flashDuration = Mathf.Max(0.01f, flashDuration);
    }

    private void CacheDefaultColor()
    {
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
        }
    }

    private void ResetVisualState()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }
    }
}