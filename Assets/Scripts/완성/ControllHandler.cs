using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StateHandler1 : MonoBehaviour
{
    private static StateHandler1 instance;
    public static StateHandler1 Instance { get { return instance; } }

    public Button tutorialButton;
    public Button clearButton;
    public Button Arduino;
    public Button backButton;
    public Button RealButton;
    public Button NoButton;

    [SerializeField]
    public GameObject songSelectPanel;

    private Blemanager bleManager =null;
    public TextMeshProUGUI arduinoText =null;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    void Start()
    {
        songSelectPanel.SetActive(false);
        bleManager = FindObjectOfType<Blemanager>();

        if (tutorialButton != null) tutorialButton.onClick.AddListener(OnTutorialButtonClick);
        if (Arduino != null) Arduino.onClick.AddListener(ArduinoConnect);
        if (clearButton != null) clearButton.onClick.AddListener(OnClearButtonClick);
        if (backButton != null) backButton.onClick.AddListener(MainPage);
        if (RealButton != null) RealButton.onClick.AddListener(ClearButtonClick);
        if (NoButton != null) NoButton.onClick.AddListener(OffClearButtonClick);
    }
    private void OnEnable()
    {
      //  Blemanager.Instance.OnDataReceived += HandleArduinoData;
    }

    private void HandleArduinoData(string data)
    {
        Debug.Log($"Received data from Arduino: {data}");
        arduinoText.text = data;
    }
    public void OnTutorialButtonClick()
    {
        if (GlobalHandler.Instance != null)
        {
            GlobalHandler.Instance.SetSelectedSongFile("Tutorial");
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
        //bleManager.StartCoroutine(bleManager.InitializeBLE());
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