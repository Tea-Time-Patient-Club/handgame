using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DataReadSceneController : MonoBehaviour
{
    public Button readDataButton; // 데이터 읽기 버튼 컴포넌트
    public TextMeshProUGUI statusText; // 상태를 표시할 UI 텍스트 컴포넌트

    void Start()
    {
        if (readDataButton != null)
        {
            readDataButton.onClick.AddListener(OnReadDataButtonClick); // 데이터 읽기 버튼 클릭 리스너 추가
        }

        // BLEManager 인스턴스의 statusText와 연결
        if (BLEManager.Instance != null)
        {
            BLEManager.Instance.statusText = statusText;
        }
    }

    void OnReadDataButtonClick()
    {
        if (BLEManager.Instance != null)
        {
            BLEManager.Instance.OnReadDataButtonClick();
        }
        else
        {
            Debug.LogWarning("BLEManager instance not found.");
        }
    }
}
