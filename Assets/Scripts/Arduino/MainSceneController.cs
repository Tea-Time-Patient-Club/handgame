using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Assets.SimpleSpinner;

public class MainSceneController : MonoBehaviour
{
    public Button backButton; // 뒤로가기

    public TextMeshProUGUI statusText; // 상태를 표시할 UI 텍스트 컴포넌트
    public Button connectButton; // 연결 버튼 컴포넌트
    public Button readDataButton; // 데이터 읽기 버튼 컴포넌트
    public SimpleSpinner spinner; // 빙글빙글 도는 로딩 스피너
    public GameObject statusLogo; //상태 표시 위한 object

    public Button troubleshootOpen;
    public GameObject troubleshootingPanel; //대화상자
    public Button troubleshootClose;

    void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackButtonClick); // 데이터 읽기 씬으로 이동하는 버튼 클릭 리스너 추가
        }
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClick); // 연결 버튼 클릭 리스너 추가
        }
        if (readDataButton != null)
        {
            readDataButton.onClick.AddListener(OnReadDataButtonClick); // 데이터 읽기 버튼 클릭 리스너 추가
            readDataButton.interactable = false; // 초기에는 버튼 비활성화
        }
        if (troubleshootOpen != null)
        {
            troubleshootOpen.onClick.AddListener(OnTrobuleShootingButtonClick);
        }
        if (troubleshootClose != null)
        {
            troubleshootClose.onClick.AddListener(OnTroubeShootingCloseClick);
        }
        troubleshootingPanel.SetActive(false);
        spinner.gameObject.SetActive(true);
        statusLogo.SetActive(false);

        StartCoroutine(BLEManager.Instance.InitializeBLE(statusText, spinner, statusLogo)); // BLE 초기화 코루틴 시작
    }

    public void OnBackButtonClick()
    {
        SceneManager.LoadScene("Controll"); // 데이터 읽기 씬으로 이동
    }

    public void OnConnectButtonClick()
    {
        spinner.Rotation = true;
        spinner.gameObject.SetActive(true);
        statusLogo.SetActive(false);
        BLEManager.Instance.StartScanning(statusText, spinner, statusLogo, readDataButton); // 스캔 시작
    }

    public void OnReadDataButtonClick()
    {
        BLEManager.Instance.ReadDataFromDevice(statusText); // 데이터 읽기 시작
        if (spinner)
        {
            spinner.Rotation = true;
            spinner.gameObject.SetActive(true);
            statusLogo.SetActive(false);
        }
    }

    public void OnTrobuleShootingButtonClick()
    {
        troubleshootingPanel.SetActive(true);
    }

    public void OnTroubeShootingCloseClick()
    {
        troubleshootingPanel.SetActive(false);
    }
}
