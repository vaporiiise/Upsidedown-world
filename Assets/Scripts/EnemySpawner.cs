using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs & Points")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints; 

    [Header("Session Settings")]
    public float sessionDuration = 120f; 
    private float _timer;
    private bool _isSessionActive = true;
    public int enemiesAtOnce = 6;

    [Header("Score Tracking")]
    public int totalScore = 0;
    
    private List<GameObject> _activeEnemies = new List<GameObject>();

    void Start()
    {
        _timer = sessionDuration;
        // Initial spawn
        FillEnemies();
    }

    void Update()
    {
        if (!_isSessionActive) return;

        // 1. Update Timer
        _timer -= Time.deltaTime;
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
        Debug.Log($"<color=yellow>Points Added: {points}. Total Score: {totalScore}</color>");
    }

    void EndSession()
    {
        _isSessionActive = false;
        Debug.Log($"<color=green>TIME UP! Final Score: {totalScore}</color>");
        
        foreach(var enemy in _activeEnemies)
        {
            if(enemy != null) Destroy(enemy);
        }
        _activeEnemies.Clear();
    }
}