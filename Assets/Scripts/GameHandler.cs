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
    public GameObject ArrowPrefab;
    public GameObject BallPrefab;
    public GameObject Background;

    [Header("Map")]
    public AudioClip HitSound;

    [Header("UI")]
    public TextMeshPro hitText;
    public TextMeshPro missText;

    [SerializeField]
    private GameObject cursorTrailPrefab;

    const int SPAWN = -100;
    public static double timer = 0;

    private List<GameObject> CircleList;
    private List<HitObject> hitObjects = new List<HitObject>();
    private List<ArrowInfo> arrowInfoList = new List<ArrowInfo>();
    private List<BallInfo> ballInfoList = new List<BallInfo>();
    private static string[] LineParams;

    private AudioSource Sounds;
    private AudioSource Music;
    public static AudioSource pSounds;
    public static AudioClip pHitSound;

    private Camera MainCamera;
    private Vector3 MousePosition;
    private Ray MainRay;
    private RaycastHit MainHit;

    private Dictionary<int, GameObject> cursorTrailInstances = new Dictionary<int, GameObject>();
    private List<HitObject> activeHitObjects = new List<HitObject>();

    private struct ArrowInfo
    {
        public GameObject ArrowObject;
        public double DisableTime;

        public ArrowInfo(GameObject arrowObject, double disableTime)
        {
            ArrowObject = arrowObject;
            DisableTime = disableTime;
        }
    }

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

    void ReadCircles(string mapContent)
    {
        using (StringReader reader = new StringReader(mapContent))
        {
            string line;
            bool hitObjectsSection = false;
            int totalLines = 0;
            int circleNumber = 0;
            List<string> hitObjectLines = new List<string>();

            while ((line = reader.ReadLine()) != null)
            {
                if (hitObjectsSection)
                {
                    hitObjectLines.Add(line);
                    totalLines++;
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

            GlobalHandler.TotalNotes = totalLines;
            GlobalHandler.HitCounts = new int[totalLines];

            int foreOrder = totalLines + 2;
            int backOrder = totalLines + 1;
            int approachOrder = totalLines;
            float zIndex = -(float.Parse("0." + totalLines.ToString().PadLeft(3, '0')));

            for (int i = 0; i < hitObjectLines.Count; i++)
            {
                line = hitObjectLines[i];
                string[] lineParams = line.Split(',');

                if (lineParams.Length < 3)
                {
                    Debug.LogWarning($"Invalid line format: {line}");
                    continue;
                }

                GameObject circleObject = Instantiate(CirclePrefab, new Vector2(SPAWN, SPAWN), Quaternion.identity);
                Circle circleComponent = circleObject.GetComponent<Circle>();

                if (circleComponent != null)
                {
                    circleComponent.Fore.sortingOrder = foreOrder;
                    circleComponent.Back.sortingOrder = backOrder;
                    circleComponent.Appr.sortingOrder = approachOrder;
                }

                circleObject.transform.localPosition += new Vector3(0, 0, zIndex);
                circleObject.transform.SetAsFirstSibling();

                foreOrder--;
                backOrder--;
                approachOrder--;
                zIndex += 0.01f;

                int x = int.Parse(lineParams[0]);
                int y = 384 - int.Parse(lineParams[1]);
                int beatTime = int.Parse(lineParams[2]);

                Vector3 circlePosition = CalculateCirclePosition(x, y);

                if (lineParams.Length > 6 && !string.IsNullOrEmpty(lineParams[6]))
                {
                    circleNumber = int.Parse(lineParams[6]);
                }
                else
                {
                    circleNumber = 0;
                }

                if (circleComponent != null)
                {
                    circleComponent.Set(circlePosition.x, circlePosition.y, zIndex, beatTime - GlobalHandler.ApprRate, circleNumber);
                    GlobalHandler.HitCounts[circleNumber]++;
                }

                hitObjects.Add(new HitObject(circlePosition, beatTime - GlobalHandler.ApprRate, beatTime));
                CircleList.Add(circleObject);
            }

            Debug.Log($"Total circles created: {CircleList.Count}");
        }

        GameStart();
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

        return MainCamera.ScreenToWorldPoint(new Vector3(newValueX, newValueY, 0));
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

            if (GlobalHandler.ObjCount < CircleList.Count)
            {
                Circle currentCircle = CircleList[GlobalHandler.ObjCount].GetComponent<Circle>();
                GlobalHandler.DelayPos = currentCircle.PosA;

                if (timer >= GlobalHandler.DelayPos)
                {
                    currentCircle.Spawn();
                    activeHitObjects.Add(new HitObject(currentCircle.transform.position, GlobalHandler.DelayPos, GlobalHandler.DelayPos + GlobalHandler.ApprRate));

                    if (GlobalHandler.ObjCount % 2 == 0 && GlobalHandler.ObjCount + 1 < CircleList.Count)
                    {
                        GameObject newArrow = CreateArrow(
                            hitObjects[GlobalHandler.ObjCount].Position,
                            hitObjects[GlobalHandler.ObjCount + 1].Position,
                            hitObjects[GlobalHandler.ObjCount].BeatTime,
                            hitObjects[GlobalHandler.ObjCount + 1].BeatTime
                        );

                        double disableTime = hitObjects[GlobalHandler.ObjCount + 1].SpawnTime + GlobalHandler.ApprRate;
                        arrowInfoList.Add(new ArrowInfo(newArrow, disableTime));
                    }

                    GlobalHandler.ObjCount++;
                }

                for (int i = activeHitObjects.Count - 1; i >= 0; i--)
                {
                    HitObject hitObject = activeHitObjects[i];
                    if (timer > hitObject.BeatTime + GlobalHandler.HitWindow - 0.1f)
                    {
                        CircleList[i].SetActive(false);
                        ShowMissText();
                        GlobalHandler.Combo = 0;
                        Debug.Log("Miss due to timeout!");
                        activeHitObjects.RemoveAt(i);
                    }
                }

                HandleInput();
                CheckArrowDisableTimers();
                UpdateBallPositions();
            }

            if (Music != null && Music.clip != null && !Music.isPlaying && Music.time >= Music.clip.length - 0.1f)
            {
                EndGame();
            }

            yield return null;
        }
    }

    private void HandleInput()
    {
        foreach (Touch touch in Input.touches)
        {
            Vector2 touchPosition = touch.position;
            if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved)
            {
                CreateOrUpdateCursorTrail(touch.fingerId, touchPosition);
                PerformCollisionDetection(touchPosition);
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                RemoveCursorTrail(touch.fingerId);
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

    private void PerformCollisionDetection(Vector2 position)
    {
        Vector3 worldPosition = MainCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, MainCamera.nearClipPlane));
        bool hitDetected = false;

        for (int i = activeHitObjects.Count - 1; i >= 0; i--)
        {
            HitObject hitObject = activeHitObjects[i];
            float distance = Vector2.Distance(worldPosition, hitObject.Position);
            float hitRadius = 1f;

            if (distance <= hitRadius)
            {
                float timeDifference = Mathf.Abs((float)timer - hitObject.BeatTime);
                if (timeDifference <= GlobalHandler.HitWindow)
                {
                    CircleList[GlobalHandler.ObjCount - activeHitObjects.Count + i].GetComponent<Circle>().Got();
                    CircleList[GlobalHandler.ObjCount - activeHitObjects.Count + i].SetActive(false);
                    GlobalHandler.ClickedObject++;
                    GlobalHandler.SuccessfulHits++;
                    GlobalHandler.Combo++;
                    if (GlobalHandler.Combo > GlobalHandler.MaxCombo)
                    {
                        GlobalHandler.MaxCombo = GlobalHandler.Combo;
                    }
                    ShowHitText();
                    hitDetected = true;
                    activeHitObjects.RemoveAt(i);
                    
                    Debug.Log($"Hit successful! Distance: {distance}, Time difference: {timeDifference}");
                    break;
                }
            }
        }
        GlobalHandler.ClickedCount++;
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

    private void CheckArrowDisableTimers()
    {
        for (int i = arrowInfoList.Count - 1; i >= 0; i--)
        {
            if (timer >= arrowInfoList[i].DisableTime)
            {
                DisableArrow(arrowInfoList[i].ArrowObject, i);
                arrowInfoList.RemoveAt(i);
            }
        }
    }

    private GameObject CreateArrow(Vector3 startPos, Vector3 endPos, int startBeatTime, int endBeatTime)
    {
        float arrowZ = -5.0f;
        Vector3 midPoint = (startPos + endPos) / 2;
        midPoint.z = arrowZ;

        GameObject arrowObject = Instantiate(ArrowPrefab, midPoint, Quaternion.identity);

        float distance = Vector3.Distance(startPos, endPos);
        Vector3 originalScale = arrowObject.transform.localScale;
        arrowObject.transform.localScale = new Vector3(distance / originalScale.x, originalScale.y, originalScale.z);

        float angle = Mathf.Atan2(endPos.y - startPos.y, endPos.x - startPos.x) * Mathf.Rad2Deg;
        arrowObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        CreateAndMoveBall(startPos, endPos, startBeatTime, endBeatTime);

        return arrowObject;
    }

    private void DisableArrow(GameObject arrowObject, int index)
    {
        if (arrowObject != null && arrowObject.GetComponent<SpriteRenderer>() != null)
        {
            arrowObject.GetComponent<SpriteRenderer>().enabled = false;
        }
        else
        {
            Debug.LogWarning($"Arrow at index {index} is null or missing SpriteRenderer.");
        }
    }

    private void CreateAndMoveBall(Vector3 startPos, Vector3 endPos, int startBeatTime, int endBeatTime)
    {
        float ballZ = -4.9f;
        startPos.z = ballZ;
        endPos.z = ballZ;

        GameObject ball = Instantiate(BallPrefab, startPos, Quaternion.identity);
        float startTime = (float)timer;
        float endTime = startTime + (endBeatTime - startBeatTime) + GlobalHandler.ApprRate;

        ballInfoList.Add(new BallInfo(ball, startPos, endPos, startTime, endTime));
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
        arrowInfoList.Clear();
        ballInfoList.Clear();
        activeHitObjects.Clear();

        SceneManager.LoadScene("endgame");
    }
}
