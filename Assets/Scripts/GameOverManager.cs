using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void ShowGameOver(int score)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);
        if (finalScoreText != null)
        {
            finalScoreText.text = $"FINAL SCORE: {score}";
        }
        
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}