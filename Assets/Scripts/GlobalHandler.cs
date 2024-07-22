using System.Collections.Generic;
using UnityEngine;

public class GlobalHandler : MonoBehaviour
{
    public static GlobalHandler Instance { get; private set; }

    public string SelectedHand { get; private set; }
    public string SelectedSongFile { get; private set; }
    public string SelectedSongTitle { get; private set; }
    public string SelectedSongArtist { get; private set; }

    // Combo 및 히트 카운트 배열
    public static int Combo { get; set; }
    public static int MaxCombo { get; set; }
    public static int[] HitCounts { get; set; }

    public static int ApprRate = 0;
    public static float HitWindow = 100f;
    public static int Level = 1;
    public static int ClickedCount = 0;
    public static int ClickedObject = 0;
    public static int ObjCount = 0;
    public static int DelayPos = 0;
    public static int SuccessfulHits = 0;
    public static int TotalNotes = 0;
    public static int PlayerTool = 0;
    public static int active = 0;
    public static float alpha = -1;
    public static float levelSystemY = 0;
    public static float levelSystemX = 0;
    public static string genre;

    // 새로 추가된 속성들
    public static float ApproachCircleStartScale = 2.0f;
    public static float ApproachCircleEndScale = 0.9f;
    public static float HitRadius = 100f;
    public static float FallSpeed = 2f;
    public static float ExpandSpeed = 2f;
    public static float OffScreenPosition = -101f;

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

        HitCounts = new int[6];
    }

    public void SetSelectedHand(string hand)
    {
        SelectedHand = hand;
    }

    public void SetSelectedSongFile(string songFilePath, string songTitle, string songArtist)
    {
        SelectedSongFile = songFilePath;
        SelectedSongTitle = songTitle;
        SelectedSongArtist = songArtist;
        Debug.Log("Selected Song File: " + SelectedSongTitle);
    }


    // 게임 초기화를 위한 메서드
    public static void ResetGameStats()
    {
        Combo = 0;
        MaxCombo = 0;
        ClickedCount = 0;
        ClickedObject = 0;
        ObjCount = 0;
        DelayPos = 0;
        SuccessfulHits = 0;
        TotalNotes = 0;

        for (int i = 0; i < HitCounts.Length; i++)
        {
            HitCounts[i] = 0;
        }
    }
}