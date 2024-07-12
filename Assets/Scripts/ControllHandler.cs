using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class StateHandler1 : MonoBehaviour
{
    private static StateHandler1 instance;
    public static StateHandler1 Instance { get { return instance; } }


    public UnityEngine.UI.Button tutorialButton;
    public UnityEngine.UI.Button clearButton; 
    public UnityEngine.UI.Button backButton;
    public UnityEngine.UI.Button RealButton;
    public UnityEngine.UI.Button NoButton;
    
    [SerializeField]    
    public GameObject songSelectPanel;


    private string gameDataPath = Path.Combine(Application.dataPath, "Resources/GameData");

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            // DontDestroyOnLoad(gameObject); // 이 줄을 제거합니다.
        }
    }

    void Start()
    {
        songSelectPanel.SetActive(false);
        
        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(OnTutorialButtonClick);
        }

        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearButtonClick);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(MainPage);
        }

        if (RealButton != null)
        {
            RealButton.onClick.AddListener(ClearButtonClick);
        }

        if (NoButton != null)
        {
            NoButton.onClick.AddListener(OffClearButtonClick);
        }
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
    public void OffClearButtonClick()
    {
        songSelectPanel.SetActive(false);
    }

    public void ClearButtonClick()
    {
        if (Directory.Exists(gameDataPath))
        {
            DirectoryInfo dir = new DirectoryInfo(gameDataPath);
            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
        }
        songSelectPanel.SetActive(false);
    }

    public void MainPage()
    {
        SceneManager.LoadScene("Start");
        Debug.Log("Start");
    }
}