using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameplayUI : MonoBehaviour
{
    public int currentScore = 0;
    public int currentScoreTreshhold = 100;
    public Label scoreLabel;
    public ProgressBar scoreProgress;
    public bool nextLevelButtonVisible = false;
    public Button nextLevelButton;

    public VisualTreeAsset starTemplate;
    private int starScore = 3;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        scoreLabel = root.Query<Label>("ScoreNumber").First();
        scoreProgress = root.Query<ProgressBar>("ScoreProgress").First();
        nextLevelButton = root.Query<Button>("NextLevelButton").First();

        VisualElement star = starTemplate.CloneTree();
        VisualElement holder = root.Query<VisualElement>("StarHolder");

        for (int i = 0; i < starScore; i++)
        {
            starTemplate.CloneTree(holder);
        }
        

        //scoreProgress.highValue = currentScoreTreshhold;

        nextLevelButton.RegisterCallback<ClickEvent>(StartNewLevel);
    }

    void Update()
    {
        string stringifiedScore = currentScore.ToString();
        while (stringifiedScore.Length < 3)
        {
            stringifiedScore = $"0{stringifiedScore}";   
        }
        scoreLabel.text = stringifiedScore;

        scoreProgress.value = currentScore;

        if (currentScore > currentScoreTreshhold)
        {
            nextLevelButton.style.display = DisplayStyle.Flex;
            nextLevelButtonVisible = true;
        }

    }

    public void AddScore(int score = 2)
    {
        currentScore += score;
    }

    public void ReduceScore(int score = 2)
    {
        if (currentScore - 1 <= 0) currentScore = 0;
        else currentScore -= score;
    }

    public void StartNewLevel(ClickEvent evt)
    {
        Debug.Log("SHOULD START SOMETHING");
        starScore += 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
