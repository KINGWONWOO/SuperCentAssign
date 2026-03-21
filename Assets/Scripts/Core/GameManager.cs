using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameSettings settings;
    public GameSettings Settings => settings;

    public bool IsPlaying { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        IsPlaying = true;
    }

    public void PauseGame()
    {
        IsPlaying = false;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        IsPlaying = true;
        Time.timeScale = 1f;
    }
}
