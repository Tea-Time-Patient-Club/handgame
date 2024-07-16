using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.SceneManagement;
using static GameHandler;

public class GameHandler : MonoBehaviour
{
    [Header("Map")]
    public TextAsset MapFilePath;
    public AudioClip MainMusic;
    public Sprite BackgroundImage;

    [Header("Objects")]
    public GameObject CirclePrefab;
    public GameObject BallPrefab;
    public GameObject Background;
    public GameObject SliderLinePrefab;
    private GameObject arduinoCursor;

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
    public static double timer = 0;

    private List<GameObject> CircleList;
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

    private object hitObjectLock = new object();
    private ArduinoDataHandler arduinoDataHandler;

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

        public SliderInfo(GameObject startCircle, GameObject sliderObject, GameObject endCircle, int beatTime, int endTime, SpriteRenderer renderer, GameObject ball)
        {
            StartCircle = startCircle;
            SliderObject = sliderObject;
            EndCircle = endCircle;
            BeatTime = beatTime;
            EndTime = endTime;
            SliderRenderer = renderer;
            Ball = ball;
        }
    }

    public class HitObject
    {
        public Vector3 Position { get; private set; }
        public int SpawnTime { get; private set; }
        public int BeatTime { get; private set; }

        public HitObject(Vector3 position, int spawnTime, int beatTime)
        {
            Position = position;
            SpawnTime = spawnTime;
            BeatTime = beatTime;
        }
    }

    //////////////////////////////////
    /// 초기화 및 리소스 로딩

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
            LoadResources("Tutorial");
            Debug.LogError("No song file selected.");
        }
        arduinoDataHandler = FindObjectOfType<ArduinoDataHandler>();
        if (arduinoDataHandler == null)
        {
            Debug.LogError("ArduinoDataHandler not found in the scene!");
        }

        MainCamera = Camera.main;
        Music = GameObject.Find("Music Source").GetComponent<AudioSource>();
        Sounds = gameObject.GetComponent<AudioSource>();
        Music.clip = MainMusic;
        pSounds = Sounds;
        pHitSound = HitSound;
        CircleList = new List<GameObject>();

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

    /// //////////////////////////////
    ///맵 읽기 및 처리

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

                    if (lineParams.Length >= 7 && lineParams[5].Contains("|"))
                    {
                        hitObjectLines.Add(line);
                    }
                    else if (int.TryParse(lineParams[3], out _))
                    {
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
                    CreateSlider(circlePosition, lineParams);
                }
                else
                {
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

        GameObject startCircle = Instantiate(CirclePrefab, startPosition, Quaternion.identity);
        Circle startCircleComponent = startCircle.GetComponent<Circle>();
        if (startCircleComponent != null)
        {
            startCircleComponent.Set(startPosition.x, startPosition.y, 0, beatTime - GlobalHandler.ApprRate, lastParameter);
            startCircle.SetActive(false);
        }

        GameObject sliderLineObject = Instantiate(SliderLinePrefab);
        SpriteRenderer sliderRenderer = sliderLineObject.GetComponent<SpriteRenderer>();
        sliderRenderer.enabled = false;

        Vector3 direction = endPosition - startPosition;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        sliderLineObject.transform.position = (startPosition + endPosition) / 2;
        sliderLineObject.transform.rotation = Quaternion.Euler(0, 0, angle);
        sliderLineObject.transform.localScale = new Vector3(length / sliderRenderer.sprite.bounds.size.x, 1.5f, 1);

        float sliderDuration = GlobalHandler.ApprRate;
        int endTime = beatTime + Mathf.RoundToInt(sliderDuration * 2);


        GameObject endCircle = Instantiate(CirclePrefab, endPosition, Quaternion.identity);
        Circle endCircleComponent = endCircle.GetComponent<Circle>();
        if (endCircleComponent != null)
        {
            endCircleComponent.Set(endPosition.x, endPosition.y, 0, endTime - GlobalHandler.ApprRate, lastParameter);
            endCircle.SetActive(false);
        }

        GameObject ball = Instantiate(BallPrefab, startPosition, Quaternion.identity);
        ball.SetActive(false);
        BallInfo ballInfo = new BallInfo(ball, startPosition, endPosition, (float)beatTime, (float)endTime);
        ballInfoList.Add(ballInfo);

        hitObjects.Add(new HitObject(startPosition, beatTime - GlobalHandler.ApprRate, beatTime));
        hitObjects.Add(new HitObject(endPosition, endTime - GlobalHandler.ApprRate, endTime));

        CircleList.Add(startCircle);
        CircleList.Add(sliderLineObject);
        CircleList.Add(endCircle);

        sliderInfoList.Add(new SliderInfo(startCircle, sliderLineObject, endCircle, beatTime, endTime, sliderRenderer, ball));

        GlobalHandler.TotalNotes += 2;
    }

    //////////////////////////
    /// 게임 루프 및 업데이트

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

            bool updated = false;
            lock (hitObjectLock)
            {
                while (GlobalHandler.ObjCount < CircleList.Count)
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
                                activeHitObjects.Add(new HitObject(currentCircle.transform.position, GlobalHandler.DelayPos, GlobalHandler.DelayPos + GlobalHandler.ApprRate));
                                GlobalHandler.ObjCount++;
                                updated = true;
                            }
                            else
                            {
                                break; // 아직 시간이 되지 않았으므로 루프 종료
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
            }

            if (updated)
            {
                UpdateActiveHitObjects();
            }

            HandleInput();
            UpdateBallPositions();

            if (Music != null && Music.clip != null && !Music.isPlaying && Music.time >= Music.clip.length - 0.1f)
            {
                EndGame();
                yield break;
            }

            yield return null;
        }
    }

    private void UpdateSliders()
    {
        List<int> indicesToRemove = new List<int>();

        for (int i = 0; i < sliderInfoList.Count; i++)
        {
            SliderInfo sliderInfo = sliderInfoList[i];

            if (sliderInfo.StartCircle == null || sliderInfo.SliderRenderer == null || sliderInfo.EndCircle == null || sliderInfo.Ball == null)
            {
                indicesToRemove.Add(i);
                continue;
            }

            if (timer >= sliderInfo.BeatTime - GlobalHandler.ApprRate && !sliderInfo.StartCircle.activeSelf)
            {
                sliderInfo.StartCircle.SetActive(true);
            }

            if (timer >= sliderInfo.BeatTime && !sliderInfo.SliderRenderer.enabled)
            {
                sliderInfo.SliderRenderer.enabled = true;
                sliderInfo.Ball.SetActive(true);
            }

            if (timer >= sliderInfo.EndTime - GlobalHandler.ApprRate && !sliderInfo.EndCircle.activeSelf)
            {
                sliderInfo.EndCircle.SetActive(true);
            }

            float t = Mathf.InverseLerp(sliderInfo.BeatTime, sliderInfo.EndTime, (float)timer);
            Vector3 newPos = Vector3.Lerp(sliderInfo.StartCircle.transform.position, sliderInfo.EndCircle.transform.position, t);
            sliderInfo.Ball.transform.position = newPos;

            if (timer > sliderInfo.EndTime + GlobalHandler.HitWindow)
            {
                indicesToRemove.Add(i);
            }
        }

        if (indicesToRemove.Count > 0)
        {
            lock (hitObjectLock)
            {
                for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                {
                    int index = indicesToRemove[i];
                    SliderInfo sliderInfo = sliderInfoList[index];

                    sliderInfo.Ball.SetActive(false);
                    sliderInfo.SliderRenderer.enabled = false;

                    if (CircleList.Contains(sliderInfo.StartCircle)) CircleList.Remove(sliderInfo.StartCircle);
                    sliderInfo.StartCircle.transform.position = new Vector2(-101, -101);
                    sliderInfo.StartCircle.SetActive(false);

                    if (CircleList.Contains(sliderInfo.SliderObject)) CircleList.Remove(sliderInfo.SliderObject);
                    sliderInfo.SliderObject.transform.position = new Vector2(-101, -101);
                    sliderInfo.SliderObject.SetActive(false);

                    if (CircleList.Contains(sliderInfo.EndCircle)) CircleList.Remove(sliderInfo.EndCircle);
                    sliderInfo.EndCircle.transform.position = new Vector2(-101, -101);
                    sliderInfo.EndCircle.SetActive(false);

                    sliderInfoList.RemoveAt(index);
                }
            }
        }
    }

    private void UpdateActiveHitObjects()
    {
        List<int> indicesToRemove = new List<int>();

        for (int i = 0; i < activeHitObjects.Count; i++)
        {
            HitObject hitObject = activeHitObjects[i];
            if (timer > hitObject.BeatTime + GlobalHandler.HitWindow)
            {
                indicesToRemove.Add(i);
            }
        }

        if (indicesToRemove.Count > 0)
        {
            lock (hitObjectLock)
            {
                for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                {
                    int index = indicesToRemove[i];
                    int circleIndex = GlobalHandler.ObjCount - activeHitObjects.Count + index;
                    if (circleIndex >= 0 && circleIndex < CircleList.Count && CircleList[circleIndex] != null)
                    {
                        Circle circleComponent = CircleList[circleIndex].GetComponent<Circle>();
                        if (circleComponent != null)
                        {
                            circleComponent.Remove();
                            ShowMissText();
                            GlobalHandler.Combo = 0;
                            Debug.Log("Miss due to timeout!");
                        }
                    }
                    activeHitObjects.RemoveAt(index);
                }
            }
        }
    }

    private void UpdateBallPositions()
    {
        List<int> indicesToRemove = new List<int>();

        for (int i = 0; i < ballInfoList.Count; i++)
        {
            BallInfo ballInfo = ballInfoList[i];
            float t = Mathf.InverseLerp(ballInfo.StartTime, ballInfo.EndTime, (float)timer);

            if (t >= 1f)
            {
                indicesToRemove.Add(i);
            }
            else
            {
                Vector3 newPos = Vector3.Lerp(ballInfo.StartPos, ballInfo.EndPos, t);
                ballInfo.BallObject.transform.position = newPos;
            }
        }

        if (indicesToRemove.Count > 0)
        {
            lock (hitObjectLock)
            {
                for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                {
                    int index = indicesToRemove[i];
                    Destroy(ballInfoList[index].BallObject);
                    ballInfoList.RemoveAt(index);
                }
            }
        }
    }

    //////////////////////////
    /// 입력 처리

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
    private void RemoveCursorTrail(int fingerId)
    {
        if (cursorTrailInstances.ContainsKey(fingerId))
        {
            Destroy(cursorTrailInstances[fingerId]);
            cursorTrailInstances.Remove(fingerId);
        }
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
    private void PerformCollisionDetection(Vector3 position)
    {
        if (MainCamera == null)
        {
            Debug.LogError("MainCamera is null");
            return;
        }

        List<int> hitObjectIndicesToRemove = new List<int>();

        for (int i = activeHitObjects.Count - 1; i >= 0; i--)
        {
            HitObject hitObject = activeHitObjects[i];
            if (hitObject == null)
            {
                continue;
            }

            float distance = Vector2.Distance(position, hitObject.Position);

            if (distance <= GlobalHandler.HitRadius)
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
                            if (circleComponent != null && circleComponent.Got())
                            {
                                hitCircle.SetActive(false);
                                GlobalHandler.ClickedObject++;
                                GlobalHandler.SuccessfulHits++;
                                GlobalHandler.Combo++;
                                if (GlobalHandler.Combo > GlobalHandler.MaxCombo)
                                {
                                    GlobalHandler.MaxCombo = GlobalHandler.Combo;
                                }
                                ShowHitText();
                                hitObjectIndicesToRemove.Add(i);

                                Debug.Log($"Hit successful! Distance: {distance}, Time difference: {timeDifference}");
                            }
                        }
                    }
                }
            }
        }

        for (int i = hitObjectIndicesToRemove.Count - 1; i >= 0; i--)
        {
            activeHitObjects.RemoveAt(hitObjectIndicesToRemove[i]);
        }

        GlobalHandler.ClickedCount++;

        // Arduino cursor 제거
        if (arduinoCursor != null)
        {
            Destroy(arduinoCursor);
            arduinoCursor = null;
        }
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

    ///////////////////////////////////////////////////////////
    ///아두이노 관련 처리 

    public void OnArduinoDataReceived(string data)
    {
        Debug.Log($"Received Arduino data: {data}");

        if (data.Trim().ToUpper() == "HIT")
        {
            CreateArduinoCursor();
            PerformCollisionDetection(arduinoCursor.transform.position);
        }
    }

    private void CreateArduinoCursor()
    {
        if (arduinoCursor != null)
        {
            Destroy(arduinoCursor);
        }

        HitObject nearestHitObject = null;
        float nearestTimeDifference = float.MaxValue;

        foreach (var hitObject in activeHitObjects)
        {
            float timeDifference = Mathf.Abs((float)timer - hitObject.BeatTime);
            if (timeDifference < nearestTimeDifference)
            {
                nearestTimeDifference = timeDifference;
                nearestHitObject = hitObject;
            }
        }

        if (nearestHitObject != null)
        {
            arduinoCursor = Instantiate(cursorTrailPrefab, nearestHitObject.Position, Quaternion.identity);
        }
    }

    ////////////////////////////////////
    /// UI 및 시각적 효과

    private IEnumerator HideTextAfterDelay(TextMeshPro text, float delay)
    {
        yield return new WaitForSeconds(delay);
        text.gameObject.SetActive(false);
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

    ////////////////////////////////////
    /// 유틸리티 함수
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

    ////////////////////////////////////
    /// 게임 종료
    private void EndGame()
    {
        Debug.Log("Game Over!");

        lock (hitObjectLock)
        {
            CircleList.Clear();
            hitObjects.Clear();
            activeHitObjects.Clear();
            sliderInfoList.Clear();
            ballInfoList.Clear();
        }

        SceneManager.LoadScene("endgame");
    }
}
