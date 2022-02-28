using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Analytics;

public class VictorySceneManager : MonoBehaviour
{
    // Start is called before the first frame update
    public Text VictoryText;
    public Button nextLevelButton;
    private LevelManager levelManager;

    void Start()
    {
        VictoryText.text += PlayerData.NumberOfSeconds;
        AnalyticsSender.SendLevelFinishedEvent(PlayerData.CurrentLevel, PlayerData.NumberOfSeconds);
        levelManager = GameObject.Find("GameManager").GetComponent<LevelManager>();

        if (PlayerData.CurrentLevel == levelManager.levels.Length - 1)
        {
            nextLevelButton.gameObject.SetActive(false);
        }
    }

    public void StartNextLevel()
    {
        PlayerData.CurrentLevel += 1;
        levelManager.LoadLevel();
    }
}
