using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StateHandler : MonoBehaviour
{
    private const string DATA_DIRECTORY_PATH = "Resources/GameData";

    public GameObject handPanel;
    public GameObject panel;
    public GameObject monthPanel;
    public GameObject weekPanel;
    public GameObject HandTextWeek;
    public GameObject HandTextMonth;
    public GameObject weekController;
    public GameObject monthController;

    public Button backButton;
    public Button rightHandButton;
    public Button leftHandButton;
    public Button monthButton;
    public Button weekButton;

    public TextMeshProUGUI WeekFeedBack;
    public TextMeshProUGUI MonthFeedBack;

    public GameObject fingerSliderPrefab;
    public GameObject fiveFingerSliderPrefab;
    public Transform weekContentPanel;
    public Transform monthContentPanel;
    public Transform dataContentPanel;

    private List<GameData> weekPlayData = new List<GameData>();
    private List<GameData> monthPlayData = new List<GameData>();

    public string selectedHand;

    void Start()
    {
        handPanel.SetActive(true);
        panel.SetActive(false);
        monthPanel.SetActive(false);
        weekPanel.SetActive(false);

        backButton.onClick.AddListener(OnBackButtonSelected);
        rightHandButton.onClick.AddListener(() => OnHandSelected("Right"));
        leftHandButton.onClick.AddListener(() => OnHandSelected("Left"));
        monthButton.onClick.AddListener(OnMonthSelected);
        weekButton.onClick.AddListener(OnWeekSelected);
    }

    void OnBackButtonSelected()
    {
        Debug.Log("Back Clicked");

        if(panel.activeSelf)
        {
            Debug.Log("Trying to change panel");
            panel.SetActive(false);
            handPanel.SetActive(true);
        } else
        {
            SceneManager.LoadScene("Start");
        }
    }

    void OnHandSelected(string hand)
    {
        // HandTextWeek.text = hand;
        // HandTextMonth.text = hand;
        selectedHand = hand;
        handPanel.SetActive(false);
        LoadPlayData();
        panel.SetActive(true);
        weekPanel.SetActive(true);
    }

    void OnMonthSelected()
    {
        monthPanel.SetActive(true);
        weekPanel.SetActive(false);

        DisplayMonthlyData(monthPlayData, monthContentPanel);
    }

    void OnWeekSelected()
    {
        weekPanel.SetActive(true);
        monthPanel.SetActive(false);

        DisplayPlayData(weekPlayData, weekContentPanel, "Week");
    }

    private float CalculateAverageAlpha(List<GameData> gameDataList)
    {
        if (gameDataList == null || gameDataList.Count == 0)
            return 0;

        var sortedList = gameDataList.OrderByDescending(g => DateTime.Parse(g.PlayDate)).ToList();

        float sum = 0;
        int count = 0;

        for (int i = 0; i < Math.Min(2, sortedList.Count); i++)
        {
            sum += sortedList[i].alpha;
            count++;
        }

        return count > 0 ? sum / count : 0;
    }

    private string GenerateFeedback(float averageAlpha)
    {
        if (averageAlpha > 0) // 양수
            return "fight";
        else if (averageAlpha < 0) // 음수
            return "Good";
        else
            return "SoSo";
    }

    void LoadPlayData()
    {
        GameDataList allGameData = DataManager.LoadAllGameData();
        DateTime now = DateTime.Now;

        weekPlayData.Clear();
        monthPlayData.Clear();

        foreach (var gameData in allGameData.Games)
        {
            if (!string.IsNullOrEmpty(gameData.PlayDate))
            {
                DateTime playDate = DateTime.Parse(gameData.PlayDate);

                if (gameData.SelectedHand.Equals(selectedHand))
                {
                    if (playDate >= DateTime.Now.AddDays(-7))
                    {
                        weekPlayData.Add(gameData);
                    }
                    if (playDate >= now.AddMonths(-5) && playDate <= now)
                    {
                        monthPlayData.Add(gameData);
                    }
                }
            }
        }

        float weeklyAverageAlpha = CalculateAverageAlpha(weekPlayData);
        float monthlyAverageAlpha = CalculateAverageAlpha(monthPlayData);

        Debug.Log($"Weekly Average Alpha: {weeklyAverageAlpha}, Feedback: {WeekFeedBack.text}");
        Debug.Log($"Monthly Average Alpha: {monthlyAverageAlpha}, Feedback: {MonthFeedBack.text}");

        WeekFeedBack.text = GenerateFeedback(weeklyAverageAlpha);
        MonthFeedBack.text = GenerateFeedback(monthlyAverageAlpha);

    }

    void DisplayPlayData(List<GameData> playDataList, Transform contentPanel, string context)
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        Dictionary<string, int[]> dailyActiveCounts = new Dictionary<string, int[]>();
        Dictionary<string, int> dailyPlayCounts = new Dictionary<string, int>();

        foreach (var gameData in playDataList)
        {
            DateTime playDate = DateTime.Parse(gameData.PlayDate);
            string dateKey = playDate.ToString("yyyy-MM-dd");

            if (!dailyActiveCounts.ContainsKey(dateKey))
            {
                dailyActiveCounts[dateKey] = new int[3];
                dailyPlayCounts[dateKey] = 0;
            }

            if (gameData.Active >= 1 && gameData.Active <= 3)
            {
                dailyActiveCounts[dateKey][gameData.Active - 1] += gameData.SuccessfulHits;
                dailyPlayCounts[dateKey]++;
            }
        }

        foreach (var entry in dailyActiveCounts)
        {
            string dateDisplay = context == "Month" ?
                DateTime.Parse(entry.Key).ToString("MM") :
                entry.Key.Substring(5);

            int[] activeCounts = entry.Value;
            int playCount = dailyPlayCounts[entry.Key];

            GameObject sliderObject = Instantiate(fingerSliderPrefab, contentPanel);
            FingerSliderPrefab slider = sliderObject.GetComponent<FingerSliderPrefab>();

            slider.changeSliderValue(
                playCount > 0 ? (float)activeCounts[0] / playCount : 0,
                playCount > 0 ? (float)activeCounts[1] / playCount : 0,
                playCount > 0 ? (float)activeCounts[2] / playCount : 0,
                dateDisplay
            );
        }
    }

    void DisplayMonthlyData(List<GameData> playDataList, Transform contentPanel)
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        Dictionary<string, int[]> monthlyActiveCounts = new Dictionary<string, int[]>();
        Dictionary<string, int> monthlyPlayCounts = new Dictionary<string, int>();

        DateTime now = DateTime.Now;

        for (int i = 0; i < 5; i++)
        {
            DateTime monthStart = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            DateTime monthEnd = monthStart.AddMonths(1).AddDays(-1);

            foreach (var gameData in playDataList)
            {
                DateTime playDate = DateTime.Parse(gameData.PlayDate);
                if (playDate >= monthStart && playDate <= monthEnd)
                {
                    string monthKey = monthStart.ToString("yyyy-MM");

                    if (!monthlyActiveCounts.ContainsKey(monthKey))
                    {
                        monthlyActiveCounts[monthKey] = new int[3];
                        monthlyPlayCounts[monthKey] = 0;
                    }

                    if (gameData.Active >= 1 && gameData.Active <= 3)
                    {
                        monthlyActiveCounts[monthKey][gameData.Active - 1] += gameData.SuccessfulHits;
                        monthlyPlayCounts[monthKey]++;
                    }
                }
            }
        }

        foreach (var month in monthlyActiveCounts.OrderByDescending(m => m.Key))
        {
            string monthDisplay = month.Key.Substring(5);

            int[] activeCounts = month.Value;
            int playCount = monthlyPlayCounts[month.Key];

            GameObject sliderObject = Instantiate(fingerSliderPrefab, contentPanel);
            FingerSliderPrefab slider = sliderObject.GetComponent<FingerSliderPrefab>();

            slider.changeSliderValue(
                playCount > 0 ? (float)activeCounts[0] / playCount : 0,
                playCount > 0 ? (float)activeCounts[1] / playCount : 0,
                playCount > 0 ? (float)activeCounts[2] / playCount : 0,
                monthDisplay
            );
        }
    }
}