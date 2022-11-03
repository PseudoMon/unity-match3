using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentData : MonoBehaviour
{
    public static PersistentData Data;
    public int starScore = 0;
    // Start is called before the first frame update
    
    private void Awake()
    {
        if (Data != null)
        {
            Destroy(gameObject);
            return;
        }

        Data = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddStar()
    {
        starScore += 1;
    }
}
