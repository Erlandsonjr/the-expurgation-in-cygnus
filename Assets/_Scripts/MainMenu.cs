using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class MainMenu : MonoBehaviour
{
    private const string GameplaySceneName = "SampleScene";

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameplaySceneName);
    }
}