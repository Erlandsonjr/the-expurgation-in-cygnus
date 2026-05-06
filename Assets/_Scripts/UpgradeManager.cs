using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages inter-wave upgrade panels:
///   Weapon Selection (Wave 3 only):  shows UpgradePanel with HeavyRifle / ExplosiveBow / LaserPistol.
///   General Upgrades  (all other waves): shows GeneralUpgradePanel with stat buffs.
/// </summary>
[DisallowMultipleComponent]
public sealed class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    // ── Weapon Panel ──────────────────────────────────────────────────────────
    [Header("Weapon Panel (Wave 3)")]
    [SerializeField] public GameObject upgradePanel;
    public Button[] choiceButtons;

    [Header("Weapon Pool (Wave-3 choices — no FirstWeapon)")]
    public List<WeaponData> availableWeapons = new List<WeaponData>();

    // ── General Upgrade Panel ────────────────────────────────────────────────
    [Header("General Upgrade Panel (non-Wave-3)")]
    [SerializeField] public GameObject generalUpgradePanel;
    public Button[] generalChoiceButtons;
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

        if (upgradePanel        != null) upgradePanel.SetActive(false);
        if (generalUpgradePanel != null) generalUpgradePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Weapon Selection (Wave 3) ─────────────────────────────────────────────
    public void ShowWeaponUpgrades()
    {
        if (upgradePanel != null) upgradePanel.SetActive(true);

        if (choiceButtons == null || choiceButtons.Length == 0 || availableWeapons == null || availableWeapons.Count == 0)
            return;

        var pool    = new List<WeaponData>(availableWeapons);
        int choices = System.Math.Min(choiceButtons.Length, pool.Count);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] == null) continue;

            if (i < choices)
            {
                int idx = Random.Range(0, pool.Count);
                WeaponData selected = pool[idx];
                pool.RemoveAt(idx);

                choiceButtons[i].gameObject.SetActive(true);

                var tmp = choiceButtons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.text = selected.WeaponName;

                foreach (var img in choiceButtons[i].GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject != choiceButtons[i].gameObject && img.gameObject.name == "WeaponIcon")
                    {
                        img.sprite = selected.WeaponSprite;
                        img.preserveAspect = true;
                        break;
                    }
                }

                choiceButtons[i].onClick.RemoveAllListeners();
                WeaponData captured = selected;
                choiceButtons[i].onClick.AddListener(() => SelectWeapon(captured));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void SelectWeapon(WeaponData chosenWeapon)
    {
        if (chosenWeapon != null && playerController != null)
        {
            playerController.EquipWeapon(chosenWeapon);
            Debug.Log($"[UpgradeManager] Weapon equipped: {chosenWeapon.WeaponName}");
        }
        Time.timeScale = 1f;
        HideWeaponPanel();
    }

    private void HideWeaponPanel()
    {
        if (upgradePanel != null) upgradePanel.SetActive(false);
        AdvanceWave();
    }

    // ── General Upgrades (non-Wave-3) ─────────────────────────────────────────
    public void ShowGeneralUpgrades()
    {
        if (generalUpgradePanel != null) generalUpgradePanel.SetActive(true);

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

                var tmp = generalChoiceButtons[i].GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.text = selectedCard.cardName + "\n<size=70%>" + selectedCard.description + "</size>";
                }

                foreach (var img in generalChoiceButtons[i].GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject == generalChoiceButtons[i].gameObject)
                    {
                        continue;
                    }

                    img.sprite = selectedCard.cardIcon;
                    img.enabled = selectedCard.cardIcon != null;
                    img.preserveAspect = true;
                    break;
                }

                generalChoiceButtons[i].onClick.RemoveAllListeners();
                CardData captured = selectedCard;
                generalChoiceButtons[i].onClick.AddListener(() => SelectCard(captured));
            }
            else
            {
                generalChoiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void SelectCard(CardData chosenCard)
    {
        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }

        if (chosenCard != null)
        {
            if (chosenCard.effectType == CardEffectType.Luck)
            {
                luckBonus += chosenCard.effectValue;
            }

            if (playerController != null)
            {
                playerController.ApplyCard(chosenCard);
            }

            Debug.Log($"[UpgradeManager] Card selected: {chosenCard.cardName}");
        }

        Time.timeScale = 1f;
        HideGeneralPanel();
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

    // ── Shared ────────────────────────────────────────────────────────────────
    private void AdvanceWave()
    {
        if (waveManager != null)
            waveManager.AdvanceToNextWave();
        else
            Time.timeScale = 1f;
    }

    /// <summary>Legacy entry point — routes to weapon upgrades.</summary>
    public void ShowPanel() => ShowWeaponUpgrades();

    /// <summary>Legacy hide — hides both panels and advances.</summary>
    public void HidePanel()
    {
        if (upgradePanel        != null) upgradePanel.SetActive(false);
        if (generalUpgradePanel != null) generalUpgradePanel.SetActive(false);
        AdvanceWave();
    }
}

