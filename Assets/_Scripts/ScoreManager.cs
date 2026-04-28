using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] public TextMeshProUGUI scoreText;

    private int currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;

        if (scoreText != null)
        {
            scoreText.text = "PURIFIED: " + currentScore;
        }
    }

    public int CurrentScore => currentScore;

    public void ResetScore()
    {
        currentScore = 0;

        if (scoreText != null)
        {
            scoreText.text = "PURIFIED: 0";
        }
    }
}
