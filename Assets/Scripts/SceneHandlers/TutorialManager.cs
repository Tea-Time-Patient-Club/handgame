using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System;
using UnityEngine.SceneManagement;
public class TutorialManager : MonoBehaviour
{
    public GameObject circle; // 작아질 원의 GameObject (Inspector에서 설정)
    public GameObject note;
    public GameObject circle2;
    public GameObject note2;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI noteText;
    public TextMeshProUGUI note2Text;
    public Button screenButton; // 스크린 버튼
    public GameObject hand;
    public Button backButton;

    private int touchCount = 0;
    private bool isCircleSmall = false;
    private bool isCircle2Small = false;
    private float scaleSpeed = 50.0f; // 원의 크기가 줄어드는 속도
    private float targetScale = 80.0f; // 원이 줄어들 최종 크기
    public float speed = 1.0f; // 이동 속도
    private Vector3 targetPosition; // 목표 지점



    void Start()
    {
        if (screenButton != null)
        {
            screenButton.onClick.AddListener(OnscreenButtonClick); // 연결 버튼 클릭 리스너 추가

        }
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnbackButtonClick); // 연결 버튼 클릭 리스너 추가

        }
        note.SetActive(false); // 원을 초기에는 비활성화 상태로 설정
        circle.SetActive(false);
        note2.SetActive(false); // 원을 초기에는 비활성화 상태로 설정
        circle2.SetActive(false);
        hand.SetActive(false);
        messageText.text = "Hello,\nYour Rehabilitation Assistant\ngreets you.";
    }

    void Update()
    {
        switch (touchCount)
        {
            case 1:
                messageText.text = "A~E each represent the thumb to the pinky finger.\n put your hand like image.";



                circle.transform.localPosition = (new Vector3(-213, -100, 0));
                note.transform.localPosition = (new Vector3(-213, -100, 0));

                hand.SetActive(true);
                break;
            case 2:
                if (!isCircleSmall)
                {
                    note.SetActive(true);
                    circle.SetActive(true);
                    noteText.text = "A";
                    StartCoroutine(ScaleDownCircle());
                    isCircleSmall = true;

                }


                break;
            case 3:
                note.SetActive(false);
                isCircleSmall = false;

                circle.transform.localPosition = (new Vector3(-122, 15, 0));
                note.transform.localPosition = (new Vector3(-122, 15, 0));

                messageText.text = "Great!";

                break;
            case 4:
                messageText.text = "Let's practice with the other fingers";


                break;
            case 5:
                if (!isCircleSmall)
                {
                    note.SetActive(true);
                    circle.SetActive(true);
                    noteText.text = "B";
                    StartCoroutine(ScaleDownCircle());
                    isCircleSmall = true;

                }

                break;
            case 6:
                note.SetActive(false);
                isCircleSmall = false;

                circle.transform.localPosition = (new Vector3(-1, 35, 0));
                note.transform.localPosition = (new Vector3(-1, 35, 0));

                messageText.text = "Great!";

                break;
            case 7:
                if (!isCircleSmall)
                {
                    note.SetActive(true);
                    circle.SetActive(true);
                    noteText.text = "C";
                    StartCoroutine(ScaleDownCircle());
                    isCircleSmall = true;

                }

                break;
            case 8:
                note.SetActive(false);
                isCircleSmall = false;

                circle.transform.localPosition = (new Vector3(145, 15, 0));
                note.transform.localPosition = (new Vector3(145, 15, 0));

                messageText.text = "Great!";
                break;
            case 9:
                if (!isCircleSmall)
                {
                    note.SetActive(true);
                    circle.SetActive(true);
                    noteText.text = "D";
                    StartCoroutine(ScaleDownCircle());
                    isCircleSmall = true;

                }
                break;
            case 10:
                note.SetActive(false);
                isCircleSmall = false;

                //circle.transform.position= (new Vector3(280,-30,0));
                //note.transform.position= (new Vector3(280,-30,0));
                note.transform.localPosition = new Vector3(280f, -30f, 0f);
                circle.transform.localPosition = new Vector3(280f, -30f, 0f);
                messageText.text = "Great!";
                break;
            case 11:
                if (!isCircleSmall)
                {
                    note.SetActive(true);
                    circle.SetActive(true);
                    noteText.text = "E";
                    StartCoroutine(ScaleDownCircle());
                    isCircleSmall = true;

                }
                break;
            case 12:
                note.SetActive(false);
                isCircleSmall = false;



                messageText.text = "Great!";
                hand.SetActive(false);
                break;
            case 13:
                messageText.text = "Finally, I'll teach you how to bend through the slide.";
                break;
            case 14:
                messageText.text = "Touch it and follow the circle.";
                break;

        }
        if (touchCount == 15)
        {
            circle.transform.localPosition = new Vector3(-300f, -100f, 0f);
            circle2.transform.localPosition = new Vector3(300f, -100f, 0f);
            if (!isCircleSmall)
            {
                circle.SetActive(true);
                noteText.text = "";
                StartCoroutine(ScaleDownCircle());
                isCircleSmall = true;

            }
            if (!isCircle2Small)
            {
                circle2.SetActive(true);
                StartCoroutine(ScaleDownCircle2());
                isCircle2Small = true;
            }
            note.SetActive(true);
            note2.SetActive(true);
            noteText.text = "";
            note2Text.text = "";
            note.transform.localPosition = new Vector3(-300f, -100f, 0f);
            note2.transform.localPosition = new Vector3(300f, -100f, 0f);
            StartMovingObjects();
        }
        else if (touchCount == 18)
        {
            note.SetActive(false);
            note2.SetActive(false);
            isCircle2Small = false;
            isCircleSmall = false;
            messageText.text = "Great!";
        }
        else if (touchCount == 19)
        {
            messageText.text = "Goodbye";
        }
        else if (touchCount == 20)
        {
            SceneManager.LoadScene("select");
            Debug.Log("select");
        }

    }
    private IEnumerator MoveToMidpoint()
    {
        while (Vector3.Distance(note.transform.localPosition, targetPosition) > 0.01f ||
               Vector3.Distance(note2.transform.localPosition, targetPosition) > 0.01f)
        {
            // 두 오브젝트 사이의 중간 지점 계산
            targetPosition = (note.transform.localPosition + note2.transform.localPosition) / 2;

            // 오브젝트1을 중간 지점으로 이동
            note.transform.localPosition = Vector3.MoveTowards(note.transform.localPosition, targetPosition, speed * Time.deltaTime);
            // 오브젝트2를 중간 지점으로 이동
            note2.transform.localPosition = Vector3.MoveTowards(note2.transform.localPosition, targetPosition, speed * Time.deltaTime);

            // 한 프레임 대기
            yield return null;


        }
    }

    // 이동을 시작하는 함수
    public void StartMovingObjects()
    {
        // 코루틴 시작
        StartCoroutine(MoveToMidpoint());
    }
    IEnumerator ScaleDownCircle()
    {
        circle.SetActive(true); // 원을 활성화하여 보이게 함
        circle.transform.localScale = new Vector3(200f, 200f, 0f);

        // 원이 최종 크기에 도달할 때까지 반복
        while (circle.transform.localScale.x > targetScale)
        {
            // 원의 크기를 감소시킴
            float newScale = circle.transform.localScale.x - scaleSpeed * Time.deltaTime;
            circle.transform.localScale = new Vector3(newScale, newScale, newScale);

            yield return null;
        }

        // 원의 크기가 targetScale 이하로 줄어들면 최종 크기로 설정
        circle.transform.localScale = new Vector3(targetScale, targetScale, targetScale);
        circle.SetActive(false);



        messageText.text = "Now touch the circle!";
    }
    IEnumerator ScaleDownCircle2()
    {
        circle2.SetActive(true); // 원을 활성화하여 보이게 함
        circle2.transform.localScale = new Vector3(200f, 200f, 0f);

        // 원이 최종 크기에 도달할 때까지 반복
        while (circle2.transform.localScale.x > targetScale)
        {
            // 원의 크기를 감소시킴
            float newScale2 = circle2.transform.localScale.x - scaleSpeed * Time.deltaTime;
            circle2.transform.localScale = new Vector3(newScale2, newScale2, newScale2);

            yield return null;
        }

        // 원의 크기가 targetScale 이하로 줄어들면 최종 크기로 설정
        circle2.transform.localScale = new Vector3(targetScale, targetScale, targetScale);
        circle2.SetActive(false);



        messageText.text = "Now touch the circle!";
    }


    void OnscreenButtonClick()
    {
        touchCount++;
    }
    void OnbackButtonClick()
    {
        SceneManager.LoadScene("Start");
    }
}
