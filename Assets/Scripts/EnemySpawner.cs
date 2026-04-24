using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Required for TextMeshPro

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs & Points")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints; 

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public GameObject gameOverPanel; 

    [Header("Session Settings")]
    public float sessionDuration = 120f; 
    private float _timer;
    private bool _isSessionActive = true;
    public int enemiesAtOnce = 6;
    public GameOverManager gameOverUI;

    [Header("Score Tracking")]
    public int totalScore = 0;
    
    private List<GameObject> _activeEnemies = new List<GameObject>();

    void Start()
    {
        _timer = sessionDuration;
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
        UpdateUI();
        FillEnemies();
    }

    void Update()
    {
        if (!_isSessionActive) return;

        _timer -= Time.deltaTime;
        UpdateUI();

        if (_timer <= 0)
        {
            EndSession();
            return;
        }

        _activeEnemies.RemoveAll(item => item == null); 

        if (_activeEnemies.Count < enemiesAtOnce)
        {
            FillEnemies();
        }
    }

    void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"Score: {totalScore}";
        
        if (timerText != null)
        {
            float seconds = Mathf.Max(0, _timer);
            timerText.text = string.Format("{0:0}:{1:00}", Mathf.FloorToInt(seconds / 60), Mathf.FloorToInt(seconds % 60));
        }
    }

    void FillEnemies()
    {
        if (!_isSessionActive) return;

        int amountToSpawn = enemiesAtOnce - _activeEnemies.Count;

        for (int i = 0; i < amountToSpawn; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
            _activeEnemies.Add(newEnemy);
        }
    }

    public void AddScore(bool killedInAir)
    {
        int points = killedInAir ? 500 : 200;
        totalScore += points;
        UpdateUI(); 
    }

    void EndSession()
    {
        _isSessionActive = false;
        if(gameOverPanel != null) gameOverPanel.SetActive(true);
        
        foreach(var enemy in _activeEnemies)
        {
            if(enemy != null) Destroy(enemy);
        }
        _activeEnemies.Clear();
        
        if(gameOverUI != null) gameOverUI.ShowGameOver(totalScore);
    }
}