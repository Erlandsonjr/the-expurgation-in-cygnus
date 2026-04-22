using UnityEngine;
using UnityEngine.UI;

public sealed class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private PlayerController playerController;

    private void Awake()
    {
        healthSlider ??= GetComponent<Slider>();
    }

    private void OnEnable()
    {
        ResolvePlayerController();
        SubscribeToPlayer();
        RefreshHealthBar();
    }

    private void Start()
    {
        RefreshHealthBar();
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.HealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        if (playerController == null)
        {
            ResolvePlayerController();
            SubscribeToPlayer();
            RefreshHealthBar();
        }
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (healthSlider == null)
        {
            return;
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    private void RefreshHealthBar()
    {
        if (playerController == null || healthSlider == null)
        {
            return;
        }

        healthSlider.maxValue = playerController.MaxHealth;
        healthSlider.value = playerController.CurrentHealth;
    }

    private void ResolvePlayerController()
    {
        if (playerController != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerController = playerObject.GetComponent<PlayerController>();
        }
    }

    private void SubscribeToPlayer()
    {
        if (playerController == null)
        {
            return;
        }

        playerController.HealthChanged -= HandleHealthChanged;
        playerController.HealthChanged += HandleHealthChanged;
    }
}