using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PersistentData : MonoBehaviour
{
    public static PersistentData Data;
    public int starScore = 0;
    
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
        SaveData();
    }

    public void ResetStar()
    {
        starScore = 0;
        SaveData();
    }

    [System.Serializable]
    class SerializedSaveData
    {
        public int starScore;
    }

    public void SaveData()
    {
        SerializedSaveData serializedData = new SerializedSaveData();
        serializedData.starScore = starScore;

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
            Debug.Log("DATA LOADED");
        }
    }
}
