using System.Collections.Generic;
using UnityEngine;

public class GlobalHandler : MonoBehaviour
{
    public static GlobalHandler Instance { get; private set; }

    public string SelectedHand { get; private set; }
    public string SelectedSongFile { get; private set; }

    // Combo 및 히트 카운트 배열
    public static int Combo { get; set; }
    public static int MaxCombo { get; set; }
    public static int[] HitCounts { get; set; }

    public static int ApprRate = 600;
    public static float HitWindow = 300f;
    public static int ClickedCount = 0;
    public static int ClickedObject = 0;
    public static int ObjCount = 0;
    public static int DelayPos = 0;
    public static int SuccessfulHits = 0;
    public static int TotalNotes = 0;
    public static int Level = 1;
    public static int PlayerTool = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 히트 카운트 배열 초기화 (예를 들어 1000개의 히트 오브젝트를 가정)
        HitCounts = new int[6];
    }

    public void SetSelectedHand(string hand)
    {
        SelectedHand = hand;
    }

    public void SetSelectedSongFile(string songFile)
    {
        SelectedSongFile = songFile;
        Debug.Log("Selected Song File: " + SelectedSongFile);
    }
}
