using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class selectGameHandler : MonoBehaviour
{
    // 기존 변수
    public GameObject handSelectPanel;
    public GameObject songSelectPanel;

    public Button rightHandButton;
    public Button leftHandButton;

    // 추가된 변수
    public GameObject songPrefab;
    public Transform contentPanel;

    // 새로운 변수 추가
    public Image selectedSongImage;
    public Image selectedLevelImage;
    public TextMeshProUGUI selectedSongNameText;
    public TextMeshProUGUI selectedLevelText;
    public TextMeshProUGUI selectedLevelAdviceText;
    public TextMeshProUGUI selectedSongCreaterText;

    private List<SongDataEntry> songList;
    public Scrollbar slider; // 슬라이더를 연결할 변수

    private void Start()
    {
        // 초기 상태 설정
        handSelectPanel.SetActive(true);
        songSelectPanel.SetActive(false);

        // 버튼 클릭 이벤트 설정
        rightHandButton.onClick.AddListener(() => OnHandSelected("Right"));
        leftHandButton.onClick.AddListener(() => OnHandSelected("Left"));

        // 노래 데이터를 초기화합니다.
        InitializeSongData();
        
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    void OnSliderValueChanged(float value)
    {
        if(value < 0.3)
        {
            selectedLevelImage.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
            selectedLevelText.text = "E";
            selectedLevelAdviceText.text = "It's easy to select the level.";
            selectedLevelAdviceText.color = Color.Lerp(Color.red, Color.yellow, 0.5f);

        }
        else if(value < 0.6)
        {
            selectedLevelImage.color = Color.Lerp(Color.yellow, Color.green, 0.5f);
            selectedLevelText.text = "M";
            selectedLevelAdviceText.text = "You have selected a medium difficulty.";
            selectedLevelAdviceText.color = Color.Lerp(Color.yellow, Color.green, 0.5f);
        }
        else
        {
            selectedLevelImage.color = Color.red;
            selectedLevelText.text = "H";
            selectedLevelAdviceText.text = "You have selected a high difficulty.";
            selectedLevelAdviceText.color = Color.red;
        }
        Debug.Log($"{value}");
        GlobalHandler.Level = (int)(value*10);
        Debug.Log($"{GlobalHandler.Level}");
    }

    private void OnHandSelected(string hand)
    {
        // 선택한 손을 GlobalHandler에 저장
        if (GlobalHandler.Instance != null)
        {
            GlobalHandler.Instance.SetSelectedHand(hand);

            // 패널 상태 변경
            handSelectPanel.SetActive(false);
            songSelectPanel.SetActive(true);

            // 노래 리스트를 Scroll View에 추가
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

            // 모든 자식 오브젝트에서 SongItem 컴포넌트를 검색
            SongItem songItem = songObject.GetComponentInChildren<SongItem>();
            if (songItem == null)
            {
                Debug.LogError("Failed to get SongItem component from instantiated prefab.");
                continue;
            }

            string imagePath = $"{songData.file}/Image";
            Sprite songImage = Resources.Load<Sprite>(imagePath);

            if (songImage == null)
            {
                Debug.LogError($"Failed to load image at path: {imagePath}");
            }

            songItem.Setup(songImage, songData.title, songData.creater);
            songItem.button.onClick.AddListener(() => OnSongItemSelected(songData.title, songImage, songData.file,songData.creater));
        }
    }

    private string GetCreatorFromFile(string file)
    {
        string[] parts = file.Split('-');
        return parts.Length > 1 ? parts[0].Trim() : "";
    }

    private void OnSongItemSelected(string songName, Sprite songImage, string songFile, string songCreater)
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

        // 선택한 곡의 파일명을 GlobalHandler에 저장
        if (GlobalHandler.Instance != null)
        {
            GlobalHandler.Instance.SetSelectedSongFile(songFile);
        }
    }

    [System.Serializable]
    public class SongDataEntry
    {
        public string title;
        public string file;
        internal string creater;
    }
    private void InitializeSongData()
    {
        songList = new List<SongDataEntry>
        {
            new SongDataEntry
            {
                title = "Sample",
                file = "Sample",
                creater = "Kevin"
            },
            new SongDataEntry
            {
                title = "Picnic Party",
                file = "Picnic Party",
                creater = "SunoAI"
            },
            new SongDataEntry
            {
                title = "Warm Up Groove",
                file = "Warm Up Groove",
                creater = "Kevin MacLeod"
            },
        };
    }
}