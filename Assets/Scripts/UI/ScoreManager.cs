using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager _instance;
    
    public static ScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ScoreManager");
                _instance = go.AddComponent<ScoreManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Dictionary<string, int> highScores = new Dictionary<string, int>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllHighScores();
    }

    public void SaveHighScore(string levelName, int score)
    {
        // Only save if it's a new high score
        if (!highScores.ContainsKey(levelName) || score > highScores[levelName])
        {
            highScores[levelName] = score;
            PlayerPrefs.SetInt($"HighScore_{levelName}", score);
            PlayerPrefs.Save();
        }
    }

    public int GetHighScore(string levelName)
    {
        if (highScores.ContainsKey(levelName))
        {
            return highScores[levelName];
        }
        return 0;
    }

    private void LoadAllHighScores()
    {
        // You can define a list of your level names here if you know them all
        // Or just load what's available in PlayerPrefs
        string[] levels = GetAllLevelNames();
        
        foreach (string level in levels)
        {
            int score = PlayerPrefs.GetInt($"HighScore_{level}", 0);
            highScores[level] = score;
        }
    }

    private string[] GetAllLevelNames()
    {
        // Replace this with your actual level/stage names
        return new string[] { "Stage 1", "Stage 2", "Stage 3", "MainMenu" };
    }
}