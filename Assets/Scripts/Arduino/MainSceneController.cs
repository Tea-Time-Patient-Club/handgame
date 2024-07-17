using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainSceneController : MonoBehaviour
{
    public Button connectButton; // 연결 버튼 컴포넌트
    public Button goToDataReadSceneButton; // 데이터 읽기 씬으로 이동하는 버튼 컴포넌트
    public TextMeshProUGUI statusText; // 상태를 표시할 UI 텍스트 컴포넌트

    void Start()
    {
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClick); // 연결 버튼 클릭 리스너 추가
        }

        if (goToDataReadSceneButton != null)
        {
            goToDataReadSceneButton.onClick.AddListener(LoadDataReadScene); // 데이터 읽기 씬으로 이동하는 버튼 클릭 리스너 추가
        }
    }

    void OnConnectButtonClick()
    {
        if (BLEManager.Instance != null)
        {
            BLEManager.Instance.OnConnectButtonClick();
        }
        else
        {
            Debug.LogWarning("BLEManager instance not found.");
        }
    }

    public void LoadDataReadScene()
    {
        SceneManager.LoadScene("Controll"); // 데이터 읽기 씬으로 이동
    }
}
