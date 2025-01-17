using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Burst.Intrinsics;

public class GameHandler : MonoBehaviour
{
    //--------------------------------------------------------------
    [Header("BackGround")]
    public TextMeshProUGUI combo;
    public TextMeshProUGUI songName;
    public TextMeshProUGUI songCreater;
    public Image songImage;

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

    [Header("Slider")]
    public Sprite sliderBodySprite;
    public float sliderWidth = 1.5f;

    [SerializeField]
    private GameObject cursorTrailPrefab;

    const int SPAWN = -100;
    private int miss = 0;
    public static double timer = 0;
    public static int ArduinoHit = 0;

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
    private readonly object sliderCreationLock = new object();

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
        songName.text = GlobalHandler.Instance?.SelectedSongTitle;
        songCreater.text = GlobalHandler.Instance?.SelectedSongArtist;
        songImage.sprite = Resources.Load<Sprite>($"{GlobalHandler.Instance?.SelectedSongFile}/Image");

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
        ResetGame(); // 새 게임 시작 시 모든 상태 초기화

        string selectedSongFile = GlobalHandler.Instance?.SelectedSongFile;
        startTime = Time.time;

        if (BLEManager.Instance != null)
        {
            BLEManager.Instance.SubscribeToCharacteristic(); // 구독 시작
        }
        if (!string.IsNullOrEmpty(selectedSongFile))
        {
            LoadResources(selectedSongFile);
        }
        else
        {
            Debug.LogError("No song file selected.");
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
        lock (sliderCreationLock)
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
                // 시작점에 대한 BLE 데이터 읽기
                StartCoroutine(ReadBLEDataAfterDelay(0.5f, sliderInfo.LastParameter));
            }

            // 슬라이더 라인 및 공 활성화
            if (timer >= sliderInfo.BeatTime && !sliderInfo.SliderRenderer.enabled)
            {
                sliderInfo.SliderRenderer.enabled = true;
                sliderInfo.Ball.SetActive(true);
            }

            // 슬라이더 활성화 중 지속적인 BLE 데이터 확인
            if (timer >= sliderInfo.BeatTime && timer < sliderInfo.EndTime)
            {
                // 주기적으로 BLE 데이터 읽기 (예: 100ms마다)
                if (Mathf.FloorToInt((float)timer * 10) % 10 == 0)
                {
                    StartCoroutine(ReadBLEDataAfterDelay(0.1f, sliderInfo.LastParameter));
                }
            }

            // 종료 원 활성화
            if (timer >= sliderInfo.EndTime - GlobalHandler.ApprRate && !sliderInfo.EndCircle.activeSelf)
            {
                sliderInfo.EndCircle.SetActive(true);
                // 종료점에 대한 BLE 데이터 읽기
                StartCoroutine(ReadBLEDataAfterDelay(0.5f, sliderInfo.LastParameter));
            }

            // 슬라이더 진행 중 Ball 위치 업데이트
            float t = Mathf.InverseLerp(sliderInfo.BeatTime, sliderInfo.EndTime, (float)timer);
            Vector3 newPos = Vector3.Lerp(sliderInfo.StartCircle.transform.position, sliderInfo.EndCircle.transform.position, t);
            sliderInfo.Ball.transform.position = newPos;

            // 슬라이더 종료 처리
            if (timer > sliderInfo.EndTime + GlobalHandler.HitWindow)
            {
                // CircleList에서 제거
                if (CircleList.Contains(sliderInfo.StartCircle)) CircleList.Remove(sliderInfo.StartCircle);
                if (CircleList.Contains(sliderInfo.SliderObject)) CircleList.Remove(sliderInfo.SliderObject);
                if (CircleList.Contains(sliderInfo.EndCircle)) CircleList.Remove(sliderInfo.EndCircle);

                // 오브젝트 삭제
                Destroy(sliderInfo.StartCircle);
                Destroy(sliderInfo.SliderObject);
                Destroy(sliderInfo.EndCircle);
                Destroy(sliderInfo.Ball);

                sliderInfoList.RemoveAt(i);

                // Miss 처리
                ShowMissText();
                GlobalHandler.Combo = 0;
                combo.text = GlobalHandler.Combo.ToString();
                Debug.Log("Slider missed!");
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
            try
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

                                StartCoroutine(ReadBLEDataAfterDelay(0.5f, currentCircle.lastParameter));

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
                            combo.text = GlobalHandler.Combo.ToString();
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
                    yield break;  // 게임이 끝나면 루틴을 종료합니다.
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in UpdateRoutine: {ex.Message}");
            }

            yield return null;
        }
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
            if (ArduinoHit == 1)
            {
                worldPosition = hitObject.Position;
            }

            if (hitObject == null)
            {
                continue;
            }

            float distance = Vector2.Distance(worldPosition, hitObject.Position);

            if (distance <= GlobalHandler.HitRadius)
            {
                float timeDifference = Mathf.Abs((float)timer - hitObject.BeatTime);
                if (timeDifference <= GlobalHandler.HitWindow || ArduinoHit == 1)
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
                                    if (hitObject.IsEnd)
                                    {
                                        HandleEndCircleHit(circleComponent);
                                    }
                                    else
                                    {
                                        HandleStartCircleHit(circleComponent);
                                    }
                                }
                                else
                                {
                                    HandleCircleHit(circleComponent);
                                }
                                hitObjectIndicesToRemove.Add(i);
                                break; // 하나의 히트 객체만 처리하고 루프를 종료
                            }
                        }
                    }
                }
            }
        }

        // Remove hit objects in reverse order to maintain correct indices
        for (int i = hitObjectIndicesToRemove.Count - 1; i >= 0; i--)
        {
            int indexToRemove = hitObjectIndicesToRemove[i];
            if (indexToRemove >= 0 && indexToRemove < activeHitObjects.Count)
            {
                activeHitObjects.RemoveAt(indexToRemove);
            }
        }

        GlobalHandler.ClickedCount++;
        ArduinoHit = 0; // Hit 처리 후 초기화
    }


    private IEnumerator ReadBLEDataAfterDelay(double delayTime, int lastParameter)
    {
        double startTime = Time.time * 1000;
        double endTime = startTime + delayTime;
        TextMeshProUGUI statusText = null;

        while ((Time.time * 1000) < endTime)
        {
            // BLE 데이터 읽기
            if (BLEManager.Instance != null)
            {
                BLEManager.Instance.ReadDataFromDevice(statusText, lastParameter);
                yield return new WaitForSeconds(1f);
                PerformCollisionDetection(new Vector2(0, 0)); // Hit 판정 수행
            }

            // 0.3초 대기
            yield return null;
        }
    }

    private void HandleInput()
    {
        if (GlobalHandler.PlayerTool == 1) return; // 아두이노 히트 시 터치 입력 무시

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

    private void HandleCircleHit(Circle circle)
    {
        circle.Got();
        circle.gameObject.SetActive(false);
        circle.gameObject.transform.position = new Vector3(-101,-101, 0);
        GlobalHandler.ClickedObject++;
        GlobalHandler.Combo++;
        if (circle.lastParameter >= 0 && circle.lastParameter < GlobalHandler.HitCounts.Length)
        {
            GlobalHandler.HitCounts[circle.lastParameter]++;
        }
        if (GlobalHandler.Combo > GlobalHandler.MaxCombo)
        {
            GlobalHandler.MaxCombo = GlobalHandler.Combo;
        }
        ShowHitText();
    }

    private void HandleStartCircleHit(Circle circle)
    {
        circle.Got();
        GlobalHandler.ClickedObject++;
        GlobalHandler.Combo++;
        if (circle.lastParameter >= 0 && circle.lastParameter < GlobalHandler.HitCounts.Length)
        {
            GlobalHandler.HitCounts[circle.lastParameter]++;
        }
        if (GlobalHandler.Combo > GlobalHandler.MaxCombo)
        {
            GlobalHandler.MaxCombo = GlobalHandler.Combo;
        }
        ShowHitText();
        Debug.Log($"Hit start circle with lastParameter: {circle.lastParameter}");

        SliderInfo? sliderInfo = sliderInfoList.Find(s => s.StartCircle == circle.gameObject);
        if (sliderInfo.HasValue)
        {
            SliderInfo validSliderInfo = sliderInfo.Value;
            if (validSliderInfo.StartCircle != null)
            {
                validSliderInfo.StartCircle.SetActive(false);
                validSliderInfo.SliderRenderer.enabled = true;
                validSliderInfo.Ball.SetActive(true);
            }
        }
    }

    private void HandleEndCircleHit(Circle circle)
    {
        Debug.Log($"Hit end circle with lastParameter: {circle.lastParameter}");
        circle.Got();
        GlobalHandler.ClickedObject++;
        GlobalHandler.Combo++;
        if (circle.lastParameter >= 0 && circle.lastParameter < GlobalHandler.HitCounts.Length)
        {
            GlobalHandler.HitCounts[circle.lastParameter]++;
            Debug.Log($"Updated HitCounts[{circle.lastParameter}] = {GlobalHandler.HitCounts[circle.lastParameter]}");
        }
        else
        {
            Debug.LogWarning($"Invalid lastParameter: {circle.lastParameter}");
        }
        if (GlobalHandler.Combo > GlobalHandler.MaxCombo)
        {
            GlobalHandler.MaxCombo = GlobalHandler.Combo;
        }
        ShowHitText();

        SliderInfo? sliderInfo = sliderInfoList.Find(s => s.EndCircle == circle.gameObject);
        if (sliderInfo.HasValue)
        {
            SliderInfo validSliderInfo = sliderInfo.Value;
            if (validSliderInfo.EndCircle != null)
            {
                // CircleList에서 제거
                if (CircleList.Contains(validSliderInfo.EndCircle))
                    CircleList.Remove(validSliderInfo.EndCircle);

                // 오브젝트 삭제
                Destroy(validSliderInfo.EndCircle);
                Destroy(validSliderInfo.Ball);
                Destroy(validSliderInfo.SliderObject);

                // 슬라이더 정보 제거
                sliderInfoList.Remove(validSliderInfo);
            }
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

    private void ShowHitText()
    {
        hitText.gameObject.SetActive(true);
        GlobalHandler.SuccessfulHits++;
        combo.text = GlobalHandler.Combo.ToString();
        StartCoroutine(HideTextAfterDelay(hitText, 0.5f));
    }

    private void ShowMissText()
    {
        missText.gameObject.SetActive(true);
        miss++;
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
    public void ResetGame()
    {
        // 리스트 초기화
        CircleList.Clear();
        hitObjects.Clear();
        ballInfoList.Clear();
        activeHitObjects.Clear();
        sliderInfoList.Clear();

        // 변수 초기화
        miss = 0;
        timer = 0;
        ArduinoHit = 0;
        startTime = Time.time;

        // GlobalHandler 변수 초기화
        GlobalHandler.ObjCount = 0;
        GlobalHandler.DelayPos = 0;
        GlobalHandler.TotalNotes = 0;
        GlobalHandler.ClickedObject = 0;
        GlobalHandler.ClickedCount = 0;
        GlobalHandler.SuccessfulHits = 0;
        GlobalHandler.Combo = 0;
        GlobalHandler.MaxCombo = 0;
        for (int i = 0; i < GlobalHandler.HitCounts.Length; i++)
        {
            GlobalHandler.HitCounts[i] = 0;
        }

        // UI 초기화
        if (combo != null) combo.text = "0";
        if (hitText != null) hitText.gameObject.SetActive(false);
        if (missText != null) missText.gameObject.SetActive(false);

        // 오디오 초기화
        if (Music != null)
        {
            Music.Stop();
            Music.time = 0;
        }

        // 커서 트레일 제거
        foreach (var trail in cursorTrailInstances.Values)
        {
            if (trail != null) Destroy(trail);
        }
        cursorTrailInstances.Clear();

        // 기존에 생성된 Circle, Slider 등의 게임 오브젝트 제거
        foreach (var obj in CircleList)
        {
            if (obj != null) Destroy(obj);
        }

        // 카메라 위치 초기화 (필요한 경우)
        if (MainCamera != null)
        {
            MainCamera.transform.position = new Vector3(0, 0, -10); // 기본 위치로 설정
        }
    }

    void EndGame()
    {
        try
        {
            Debug.Log("Game Over! Switching to endgame scene.");

            GlobalHandler.SuccessfulHits = GlobalHandler.TotalNotes - miss;

            // 모든 코루틴 중지
            StopAllCoroutines();

            // 모든 Circle 오브젝트 비활성화
            foreach (GameObject circle in CircleList)
            {
                if (circle != null)
                {
                    circle.SetActive(false);
                    Destroy(circle);
                }
            }

            // 리스트 초기화
            CircleList.Clear();
            hitObjects.Clear();
            ballInfoList.Clear();
            activeHitObjects.Clear();
            sliderInfoList.Clear();

            SceneManager.LoadScene("endgame");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in EndGame: {ex.Message}");
        }
    }
}