using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StateHandler : MonoBehaviour
{
    public GameObject handPanel; // Hand 선택 패널
    public GameObject panel; // Panel
    public GameObject monthPanel; // Month 패널
    public GameObject weekPanel; // Week 패널

    public Button rightHandButton; // Right Hand 버튼
    public Button leftHandButton; // Left Hand 버튼
    public Button monthButton; // Month 버튼
    public Button weekButton; // Week 버튼

    public TextMeshProUGUI HandText; // Right Hand 버튼
    public TextMeshProUGUI HandText2; // Right Hand 버튼
    void Start()
    {
        // 초기 상태 설정
        handPanel.SetActive(true);
        panel.SetActive(false);
        monthPanel.SetActive(false);
        weekPanel.SetActive(false);

        // 버튼 클릭 이벤트 설정
        rightHandButton.onClick.AddListener(() => OnHandSelected("Right"));
        leftHandButton.onClick.AddListener(() => OnHandSelected("Left"));
        monthButton.onClick.AddListener(OnMonthSelected);
        weekButton.onClick.AddListener(OnWeekSelected);
    }

    void OnHandSelected(string hand)
    {
        HandText.text = HandText2.text = hand;
        handPanel.SetActive(false);
        panel.SetActive(true);
    }

    void OnMonthSelected()
    {
        monthPanel.SetActive(true);
        weekPanel.SetActive(false);
    }

    void OnWeekSelected()
    {
        weekPanel.SetActive(true);
        monthPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
