using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the inter-wave upgrade card flow through a single randomized pool.
/// Weapon pickups are represented as epic card rewards and can appear on any wave.
/// </summary>
[DisallowMultipleComponent]
public sealed class UpgradeManager : MonoBehaviour
{
    private static readonly Vector2 CardSize = new Vector2(420f, 620f);
    private static readonly Vector2 CardIconSize = new Vector2(96f, 96f);
    private const float CardTextPadding = 45f;

    public static UpgradeManager Instance { get; private set; }

    // ── Legacy Panel Ref ──────────────────────────────────────────────────────
    [Header("Legacy Weapon Panel")]
    [SerializeField] public GameObject upgradePanel;

    // ── General Upgrade Panel ────────────────────────────────────────────────
    [Header("General Upgrade Panel")]
    [SerializeField] public GameObject generalUpgradePanel;
    public Button[] generalChoiceButtons;
    [SerializeField] private GameObject droneCompanionPrefab;
    [SerializeField] private Sprite upgradeCardSprite;
    public List<CardData> cardPool = new List<CardData>();
    public float luckBonus = 0f;

    // ── Internal refs ────────────────────────────────────────────────────────
    private WaveManager waveManager;
    private PlayerController playerController;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        waveManager      = FindAnyObjectByType<WaveManager>();
        playerController = FindAnyObjectByType<PlayerController>();

        SetUpgradeHeaderVisible(false);

        if (upgradePanel        != null) upgradePanel.SetActive(false);
        if (generalUpgradePanel != null) generalUpgradePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── General Upgrades ──────────────────────────────────────────────────────
    public void ShowGeneralUpgrades()
    {
        if (generalUpgradePanel != null) generalUpgradePanel.SetActive(true);
        SetUpgradeHeaderVisible(false);

        if (generalChoiceButtons == null || generalChoiceButtons.Length == 0) return;

        List<CardData> selectedCards = GetRandomUniqueCards(3);
        int count = System.Math.Min(generalChoiceButtons.Length, selectedCards.Count);

        for (int i = 0; i < generalChoiceButtons.Length; i++)
        {
            if (generalChoiceButtons[i] == null) continue;

            if (i < count)
            {
                CardData selectedCard = selectedCards[i];

                generalChoiceButtons[i].gameObject.SetActive(true);
                ConfigureCardButton(
                    generalChoiceButtons[i],
                    GetDisplayCardTitle(selectedCard),
                    BuildCardDescription(selectedCard),
                    GetDisplayCardIcon(selectedCard));

                generalChoiceButtons[i].onClick.RemoveAllListeners();
                CardData captured = selectedCard;
                generalChoiceButtons[i].onClick.AddListener(() => SelectCard(captured));
            }
            else
            {
                generalChoiceButtons[i].gameObject.SetActive(false);
            }
        }

        ConfigureCardContainer(generalChoiceButtons, count);
    }

    private void SelectCard(CardData chosenCard)
    {
        if (chosenCard != null)
        {
            ApplyCardEffect(chosenCard);

            if (ShouldRemoveCardFromPool(chosenCard))
            {
                RemoveCardFromPool(chosenCard);
            }

            Debug.Log($"[UpgradeManager] Card selected: {GetDisplayCardTitle(chosenCard)}");
        }

        Time.timeScale = 1f;
        HideGeneralPanel();
    }

    private void SpawnCompanionDrone()
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }

        if (playerController == null)
        {
            Debug.LogWarning("[UpgradeManager] Cannot spawn Companion Drone without a PlayerController.");
            return;
        }

        DroneAI existingDrone = FindAnyObjectByType<DroneAI>();
        if (existingDrone != null)
        {
            existingDrone.playerTransform = playerController.transform;
            return;
        }

        if (droneCompanionPrefab == null)
        {
            Debug.LogWarning("[UpgradeManager] Drone Companion prefab is not assigned. Assign Assets/_Prefabs/DroneCompanion.prefab in the UpgradeManager inspector.");
            return;
        }

        GameObject droneInstance = Instantiate(
            droneCompanionPrefab,
            playerController.transform.position,
            Quaternion.identity);
        if (droneInstance.TryGetComponent(out DroneAI droneAI))
        {
            droneAI.playerTransform = playerController.transform;
        }
    }

    private void ApplyCardEffect(CardData chosenCard)
    {
        if (chosenCard == null)
        {
            return;
        }

        if (chosenCard.effectType == CardEffectType.Luck)
        {
            luckBonus += chosenCard.effectValue;
        }

        if (IsWeaponCard(chosenCard))
        {
            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (playerController != null && chosenCard.weaponReward != null)
            {
                playerController.EquipWeapon(chosenCard.weaponReward);
                Debug.Log($"[UpgradeManager] Weapon equipped: {GetDisplayWeaponTitle(chosenCard.weaponReward)}");
            }

            return;
        }

        if (chosenCard.effectType == CardEffectType.CompanionDrone)
        {
            SpawnCompanionDrone();
            return;
        }

        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }

        if (playerController == null)
        {
            return;
        }

        playerController.ApplyCard(chosenCard);
    }

    private bool ShouldRemoveCardFromPool(CardData chosenCard)
    {
        return chosenCard != null
            && (chosenCard.isUnique
                || chosenCard.rarity == CardRarity.Epic
                || IsNonStackingCard(chosenCard)
                || IsWeaponCard(chosenCard));
    }

    private void RemoveCardFromPool(CardData chosenCard)
    {
        if (cardPool == null || chosenCard == null)
        {
            return;
        }

        for (int i = cardPool.Count - 1; i >= 0; i--)
        {
            if (cardPool[i] == chosenCard)
            {
                cardPool.RemoveAt(i);
            }
        }
    }

    private static bool IsNonStackingCard(CardData card)
    {
        if (card == null)
        {
            return false;
        }

        switch (card.effectType)
        {
            case CardEffectType.CritNextShot:
            case CardEffectType.CryoAmmo:
            case CardEffectType.BerserkerRage:
            case CardEffectType.RadiationAura:
            case CardEffectType.SpreadShot:
            case CardEffectType.CompanionDrone:
            case CardEffectType.EquipWeapon:
                return true;
            default:
                return false;
        }
    }

    private List<CardData> GetRandomUniqueCards(int count)
    {
        HashSet<CardData> uniqueCards = new HashSet<CardData>();
        if (cardPool != null)
        {
            foreach (CardData card in cardPool)
            {
                if (card != null)
                {
                    uniqueCards.Add(card);
                }
            }
        }

        List<CardData> availableCards = new List<CardData>(uniqueCards);
        List<CardData> results = new List<CardData>();
        int targetCount = System.Math.Min(count, availableCards.Count);

        while (results.Count < targetCount && availableCards.Count > 0)
        {
            CardData selected = DrawWeightedCard(availableCards);
            if (selected == null)
            {
                break;
            }

            results.Add(selected);
            availableCards.Remove(selected);
        }

        return results;
    }

    private CardData DrawWeightedCard(List<CardData> availableCards)
    {
        if (availableCards.Count == 0)
        {
            return null;
        }

        CardRarity rolledRarity = RollRarity(availableCards);
        List<CardData> candidates = new List<CardData>();
        foreach (CardData card in availableCards)
        {
            if (card.rarity == rolledRarity)
            {
                candidates.Add(card);
            }
        }

        List<CardData> source = candidates.Count > 0 ? candidates : availableCards;
        return source[Random.Range(0, source.Count)];
    }

    private static CardRarity RollRarity(List<CardData> availableCards)
    {
        float commonWeight = HasRarity(availableCards, CardRarity.Common) ? 60f : 0f;
        float uncommonWeight = HasRarity(availableCards, CardRarity.Uncommon) ? 25f : 0f;
        float rareWeight = HasRarity(availableCards, CardRarity.Rare) ? 10f : 0f;
        float epicWeight = HasRarity(availableCards, CardRarity.Epic) ? 5f : 0f;
        float totalWeight = commonWeight + uncommonWeight + rareWeight + epicWeight;

        if (totalWeight <= 0f)
        {
            return CardRarity.Common;
        }

        float roll = Random.value * totalWeight;
        if (roll < commonWeight)
        {
            return CardRarity.Common;
        }

        roll -= commonWeight;
        if (roll < uncommonWeight)
        {
            return CardRarity.Uncommon;
        }

        roll -= uncommonWeight;
        if (roll < rareWeight)
        {
            return CardRarity.Rare;
        }

        return CardRarity.Epic;
    }

    private static float GetRarityWeight(CardRarity rarity)
    {
        switch (rarity)
        {
            case CardRarity.Common:
                return 60f;
            case CardRarity.Uncommon:
                return 25f;
            case CardRarity.Rare:
                return 10f;
            case CardRarity.Epic:
                return 5f;
            default:
                return 0f;
        }
    }

    private static bool HasRarity(List<CardData> cards, CardRarity rarity)
    {
        foreach (CardData card in cards)
        {
            if (card.rarity == rarity)
            {
                return true;
            }
        }

        return false;
    }

    private void HideGeneralPanel()
    {
        if (generalUpgradePanel != null) generalUpgradePanel.SetActive(false);
        AdvanceWave();
    }

    private void ConfigureCardButton(Button button, string title, string description, Sprite iconSprite)
    {
        if (button == null)
        {
            return;
        }

        RectTransform buttonRect = button.transform as RectTransform;
        if (buttonRect != null)
        {
            buttonRect.sizeDelta = CardSize;
        }

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = CardSize.x;
        layoutElement.minHeight = CardSize.y;
        layoutElement.preferredWidth = CardSize.x;
        layoutElement.preferredHeight = CardSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        Image frameImage = button.GetComponent<Image>();
        if (frameImage != null)
        {
            if (upgradeCardSprite != null)
            {
                frameImage.sprite = upgradeCardSprite;
                frameImage.color = Color.white;
            }

            frameImage.type = Image.Type.Simple;
            frameImage.preserveAspect = true;
            frameImage.enabled = true;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        button.colors = colors;

        Image iconImage = FindCardIcon(button.transform);
        if (iconImage == null && iconSprite != null)
        {
            iconImage = CreateCardIcon(button.transform);
        }

        bool hasIcon = iconImage != null && iconSprite != null;
        if (iconImage != null)
        {
            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.74f);
            iconRect.anchorMax = new Vector2(0.5f, 0.74f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = CardIconSize;
            iconImage.sprite = iconSprite;
            iconImage.enabled = iconSprite != null;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        TextMeshProUGUI titleText = GetOrCreateTitleText(button.transform);
        TextMeshProUGUI descriptionText = GetOrCreateDescriptionText(button.transform, titleText);

        ConfigureCardText(titleText, title, true, hasIcon);
        ConfigureCardText(descriptionText, description, false, hasIcon);
    }

    private void SetUpgradeHeaderVisible(bool isVisible)
    {
        SetPanelHeaderVisible(upgradePanel, isVisible);
        SetPanelHeaderVisible(generalUpgradePanel, isVisible);
    }

    private static void ConfigureCardContainer(Button[] buttons, int activeCount)
    {
        if (buttons == null || activeCount <= 0)
        {
            return;
        }

        Button firstButton = null;
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                firstButton = button;
                break;
            }
        }

        if (firstButton == null)
        {
            return;
        }

        RectTransform containerRect = firstButton.transform.parent as RectTransform;
        if (containerRect == null || !containerRect.TryGetComponent(out HorizontalLayoutGroup layoutGroup))
        {
            return;
        }

        layoutGroup.spacing = activeCount > 1 ? 80f : 0f;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = true;
    }

    private static void SetPanelHeaderVisible(GameObject panel, bool isVisible)
    {
        if (panel == null)
        {
            return;
        }

        foreach (TextMeshProUGUI text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text == null)
            {
                continue;
            }

            if (text.gameObject.name == "UpgradeTitle" || text.text == "CHOOSE AN UPGRADE")
            {
                text.gameObject.SetActive(isVisible);
            }
        }
    }

    private static TextMeshProUGUI GetOrCreateTitleText(Transform parent)
    {
        TextMeshProUGUI titleText = FindCardText(parent, "Title");
        if (titleText != null)
        {
            return titleText;
        }

        titleText = parent.GetComponentInChildren<TextMeshProUGUI>(true);
        if (titleText != null)
        {
            titleText.gameObject.name = "Title";
            return titleText;
        }

        return CreateCardText(parent, "Title", null);
    }

    private static TextMeshProUGUI GetOrCreateDescriptionText(Transform parent, TextMeshProUGUI template)
    {
        TextMeshProUGUI descriptionText = FindCardText(parent, "Description");
        return descriptionText ?? CreateCardText(parent, "Description", template);
    }

    private static TextMeshProUGUI FindCardText(Transform parent, string objectName)
    {
        foreach (TextMeshProUGUI tmp in parent.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.gameObject.name == objectName)
            {
                return tmp;
            }
        }

        return null;
    }

    private static TextMeshProUGUI CreateCardText(Transform parent, string objectName, TextMeshProUGUI template)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (template != null)
        {
            text.font = template.font;
            text.fontStyle = template.fontStyle;
            text.fontSize = template.fontSize;
            text.color = template.color;
            text.alignment = template.alignment;
        }

        return text;
    }

    private static Image FindCardIcon(Transform parent)
    {
        foreach (Image image in parent.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject == parent.gameObject)
            {
                continue;
            }

            if (image.gameObject.name == "WeaponIcon" || image.gameObject.name == "CardIcon")
            {
                return image;
            }
        }

        return null;
    }

    private static Image CreateCardIcon(Transform parent)
    {
        GameObject iconObject = new GameObject("CardIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.layer = parent.gameObject.layer;
        RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return iconObject.GetComponent<Image>();
    }

    private static void ConfigureCardText(TextMeshProUGUI text, string value, bool isTitle, bool hasIcon)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rectTransform = text.rectTransform;
        if (isTitle)
        {
            rectTransform.anchorMin = new Vector2(0f, hasIcon ? 0.5f : 0.62f);
            rectTransform.anchorMax = new Vector2(1f, hasIcon ? 0.68f : 0.84f);
            text.fontSize = 32f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            rectTransform.anchorMin = new Vector2(0f, 0.12f);
            rectTransform.anchorMax = new Vector2(1f, hasIcon ? 0.46f : 0.58f);
            text.fontSize = 22f;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.Top;
        }

        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = new Vector2(CardTextPadding, CardTextPadding);
        rectTransform.offsetMax = new Vector2(-CardTextPadding, -CardTextPadding);
        text.text = value ?? string.Empty;
        text.enableAutoSizing = true;
        text.fontSizeMax = isTitle ? 32f : 22f;
        text.fontSizeMin = isTitle ? 18f : 14f;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.color = Color.white;
    }

    private static string GetDisplayCardTitle(CardData card)
    {
        if (card == null)
        {
            return string.Empty;
        }

        return IsWeaponCard(card) ? GetDisplayWeaponTitle(card.weaponReward) : card.cardName;
    }

    private static string BuildCardDescription(CardData card)
    {
        if (card == null)
        {
            return string.Empty;
        }

        return IsWeaponCard(card) ? BuildWeaponDescription(card.weaponReward) : card.description;
    }

    private static Sprite GetDisplayCardIcon(CardData card)
    {
        if (card == null)
        {
            return null;
        }

        if (IsWeaponCard(card) && card.weaponReward != null && card.weaponReward.WeaponSprite != null)
        {
            return card.weaponReward.WeaponSprite;
        }

        return card.cardIcon;
    }

    private static bool IsWeaponCard(CardData card)
    {
        return card != null && (card.effectType == CardEffectType.EquipWeapon || card.weaponReward != null);
    }

    private static string BuildWeaponDescription(WeaponData weapon)
    {
        if (weapon == null)
        {
            return string.Empty;
        }

        string damageText = IsContinuousLaserDisplay(weapon) ? "2" : weapon.Damage.ToString("0.#");
        return $"{weapon.weaponType} weapon\nDamage: {damageText}\nFire Rate {weapon.FireRate:0.#}\nSpeed {weapon.ProjectileSpeed:0.#}";
    }

    private static string GetDisplayWeaponTitle(WeaponData weapon)
    {
        if (weapon == null)
        {
            return string.Empty;
        }

        if (IsNamedWeapon(weapon, "NormalWeapon") || IsNamedWeapon(weapon, "HeavyRifle"))
        {
            return "Heavy Rifle";
        }

        if (IsNamedWeapon(weapon, "ExplosiveBow"))
        {
            return "Explosive Bow";
        }

        if (IsNamedWeapon(weapon, "LaserPistol"))
        {
            return "Laser Pistol";
        }

        return weapon.WeaponName;
    }

    private static bool IsContinuousLaserDisplay(WeaponData weapon)
    {
        return IsNamedWeapon(weapon, "ContinuousLaser") || weapon.weaponType == WeaponType.ContinuousLaser;
    }

    private static bool IsNamedWeapon(WeaponData weapon, string name)
    {
        if (weapon == null || string.IsNullOrEmpty(name))
        {
            return false;
        }

        return string.Equals(weapon.WeaponName, name, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(weapon.name, name, System.StringComparison.OrdinalIgnoreCase);
    }

    // ── Shared ────────────────────────────────────────────────────────────────
    private void AdvanceWave()
    {
        if (waveManager != null)
            waveManager.AdvanceToNextWave();
        else
            Time.timeScale = 1f;
    }

    /// <summary>Legacy entry point — routes to the shared randomized card flow.</summary>
    public void ShowPanel() => ShowGeneralUpgrades();

    /// <summary>Legacy hide — hides both panels and advances.</summary>
    public void HidePanel()
    {
        if (upgradePanel        != null) upgradePanel.SetActive(false);
        if (generalUpgradePanel != null) generalUpgradePanel.SetActive(false);
        AdvanceWave();
    }
}

