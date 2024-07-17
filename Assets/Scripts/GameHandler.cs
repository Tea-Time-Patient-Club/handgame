using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;

public class GameHandler : MonoBehaviour
{
    //--------------------------------------------------------------

    [Header("Map")]
    public TextAsset MapFilePath;
    public AudioClip MainMusic;
    public Sprite BackgroundImage;

    //--------------------------------------------------------------

    [Header("Objects")]
    public GameObject CirclePrefab;
    public GameObject BallPrefab;
    public GameObject Background;
    public GameObject SliderLinePrefab;

    [Header("Map")]
    public AudioClip HitSound;

    [Header("UI")]
    public TextMeshPro hitText;
    public TextMeshPro missText;
    public TextMeshPro Aduino;
    private TextMeshProUGUI statusText; // arduino data를 받은 text

    [Header("Slider")]
    public Sprite sliderBodySprite;
    public float sliderWidth = 1.5f;

    [SerializeField]
    private GameObject cursorTrailPrefab;

    const int SPAWN = -100;
    public static double timer = 0;
    private int ArduinoHit = 0;

    private List<GameObject> CircleList = new List<GameObject>();
    private List<HitObject> hitObjects = new List<HitObject>();
    private List<BallInfo> ballInfoList = new List<BallInfo>();
    private List<SliderInfo> sliderInfoList = new List<SliderInfo>();
    private static string[] LineParams;

    private AudioSource Sounds;
    private AudioSource Music;
    public static AudioSource pSounds;
    public static AudioClip pHitSound;

    private Camera MainCamera;
    private Vector3 MousePosition;
    private Ray MainRay;
    private RaycastHit MainHit;
    public static float startTime;

    private Dictionary<int, GameObject> cursorTrailInstances = new Dictionary<int, GameObject>();
    private List<HitObject> activeHitObjects = new List<HitObject>();

    private struct BallInfo
    {
        public GameObject BallObject;
        public Vector3 StartPos;
        public Vector3 EndPos;
        public float StartTime;
        public float EndTime;

        public BallInfo(GameObject ballObject, Vector3 startPos, Vector3 endPos, float startTime, float endTime)
        {
            BallObject = ballObject;
            StartPos = startPos;
            EndPos = endPos;
            StartTime = startTime;
            EndTime = endTime;
        }
    }

    private struct SliderInfo
    {
        public GameObject StartCircle;
        public GameObject SliderObject;
        public GameObject EndCircle;
        public int BeatTime;
        public int EndTime;
        public SpriteRenderer SliderRenderer;
        public GameObject Ball;
        public int LastParameter;

        public SliderInfo(GameObject startCircle, GameObject sliderObject, GameObject endCircle, int beatTime, int endTime, SpriteRenderer renderer, GameObject ball, int lastParameter)
        {
            StartCircle = startCircle;
            SliderObject = sliderObject;
            EndCircle = endCircle;
            BeatTime = beatTime;
            EndTime = endTime;
            SliderRenderer = renderer;
            Ball = ball;
            LastParameter = lastParameter;
        }
    }

    public class HitObject
    {
        public Vector3 Position { get; private set; }
        public int SpawnTime { get; private set; }
        public int BeatTime { get; private set; }
        public bool IsSlider { get; private set; }
        public bool IsEnd { get; private set; }

        public HitObject(Vector3 position, int spawnTime, int beatTime, bool isSlider = false, bool isEnd = false)
        {
            Position = position;
            SpawnTime = spawnTime;
            BeatTime = beatTime;
            IsSlider = isSlider;
            IsEnd = isEnd;
        }
    }

    private void LoadResources(string songFile)
    {
        MapFilePath = Resources.Load<TextAsset>($"{songFile}/Beatmap");
        MainMusic = Resources.Load<AudioClip>($"{songFile}/audio");
        BackgroundImage = Resources.Load<Sprite>($"{songFile}/BG");

        if (MapFilePath == null)
        {
            Debug.LogError($"Failed to load map file at path: {songFile}/Beatmap");
        }

        if (MainMusic == null)
        {
            Debug.LogError($"Failed to load audio file at path: {songFile}/audio");
        }

        if (BackgroundImage == null)
        {
            Debug.LogError($"Failed to load image file at path: {songFile}/BG");
        }
    }

    private void Start()
    {
        string selectedSongFile = GlobalHandler.Instance?.SelectedSongFile;
        startTime = Time.time;

        if (!string.IsNullOrEmpty(selectedSongFile))
        {
            LoadResources(selectedSongFile);
        }
        else
        {
            Debug.LogError("No song file selected.");
        }

        if (BLEManager.Instance != null&& GlobalHandler.PlayerTool == 1)
        {
            BLEManager.Instance.statusText = statusText;
        }

        MainCamera = Camera.main;
        Music = GameObject.Find("Music Source").GetComponent<AudioSource>();
        Sounds = gameObject.GetComponent<AudioSource>();
        Music.clip = MainMusic;
        pSounds = Sounds;
        pHitSound = HitSound;

        Input.multiTouchEnabled = true;

        hitText.gameObject.SetActive(false);
        missText.gameObject.SetActive(false);

        if (Background != null && BackgroundImage != null)
        {
            SpriteRenderer backgroundRenderer = Background.GetComponent<SpriteRenderer>();
            if (backgroundRenderer != null)
            {
                backgroundRenderer.sprite = BackgroundImage;
            }
        }

        if (MapFilePath != null)
        {
            ReadCircles(MapFilePath.text);
        }
        else
        {
            Debug.LogError("Map file path is null.");
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            CreateOrUpdateCursorTrail(-1, mousePosition);
            PerformCollisionDetection(mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            RemoveCursorTrail(-1);
        }
#endif
    }

    void ReadCircles(string mapContent)
    {
        using (StringReader reader = new StringReader(mapContent))
        {
            string line;
            bool hitObjectsSection = false;
            List<string> hitObjectLines = new List<string>();

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                {
                    continue;
                }

                if (hitObjectsSection)
                {
                    string[] lineParams = line.Split(',');

                    if (lineParams.Length < 6)
                    {
                        Debug.LogWarning($"Invalid line format: {line}");
                        continue;
                    }

                    // 슬라이더와 일반 원 구분
                    if (lineParams.Length >= 7 && lineParams[5].Contains("|"))
                    {
                        // 슬라이더일 경우
                        hitObjectLines.Add(line);
                    }
                    else if (int.TryParse(lineParams[3], out _))
                    {
                        // 일반 원일 경우
                        hitObjectLines.Add(line);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid line format: {line}");
                    }
                }
                else if (line == "[HitObjects]")
                {
                    hitObjectsSection = true;
                }
            }

            if (!hitObjectsSection)
            {
                Debug.LogError("Could not find [HitObjects] section in the map file.");
                return;
            }

            ProcessHitObjectGroup(hitObjectLines);
        }
    }

    void ProcessHitObjectGroup(List<string> hitObjectLines)
    {
        foreach (string line in hitObjectLines)
        {
            string[] lineParams = line.Split(',');
            if (lineParams.Length >= 3 &&
                int.TryParse(lineParams[0], out int x) &&
                int.TryParse(lineParams[1], out int y) &&
                int.TryParse(lineParams[2], out int beatTime))
            {
                Vector3 circlePosition = CalculateCirclePosition(x, y);

                int lastParameter = 0;
                if (lineParams.Length > 0 && int.TryParse(lineParams[lineParams.Length - 1], out int parsedValue))
                {
                    lastParameter = parsedValue;
                }

                if (lineParams.Length >= 7 && lineParams[5].Contains("|"))
                {
                    // 슬라이더일 경우
                    CreateSlider(circlePosition, lineParams);
                }
                else
                {
                    // 일반 원일 경우
                    GameObject circleObject = Instantiate(CirclePrefab, new Vector2(SPAWN, SPAWN), Quaternion.identity);
                    Circle circleComponent = circleObject.GetComponent<Circle>();
                    if (circleComponent != null)
                    {
                        circleComponent.Set(circlePosition.x, circlePosition.y, 0, beatTime - GlobalHandler.ApprRate, lastParameter);
                    }
                    hitObjects.Add(new HitObject(circlePosition, beatTime - GlobalHandler.ApprRate, beatTime));
                    CircleList.Add(circleObject);
                    GlobalHandler.TotalNotes++;
                }

                Debug.Log($"Processed hit object: position={circlePosition}, beatTime={beatTime}, lastParameter={lastParameter}");
            }
            else
            {
                Debug.LogWarning($"Invalid parameters in line: {line}");
            }
        }

        // CircleList를 정렬
        CircleList.Sort((a, b) =>
        {
            Circle aCircle = a.GetComponent<Circle>();
            Circle bCircle = b.GetComponent<Circle>();
            if (aCircle == null || bCircle == null)
            {
                return 0;
            }
            return aCircle.PosA.CompareTo(bCircle.PosA);
        });

        GameStart();
    }

    private void CreateSlider(Vector3 startPosition, string[] lineParams)
    {
        int beatTime = int.Parse(lineParams[2]);
        Vector3 endPosition = CalculateCirclePosition(
            int.Parse(lineParams[5].Split('|')[1].Split(':')[0]),
            int.Parse(lineParams[5].Split('|')[1].Split(':')[1])
        );

        int lastParameter = 0;
        if (lineParams.Length > 0 && int.TryParse(lineParams[lineParams.Length - 1], out int parsedValue))
        {
            lastParameter = parsedValue;
        }

        // 시작 원 생성 (비활성 상태로)
        GameObject startCircle = Instantiate(CirclePrefab, startPosition, Quaternion.identity);
        Circle startCircleComponent = startCircle.GetComponent<Circle>();
        if (startCircleComponent != null)
        {
            startCircleComponent.Set(startPosition.x, startPosition.y, 0, beatTime - GlobalHandler.ApprRate, lastParameter);
            startCircle.SetActive(false); // 처음에는 비활성 상태
        }

        // 슬라이더 라인 프리팹 인스턴스화
        GameObject sliderLineObject = Instantiate(SliderLinePrefab);
        SpriteRenderer sliderRenderer = sliderLineObject.GetComponent<SpriteRenderer>();
        sliderRenderer.enabled = false; // 처음에는 비활성 상태

        // 슬라이더 위치, 크기, 회전 설정
        Vector3 direction = endPosition - startPosition;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        sliderLineObject.transform.position = (startPosition + endPosition) / 2;
        sliderLineObject.transform.rotation = Quaternion.Euler(0, 0, angle);
        sliderLineObject.transform.localScale = new Vector3(length / sliderRenderer.sprite.bounds.size.x, 1.5f, 1);

        // 종료 시간 계산
        float sliderDuration = GlobalHandler.ApprRate;
        int endTime = beatTime + Mathf.RoundToInt(sliderDuration * 2);

        // 종료 원 생성 (비활성 상태로)
        GameObject endCircle = Instantiate(CirclePrefab, endPosition, Quaternion.identity);
        Circle endCircleComponent = endCircle.GetComponent<Circle>();
        if (endCircleComponent != null)
        {
            endCircleComponent.Set(endPosition.x, endPosition.y, 0, endTime - GlobalHandler.ApprRate, lastParameter);
            endCircle.SetActive(false); // 처음에는 비활성 상태
        }

        // Ball 생성 (비활성 상태로)
        GameObject ball = Instantiate(BallPrefab, startPosition, Quaternion.identity);
        ball.SetActive(false); // 처음에는 비활성 상태
        BallInfo ballInfo = new BallInfo(ball, startPosition, endPosition, (float)beatTime, (float)endTime);
        ballInfoList.Add(ballInfo);

        // hitObjects에 시작과 끝 원 추가
        hitObjects.Add(new HitObject(startPosition, beatTime - GlobalHandler.ApprRate, beatTime, true));
        hitObjects.Add(new HitObject(endPosition, endTime - GlobalHandler.ApprRate, endTime, true, true));

        // CircleList에 추가
        CircleList.Add(startCircle);
        CircleList.Add(sliderLineObject);
        CircleList.Add(endCircle);

        // 슬라이더 정보 저장
        sliderInfoList.Add(new SliderInfo(startCircle, sliderLineObject, endCircle, beatTime, endTime, sliderRenderer, ball, lastParameter));

        GlobalHandler.TotalNotes += 2;  // 시작과 끝 노트를 각각 카운트
    }

    private void UpdateSliders()
    {
        for (int i = sliderInfoList.Count - 1; i >= 0; i--)
        {
            SliderInfo sliderInfo = sliderInfoList[i];

            if (sliderInfo.StartCircle == null || sliderInfo.SliderRenderer == null || sliderInfo.EndCircle == null || sliderInfo.Ball == null)
            {
                sliderInfoList.RemoveAt(i);
                continue;
            }

            // 시작 원 활성화
            if (timer >= sliderInfo.BeatTime - GlobalHandler.ApprRate && !sliderInfo.StartCircle.activeSelf)
            {
                sliderInfo.StartCircle.SetActive(true);
            }

            // 슬라이더 라인 및 공 활성화
            if (timer >= sliderInfo.BeatTime && !sliderInfo.SliderRenderer.enabled)
            {
                sliderInfo.SliderRenderer.enabled = true;
                sliderInfo.Ball.SetActive(true);

                // 슬라이더가 활성화된 후 0.5초 뒤에 BLE 데이터를 읽는 코루틴 시작
                StartCoroutine(ReadBLEDataAfterDelay(0.5f));
            }

            // 종료 원 활성화
            if (timer >= sliderInfo.EndTime - GlobalHandler.ApprRate && !sliderInfo.EndCircle.activeSelf)
            {
                sliderInfo.EndCircle.SetActive(true);
            }

            // 슬라이더 진행 중 Ball 위치 업데이트
            float t = Mathf.InverseLerp(sliderInfo.BeatTime, sliderInfo.EndTime, (float)timer);
            Vector3 newPos = Vector3.Lerp(sliderInfo.StartCircle.transform.position, sliderInfo.EndCircle.transform.position, t);
            sliderInfo.Ball.transform.position = newPos;

            // 슬라이더 종료 처리
            if (timer > sliderInfo.EndTime + GlobalHandler.HitWindow)
            {
                sliderInfo.Ball.SetActive(false);
                sliderInfo.SliderRenderer.enabled = false;

                if (CircleList.Contains(sliderInfo.StartCircle)) CircleList.Remove(sliderInfo.StartCircle);
                if (CircleList.Contains(sliderInfo.SliderObject)) CircleList.Remove(sliderInfo.SliderObject);
                if (CircleList.Contains(sliderInfo.EndCircle)) CircleList.Remove(sliderInfo.EndCircle);

                Destroy(sliderInfo.StartCircle);
                Destroy(sliderInfo.SliderObject);
                Destroy(sliderInfo.EndCircle);
                Destroy(sliderInfo.Ball);

                sliderInfoList.RemoveAt(i);
            }
        }
    }

    private Vector3 CalculateCirclePosition(int x, int y)
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float adjustedX = screenHeight * 1.333333f;
        float paddingX = adjustedX / 8f;
        float paddingY = screenHeight / 8f;

        float newRangeX = adjustedX - 2 * paddingX;
        float newRangeY = screenHeight - 2 * paddingY;

        float newValueX = ((x * newRangeX) / 512f) + paddingX + ((screenWidth - adjustedX) / 2f);
        float newValueY = ((y * newRangeY) / 384f) + paddingY;

        return MainCamera.ScreenToWorldPoint(new Vector3(newValueX, newValueY, MainCamera.nearClipPlane));
    }

    private void GameStart()
    {
        Application.targetFrameRate = -1;
        Debug.Log($"Starting game. Music clip: {Music.clip}, Is playing: {Music.isPlaying}");
        Music.Play();
        StartCoroutine(UpdateRoutine());
    }

    private IEnumerator UpdateRoutine()
    {
        while (true)
        {
            timer = (Music.time * 1000);
            UpdateSliders();

            if (GlobalHandler.ObjCount < CircleList.Count)
            {
                GameObject currentObject = CircleList[GlobalHandler.ObjCount];
                if (currentObject != null)
                {
                    Circle currentCircle = currentObject.GetComponent<Circle>();

                    if (currentCircle != null)
                    {
                        GlobalHandler.DelayPos = currentCircle.PosA;

                        if (timer >= GlobalHandler.DelayPos)
                        {
                            currentCircle.Spawn();

                            // 일정 시간 뒤에 BLE 데이터를 읽는 코루틴 시작
                            StartCoroutine(ReadBLEDataAfterDelay(GlobalHandler.DelayPos + 500));

                            activeHitObjects.Add(new HitObject(currentCircle.transform.position, GlobalHandler.DelayPos, GlobalHandler.DelayPos + GlobalHandler.ApprRate));
                            GlobalHandler.ObjCount++;
                        }
                    }
                    else
                    {
                        GlobalHandler.ObjCount++;
                    }
                }
                else
                {
                    GlobalHandler.ObjCount++;
                }
            }

            for (int i = activeHitObjects.Count - 1; i >= 0; i--)
            {
                HitObject hitObject = activeHitObjects[i];
                if (timer > hitObject.BeatTime + GlobalHandler.HitWindow)
                {
                    int index = GlobalHandler.ObjCount - activeHitObjects.Count + i;
                    if (index >= 0 && index < CircleList.Count && CircleList[index] != null)
                    {
                        CircleList[index].SetActive(false);
                        ShowMissText();
                        GlobalHandler.Combo = 0;
                        Debug.Log("Miss due to timeout!");
                    }
                    activeHitObjects.RemoveAt(i);
                }
            }

            HandleInput();
            UpdateBallPositions();

            if (Music != null && Music.clip != null && !Music.isPlaying && Music.time >= Music.clip.length - 0.1f)
            {
                EndGame();
            }

            yield return null;
        }
    }
    private IEnumerator ReadBLEDataAfterDelay(double delayTime)
    {
        // 일정 시간 대기 (0.5초)
        double startTime = Time.time * 1000;
        while ((Time.time * 1000) < startTime + delayTime)
        {
            yield return null;
        }

        // BLE 데이터 읽기
        if (BLEManager.Instance != null)
        {
            BLEManager.Instance.OnReadDataButtonClick();
            string data = statusText.ToString();
            Aduino.text = data;
            // 데이터에 따라 히트 판정
            if (data.Contains("31"))
            {
                HandleHit();
            }
        }
        else
        {
            Debug.LogWarning("BLEManager instance not found.");
        }
    }

    private void HandleHit()
    {
        Debug.Log("Hit detected via BLE data");
        // 히트 판정 로직을 여기에 추가합니다.
        ArduinoHit = 1;
        PerformCollisionDetection(new Vector2(999,999));
    }

    private void HandleInput()
    {
        const int MAX_TOUCHES = 5;
        int touchCount = Mathf.Min(Input.touchCount, MAX_TOUCHES);

        for (int i = 0; i < touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            Vector2 touchPosition = touch.position;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                case TouchPhase.Moved:
                    CreateOrUpdateCursorTrail(touch.fingerId, touchPosition);
                    PerformCollisionDetection(touchPosition);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    RemoveCursorTrail(touch.fingerId);
                    break;
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            CreateOrUpdateCursorTrail(-1, mousePosition);
            PerformCollisionDetection(mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            RemoveCursorTrail(-1);
        }
#endif

        RemoveUnusedCursorTrails();
    }

    private void PerformCollisionDetection(Vector2 position)
    {
        if (MainCamera == null)
        {
            Debug.LogError("MainCamera is null");
            return;
        }

        Vector3 worldPosition = MainCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, MainCamera.nearClipPlane));

        List<int> hitObjectIndicesToRemove = new List<int>();

        for (int i = activeHitObjects.Count - 1; i >= 0; i--)
        {
            HitObject hitObject = activeHitObjects[i];
            if (hitObject == null)
            {
                continue;
            }

            float distance = Vector2.Distance(worldPosition, hitObject.Position);

            if (distance <= GlobalHandler.HitRadius || ArduinoHit == 1)
            {
                float timeDifference = Mathf.Abs((float)timer - hitObject.BeatTime);
                if (timeDifference <= GlobalHandler.HitWindow)
                {
                    int circleIndex = GlobalHandler.ObjCount - activeHitObjects.Count + i;
                    if (circleIndex >= 0 && circleIndex < CircleList.Count)
                    {
                        GameObject hitCircle = CircleList[circleIndex];
                        if (hitCircle != null)
                        {
                            Circle circleComponent = hitCircle.GetComponent<Circle>();
                            if (circleComponent != null)
                            {
                                if (hitObject.IsSlider)
                                {
                                    HandleSliderHit(circleComponent, hitObject.IsEnd);
                                }
                                else
                                {
                                    HandleCircleHit(circleComponent);
                                }

                                ArduinoHit = 0;

                                hitObjectIndicesToRemove.Add(i);

                                Debug.Log($"Hit successful! Distance: {distance}, Time difference: {timeDifference}");
                            }
                        }
                    }
                }
            }
        }

        // Remove hit objects in reverse order to maintain correct indices
        for (int i = hitObjectIndicesToRemove.Count - 1; i >= 0; i--)
        {
            activeHitObjects.RemoveAt(hitObjectIndicesToRemove[i]);
        }

        GlobalHandler.ClickedCount++;
    }

    private void HandleCircleHit(Circle circle)
    {
        circle.Got();
        circle.gameObject.SetActive(false);
        GlobalHandler.ClickedObject++;
        GlobalHandler.SuccessfulHits++;
        GlobalHandler.Combo++;
        if (GlobalHandler.Combo > GlobalHandler.MaxCombo)
        {
            GlobalHandler.MaxCombo = GlobalHandler.Combo;
        }
        ShowHitText();
    }

    private void HandleSliderHit(Circle circle, bool isEnd)
    {
        circle.Got();
        GlobalHandler.ClickedObject++;
        GlobalHandler.SuccessfulHits++;
        GlobalHandler.Combo++;
        if (GlobalHandler.Combo > GlobalHandler.MaxCombo)
        {
            GlobalHandler.MaxCombo = GlobalHandler.Combo;
        }
        ShowHitText();

        SliderInfo sliderInfo = sliderInfoList.Find(s => s.StartCircle == circle.gameObject || s.EndCircle == circle.gameObject);
        if (sliderInfo.StartCircle == circle.gameObject)
        {
            sliderInfo.StartCircle.SetActive(false);
            sliderInfo.SliderRenderer.enabled = true;
            sliderInfo.Ball.SetActive(true);
        }
        else if (sliderInfo.EndCircle == circle.gameObject && isEnd)
        {
            sliderInfo.EndCircle.SetActive(false);
            sliderInfo.Ball.SetActive(false);
            sliderInfo.SliderRenderer.enabled = false;
        }
    }

    private void ShowHitText()
    {
        hitText.gameObject.SetActive(true);
        StartCoroutine(HideTextAfterDelay(hitText, 0.5f));
    }

    private void ShowMissText()
    {
        missText.gameObject.SetActive(true);
        StartCoroutine(HideTextAfterDelay(missText, 0.5f));
    }

    private IEnumerator HideTextAfterDelay(TextMeshPro text, float delay)
    {
        yield return new WaitForSeconds(delay);
        text.gameObject.SetActive(false);
    }

    private void CreateOrUpdateCursorTrail(int fingerId, Vector2 touchPosition)
    {
        Vector3 worldPosition = MainCamera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, 10));

        if (cursorTrailInstances.ContainsKey(fingerId))
        {
            cursorTrailInstances[fingerId].transform.position = worldPosition;
        }
        else
        {
            GameObject cursorTrailInstance = Instantiate(cursorTrailPrefab, worldPosition, Quaternion.identity);
            cursorTrailInstances.Add(fingerId, cursorTrailInstance);
        }
    }

    private void RemoveCursorTrail(int fingerId)
    {
        if (cursorTrailInstances.ContainsKey(fingerId))
        {
            Destroy(cursorTrailInstances[fingerId]);
            cursorTrailInstances.Remove(fingerId);
        }
    }

    private void RemoveUnusedCursorTrails()
    {
        List<int> keysToRemove = new List<int>();

        foreach (var kvp in cursorTrailInstances)
        {
            if (!Input.touchSupported || !Input.GetMouseButton(0))
            {
                if (kvp.Key != -1)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }

        foreach (int key in keysToRemove)
        {
            Destroy(cursorTrailInstances[key]);
            cursorTrailInstances.Remove(key);
            Debug.Log($"Removed cursor trail with id: {key}");
        }
    }

    private void UpdateBallPositions()
    {
        for (int i = ballInfoList.Count - 1; i >= 0; i--)
        {
            BallInfo ballInfo = ballInfoList[i];
            float t = Mathf.InverseLerp(ballInfo.StartTime, ballInfo.EndTime, (float)timer);

            if (t >= 1f)
            {
                Destroy(ballInfo.BallObject);
                ballInfoList.RemoveAt(i);
            }
            else
            {
                Vector3 newPos = Vector3.Lerp(ballInfo.StartPos, ballInfo.EndPos, t);
                ballInfo.BallObject.transform.position = newPos;
            }
        }
    }

    void EndGame()
    {
        Debug.Log("Game Over!");

        CircleList.Clear();
        hitObjects.Clear();
        ballInfoList.Clear();
        activeHitObjects.Clear();
        sliderInfoList.Clear();

        SceneManager.LoadScene("endgame");
    }
}
