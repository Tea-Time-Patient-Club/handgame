using System;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class selectGameHandler : MonoBehaviour
{
    public GameObject handSelectPanel;
    public GameObject songSelectPanel;

    public Button rightHandButton;
    public Button leftHandButton;

    public GameObject songPrefab;
    public Transform contentPanel;

    public Image selectedSongImage;
    public Image selectedLevelImage;
    public TextMeshProUGUI selectedSongNameText;
    public TextMeshProUGUI selectedLevelAdviceText;
    public TextMeshProUGUI selectedSongCreaterText;

    public Scrollbar slider;

    public Button recommendationButton;
    public Button difficultyAdjustmentButton;

    public Sprite difficultyEasy;
    public Sprite difficultyNormal;
    public Sprite difficultyHard;

    private float newHitwindow = 0;

    private const string DATA_FILE_NAME = "GameData/SongData"; // Resources/GameData 폴더 내에 SongData.json 파일이 있어야 합니다.
    private List<SongDataEntry> songList;

    private void Start()
    {
        handSelectPanel.SetActive(true);
        songSelectPanel.SetActive(false);

        rightHandButton.onClick.AddListener(() => OnHandSelected("Right"));
        leftHandButton.onClick.AddListener(() => OnHandSelected("Left"));

        recommendationButton.onClick.AddListener(OnRecommendationButtonClicked);

        LoadSongData();

        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        difficultyAdjustmentButton.onClick.AddListener(OnDifficultyAdjustmentButtonClicked);
        slider.value = 0.01f;
    }

    private void LoadSongData()
    {
        TextAsset jsonData = Resources.Load<TextAsset>(DATA_FILE_NAME);
        if (jsonData != null)
        {
            SongDataList songDataList = JsonUtility.FromJson<SongDataList>(jsonData.text);
            songList = songDataList.songs;

            int selectedSongIndex = UnityEngine.Random.Range(0, songList.Count);
            SelectSongItemByCode(songList[selectedSongIndex].title, songList[selectedSongIndex].creater);
        }
        else
        {
            Debug.LogError($"Failed to load song data from {DATA_FILE_NAME}.json");
        }
    }

    void OnSliderValueChanged(float value)
    {
        if (value < 0.3)
        {
            selectedLevelImage.sprite = difficultyEasy;
            selectedLevelAdviceText.text = "Your starting point to rehabilite.";
        }
        else if (value < 0.6)
        {
            selectedLevelImage.sprite = difficultyNormal;
            selectedLevelAdviceText.text = "Too easy? Try this level.";
        }
        else
        {
            selectedLevelImage.sprite = difficultyHard;
            selectedLevelAdviceText.text = "May similar to common rhythm games.";
        }
        if(slider.value == 0)
        {
            slider.value = 0.1f;
        }
        GlobalHandler.Level = (int)(value * 10);
        SetDifficultyValues(GlobalHandler.Level);
    }

    private void OnHandSelected(string hand)
    {
        if (GlobalHandler.Instance != null)
        {
            GlobalHandler.Instance.SetSelectedHand(hand);
            handSelectPanel.SetActive(false);
            songSelectPanel.SetActive(true);
            PopulateSongList();
        }
        else
        {
            Debug.LogError("GlobalHandler instance not found!");
        }
    }

    private void PopulateSongList()
    {
        foreach (SongDataEntry songData in songList)
        {
            GameObject songObject = Instantiate(songPrefab, contentPanel);
            SongItem songItem = songObject.GetComponentInChildren<SongItem>();

            if (songItem == null)
            {
                Debug.LogError("Failed to get SongItem component from instantiated prefab.");
                continue;
            }

            string imagePath = $"{songData.filePath}/Image";
            Sprite songImage = Resources.Load<Sprite>(imagePath);

            if (songImage == null)
            {
                Debug.LogError($"Failed to load image at path: {imagePath}");
            }

            songItem.Setup(songImage, songData.title, songData.creater);
            songItem.button.onClick.AddListener(() => OnSongItemSelected(songData.title, songImage, songData.filePath, songData.creater, songData.active, songData.genre));
        }
    }

    private void OnSongItemSelected(string songName, Sprite songImage, string songFilePath, string songCreater, int active, string genre)
    {
        if (selectedSongNameText != null)
        {
            selectedSongNameText.text = songName;
        }

        if (selectedSongImage != null)
        {
            selectedSongImage.sprite = songImage;
        }

        if (selectedSongCreaterText != null)
        {
            selectedSongCreaterText.text = songCreater;
        }

        if (GlobalHandler.Instance != null)
        {
            GlobalHandler.Instance.SetSelectedSongFile(songFilePath, songName, songCreater);
            GlobalHandler.active = active;
            GlobalHandler.genre = genre;
        }
    }

    private void SelectSongItemByCode(string title, string artist)
    {
        foreach (Transform child in contentPanel)
        {
            SongItem song = child.gameObject.GetComponentInChildren<SongItem>();
            
            if (song.songNameText.text == title && song.creatorText.text == artist)
            {
                song.button.onClick.Invoke();
                break;
            }
        }
    }

    private void OnRecommendationButtonClicked()
    {
        string lastPlayedGenre = GetLastPlayedGenre();

        List<SongDataEntry> recommendedSongs;

        if (string.IsNullOrEmpty(lastPlayedGenre))
        {
            Debug.Log("No last played genre found. Recommending all songs.");
            recommendedSongs = new List<SongDataEntry>(songList);
        }
        else
        {
            recommendedSongs = songList.FindAll(song =>
            {
                bool isMatch = string.Equals(lastPlayedGenre, song.genre, StringComparison.OrdinalIgnoreCase);
                Debug.Log($"Song: {song.title}, Genre: {song.genre}, Matches last played genre: {isMatch}");
                return isMatch;
            });

            Debug.Log($"Recommended songs based on last played genre: {recommendedSongs.Count}");

            if (recommendedSongs.Count == 0)
            {
                Debug.Log("No songs match the last played genre. Recommending all songs.");
                recommendedSongs = new List<SongDataEntry>(songList);
            }
        }

        int selectedSongIndex = UnityEngine.Random.Range(0, recommendedSongs.Count);

        SelectSongItemByCode(recommendedSongs[selectedSongIndex].title, recommendedSongs[selectedSongIndex].creater);
        Debug.Log($"Total recommended songs displayed: {recommendedSongs.Count}");
    }

    private string GetLastPlayedGenre()
    {
        GameDataList gameDataList = DataManager.LoadAllGameData();

        if (gameDataList != null && gameDataList.Games != null && gameDataList.Games.Count > 0)
        {
            GameData lastGame = gameDataList.Games[gameDataList.Games.Count - 1];
            return lastGame.Genre;
        }

        return string.Empty;
    }

    private void UpdateSongList(List<SongDataEntry> songs)
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        foreach (SongDataEntry songData in songs)
        {
            GameObject songObject = Instantiate(songPrefab, contentPanel);
            SongItem songItem = songObject.GetComponentInChildren<SongItem>();

            if (songItem == null)
            {
                Debug.LogError("Failed to get SongItem component from instantiated prefab.");
                continue;
            }

            string imagePath = $"{songData.filePath}/Image";
            Sprite songImage = Resources.Load<Sprite>(imagePath);

            if (songImage == null)
            {
                Debug.LogWarning($"Failed to load image at path: {imagePath}");
            }

            songItem.Setup(songImage, songData.title, songData.creater);
            songItem.button.onClick.AddListener(() => OnSongItemSelected(songData.title, songImage, songData.filePath, songData.creater, songData.active, songData.genre));

            Debug.Log($"Added song to list: {songData.title}, Genre: {songData.genre}");
        }
    }

    private float GetSuccessRate()
    {
        GameDataList gameDataList = DataManager.LoadAllGameData();

        if (gameDataList != null && gameDataList.Games != null && gameDataList.Games.Count > 0)
        {
            GameData lastGame = gameDataList.Games[gameDataList.Games.Count - 1];
            Debug.Log($"{lastGame.alpha}");
            GlobalHandler.alpha = (lastGame.alpha >= 0) ? -1 : lastGame.alpha;
            GlobalHandler.Level = lastGame.Level;
            Debug.Log($"{GlobalHandler.alpha}");
            if (lastGame.TotalNotes > 0)
            {
                float successRate = (float)lastGame.SuccessfulHits / lastGame.TotalNotes;
                GlobalHandler.levelSystemX = successRate;
                return successRate;
            }
        }
        return 0f;
    }

    // 난이도 추천 시스템
    public static class DifficultyAdjustment
    {
        public static float GetDifficultyLevel(float successRate, float alpha)
        {
            if (0.9f <= successRate || successRate <= 0.7f)
            {
                GlobalHandler.alpha = successRate * alpha;
                return successRate * alpha;
            }
            return alpha;
        }
    }

    private void OnDifficultyAdjustmentButtonClicked()
    {
        float successRate = GetSuccessRate();
        newHitwindow = DifficultyAdjustment.GetDifficultyLevel(successRate, GlobalHandler.alpha) * 80;
        GlobalHandler.Level = (int)slider.value * 10;
        SetDifficultyValues(GlobalHandler.Level, newHitwindow);
    }

    private void SetDifficultyValues(float level, float newHitWindow = 0f)
    {
        float movementTime; // 4cm 이동하는 데 걸리는 시간 (초)
        float movesize = 40;

        if (level <= 3)
        {
            movementTime = movesize / 7.3f; // 약 5.48초
        }
        else if (level <= 6)
        {
            movementTime = movesize / 5.5f; // 약 7.27초
        }
        else // level <= 10
        {
            movementTime = movesize / 4.4f; // 약 9.09초
        }

        // ApprRate 계산: 움직임 시간을 기준으로 설정
        // 예: 가장 느린 속도(level <= 3)를 기준으로 1200으로 설정
        GlobalHandler.ApprRate = (int)(1200 * (5.48f / movementTime));

        // HitWindow 계산 또는 설정
        if (newHitWindow != 0)
        {
            // 새로운 HitWindow 값이 제공된 경우 이를 사용
            GlobalHandler.HitWindow = 100.0f + newHitWindow;
            GlobalHandler.levelSystemY = GlobalHandler.HitWindow; 
            slider.value = Mathf.Lerp(0.0f, 1.0f, Mathf.InverseLerp(0f, 100f, GlobalHandler.HitWindow));
        }
        else
        {
            // 기존 방식대로 HitWindow 계산
            GlobalHandler.HitWindow = Mathf.Lerp(10f, 100f, (9.09f - movementTime) / (9.09f - 5.48f));
        }
    }
}
