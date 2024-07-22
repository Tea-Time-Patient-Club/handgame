using System.Collections;
using UnityEngine;
using TMPro;

public class Circle : MonoBehaviour
{
    // Circle parameters
    private float PosX = 0;
    private float PosY = 0;
    private float PosZ = 0;

    [HideInInspector]
    public int PosA = 0;

    private Color MainColor, MainColor1, MainColor2; // Circle sprites color
    public GameObject MainApproach, MainFore, MainBack; // Circle objects

    [HideInInspector]
    public SpriteRenderer Fore, Back, Appr; // Circle sprites

    // Checker stuff
    private bool RemoveNow = false;
    private bool GotIt = false;

    public TextMeshPro numberText; // TextMeshPro 컴포넌트 참조
    public int lastParameter;

    private void Awake()
    {
        Fore = MainFore.GetComponent<SpriteRenderer>();
        Back = MainBack.GetComponent<SpriteRenderer>();
        Appr = MainApproach.GetComponent<SpriteRenderer>();
        numberText = GetComponentInChildren<TextMeshPro>();

        // 텍스트의 초기 설정
        if (numberText != null)
        {
            numberText.alignment = TextAlignmentOptions.Center;
            numberText.color = Color.white; // 필요에 따라 색상 조정
            numberText.fontSize = 5; // 필요에 따라 폰트 크기 조정 (크기 조정 필요)
        }
    }

    // Set circle configuration
    public void Set(float x, float y, float z, int a, int number)
    {
        PosX = x;
        PosY = y;
        PosZ = z;
        PosA = a;
        MainColor = Appr.color;
        MainColor1 = Fore.color;
        MainColor2 = Back.color;
        this.lastParameter = number;  // 수정된 부분

        if (numberText != null)
        {
            string selectedHand = GlobalHandler.Instance.SelectedHand;
            if (!string.IsNullOrEmpty(selectedHand) && selectedHand.Equals("Right"))
            {
                switch (number)
                {
                    case 1:
                        numberText.text = "THU"; // Thumb
                        break;
                    case 2:
                        numberText.text = "IND"; // Index
                        break;
                    case 3:
                        numberText.text = "MID"; // Middle
                        break;
                    case 4:
                        numberText.text = "RIN"; // Ring
                        break;
                    case 5:
                        numberText.text = "PIN"; // Pinky
                        break;
                }
            }
            else
            {
                switch (number)
                {
                    case 1:
                        numberText.text = "PIN"; // Pinky
                        break;
                    case 2:
                        numberText.text = "RIN"; // Ring
                        break;
                    case 3:
                        numberText.text = "MID"; // Middle
                        break;
                    case 4:
                        numberText.text = "IND"; // Index
                        break;
                    case 5:
                        numberText.text = "THU"; // Thumb
                        break;
                }
            }
        }
    }

    // Spawning the circle
    public void Spawn()
    {
        gameObject.transform.position = new Vector3(PosX, PosY, PosZ);
        this.enabled = true;
        StartCoroutine(Checker());
    }

    // If circle wasn't clicked
    public void Remove()
    {
        if (!GotIt)
        {
            RemoveNow = true;
            this.enabled = true;
            StartCoroutine(RemoveCircleAfterDelay(GlobalHandler.HitWindow)); // 0.5초 후 제거
        }
    }

    public bool Got()
    {
        if (!RemoveNow)
        {
            GotIt = true;
            MainApproach.transform.position = new Vector2(-101, -101);
            GameHandler.pSounds.PlayOneShot(GameHandler.pHitSound);
            RemoveNow = false;
            this.enabled = true;
            StartCoroutine(RemoveCircleAfterDelay(GlobalHandler.HitWindow)); // 0.5초 후 제거
            return true;
        }
        return false;
    }

    private IEnumerator RemoveCircleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.transform.position = new Vector2(-101, -101);
        this.enabled = false;
    }

    // Check if circle wasn't clicked
    protected IEnumerator Checker() // 접근 제어자를 protected로 변경
    {
        while (true)
        {
            // 75 means delay before removing
            if (GameHandler.timer >= PosA + (GlobalHandler.ApprRate + 75) && !GotIt)
            {
                Remove();
                GlobalHandler.ClickedCount++;
                break;
            }
            yield return null;
        }
    }
    // Main Update
    private void Update()
    {
        // Approach Circle modifier
        if (MainApproach.transform.localScale.x >= 0.9f)
        {
            MainApproach.transform.localScale -= new Vector3(5.166667f, 5.166667f, 0f) * Time.deltaTime;
            MainColor.a += 4f * Time.deltaTime;
            MainColor1.a += 4f * Time.deltaTime;
            MainColor2.a += 4f * Time.deltaTime;
            Fore.color = MainColor1;
            Back.color = MainColor2;
            Appr.color = MainColor;
        }
        // If circle wasn't clicked
        else if (!GotIt)
        {
            // Remove circle
            if (!RemoveNow)
            {
                MainApproach.transform.position = new Vector2(-101, -101);
                this.enabled = false;
            }
            // If circle wasn't clicked
            else
            {
                MainColor1.a -= 10f * Time.deltaTime;
                MainColor2.a -= 10f * Time.deltaTime;
                MainFore.transform.localPosition += (Vector3.down * 2) * Time.deltaTime;
                MainBack.transform.localPosition += Vector3.down * Time.deltaTime;
                Fore.color = MainColor1;
                Back.color = MainColor2;
                if (MainColor1.a <= 0f)
                {
                    gameObject.transform.position = new Vector2(-101, -101);
                    this.enabled = false;
                }
            }
        }
        // If circle was clicked
        if (GotIt)
        {
            MainColor1.a -= 10f * Time.deltaTime;
            MainColor2.a -= 10f * Time.deltaTime;

            MainFore.transform.localScale += new Vector3(2, 2, 0) * Time.deltaTime;
            MainBack.transform.localScale += new Vector3(2, 2, 0) * Time.deltaTime;

            Fore.color = MainColor1;
            Back.color = MainColor2;

            if (MainColor1.a <= 0f)
            {
                gameObject.transform.position = new Vector2(-101, -101);
                this.enabled = false;
            }
        }
    }
}
