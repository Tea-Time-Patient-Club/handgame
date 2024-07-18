using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StateHandler1 : MonoBehaviour
{
    private static StateHandler1 instance;
    public Button tutorialButton;
    public Button clearButton;
    public Button Arduino;
    public Button backButton;
    public Button RealButton;
    public Button NoButton;

    [SerializeField]
    public GameObject songSelectPanel;
    private int dataReceivedCount = 0;


    void Start()
    {
        songSelectPanel.SetActive(false);

        if (tutorialButton != null) tutorialButton.onClick.AddListener(OnTutorialButtonClick);
        if (Arduino != null) Arduino.onClick.AddListener(ArduinoConnect);
        if (clearButton != null) clearButton.onClick.AddListener(OnClearButtonClick);
        if (backButton != null) backButton.onClick.AddListener(MainPage);
        if (RealButton != null) RealButton.onClick.AddListener(ClearButtonClick);
        if (NoButton != null) NoButton.onClick.AddListener(OffClearButtonClick);
    }

    // 기존 메서드들은 그대로 유지...
    public void OnTutorialButtonClick()
    {
        if (GlobalHandler.Instance != null)
        {
            GlobalHandler.Instance.SetSelectedSongFile("GameData/Tutorial", "Tutorial", "");
        }
        else
        {
            Debug.LogError("GlobalHandler instance not found!");
        }
        SceneManager.LoadScene("Main");
    }

    public void OnClearButtonClick()
    {
        songSelectPanel.SetActive(true);
    }

    public void ArduinoConnect()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void OffClearButtonClick()
    {
        songSelectPanel.SetActive(false);
    }

    public void ClearButtonClick()
    {
        ClearGameData();
        songSelectPanel.SetActive(false);
    }

    private void ClearGameData()
    {
        DataManager.DeleteAllData();
        Debug.Log("Game data cleared");
    }

    public void MainPage()
    {
        SceneManager.LoadScene("Start");
        Debug.Log("Start");
    }
}