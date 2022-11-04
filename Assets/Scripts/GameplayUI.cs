using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameplayUI : MonoBehaviour
{
    [SerializeField]
    private int currentScoreTreshhold = 50;

    private int currentScore = 0;
    
    private Label scoreLabel;
    private ProgressBar scoreProgress;
    private Button nextLevelButton;
    
    [SerializeField]
    private VisualTreeAsset starTemplate;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        scoreLabel = root.Query<Label>("ScoreNumber").First();
        scoreProgress = root.Query<ProgressBar>("ScoreProgress").First();
        nextLevelButton = root.Query<Button>("NextLevelButton").First();
        Button resetStarButton = root.Query<Button>("ResetStarButton").First();

        scoreProgress.highValue = currentScoreTreshhold;

        nextLevelButton.RegisterCallback<ClickEvent>(StartNewLevel);
        resetStarButton.RegisterCallback<ClickEvent>(ResetStarScore);
    }

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        VisualElement star = starTemplate.CloneTree();
        VisualElement holder = root.Query<VisualElement>("StarHolder");

        for (int i = 0; i < PersistentData.Data.starScore; i++)
        {
            starTemplate.CloneTree(holder);
        }
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
        PersistentData.Data.AddStar();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ResetStarScore(ClickEvent evt)
    {
        PersistentData.Data.ResetStar();
    }
}
