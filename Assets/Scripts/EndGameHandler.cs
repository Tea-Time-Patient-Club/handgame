using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class GameData
{
    public string SongTitle;
    public string SelectedHand;
    public int MaxCombo;
    public int[] HitCounts;
    public int SuccessfulHits;
}

public class EndGameHandler : MonoBehaviour
{
    public TextMeshProUGUI successText;
    public TextMeshProUGUI failText;
    public TextMeshProUGUI TotalText;
    public TextMeshProUGUI ComboText;
    public Image songImage;
    public TextMeshProUGUI songTitleText;
    public TextMeshProUGUI selectedHandText;
    public TextMeshProUGUI PerformanceText;
    public TextMeshProUGUI PlayToolText;
    public TextMeshProUGUI LevelText;

    public Slider pointSlider;

    private void Start()
    {
        UpdateEndGameUI();
        DataSave();
    }

    private void DataSave()
    {
        if (GlobalHandler.Instance != null)
        {
            string songTitle = GlobalHandler.Instance.SelectedSongFile;
            string selectedHand = GlobalHandler.Instance.SelectedHand;
            string directoryPath = Path.Combine(Application.dataPath, "Resources", "GameData");
            string filePath = Path.Combine(directoryPath, $"{selectedHand}_{songTitle}_GameData.json");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            GameData gameData;

            // 기존 데이터가 있는지 확인
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                gameData = JsonUtility.FromJson<GameData>(json);
            }
            else
            {
                gameData = new GameData
                {
                    SongTitle = songTitle,
                    SelectedHand = selectedHand,
                    HitCounts = new int[6] // 기본값 설정, 필요에 따라 크기 조정
                };
            }

            // 데이터 업데이트
            gameData.MaxCombo = GlobalHandler.MaxCombo;
            gameData.HitCounts = GlobalHandler.HitCounts;
            gameData.SuccessfulHits = GlobalHandler.SuccessfulHits;

            // JSON으로 저장
            string updatedJson = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(filePath, updatedJson);
            Debug.Log($"Game data saved to {filePath}");
        }
        else
        {
            Debug.LogError("GlobalHandler instance is null. Cannot save game data.");
        }
    }

    private void UpdateEndGameUI()
    {
        // 성공 및 실패 횟수 업데이트
        successText.text = $"{GlobalHandler.SuccessfulHits}";
        failText.text = $"{GlobalHandler.TotalNotes - GlobalHandler.SuccessfulHits}";
        TotalText.text = $"{GlobalHandler.TotalNotes}";
        ComboText.text = $"{GlobalHandler.MaxCombo}";
        PerformanceText.text = $"{GlobalHandler.SuccessfulHits * GlobalHandler.Level}"; // 성공한 노트 수 * Level
        pointSlider.value = (float)GlobalHandler.SuccessfulHits / GlobalHandler.TotalNotes;
        
        if(GlobalHandler.PlayerTool == 1)
        {
            PlayToolText.text = "Aduino";  // 1이면 아두이노
        }
        else{
            PlayToolText.text = "Phone"; // 0이면 폰
        }
        
        // 노래 이미지 및 제목 업데이트
        string selectedSongFile = GlobalHandler.Instance?.SelectedSongFile;
        if (!string.IsNullOrEmpty(selectedSongFile))
        {
            songTitleText.text = selectedSongFile;
            songImage.sprite = Resources.Load<Sprite>($"{selectedSongFile}/Image");
        }

        // 선택된 손 정보 업데이트
        if (GlobalHandler.Instance != null)
        {
            string selectedHand = GlobalHandler.Instance.SelectedHand;
            selectedHandText.text = selectedHand.Equals("Right") ? "R" : "L";
        }

        switch(GlobalHandler.Level)
        {
            case 3:
                LevelText.text = "E";
                break;
            case 6:
                LevelText.text = "M";
                break;
            case 10:
                LevelText.text = "H";
                break;
            default:
                LevelText.text = "E";
                break;
        }
        {

        }
    }
}
