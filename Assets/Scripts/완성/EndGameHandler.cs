using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[System.Serializable]
public class GameData
{
    public string SongTitle;
    public string SelectedHand;
    public string ActiveTool;
    public int MaxCombo;
    public int[] HitCounts;
    public int SuccessfulHits;
    public string PlayDate;
    public int ApprRate;
    public float HitWindow;
    public int Level;
    public int Active;
    public string Genre;
    public int TotalNotes;
    public float alpha;
    public float LevelSystemX; 
    public float LevelSystemY;
}

[System.Serializable]
public class GameDataList
{
    public List<GameData> Games = new List<GameData>();
}

public class EndGameHandler : MonoBehaviour
{
    // Database path constants
    private const string DATA_DIRECTORY_PATH = "Resources/GameData"; // Path to GameData directory
    private const string GAME_DATA_FILE = "AllGameData.json"; // Game data file name

    public TextMeshProUGUI ComboText;
    public TextMeshProUGUI successText;
    public TextMeshProUGUI failText;
    public TextMeshProUGUI TotalText;
    
    public Image songImage;
    public TextMeshProUGUI songTitleText;
    public TextMeshProUGUI songArtistText;

    public Image playToolImage;
    public Image levelImage;
    public Image selectedHandImage;

    public Sprite toolHardwareSprite;
    public Sprite toolTouchscreenSprite;
    
    public Sprite easyLevel;
    public Sprite normalLevel;
    public Sprite hardLevel;

    public Sprite rightHand;
    public Sprite leftHand;

    public TextMeshProUGUI PerformanceText;
    public Slider pointSlider;

    private string filePath;
    
    private void Start()
    {
        pointSlider.interactable = false;
        UpdateEndGameUI();
        filePath = Path.Combine(Application.persistentDataPath, GAME_DATA_FILE);
        DataSave();

        if(GlobalHandler.Instance.SelectedSongFile == "Tutorial")
        {
            
        }
    }

    private void DataSave()
    {
        if (GlobalHandler.Instance != null)
        {
            GameData gameData = new GameData
            {
                SongTitle = GlobalHandler.Instance.SelectedSongFile,
                SelectedHand = GlobalHandler.Instance.SelectedHand,
                ActiveTool = GlobalHandler.PlayerTool == 1 ? "Arduino" : "Hand",
                HitCounts = GlobalHandler.HitCounts,
                PlayDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                MaxCombo = GlobalHandler.MaxCombo,
                SuccessfulHits = GlobalHandler.SuccessfulHits,
                ApprRate = GlobalHandler.ApprRate,
                HitWindow = GlobalHandler.HitWindow,
                Level = GlobalHandler.Level,
                Active = GlobalHandler.active,
                Genre = GlobalHandler.genre,
                TotalNotes = GlobalHandler.TotalNotes,
                alpha = GlobalHandler.alpha,
                LevelSystemX = GlobalHandler.levelSystemX,
                LevelSystemY = GlobalHandler.levelSystemY
             };

             DataManager.SaveGameData(gameData);
        }
        else
        {
            Debug.LogError("GlobalHandler instance is null. Cannot save game data.");
        }
    }

    public void ControllSlider()
    {
        pointSlider.value = (float)GlobalHandler.SuccessfulHits / GlobalHandler.TotalNotes;
    }

    private void UpdateEndGameUI()
    {
        // Update success and fail counts
        successText.text = $"{GlobalHandler.SuccessfulHits}";
        failText.text = $"{GlobalHandler.TotalNotes - GlobalHandler.SuccessfulHits}";
        TotalText.text = $"{GlobalHandler.TotalNotes}";
        ComboText.text = $"{GlobalHandler.MaxCombo}";
        PerformanceText.text = $"{GlobalHandler.SuccessfulHits * GlobalHandler.Level}"; // Successful hits * Level
        pointSlider.value = (float)GlobalHandler.SuccessfulHits / GlobalHandler.TotalNotes;
        Debug.Log($"{GlobalHandler.SuccessfulHits}"+"aasadf"+$"{GlobalHandler.TotalNotes}");

        if (GlobalHandler.PlayerTool == 1)
        {
            playToolImage.sprite = toolHardwareSprite; // 1 is Arduino
        }
        else
        {
            playToolImage.sprite = toolTouchscreenSprite; // 0 is Phone
        }

        // Update song image and title
        string selectedSongFile = GlobalHandler.Instance?.SelectedSongFile;
        string selectedSongTitle = GlobalHandler.Instance?.SelectedSongTitle;
        string selectedSongArtist = GlobalHandler.Instance?.SelectedSongArtist;

        if (!string.IsNullOrEmpty(selectedSongFile))
        {
            songTitleText.text = selectedSongTitle;
            songArtistText.text = selectedSongArtist;
            songImage.sprite = Resources.Load<Sprite>($"{selectedSongFile}/Image");
        }

        // Update selected hand information
        if (GlobalHandler.Instance != null)
        {
            string selectedHand = GlobalHandler.Instance.SelectedHand;
            selectedHandImage.sprite = selectedHand.Equals("Right") ? rightHand : leftHand;
        }

        switch (GlobalHandler.Level)
        {
            case 3:
                levelImage.sprite = easyLevel;
                break;
            case 6:
                levelImage.sprite = normalLevel;
                break;
            case 10:
            default:
                levelImage.sprite = hardLevel;
                break;
        }
    }
}
