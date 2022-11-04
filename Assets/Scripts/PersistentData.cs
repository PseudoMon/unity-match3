using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PersistentData : MonoBehaviour
{
    public static PersistentData Data;
    public int starScore = 0;
    public int scoreReducer = 2;
    public int scoreAdder = 2;
    public int scoreTreshhold = 50;
    
    private void Awake()
    {
        if (Data != null)
        {
            Destroy(gameObject);
            return;
        }

        LoadData();
        Data = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddStar()
    {
        starScore += 1;
        scoreReducer += 1;
        if (scoreReducer > scoreAdder * 3)
        {
            scoreAdder += 1;
            scoreTreshhold += 20;
        }

        SaveData();
    }

    public void ResetStar()
    {
        starScore = 0;
        scoreReducer = 2;
        scoreAdder = 2;
        scoreTreshhold = 50;
        SaveData();
    }

    [System.Serializable]
    class SerializedSaveData
    {
        public int starScore;
        public int scoreReducer;
        public int scoreAdder;
        public int scoreTreshhold;
    }

    public void SaveData()
    {
        SerializedSaveData serializedData = new SerializedSaveData();
        serializedData.starScore = starScore;
        serializedData.scoreReducer = scoreReducer;
        serializedData.scoreAdder = scoreAdder;
        serializedData.scoreTreshhold = scoreTreshhold;

        string json = JsonUtility.ToJson(serializedData);
        string path = $"{Application.persistentDataPath}/savefile.json";
        File.WriteAllText(path, json);
        Debug.Log($"DATA SAVED AT {path}");
    }

    public void LoadData()
    {
        string path = $"{Application.persistentDataPath}/savefile.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SerializedSaveData data = 
                JsonUtility.FromJson<SerializedSaveData>(json);

            starScore = data.starScore;
            scoreReducer = data.scoreReducer;
            scoreAdder = data.scoreAdder;
            scoreTreshhold = data.scoreTreshhold;
            Debug.Log("DATA LOADED");
        }
    }
}
