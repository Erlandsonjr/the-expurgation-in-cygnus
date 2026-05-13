using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class PlayerController : MonoBehaviour, IDamageable
{
    private static readonly int SpeedParameterHash = Animator.StringToHash("Speed");
    private static readonly int DeathParameterHash = Animator.StringToHash("Death");
    private static readonly int IsJumpingParameterHash = Animator.StringToHash("isJumping");
    private static readonly int AimXParameterHash = Animator.StringToHash("aimX");
    private static readonly int AimYParameterHash = Animator.StringToHash("aimY");
    private static readonly int IsDashingParameterHash = Animator.StringToHash("isDashing");
    private const float InvincibilityFlickerInterval = 0.08f;
    private const float MissingReferenceRetryInterval = 0.5f;
    private static readonly Color DashRingVisibleColor = new Color(1f, 0.5f, 0f, 0.6f);
    private static readonly Vector3 WeaponVisualLocalPosition = new Vector3(0.309f, -0.202f, 0f);
    private static readonly Vector3 WeaponVisualBaseScale = new Vector3(0.4f, 0.4f, 1f);

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 90f;
    [SerializeField] private float deceleration = 110f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBuffer = 0.12f;
    [SerializeField] private float baseGravityScale = 4f;
    [SerializeField] private float fallGravityMultiplier = 2.2f;
    [SerializeField] private float lowJumpGravityMultiplier = 1.8f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckDistance = 0.08f;

    [Header("Aim")]
    [FormerlySerializedAs("pivot")]
    [SerializeField] private Transform aimPivot;

    [Header("Weapon Visual")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private SpriteRenderer weaponVisualRenderer;

    [Header("Presentation")]
    [SerializeField] private Animator bodyAnimator;
    [SerializeField] private SpriteRenderer bodySpriteRenderer;

    [Header("Health")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] private float knockbackForce = 9f;
    [SerializeField] private float knockbackTotalTime = 0.2f;

    [Header("Combat")]
    [SerializeField] private WeaponData defaultWeapon;
    [SerializeField] private WeaponData activeWeapon;
    [SerializeField] private ProjectilePooler projectilePooler;
    public AudioSource sfxSource;

    [Header("Runtime Modifiers")]
    public float fireRateMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float projSpeedMultiplier = 1f;
    public float flatDamageBonus = 0f;
    public float dashCooldownMultiplier = 1f;
    public float extraIframeTime = 0f;
    public int extraJumps = 0;
    public bool nextShotIsCrit = false;
    public bool hasCryoAmmo = false;
    public bool hasBerserkerRage = false;
    public bool hasRadiationAura = false;
    public bool hasSpreadShot = false;

    [Header("Dash")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 7f;

    [Header("UI")]
    [SerializeField] private GameOverManager gameOverManager;
    [SerializeField] private GameObject controlsOverlay;
    public Sprite dashRingSprite;
    [SerializeField] public Sprite[] healthSprites;
    [SerializeField] public UnityEngine.UI.Image healthBarImage;
    [SerializeField] public UnityEngine.UI.Image dashCooldownFill;
    [SerializeField] private UnityEngine.UI.Image dashCooldownRingImage;

    [Header("Laser")]
    public LineRenderer laserLine;
    [SerializeField] private LineRenderer spreadLaserLine;
    public GameObject continuousLaserVisual;
    public GameObject radiationAuraVisual;

    private BoxCollider2D boxCollider;
    private Color bodyDefaultColor = Color.white;
    private Camera mainCamera;
    private Coroutine continuousLaserRoutine;
    private float currentHealth;
    private Coroutine invincibilityRoutine;
    private float knockbackCounter;
    private Rigidbody2D rigidbody2d;

    private float coyoteCounter;
    private int currentJumps;
    private float jumpBufferCounter;
    private float nextDefaultWeaponLookupTime;
    private float nextProjectilePoolLookupTime;
    private float shotCooldownTimer;
    private bool isFiringLaser;
    private bool hasCritAfterDashUpgrade;
    private bool dashReadyFlashPlayed;
    private bool isInvincible;
    private bool isDead;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool jumpHeld;
    private bool hasDismissedControls;
    private Vector2 lastAimWorldPosition;
    private float lastDashTime = -10f;
    public bool isDashing = false;
    private Coroutine dashRingFlashRoutine;
    private TMP_Text controlsOverlayPrimaryLabel;

    public event Action<float, float> HealthChanged;

    public bool IsInvincible => isInvincible;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        rigidbody2d = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        if (baseGravityScale <= 0f)
        {
            baseGravityScale = rigidbody2d.gravityScale > 0f ? rigidbody2d.gravityScale : 1f;
        }

        rigidbody2d.gravityScale = baseGravityScale;
        ResolveWeaponRig();

        Transform bodyTransform = transform.Find("Body");
        if (bodyTransform != null)
        {
            bodyAnimator ??= bodyTransform.GetComponent<Animator>();
            bodySpriteRenderer ??= bodyTransform.GetComponent<SpriteRenderer>();
        }

        bodyAnimator ??= GetComponentInChildren<Animator>();
        bodySpriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
        gameOverManager ??= FindAnyObjectByType<GameOverManager>();
        projectilePooler ??= FindAnyObjectByType<ProjectilePooler>();

        ResolveWeaponRig();

        if (healthBarImage == null)
        {
            Transform healthBarTransform = GameObject.Find("Canvas")?.transform.Find("HealthBar");
            if (healthBarTransform != null)
            {
                healthBarImage = healthBarTransform.GetComponent<UnityEngine.UI.Image>();
            }
        }

        if (continuousLaserVisual == null && aimPivot != null)
        {
            continuousLaserVisual = aimPivot.Find("LaserBeamVisual")?.gameObject;
        }

        if (spreadLaserLine == null && aimPivot != null)
        {
            spreadLaserLine = aimPivot.Find("LaserBeamVisual_Spread")?.GetComponent<LineRenderer>();
        }

        if (radiationAuraVisual == null)
        {
            radiationAuraVisual = transform.Find("RadiationAuraVisual")?.gameObject;
        }

        if (laserLine == null && continuousLaserVisual != null)
        {
            laserLine = continuousLaserVisual.GetComponent<LineRenderer>();
        }

        EnsureDashCooldownRing();

        maxHealth = maxHealth > 0f ? maxHealth : 5f;
        invincibilityDuration = Mathf.Max(0f, invincibilityDuration);
        knockbackForce = Mathf.Max(0f, knockbackForce);
        currentHealth = maxHealth;
        isInvincible = false;
        isDead = false;

        if (bodySpriteRenderer != null)
        {
            bodyDefaultColor = bodySpriteRenderer.color;
        }

        WeaponData initialWeapon = activeWeapon != null ? activeWeapon : ResolveDefaultWeapon();
        if (initialWeapon != null)
        {
            EquipWeapon(initialWeapon);
        }

        EnsureCombatDependencies(true);

        NotifyHealthChanged();
    }

    private void Start()
    {
        EnsureCombatDependencies(true);

        ResolveControlsOverlay();
        UpdateControlsOverlayPrimaryLabel();

        if (controlsOverlay != null)
        {
            controlsOverlay.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void DismissControls()
    {
        hasDismissedControls = true;

        if (controlsOverlay != null)
        {
            controlsOverlay.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool IsControlsOverlayActive()
    {
        return controlsOverlay != null && controlsOverlay.activeInHierarchy;
    }

    private void ResolveWeaponRig()
    {
        aimPivot ??= transform.Find("AimPivot") ?? transform.Find("Pivot");
        weaponPivot ??= transform.Find("WeaponPivot") ?? aimPivot;

        if (weaponPivot != null)
        {
            weaponPivot.localScale = Vector3.one;
        }

        if (weaponVisualRenderer == null && weaponPivot != null)
        {
            Transform weaponVisual = weaponPivot.Find("WeaponVisual");
            if (weaponVisual != null)
            {
                weaponVisualRenderer = weaponVisual.GetComponent<SpriteRenderer>();
            }
        }

        if (weaponVisualRenderer == null && weaponPivot != null)
        {
            GameObject weaponVisualObject = new GameObject("WeaponVisual", typeof(SpriteRenderer));
            weaponVisualObject.transform.SetParent(weaponPivot, false);
            weaponVisualRenderer = weaponVisualObject.GetComponent<SpriteRenderer>();
            weaponVisualRenderer.sortingOrder = 15;
            if (bodySpriteRenderer != null)
            {
                weaponVisualRenderer.sharedMaterial = bodySpriteRenderer.sharedMaterial;
            }
        }

        ResetWeaponVisualTransform();
    }

    private void ResetWeaponVisualTransform()
    {
        if (weaponVisualRenderer == null)
        {
            return;
        }

        Transform weaponVisualTransform = weaponVisualRenderer.transform;
        if (weaponPivot != null && weaponVisualTransform.parent != weaponPivot)
        {
            weaponVisualTransform.SetParent(weaponPivot, false);
        }

        weaponVisualTransform.localPosition = WeaponVisualLocalPosition;
        weaponVisualTransform.localRotation = Quaternion.identity;
        weaponVisualTransform.localScale = WeaponVisualBaseScale;
    }

    private WeaponData ResolveDefaultWeapon()
    {
        if (defaultWeapon != null)
        {
            return defaultWeapon;
        }

        WeaponData namedWeapon = FindWeaponAsset("FirstWeapon");
        if (namedWeapon != null)
        {
            return namedWeapon;
        }

        namedWeapon = FindWeaponAsset("LaserPistol");
        if (namedWeapon != null)
        {
            return namedWeapon;
        }

        WeaponData[] allWeapons = Resources.FindObjectsOfTypeAll<WeaponData>();
        return allWeapons != null && allWeapons.Length > 0 ? allWeapons[0] : null;
    }

    private static WeaponData FindWeaponAsset(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName))
        {
            return null;
        }

        foreach (WeaponData weapon in Resources.FindObjectsOfTypeAll<WeaponData>())
        {
            if (weapon == null)
            {
                continue;
            }

            if (string.Equals(weapon.WeaponName, weaponName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(weapon.name, weaponName, StringComparison.OrdinalIgnoreCase))
            {
                return weapon;
            }
        }

        return null;
    }

    private void EnsureCombatDependencies(bool force = false)
    {
        float now = Time.unscaledTime;

        if (projectilePooler == null && (force || now >= nextProjectilePoolLookupTime))
        {
            projectilePooler = FindAnyObjectByType<ProjectilePooler>();
            nextProjectilePoolLookupTime = now + MissingReferenceRetryInterval;
        }

        if (activeWeapon == null && (force || now >= nextDefaultWeaponLookupTime))
        {
            WeaponData fallbackWeapon = ResolveDefaultWeapon();
            if (fallbackWeapon != null)
            {
                EquipWeapon(fallbackWeapon);
            }

            nextDefaultWeaponLookupTime = now + MissingReferenceRetryInterval;
        }
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        HandlePauseToggle();

        EnsureCombatDependencies();

        if (IsControlsOverlayActive())
        {
            moveInput = Vector2.zero;
            jumpHeld = false;
            UpdateAnimation();
            return;
        }

        if (knockbackCounter > 0f)
        {
            knockbackCounter = Mathf.Max(0f, knockbackCounter - Time.deltaTime);
            moveInput = Vector2.zero;
        }
        else
        {
            moveInput = ReadMoveInput();
        }

        jumpHeld = IsJumpHeld();
        UpdateAnimation();

        if (WasJumpPressedThisFrame())
        {
            jumpBufferCounter = jumpBuffer;
        }
        else
        {
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.deltaTime);
        }

        shotCooldownTimer = Mathf.Max(0f, shotCooldownTimer - Time.deltaTime);
        UpdateAim();
        HandlePrimaryFire();
        HandleDash();
        UpdateDashCooldownUI();
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            rigidbody2d.linearVelocity = Vector2.zero;
            rigidbody2d.gravityScale = baseGravityScale;
            return;
        }

        if (IsControlsOverlayActive())
        {
            rigidbody2d.linearVelocity = Vector2.zero;
            rigidbody2d.gravityScale = baseGravityScale;
            return;
        }

        isGrounded = rigidbody2d.linearVelocity.y > 0.01f ? false : CheckGrounded();
        coyoteCounter = isGrounded ? coyoteTime : Mathf.Max(0f, coyoteCounter - Time.fixedDeltaTime);

        if (isGrounded)
        {
            currentJumps = 0;
        }

        if (knockbackCounter <= 0f && !isDashing)
        {
            ApplyHorizontalMovement();
        }

        TryConsumeJump();
        ApplyVariableGravity();
    }

    private void ApplyHorizontalMovement()
    {
        float targetSpeed = moveInput.x * moveSpeed;
        float speedDelta = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float nextVelocityX = Mathf.MoveTowards(rigidbody2d.linearVelocity.x, targetSpeed, speedDelta * Time.fixedDeltaTime);

        rigidbody2d.linearVelocity = new Vector2(nextVelocityX, rigidbody2d.linearVelocity.y);
    }

    private void TryConsumeJump()
    {
        if (jumpBufferCounter <= 0f)
        {
            return;
        }

        bool canGroundJump = coyoteCounter > 0f;
        bool canExtraJump = !canGroundJump && currentJumps <= extraJumps;
        if (!canGroundJump && !canExtraJump)
        {
            return;
        }

        Vector2 nextVelocity = rigidbody2d.linearVelocity;
        nextVelocity.y = jumpForce;
        rigidbody2d.linearVelocity = nextVelocity;

        currentJumps += 1;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        isGrounded = false;
    }

    private void ApplyVariableGravity()
    {
        if (rigidbody2d.linearVelocity.y < 0f)
        {
            rigidbody2d.gravityScale = baseGravityScale * fallGravityMultiplier;
            return;
        }

        if (rigidbody2d.linearVelocity.y > 0f && !jumpHeld)
        {
            rigidbody2d.gravityScale = baseGravityScale * lowJumpGravityMultiplier;
            return;
        }

        rigidbody2d.gravityScale = baseGravityScale;
    }

    private void UpdateAim()
    {
        if (aimPivot == null || weaponPivot == null || weaponVisualRenderer == null)
        {
            ResolveWeaponRig();
        }

        if (aimPivot == null || Mouse.current == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        Vector3 mouseScreenPosition = Mouse.current.position.ReadValue();
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z - aimPivot.position.z);

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        lastAimWorldPosition = mouseWorldPosition;
        if (bodySpriteRenderer != null)
        {
            Transform bodyTransform = bodySpriteRenderer.transform;
            bool facingLeft = mouseWorldPosition.x < transform.position.x;
            Vector3 s = bodyTransform.localScale;
            bodyTransform.localScale = new Vector3(facingLeft ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, s.z);
        }

        Vector2 aimDirection = mouseWorldPosition - aimPivot.position;

        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        aimPivot.rotation = Quaternion.Euler(0f, 0f, aimAngle);

        if (weaponPivot == null || activeWeapon == null)
        {
            return;
        }

        weaponPivot.rotation = Quaternion.Euler(0f, 0f, aimAngle);

        if (weaponVisualRenderer != null)
        {
            bool mouseLeft = mouseWorldPosition.x < transform.position.x;
            Transform wvt = weaponVisualRenderer.transform;
            wvt.localScale = new Vector3(WeaponVisualBaseScale.x, mouseLeft ? -WeaponVisualBaseScale.y : WeaponVisualBaseScale.y, WeaponVisualBaseScale.z);
        }
    }

    private void UpdateAnimation()
    {
        if (bodyAnimator == null)
        {
            return;
        }

        bodyAnimator.SetFloat(SpeedParameterHash, moveInput.magnitude);
        bodyAnimator.SetBool(IsJumpingParameterHash, !isGrounded);
        bodyAnimator.SetBool(IsDashingParameterHash, isDashing);

        Vector2 aimDir = lastAimWorldPosition.sqrMagnitude > 0.0001f
            ? ((Vector3)lastAimWorldPosition - transform.position).normalized
            : Vector2.right;
        bodyAnimator.SetFloat(AimXParameterHash, aimDir.x);
        bodyAnimator.SetFloat(AimYParameterHash, aimDir.y);
    }

    private void HandlePrimaryFire()
    {
        if (!IsPrimaryFireHeld())
        {
            return;
        }

        TryFireProjectile();
    }

    private void HandleDash()
    {
        if (!WasSecondaryFirePressedThisFrame() || isDashing || Time.time < lastDashTime + GetEffectiveDashCooldown())
        {
            return;
        }

        StartCoroutine(DashRoutine());
    }

    private void UpdateDashCooldownUI()
    {
        float fillAmount = 0f;

        if (!isDashing)
        {
            float timeSinceDash = Time.time - lastDashTime;
            fillAmount = Mathf.Clamp01(timeSinceDash / GetEffectiveDashCooldown());
        }

        UpdateDashCooldownRing(fillAmount);
    }

    private IEnumerator DashRoutine()
    {
        Vector2 dashDir = ((Vector3)lastAimWorldPosition - transform.position).normalized;
        if (dashDir.sqrMagnitude < 0.0001f)
        {
            dashDir = Vector2.right;
        }

        isDashing = true;
        ResetDashCooldownRing();
        if (bodySpriteRenderer != null)
            bodySpriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);

        rigidbody2d.linearVelocity = dashDir * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        if (bodySpriteRenderer != null)
            bodySpriteRenderer.color = bodyDefaultColor;

        if (hasCritAfterDashUpgrade)
        {
            nextShotIsCrit = true;
        }

        lastDashTime = Time.time;
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null)
        {
            return;
        }

        ResolveWeaponRig();

        activeWeapon = newWeapon;
        shotCooldownTimer = 0f;

        if (weaponPivot == null)
        {
            return;
        }

        ResetWeaponVisualTransform();

        if (weaponVisualRenderer != null)
        {
            weaponVisualRenderer.sprite = newWeapon.WeaponSprite;
            weaponVisualRenderer.enabled = newWeapon.WeaponSprite != null;
        }
    }

    public void AddMaxHealth(float delta)
    {
        maxHealth = Mathf.Max(1f, maxHealth + delta);
        currentHealth = Mathf.Min(currentHealth + delta, maxHealth);
        NotifyHealthChanged();
    }

    public void AddMoveSpeed(float delta)      => moveSpeed    = Mathf.Max(1f, moveSpeed    + delta);
    public void AddDashCooldown(float delta)   => dashCooldown = Mathf.Max(1f, dashCooldown + delta);
    public void AddProjectileSpeed(float delta)
    {
        float baseProjectileSpeed = activeWeapon != null ? activeWeapon.ProjectileSpeed : 10f;
        baseProjectileSpeed = Mathf.Max(1f, baseProjectileSpeed);
        float nextMultiplier = (baseProjectileSpeed + delta) / baseProjectileSpeed;
        projSpeedMultiplier = Mathf.Max(0.01f, projSpeedMultiplier * nextMultiplier);
    }
    public void AddJumpForce(float delta)      => jumpForce    = Mathf.Max(1f, jumpForce    + delta);

    public void Heal(int amount)
    {
        if (amount <= 0 || isDead)
        {
            return;
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        NotifyHealthChanged();
    }

    public void ApplyCard(CardData card)
    {
        if (card == null)
        {
            return;
        }

        switch (card.effectType)
        {
            case CardEffectType.Heal:
                Heal(Mathf.RoundToInt(card.effectValue));
                break;

            case CardEffectType.MaxHealth:
            {
                int maxHealthBonus = Mathf.RoundToInt(card.effectValue);
                if (maxHealthBonus <= 0)
                {
                    break;
                }

                maxHealth = Mathf.Max(1f, maxHealth + maxHealthBonus);
                Heal(maxHealthBonus);
                break;
            }

            case CardEffectType.MoveSpeed:
                moveSpeed = Mathf.Max(1f, moveSpeed * card.effectValue);
                break;

            case CardEffectType.FireRate:
                fireRateMultiplier = Mathf.Max(0.01f, fireRateMultiplier * card.effectValue);
                break;

            case CardEffectType.ProjectileSpeed:
                projSpeedMultiplier = Mathf.Max(0.01f, projSpeedMultiplier * card.effectValue);
                break;

            case CardEffectType.JumpForce:
                jumpForce = Mathf.Max(1f, jumpForce * card.effectValue);
                break;

            case CardEffectType.FlatDamage:
                flatDamageBonus += card.effectValue;
                break;

            case CardEffectType.DashCooldown:
                dashCooldownMultiplier = Mathf.Max(0.01f, dashCooldownMultiplier * card.effectValue);
                break;

            case CardEffectType.Invulnerability:
                extraIframeTime += card.effectValue;
                break;

            case CardEffectType.Luck:
                break;

            case CardEffectType.ExtraJumps:
                extraJumps += Mathf.RoundToInt(card.effectValue);
                break;

            case CardEffectType.CritNextShot:
                hasCritAfterDashUpgrade = true;
                break;

            case CardEffectType.CryoAmmo:
                hasCryoAmmo = true;
                break;

            case CardEffectType.BerserkerRage:
                hasBerserkerRage = true;
                break;

            case CardEffectType.RadiationAura:
                hasRadiationAura = true;
                if (radiationAuraVisual != null)
                {
                    radiationAuraVisual.SetActive(true);
                }
                break;

            case CardEffectType.SpreadShot:
                hasSpreadShot = true;
                break;
        }

        NotifyHealthChanged();
    }

    private float GetEffectiveDamage()
    {
        if (activeWeapon == null)
        {
            return 0f;
        }

        float finalDmg = (activeWeapon.Damage + flatDamageBonus) * damageMultiplier;
        if (hasBerserkerRage && currentHealth == 1f)
        {
            finalDmg *= 2f;
        }

        return Mathf.Max(0f, finalDmg);
    }

    private float GetEffectiveShotInterval()
    {
        if (activeWeapon == null)
        {
            return float.PositiveInfinity;
        }

        return activeWeapon.ShotInterval * fireRateMultiplier;
    }

    private float GetEffectiveProjectileSpeed()
    {
        if (activeWeapon == null)
        {
            return 0f;
        }

        return activeWeapon.ProjectileSpeed * projSpeedMultiplier;
    }

    private float GetEffectiveDashCooldown()
    {
        return Mathf.Max(0.01f, dashCooldown * dashCooldownMultiplier);
    }

    private void PlayActiveWeaponShootSound()
    {
        if (sfxSource != null && activeWeapon != null && activeWeapon.shootSound != null)
        {
            sfxSource.PlayOneShot(activeWeapon.shootSound, activeWeapon.shootVolume);
        }
    }

    private void HandlePauseToggle()
    {
        if (!WasPausePressedThisFrame())
        {
            return;
        }

        if (IsControlsOverlayActive())
        {
            DismissControls();
            return;
        }

        if (!hasDismissedControls)
        {
            return;
        }

        ResolveControlsOverlay();
        UpdateControlsOverlayPrimaryLabel();

        if (controlsOverlay != null)
        {
            controlsOverlay.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    private void ResolveControlsOverlay()
    {
        if (controlsOverlay == null)
        {
            Transform controlsOverlayTransform = GameObject.Find("Canvas")?.transform.Find("ControlsOverlay");
            if (controlsOverlayTransform != null)
            {
                controlsOverlay = controlsOverlayTransform.gameObject;
            }
        }

        if (controlsOverlayPrimaryLabel == null && controlsOverlay != null)
        {
            controlsOverlayPrimaryLabel = controlsOverlay.transform.Find("StartButton/Text (TMP)")?.GetComponent<TMP_Text>();
        }
    }

    private void UpdateControlsOverlayPrimaryLabel()
    {
        ResolveControlsOverlay();

        if (controlsOverlayPrimaryLabel != null)
        {
            controlsOverlayPrimaryLabel.text = hasDismissedControls ? "RESUME" : "START";
        }
    }

    private void TryFireProjectile()
    {
        EnsureCombatDependencies();

        if (activeWeapon == null || aimPivot == null)
        {
            return;
        }

        if (activeWeapon.weaponType == WeaponType.ContinuousLaser)
        {
            if (isFiringLaser || shotCooldownTimer > 0f)
            {
                return;
            }

            if (continuousLaserVisual == null && aimPivot != null)
            {
                continuousLaserVisual = aimPivot.Find("LaserBeamVisual")?.gameObject;
            }

            if (laserLine == null && continuousLaserVisual != null)
            {
                laserLine = continuousLaserVisual.GetComponent<LineRenderer>();
            }

            if (continuousLaserVisual == null || laserLine == null)
            {
                return;
            }

            PlayActiveWeaponShootSound();
            continuousLaserRoutine = StartCoroutine(LaserBeamRoutine(1f));
            shotCooldownTimer = GetEffectiveShotInterval();
            return;
        }

        if (shotCooldownTimer > 0f)
            return;

        if (projectilePooler == null)
        {
            EnsureCombatDependencies(true);
        }

        if (projectilePooler == null)
        {
            return;
        }

        float projectileDamage = GetEffectiveDamage();
        if (nextShotIsCrit)
        {
            projectileDamage *= 3f;
            nextShotIsCrit = false;
        }

        if (!SpawnConfiguredProjectile(aimPivot.position, aimPivot.rotation, projectileDamage))
        {
            return;
        }

        PlayActiveWeaponShootSound();

        if (hasSpreadShot)
        {
            SpawnConfiguredProjectile(aimPivot.position, aimPivot.rotation * Quaternion.Euler(0f, 0f, 15f), projectileDamage);
        }

        shotCooldownTimer = GetEffectiveShotInterval();
    }

    private bool SpawnConfiguredProjectile(Vector3 spawnPosition, Quaternion spawnRotation, float projectileDamage)
    {
        GameObject projectileObject = projectilePooler.GetProjectile(spawnPosition, spawnRotation);
        if (projectileObject == null)
        {
            return false;
        }

        projectileObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (projectileObject.TryGetComponent(out Rigidbody2D projectileRigidbody))
        {
            projectileRigidbody.linearVelocity = Vector2.zero;
            projectileRigidbody.angularVelocity = 0f;
            projectileRigidbody.position = spawnPosition;
            projectileRigidbody.rotation = spawnRotation.eulerAngles.z;
        }

        if (!projectileObject.TryGetComponent(out Projectile projectile))
        {
            projectilePooler.ReturnProjectile(projectileObject);
            return false;
        }

        projectile.Setup(GetEffectiveProjectileSpeed(), projectileDamage);
        projectile.isExplosive = activeWeapon.weaponType == WeaponType.Explosive;
        projectile.isCryo = hasCryoAmmo;

        projectileObject.transform.localScale = activeWeapon.weaponType == WeaponType.Explosive
            ? new Vector3(0.025f, 0.025f, 1f)
            : new Vector3(0.6f, 0.6f, 1f);

        if (activeWeapon.weaponType == WeaponType.Explosive
            && projectile.impactFrames != null
            && projectile.impactFrames.Length > 0
            && projectileObject.TryGetComponent(out SpriteRenderer projectileSpriteRenderer))
        {
            projectileSpriteRenderer.sprite = projectile.impactFrames[0];
        }

        return true;
    }

    private IEnumerator LaserBeamRoutine(float duration)
    {
        isFiringLaser = true;

        if (continuousLaserVisual == null || laserLine == null || aimPivot == null || activeWeapon == null)
        {
            isFiringLaser = false;
            continuousLaserRoutine = null;
            yield break;
        }

        continuousLaserVisual.SetActive(true);
        laserLine.enabled = true;
        EnsureSpreadLaserLine();

        if (spreadLaserLine != null)
        {
            spreadLaserLine.gameObject.SetActive(hasSpreadShot);
            spreadLaserLine.enabled = hasSpreadShot;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (continuousLaserVisual == null || laserLine == null || aimPivot == null || activeWeapon == null)
            {
                break;
            }

            Vector3 origin = aimPivot.position;
            float totalDmg = GetEffectiveDamage();
            float damagePerFrame = totalDmg * Time.deltaTime;
            ProcessLaserBeam(origin, aimPivot.right, laserLine, damagePerFrame);

            if (hasSpreadShot && spreadLaserLine != null)
            {
                Vector2 spreadDirection = Quaternion.Euler(0f, 0f, 15f) * aimPivot.right;
                ProcessLaserBeam(origin, spreadDirection, spreadLaserLine, damagePerFrame);
            }
            else if (spreadLaserLine != null)
            {
                spreadLaserLine.enabled = false;
                spreadLaserLine.gameObject.SetActive(false);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (continuousLaserVisual != null)
        {
            continuousLaserVisual.SetActive(false);
        }

        if (laserLine != null)
        {
            laserLine.enabled = false;
        }

        if (spreadLaserLine != null)
        {
            spreadLaserLine.enabled = false;
            spreadLaserLine.gameObject.SetActive(false);
        }

        isFiringLaser = false;
        continuousLaserRoutine = null;
    }

    private void ProcessLaserBeam(Vector3 origin, Vector2 direction, LineRenderer targetLine, float damagePerFrame)
    {
        if (targetLine == null)
        {
            return;
        }

        Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector3 endPoint = origin + (Vector3)(normalizedDirection * 30f);

        targetLine.gameObject.SetActive(true);
        targetLine.enabled = true;
        targetLine.SetPosition(0, origin);
        targetLine.SetPosition(1, endPoint);

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, normalizedDirection, 30f, LayerMask.GetMask("Enemy"));
        HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

        foreach (RaycastHit2D hit in hits)
        {
            if (hasCryoAmmo)
            {
                hit.collider.GetComponentInParent<IColdAffectable>()?.ApplyCold();
            }

            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && damagedTargets.Add(damageable))
            {
                damageable.TakeDamage(damagePerFrame);
            }
        }
    }

    private void EnsureSpreadLaserLine()
    {
        if (spreadLaserLine != null || laserLine == null)
        {
            return;
        }

        spreadLaserLine = Instantiate(laserLine, laserLine.transform.parent);
        spreadLaserLine.gameObject.name = "LaserBeamVisual_Spread";
        spreadLaserLine.enabled = false;
        spreadLaserLine.gameObject.SetActive(false);
    }

    private void EnsureDashCooldownRing()
    {
        RectTransform ringCanvasTransform = null;
        RectTransform ringTransform = null;

        if (dashCooldownRingImage != null)
        {
            ringTransform = dashCooldownRingImage.rectTransform;
            ringCanvasTransform = dashCooldownRingImage.canvas != null
                ? dashCooldownRingImage.canvas.GetComponent<RectTransform>()
                : dashCooldownRingImage.transform.parent as RectTransform;

            if (ringCanvasTransform == null || ringTransform == null)
            {
                dashCooldownRingImage = null;
            }
        }

        if (dashCooldownRingImage == null)
        {
            Transform existingRing = transform.Find("DashRingCanvas/DashCooldownRing")
                ?? transform.Find("DashCooldownRingCanvas/DashCooldownRing");

            if (existingRing != null)
            {
                dashCooldownRingImage = existingRing.GetComponent<UnityEngine.UI.Image>();
                ringTransform = existingRing as RectTransform;
                ringCanvasTransform = existingRing.parent as RectTransform;
            }
        }

        if (dashCooldownRingImage == null)
        {
            GameObject canvasGO = new GameObject("DashRingCanvas", typeof(RectTransform), typeof(Canvas));
            ringCanvasTransform = canvasGO.GetComponent<RectTransform>();
            ringCanvasTransform.SetParent(transform, false);

            GameObject ringObject = new GameObject("DashCooldownRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            ringTransform = ringObject.GetComponent<RectTransform>();
            ringTransform.SetParent(ringCanvasTransform, false);

            dashCooldownRingImage = ringObject.GetComponent<UnityEngine.UI.Image>();
            dashCooldownRingImage.sprite = dashRingSprite;
            dashCooldownRingImage.type = UnityEngine.UI.Image.Type.Filled;
            dashCooldownRingImage.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            dashCooldownRingImage.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            dashCooldownRingImage.fillClockwise = false;
            dashCooldownRingImage.preserveAspect = true;
            dashCooldownRingImage.raycastTarget = false;
        }

        if (ringCanvasTransform == null || ringTransform == null)
        {
            return;
        }

        ringCanvasTransform.name = "DashRingCanvas";
        ringCanvasTransform.SetParent(transform, false);
        ringCanvasTransform.localPosition = new Vector3(0f, -0.7f, 0f);
        ringCanvasTransform.localRotation = Quaternion.identity;
        ringCanvasTransform.localScale = Vector3.one;
        ringCanvasTransform.sizeDelta = new Vector2(0.9f, 0.9f);

        Canvas ringCanvas = ringCanvasTransform.GetComponent<Canvas>();
        if (ringCanvas == null)
        {
            ringCanvas = ringCanvasTransform.gameObject.AddComponent<Canvas>();
        }

        ringCanvas.renderMode = RenderMode.WorldSpace;
        ringCanvas.overrideSorting = true;
        ringCanvas.sortingOrder = -1;

        ringTransform.SetParent(ringCanvasTransform, false);
        ringTransform.anchorMin = Vector2.zero;
        ringTransform.anchorMax = Vector2.one;
        ringTransform.offsetMin = Vector2.zero;
        ringTransform.offsetMax = Vector2.zero;
        ringTransform.localScale = Vector3.one;
        ringTransform.localRotation = Quaternion.identity;
        ringTransform.anchoredPosition = Vector2.zero;
        ringTransform.sizeDelta = Vector2.zero;

        dashCooldownRingImage.sprite = dashRingSprite;

        dashCooldownRingImage.color = DashRingVisibleColor;
        dashCooldownRingImage.fillAmount = Mathf.Clamp01(dashCooldownRingImage.fillAmount);
    }

    private void UpdateDashCooldownRing(float fillAmount)
    {
        EnsureDashCooldownRing();

        if (dashCooldownRingImage == null)
        {
            return;
        }

        if (isDashing)
        {
            ResetDashCooldownRing();
            return;
        }

        dashCooldownRingImage.fillAmount = fillAmount;

        if (fillAmount < 0.999f)
        {
            if (dashRingFlashRoutine != null)
            {
                StopCoroutine(dashRingFlashRoutine);
                dashRingFlashRoutine = null;
            }

            dashReadyFlashPlayed = false;
            dashCooldownRingImage.color = DashRingVisibleColor;
            return;
        }

        if (!dashReadyFlashPlayed && dashRingFlashRoutine == null)
        {
            dashRingFlashRoutine = StartCoroutine(FlashDashReadyRing());
            return;
        }

        if (dashReadyFlashPlayed && dashRingFlashRoutine == null)
        {
            Color hiddenColor = DashRingVisibleColor;
            hiddenColor.a = 0f;
            dashCooldownRingImage.color = hiddenColor;
        }
    }

    private void ResetDashCooldownRing()
    {
        if (dashRingFlashRoutine != null)
        {
            StopCoroutine(dashRingFlashRoutine);
            dashRingFlashRoutine = null;
        }

        dashReadyFlashPlayed = false;

        if (dashCooldownRingImage == null)
        {
            return;
        }

        dashCooldownRingImage.fillAmount = 0f;
        dashCooldownRingImage.color = DashRingVisibleColor;
    }

    private IEnumerator FlashDashReadyRing()
    {
        if (dashCooldownRingImage == null)
        {
            dashRingFlashRoutine = null;
            yield break;
        }

        dashReadyFlashPlayed = true;
        dashCooldownRingImage.fillAmount = 1f;
        dashCooldownRingImage.color = DashRingVisibleColor;

        yield return new WaitForSeconds(0.12f);

        Color hiddenColor = DashRingVisibleColor;
        hiddenColor.a = 0f;
        dashCooldownRingImage.color = hiddenColor;
        dashRingFlashRoutine = null;
    }

    private System.Collections.IEnumerator HideLaserAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (laserLine != null) laserLine.enabled = false;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, transform.position);
    }

    public void TakeDamage(float damage, Vector2 sourcePosition)
    {
        if (isDashing) return;

        if (damage <= 0f || isInvincible || isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        NotifyHealthChanged();

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        knockbackCounter = knockbackTotalTime;
        StartInvincibilityFrames();
        ApplyKnockback(sourcePosition);
    }

    public void UpdateUI()
    {
        NotifyHealthChanged();
    }

    private void ApplyKnockback(Vector2 sourcePosition)
    {
        if (knockbackForce <= 0f)
        {
            return;
        }

        Vector2 knockbackDirection = (Vector2)transform.position - sourcePosition;
        if (knockbackDirection.sqrMagnitude <= 0.0001f)
        {
            knockbackDirection = Vector2.right;
        }

        knockbackDirection = knockbackDirection.normalized;
        rigidbody2d.linearVelocity = Vector2.zero;
        rigidbody2d.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
    }

    private void StartInvincibilityFrames()
    {
        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
        }

        invincibilityRoutine = StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;
        float effectiveInvincibilityDuration = Mathf.Max(0f, invincibilityDuration + extraIframeTime);
        bool faded = false;

        while (elapsed < effectiveInvincibilityDuration)
        {
            SetBodyAlpha(faded ? 1f : 0.2f);
            faded = !faded;

            float waitDuration = Mathf.Min(InvincibilityFlickerInterval, effectiveInvincibilityDuration - elapsed);
            yield return new WaitForSeconds(waitDuration);
            elapsed += waitDuration;
        }

        SetBodyAlpha(1f);
        isInvincible = false;
        invincibilityRoutine = null;
    }

    private void SetBodyAlpha(float alpha)
    {
        if (bodySpriteRenderer == null)
        {
            return;
        }

        Color color = bodyDefaultColor;
        color.a = alpha;
        bodySpriteRenderer.color = color;
    }

    private void NotifyHealthChanged()
    {
        if (healthBarImage == null)
        {
            Transform healthBarTransform = GameObject.Find("Canvas")?.transform.Find("HealthBar");
            if (healthBarTransform != null)
            {
                healthBarImage = healthBarTransform.GetComponent<UnityEngine.UI.Image>();
            }
        }

        if (healthBarImage != null && healthSprites != null && healthSprites.Length > 0)
        {
            float clampedHealth = Mathf.Clamp(currentHealth, 0f, healthSprites.Length - 1);
            int spriteIndex = Mathf.Clamp(Mathf.CeilToInt((healthSprites.Length - 1) - clampedHealth), 0, healthSprites.Length - 1);
            healthBarImage.sprite = healthSprites[spriteIndex];
            healthBarImage.color = Color.white;
        }

        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHealth = 0f;
        moveInput = Vector2.zero;
        jumpHeld = false;
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        shotCooldownTimer = 0f;
        knockbackCounter = 0f;

        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }

        isInvincible = false;
        SetBodyAlpha(1f);

        if (bodyAnimator != null)
        {
            bodyAnimator.SetFloat(SpeedParameterHash, 0f);

            if (HasAnimatorTrigger(bodyAnimator, DeathParameterHash))
            {
                bodyAnimator.SetTrigger(DeathParameterHash);
            }
        }

        rigidbody2d.linearVelocity = Vector2.zero;
        rigidbody2d.angularVelocity = 0f;

        gameOverManager ??= FindAnyObjectByType<GameOverManager>();

        if (gameOverManager != null)
        {
            gameOverManager.ShowGameOver();
            return;
        }

        Debug.LogWarning("Player died but no GameOverManager was found in the scene.", this);
    }

    private static bool HasAnimatorTrigger(Animator animator, int parameterHash)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == parameterHash)
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckGrounded()
    {
        Bounds bounds = boxCollider.bounds;
        Vector2 boxSize = new Vector2(bounds.size.x * 0.95f, bounds.size.y * 0.98f);

        RaycastHit2D hit = Physics2D.BoxCast(bounds.center, boxSize, 0f, Vector2.down, groundCheckDistance, groundLayers);
        return hit.collider != null;
    }

    private static Vector2 ReadMoveInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        float horizontalInput = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontalInput += 1f;
        }

        return new Vector2(horizontalInput, 0f);
    }

    private static bool IsJumpHeld()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.isPressed;
    }

    private static bool WasJumpPressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
    }

    private static bool WasPausePressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
    }

    private static bool IsPrimaryFireHeld()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.isPressed;
    }

    private static bool WasSecondaryFirePressedThisFrame()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.rightButton.wasPressedThisFrame;
    }

    private void OnDisable()
    {
        if (continuousLaserRoutine != null)
        {
            StopCoroutine(continuousLaserRoutine);
            continuousLaserRoutine = null;
        }

        isFiringLaser = false;

        if (continuousLaserVisual != null)
        {
            continuousLaserVisual.SetActive(false);
        }

        if (laserLine != null)
        {
            laserLine.enabled = false;
        }

        if (invincibilityRoutine != null)
        {
            StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = null;
        }

        isInvincible = false;

        if (bodySpriteRenderer != null)
        {
            bodySpriteRenderer.color = bodyDefaultColor;
        }
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        deceleration = Mathf.Max(0f, deceleration);
        jumpForce = Mathf.Max(0f, jumpForce);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpBuffer = Mathf.Max(0f, jumpBuffer);
        baseGravityScale = Mathf.Max(0.01f, baseGravityScale);
        fallGravityMultiplier = Mathf.Max(1f, fallGravityMultiplier);
        lowJumpGravityMultiplier = Mathf.Max(1f, lowJumpGravityMultiplier);
        groundCheckDistance = Mathf.Max(0.01f, groundCheckDistance);
        maxHealth = Mathf.Max(0.01f, maxHealth);
        invincibilityDuration = Mathf.Max(0f, invincibilityDuration);
        knockbackForce = Mathf.Max(0f, knockbackForce);
        knockbackTotalTime = Mathf.Max(0f, knockbackTotalTime);
        dashSpeed = Mathf.Max(0f, dashSpeed);
        dashDuration = Mathf.Max(0f, dashDuration);
        dashCooldown = Mathf.Max(0f, dashCooldown);
    }
}