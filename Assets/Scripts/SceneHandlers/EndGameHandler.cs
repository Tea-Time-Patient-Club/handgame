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
    private const string DATA_DIRECTORY_PATH = "Resources/GameData";
    private const string GAME_DATA_FILE = "AllGameData.json";

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

    public GameObject animatedObject; // "New Treatment" 텍스트에 해당하는 오브젝트

    private string filePath;

    private void Start()
    {
        pointSlider.interactable = false;
        UpdateEndGameUI();
        DataSave(); // 게임 종료 시 데이터 저장
        filePath = DataManager.GetDataPath(DataManager.ALL_GAME_DATA_FILE);
    }
    private void OnDisable()
    {
        DataSave(); // 씬이 비활성화될 때 데이터 저장
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
            Debug.Log("Game data saved successfully.");
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
        successText.text = $"{GlobalHandler.SuccessfulHits}";
        failText.text = $"{GlobalHandler.TotalNotes - GlobalHandler.SuccessfulHits}";
        TotalText.text = $"{GlobalHandler.TotalNotes}";
        ComboText.text = $"{GlobalHandler.MaxCombo}";
        pointSlider.value = (float)GlobalHandler.SuccessfulHits / GlobalHandler.TotalNotes;

        if (GlobalHandler.Level == 0)
            PerformanceText.text = $"{GlobalHandler.SuccessfulHits * 1}";
        else
            PerformanceText.text = $"{GlobalHandler.SuccessfulHits * GlobalHandler.Level}";

        if (GlobalHandler.PlayerTool == 1)
        {
            playToolImage.sprite = toolHardwareSprite;
        }
        else
        {
            playToolImage.sprite = toolTouchscreenSprite;
        }

        string selectedSongFile = GlobalHandler.Instance?.SelectedSongFile;
        string selectedSongTitle = GlobalHandler.Instance?.SelectedSongTitle;
        string selectedSongArtist = GlobalHandler.Instance?.SelectedSongArtist;

        if (!string.IsNullOrEmpty(selectedSongFile))
        {
            songTitleText.text = selectedSongTitle;
            songArtistText.text = selectedSongArtist;
            songImage.sprite = Resources.Load<Sprite>($"{selectedSongFile}/Image");
        }

        if (GlobalHandler.Instance != null)
        {
            string selectedHand = GlobalHandler.Instance.SelectedHand;
            selectedHandImage.sprite = selectedHand.Equals("Right") ? rightHand : leftHand;
        }

        if (GlobalHandler.Level <= 3)
        {
            levelImage.sprite = easyLevel;
        }
        else if (GlobalHandler.Level <= 6)
        {
            levelImage.sprite = normalLevel;
        }
        else
        {
            levelImage.sprite = hardLevel;
        }

        CheckAccuracyAndAnimate();
    }

    private void CheckAccuracyAndAnimate()
    {
        float accuracy = (float)GlobalHandler.SuccessfulHits / GlobalHandler.TotalNotes;
        // Custom logic for checking accuracy and triggering animation can be added here
        if (accuracy > 0.8f)
        {
            StartCoroutine(AnimateObject());
            DataManager.CopySequentialSongData();
        }
    }


    private IEnumerator AnimateObject()
    {
        if (animatedObject == null)
        {
            Debug.LogError("Animated object is not assigned!");
            yield break;
        }

        Vector3 originalPosition = animatedObject.transform.localPosition;
        Vector3 leftPosition = originalPosition + new Vector3(-300f, 0f, 0f); // 왼쪽으로 이동하는 거리 설정
        Vector3 rightPosition = originalPosition + new Vector3(300f, 0f, 0f); // 오른쪽으로 이동하는 거리 설정

        // 왼쪽으로 이동
        float elapsedTime = 0f;
        while (elapsedTime < 10f) // 10초 동안 이동
        {
            animatedObject.transform.localPosition = Vector3.Lerp(originalPosition, leftPosition, elapsedTime / 1f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        animatedObject.transform.localPosition = leftPosition;
    }
}
