using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameplayUI : MonoBehaviour
{
    private int currentScoreTreshhold;

    private int currentScore = 0;
    
    private Label scoreLabel;
    private ProgressBar scoreProgress;
    private Button nextLevelButton;
    private VisualElement starHolder;
    
    [SerializeField]
    private VisualTreeAsset starTemplate;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        scoreLabel = root.Query<Label>("ScoreNumber").First();
        scoreProgress = root.Query<ProgressBar>("ScoreProgress").First();
        nextLevelButton = root.Query<Button>("NextLevelButton").First();
        Button resetStarButton = root.Query<Button>("ResetStarButton").First();
        Button quitButton = root.Query<Button>("QuitButton").First();

        nextLevelButton.RegisterCallback<ClickEvent>(StartNewLevel);
        resetStarButton.RegisterCallback<ClickEvent>(ResetStarScore);
        quitButton.RegisterCallback<ClickEvent>(QuitGame);
    }

    void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        VisualElement star = starTemplate.CloneTree();
        starHolder = root.Query<VisualElement>("StarHolder");

        for (int i = 0; i < PersistentData.Data.starScore; i++)
        {
            starTemplate.CloneTree(starHolder);
        }

        currentScoreTreshhold = PersistentData.Data.scoreTreshhold;
        scoreProgress.highValue = currentScoreTreshhold;
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

    public void AddScore()
    {
        int scoreAdder = PersistentData.Data.scoreAdder;
        currentScore += scoreAdder;
    }

    public void ReduceScore()
    {
        int scoreReducer = PersistentData.Data.scoreReducer;
        // Can't go lower than 0
        if (currentScore - scoreReducer <= 0) currentScore = 0;
        else currentScore -= scoreReducer;
    }

    public void StartNewLevel(ClickEvent evt)
    {
        PersistentData.Data.AddStar();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ResetStarScore(ClickEvent evt)
    {
        PersistentData.Data.ResetStar();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame(ClickEvent evt)
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }
}
