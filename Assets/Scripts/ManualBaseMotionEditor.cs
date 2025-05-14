using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TS.DoubleSlider;
// 移除 UnityEngine.InputSystem 引用
// using UnityEngine.InputSystem;

public class ManualBaseMotionEditor : MonoBehaviour
{

    [Header("音樂分析視覺化")]
    public RectTransform timelineContainer; // 時間軸容器，用於放置視覺化元素
    public GameObject beatMarkerPrefab; // 節拍標記 Prefab
    public GameObject rmsMarkerPrefab; // 音量強度標記 Prefab
    public Color beatColor = new Color(1, 0.5f, 0.5f, 0.8f); // 節拍標記顏色
    public Color rmsColor = new Color(0.5f, 0.7f, 1, 0.6f); // 音量強度標記顏色

    private List<GameObject> beatMarkers = new List<GameObject>();
    private List<GameObject> rmsMarkers = new List<GameObject>();


    private Button play_button, pause_button, stop_button, save_button, reset_button;
    //public string danceId = "demo";
    public Image keyFrames;
    public string baseUrl = "miislab.pagekite.me"; //"http://140.118.162.43:8443"
    private DanceDataManager dataManager;
    private DoubleSlider doubleSlider;
    private Slider timeSlider;
    private GameObject jointsGroup;

    // 用於記錄目前選擇的幀範圍
    private int selectedMinFrame = 0;
    private int selectedMaxFrame = 0;
    private Dictionary<int, Vector3> startFrameEditedJoints = new Dictionary<int, Vector3>(); // 記錄開始幀被編輯的關節 <關節索引, 編輯後位置>
    private Dictionary<int, Vector3> endFrameEditedJoints = new Dictionary<int, Vector3>(); // 記錄結束幀被編輯的關節 <關節索引, 編輯後位置>
    private bool isEditingStartFrame = true; // 是否正在編輯開始幀
    private bool hasEditedStartFrame = false; // 是否已編輯開始幀

    // 用於記錄目前選擇的關節
    private int selectedJointIndex = -1;
    private Button selectedJointButton = null;
    private string selectedJointName = "";

    private string msg;

    // 關節按鈕列表
    private List<JointButtonInfo> jointButtons = new List<JointButtonInfo>();

    // 關節按鈕顏色
    private Color defaultButtonColor = new Color(1f, 1f, 1f, 1f); // 白色背景
    private Color selectedButtonColor = new Color(1f, 0f, 0f, 1f); // 紅色背景
    private Color buttonBorderColor = new Color(1f, 1f, 0f, 1f); // 黃色邊框

    private GameObject currentSphere; // 當前 Joint 上的 Sphere
    public GameObject spherePrefab; // 用於拖動 Joint 的 Sphere 預製體
    private Renderer currentSphereRenderer; // Sphere 的 Renderer
    public float JointScaleSize = .2f; // 關節球體大小
    public float controllerSensitivity = 3.0f; // 控制器靈敏度


    // 控制器控制相關
    private bool isControllingJoint = false;
    private Vector3 originalJointPosition;
    private Vector3 initialControllerPosition; // 初始控制器位置

    // 控制器參考
    private Transform rightControllerTransform;
    private Transform leftControllerTransform;
    private Transform activeControllerTransform; // 當前使用的控制器

    public TextMeshProUGUI debug;



    // 關節按鈕訊息類
    [System.Serializable]
    public class JointButtonInfo
    {
        public string jointName;
        public int jointIndex;
        public Button button;
        public Vector3 originalPosition; // 用於儲存原始位置
        public Vector2 pos;
    }

    public void EditSelectedRangePose(int jointIndex, Vector3 startValue, Vector3 endValue)
    {
        if (selectedMinFrame <= selectedMaxFrame)
        {
            dataManager.EditPose(jointIndex, selectedMinFrame, startValue, selectedMaxFrame, endValue);
            Debug.Log($"幀範圍無效：開始={selectedMinFrame}，結束={selectedMaxFrame}");
        }
    }

    public void JumpToSelectedRangeStart()
    {
        dataManager.JumpToFrame(selectedMinFrame);
    }

    public void PlaySelectedRange()
    {
        dataManager.SetPlayRange(selectedMinFrame, selectedMaxFrame, true);
        dataManager.JumpToFrame(selectedMinFrame);
        dataManager.PlayWithAudio();
    }

    void Start()
    {
        InitSettings();
        InitDebug();
        GetComponents();
        GetGameObject();
        InitializeUIEvents();
        LoadDanceData();
        SetupDoubleSlider();
        SetupTimeSlider();
        CreateJointButtons();
        InitializeControllers();
        InitializeSphere();// 訂閱資料載入完成事件
        if (dataManager != null)
        {
            dataManager.OnDataLoaded += OnDataLoaded;
        }
        //dataManager.PlayWithAudio();
    }
    private void InitializeSphere()
    {
        // 如果沒有指定球體預製體，創建一個
        if (spherePrefab == null)
        {
            spherePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spherePrefab.name = "JointSphere";

            // 設置材質
            Renderer renderer = spherePrefab.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.white;

            // 隱藏預製體
            spherePrefab.SetActive(false);
        }
    }
    private void OnDataLoaded(string danceId, bool success)
    {
        if (success)
        {
            Debug.Log($"資料 {danceId} 載入成功，更新 UI");

            // 更新時間滑塊
            if (timeSlider != null)
            {
                timeSlider.minValue = 0;
                timeSlider.maxValue = dataManager.TotalFrames - 1;
                timeSlider.value = 0;
            }

            // 更新範圍滑塊
            if (doubleSlider != null)
            {
                doubleSlider.Setup(0, dataManager.TotalFrames - 1, 0, dataManager.TotalFrames - 1);
                selectedMinFrame = 0;
                selectedMaxFrame = dataManager.TotalFrames - 1;
            }
            dataManager.PlayWithAudio();

            // 創建關節按鈕
            CreateJointButtons();
            CreateMusicVisualization();

        }
    }
    private void CreateMusicVisualization()
    {
        // 清除現有的標記
        ClearMusicVisualization();

        if (dataManager == null || dataManager.DanceAudio == null ||
            timelineContainer == null || beatMarkerPrefab == null || rmsMarkerPrefab == null)
        {
            Debug.LogWarning("無法創建音樂分析視覺化：缺少必要組件");
            return;
        }

        float totalDuration = dataManager.DanceAudio.length;
        float containerWidth = timelineContainer.rect.width;

        // 創建節拍標記
        CreateBeatMarkers(totalDuration, containerWidth);

        // 創建音量強度標記
        CreateRmsMarkers(totalDuration, containerWidth);
    }

    private void CreateBeatMarkers(float totalDuration, float containerWidth)
    {
        if (dataManager.BeatTimes == null || dataManager.BeatTimes.Count == 0)
        {
            Debug.LogWarning("沒有節拍數據可視覺化");
            return;
        }

        foreach (float beatTime in dataManager.BeatTimes)
        {
            // 計算位置（基於時間在總時長中的比例）
            // 將X座標從0開始計算，而不是從容器中心
            float normalizedPosition = beatTime / totalDuration; // 0到1之間的值
            float xPosition = normalizedPosition * containerWidth - (containerWidth * 0.5f);

            // 創建標記
            GameObject marker = Instantiate(beatMarkerPrefab, timelineContainer);
            RectTransform rt = marker.GetComponent<RectTransform>();

            // 設置錨點在底部中心，這樣線條會從底部延伸向上
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);

            rt.anchoredPosition = new Vector2(xPosition, 0);

            // 設置顏色
            Image image = marker.GetComponent<Image>();
            if (image != null)
            {
                image.color = beatColor;
            }

            beatMarkers.Add(marker);
        }
    }
    private void CreateRmsMarkers(float totalDuration, float containerWidth)
    {
        if (dataManager.RmsValues == null || dataManager.RmsValues.Count == 0)
        {
            Debug.LogWarning("沒有RMS數據可視覺化");
            return;
        }

        // 找出最大RMS值用於標準化
        float maxRms = 0;
        foreach (float rms in dataManager.RmsValues)
        {
            maxRms = Mathf.Max(maxRms, rms);
        }

        // 計算每個RMS標記的寬度
        float markerSpacing = containerWidth / dataManager.RmsValues.Count;
        float maxHeight = timelineContainer.rect.height * 0.5f;

        for (int i = 0; i < dataManager.RmsValues.Count; i++)
        {
            float rmsValue = dataManager.RmsValues[i];
            float normalizedRms = maxRms > 0 ? rmsValue / maxRms : 0;

            // 計算位置和高度
            // 從容器左側開始計算X位置，考慮到容器的錨點在中心
            float normalizedPosition = (float)i / dataManager.RmsValues.Count;
            float xPosition = normalizedPosition * containerWidth - (containerWidth * 0.5f);
            float height = normalizedRms * maxHeight;

            // 創建標記
            GameObject marker = Instantiate(rmsMarkerPrefab, timelineContainer);
            RectTransform rt = marker.GetComponent<RectTransform>();

            // 設置錨點在底部中心
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);

            // 設置位置在底部，X位置根據時間軸
            rt.anchoredPosition = new Vector2(xPosition, 0);

            // 設置寬度和高度
            rt.sizeDelta = new Vector2(markerSpacing * 0.8f, height);

            // 設置顏色
            Image image = marker.GetComponent<Image>();
            if (image != null)
            {
                image.color = rmsColor;
            }

            rmsMarkers.Add(marker);
        }
    }


    private void ClearMusicVisualization()
    {
        // 清除節拍標記
        foreach (var marker in beatMarkers)
        {
            Destroy(marker);
        }
        beatMarkers.Clear();

        // 清除RMS標記
        foreach (var marker in rmsMarkers)
        {
            Destroy(marker);
        }
        rmsMarkers.Clear();
    }
    // 初始化控制器引用
    private void InitializeControllers()
    {
        // 尋找左右控制器 - 使用 OVRCameraRig 下的控制器節點
        GameObject ovrRig = GameObject.Find("OVRCameraRig");
        if (ovrRig != null)
        {
            Transform trackingSpace = ovrRig.transform.Find("TrackingSpace");
            if (trackingSpace != null)
            {
                rightControllerTransform = trackingSpace.Find("RightHandAnchor");
                leftControllerTransform = trackingSpace.Find("LeftHandAnchor");

                if (rightControllerTransform != null)
                    Debug.Log("找到右控制器: " + rightControllerTransform.name);
                else
                    Debug.LogWarning("找不到右控制器物件");

                if (leftControllerTransform != null)
                    Debug.Log("找到左控制器: " + leftControllerTransform.name);
                else
                    Debug.LogWarning("找不到左控制器物件");
            }
            else
            {
                Debug.LogWarning("找不到 TrackingSpace");
            }
        }
        else
        {
            Debug.LogWarning("找不到 OVRCameraRig");

            // 嘗試直接找控制器
            rightControllerTransform = GameObject.Find("RightHandAnchor")?.transform;
            leftControllerTransform = GameObject.Find("LeftHandAnchor")?.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDebug($"current frame: {dataManager.CurrentFrameIndex}\n"
        + $"range from {selectedMinFrame} to {selectedMaxFrame}\n"
        + $"status: {selectedJointIndex}\n"
        + $"selected joint: {(selectedJointIndex >= 0 ? GetJointNameByIndex(selectedJointIndex) : "none")}\n"
        + $"selectedJointIndex: {(dataManager.IsPlaying ? "playing" : "pause")}\n"
        + $"isEditingStartFrame: {isEditingStartFrame}\n"
        + $"hasEditedStartFrame: {hasEditedStartFrame}\n"
        + $"{msg}");

        // 處理控制器輸入
        HandleControllerInput();
    }

    void InitDebug()
    {
        // debug = GameObject.Find("Debug").GetComponent<TextMeshPro>();
    }

    private void InitSettings()
    {
        HttpService.Instance.SetBaseUrl(baseUrl);
    }

    void UpdateDebug(string text)
    {
        if (debug != null) { debug.text = text; }
    }

    private void GetGameObject()
    {
        jointsGroup = GameObject.Find("JointsGroup");
        play_button = GameObject.Find("PlayButton").GetComponent<Button>();
        pause_button = GameObject.Find("PauseButton").GetComponent<Button>();
        stop_button = GameObject.Find("StopButton").GetComponent<Button>();
        doubleSlider = GameObject.Find("DoubleSlider").GetComponent<DoubleSlider>();
        timeSlider = GameObject.Find("TimeSlider").GetComponent<Slider>();
        reset_button = GameObject.Find("ResetButton").GetComponent<Button>();
        save_button = GameObject.Find("SaveButton").GetComponent<Button>();
    }

    private void GetComponents()
    {
        if (dataManager == null)
        {
            dataManager = FindObjectOfType<DanceDataManager>();
            if (dataManager == null)
            {
                dataManager = gameObject.AddComponent<DanceDataManager>();
            }
        }
    }

    private void InitializeUIEvents()
    {
        play_button.onClick.AddListener(() =>
        {
            dataManager.SetPlayRange(selectedMinFrame, selectedMaxFrame, true);
            dataManager.PlayWithAudio();
        });
        pause_button.onClick.AddListener(() => { dataManager.PauseWithAudio(); });
        stop_button.onClick.AddListener(() => { dataManager.Test(); });

        // 添加 Reset 按鈕事件
        reset_button.onClick.AddListener(ResetEditingState);

        // 添加 Save 按鈕事件
        save_button.onClick.AddListener(SaveEditedPose);

        // 訂閱 DanceDataManager 事件
        dataManager.OnFrameChanged += OnFrameChanged;
        dataManager.OnPlayStateChanged += OnPlayStateChanged;
    }
    private void ResetEditingState()
    {
        // 暫停播放
        dataManager.PauseWithAudio();

        // 清空編輯記錄
        startFrameEditedJoints.Clear();
        endFrameEditedJoints.Clear();
        isEditingStartFrame = true;
        hasEditedStartFrame = false;

        // 重置當前幀的所有關節位置
        int currentFrame = dataManager.CurrentFrameIndex;

        // 使用 DanceDataManager 中的方法重置當前幀
        dataManager.ResetTempFrameData(currentFrame);

        // 更新顯示
        dataManager.UpdateCubeParentJointPositions(currentFrame);

        // 取消關節選擇
        UnselectCurrentJoint();

    }

    // 保存編輯的姿勢
    // 保存編輯的姿勢
    private void SaveEditedPose()
    {
        // 獲取開始幀和結束幀
        int startFrame = selectedMinFrame;
        int endFrame = selectedMaxFrame;

        // 確保開始幀和結束幀不同
        if (startFrame == endFrame)
        {
            msg = "start and end frame are the same, no need to interpolate";
            return;
        }

        bool hasEdits = false;

        // 遍歷所有關節
        for (int jointIndex = 0; jointIndex < dataManager.TempFramesData[startFrame].Count; jointIndex++)
        {
            // 獲取開始幀和結束幀的關節位置
            Vector3 startPos = dataManager.TempFramesData[startFrame][jointIndex];
            Vector3 endPos = dataManager.TempFramesData[endFrame][jointIndex];

            // 獲取原始數據中的位置
            Vector3 originalStartPos = dataManager.FramesData[startFrame][jointIndex];
            Vector3 originalEndPos = dataManager.FramesData[endFrame][jointIndex];

            // 檢查是否有被編輯過（比較與原始數據的差異）
            bool startFrameEdited = Vector3.Distance(startPos, originalStartPos) > 0.001f;
            bool endFrameEdited = Vector3.Distance(endPos, originalEndPos) > 0.001f;

            // 如果開始幀或結束幀的關節被編輯過，則進行插值
            if (startFrameEdited || endFrameEdited)
            {
                // 應用線性插值
                dataManager.EditPose(jointIndex, startFrame, startPos, endFrame, endPos);
                Debug.Log($"已應用關節 {GetJointNameByIndex(jointIndex)} 從幀 {startFrame} 到 {endFrame} 的編輯");
                hasEdits = true;
            }
        }

        if (hasEdits)
        {
            msg = "save edited pose";
            UnselectCurrentJoint();
        }
        else { msg = "no edits to save"; }
    }
    private void UnselectCurrentJoint()
    {
        // 重置選中的關節索引
        selectedJointIndex = -1;
        selectedJointName = "";

        // 重置按鈕顏色
        if (selectedJointButton != null)
        {
            Image bgImage = selectedJointButton.transform.Find("Background").GetComponent<Image>();
            bgImage.color = defaultButtonColor;
            selectedJointButton = null;
        }

        // 移除球體
        if (currentSphere != null)
        {
            Destroy(currentSphere);
            currentSphere = null;
            currentSphereRenderer = null;
        }

        // 重置控制狀態
        isControllingJoint = false;
        activeControllerTransform = null;
    }
    private void LoadDanceData()
    {
        dataManager.CubeParent = GameObject.Find("SkeletonCubeParent").transform;
        dataManager.LoadById(dataManager.CurrentDanceId);
        Debug.Log("PlayID: " + dataManager.CurrentDanceId);
    }

    private void SetupDoubleSlider()
    {
        if (doubleSlider != null && dataManager != null && dataManager.TotalFrames > 0)
        {
            // 設定滑桿的範圍為 0 到總幀數-1，初始值也設為相同範圍
            doubleSlider.Setup(0, dataManager.TotalFrames - 1, 0, dataManager.TotalFrames - 1);

            // 初始化選定的幀範圍
            selectedMinFrame = 0;
            selectedMaxFrame = dataManager.TotalFrames - 1;

            // 為滑桿值變化添加監聽器
            doubleSlider.OnValueChanged.AddListener(OnSliderValueChanged);

            // 設定滑桿使用整數值
            doubleSlider.WholeNumbers = true;
            // 確認初始化後的值

            Debug.Log($"初始化滑桿範圍：{selectedMinFrame} 到 {selectedMaxFrame}，總幀數：{dataManager.TotalFrames}");
        }
        else
        {
            Debug.LogError($"滑桿初始化失敗：doubleSlider={doubleSlider}，dataManager={dataManager}，TotalFrames={dataManager?.TotalFrames}");
        }
    }
    private void SetupTimeSlider()
    {
        if (timeSlider != null && dataManager != null && dataManager.TotalFrames > 0)
        {
            // 設置時間滑桿的範圍
            timeSlider.minValue = 0;
            timeSlider.maxValue = dataManager.TotalFrames - 1;
            timeSlider.value = 0;

            // 添加滑桿值變化事件監聽器
            timeSlider.onValueChanged.AddListener(OnTimeSliderValueChanged);

            // 設置滑桿使用整數值
            timeSlider.wholeNumbers = true;

            Debug.Log($"初始化時間滑桿：範圍 0 到 {dataManager.TotalFrames - 1}");
        }
        else
        {
            Debug.LogError($"時間滑桿初始化失敗：timeSlider={timeSlider}，dataManager={dataManager}，TotalFrames={dataManager?.TotalFrames}");
        }
    }
    private void OnTimeSliderValueChanged(float value)
    {
        // 暫停播放
        dataManager.PauseWithAudio();

        // 跳轉到指定幀
        int frameIndex = Mathf.RoundToInt(value);
        dataManager.JumpToFrame(frameIndex);
    }
    // 創建關節按鈕
    private void CreateJointButtons()
    {
        if (jointsGroup == null)
        {
            Debug.LogError("找不到 JointsGroup 物件");
            return;
        }

        // 清除現有按鈕
        foreach (Transform child in jointsGroup.transform)
        {
            Destroy(child.gameObject);
        }
        jointButtons.Clear();

        // 定義要創建的關節按鈕
        JointButtonInfo[] jointInfos = new JointButtonInfo[]
        {
            new JointButtonInfo { pos=new Vector2(34f,-12-5f) ,jointName = "lShldrBend", jointIndex = dataManager.GetJointIndex("lShldrBend") },
            new JointButtonInfo { pos=new Vector2(-34f,-12-5f) ,jointName = "rShldrBend", jointIndex = dataManager.GetJointIndex("rShldrBend") },
            new JointButtonInfo { pos=new Vector2(0,-80) ,jointName = "hip", jointIndex = dataManager.GetJointIndex("hip") },
            new JointButtonInfo { pos=new Vector2(45,-113) ,jointName = "lHand", jointIndex = dataManager.GetJointIndex("lHand") },
            new JointButtonInfo { pos=new Vector2(-45,-113) ,jointName = "rHand", jointIndex = dataManager.GetJointIndex("rHand") },
            new JointButtonInfo { pos=new Vector2(-47,-260) ,jointName = "lFoot", jointIndex = dataManager.GetJointIndex("lFoot") },
            new JointButtonInfo { pos=new Vector2(47,-260) ,jointName = "rFoot", jointIndex = dataManager.GetJointIndex("rFoot") },
            new JointButtonInfo { pos=new Vector2(-33,-165) ,jointName = "lShin", jointIndex = dataManager.GetJointIndex("lShin") },
            new JointButtonInfo { pos=new Vector2(33,-165) ,jointName = "rShin", jointIndex = dataManager.GetJointIndex("rShin") }
        };

        // 創建圓形精靈
        Sprite circleSprite = CreateCircleSprite(30);

        // 創建按鈕
        for (int i = 0; i < jointInfos.Length; i++)
        {
            GameObject buttonObj = new GameObject(jointInfos[i].jointName + "Button");
            buttonObj.transform.SetParent(jointsGroup.transform, false);

            // 添加 RectTransform 組件
            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(30, 30);
            rectTransform.anchoredPosition = jointInfos[i].pos;

            // 先創建邊框
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(buttonObj.transform, false);

            RectTransform borderRectTransform = borderObj.AddComponent<RectTransform>();
            borderRectTransform.anchorMin = new Vector2(0, 0);
            borderRectTransform.anchorMax = new Vector2(1, 1);
            borderRectTransform.offsetMin = new Vector2(-3, -3);
            borderRectTransform.offsetMax = new Vector2(3, 3);

            Image borderImage = borderObj.AddComponent<Image>();
            borderImage.sprite = circleSprite;
            borderImage.color = buttonBorderColor; // 黃色邊框

            // 再創建按鈕背景
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(buttonObj.transform, false);

            RectTransform bgRectTransform = bgObj.AddComponent<RectTransform>();
            bgRectTransform.anchorMin = new Vector2(0, 0);
            bgRectTransform.anchorMax = new Vector2(1, 1);
            bgRectTransform.offsetMin = Vector2.zero;
            bgRectTransform.offsetMax = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.sprite = circleSprite;
            bgImage.color = defaultButtonColor; // 白色背景

            // 添加按鈕組件
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = bgImage; // 使用背景圖像作為按鈕的目標圖像

            // 設置按鈕顏色過渡
            ColorBlock colors = button.colors;
            colors.normalColor = defaultButtonColor;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = selectedButtonColor;
            button.colors = colors;

            // 保存按鈕訊息
            jointInfos[i].button = button;
            jointButtons.Add(jointInfos[i]);

            // 添加點擊事件
            int index = i;
            button.onClick.AddListener(() => OnJointButtonClicked(index));
        }
    }

    private Sprite CreateCircleSprite(int size)
    {
        // 創建一個正方形紋理
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        // 計算中心點和半徑
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        // 填充紋理
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                // 如果點在圓內，設為白色，否則設為透明
                if (distance <= radius)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }

    // 關節按鈕點擊事件
    private void OnJointButtonClicked(int buttonIndex)
    {
        // 暫停播放
        dataManager.PauseWithAudio();

        JointButtonInfo jointInfo = jointButtons[buttonIndex];

        // 檢查是否點擊了已選中的關節
        if (selectedJointIndex == jointInfo.jointIndex)
        {
            // 如果是同一個關節，則取消選擇
            UnselectCurrentJoint();
            Debug.Log($"取消選擇關節: {jointInfo.jointName}");
            return;
        }

        // 重置之前選擇的按鈕顏色
        if (selectedJointButton != null)
        {
            // 找到背景圖像並重置顏色
            Image bgImage = selectedJointButton.transform.Find("Background").GetComponent<Image>();
            bgImage.color = defaultButtonColor;
        }

        // 設置新選擇的關節和按鈕
        selectedJointIndex = jointInfo.jointIndex;
        selectedJointName = jointInfo.jointName;
        selectedJointButton = jointInfo.button;

        // 改變選中按鈕的顏色
        Image newBgImage = selectedJointButton.transform.Find("Background").GetComponent<Image>();
        newBgImage.color = selectedButtonColor;

        // 儲存當前選中關節的原始位置
        if (selectedJointIndex >= 0)
        {
            // 獲取當前幀中關節的位置
            List<Vector3> framePositions = dataManager.GetScaledJointPositions(dataManager.CurrentFrameIndex);
            originalJointPosition = framePositions[selectedJointIndex];
        }

        // 添加球體顯示 - 先移除舊的球體
        if (currentSphere != null)
        {
            Destroy(currentSphere);
        }

        // 創建新的球體標記選中的關節
        Transform jointTransform = FindJointTransform(selectedJointName);
        if (jointTransform != null && spherePrefab != null)
        {
            currentSphere = Instantiate(spherePrefab, jointTransform);
            currentSphere.transform.localPosition = Vector3.zero;
            currentSphere.transform.localScale = Vector3.one * JointScaleSize;
            currentSphereRenderer = currentSphere.GetComponent<Renderer>();

            // 如果球體沒有碰撞器，添加一個
            if (currentSphere.GetComponent<Collider>() == null)
            {
                currentSphere.AddComponent<SphereCollider>();
            }

            // 確保球體是激活的
            currentSphere.SetActive(true);
        }

        Debug.Log($"選擇了關節: {jointInfo.jointName}, 索引: {selectedJointIndex}");
    }

    private Transform FindJointTransform(string jointName)
    {
        if (dataManager.CubeParent != null)
        {
            return dataManager.CubeParent.Find(jointName);
        }
        return null;
    }

    // 獲取關節名稱
    private string GetJointNameByIndex(int jointIndex)
    {
        foreach (var jointInfo in jointButtons)
        {
            if (jointInfo.jointIndex == jointIndex)
            {
                return jointInfo.jointName;
            }
        }
        return "未知關節";
    }

    // 編輯選中的關節
    public void EditSelectedJoint(Vector3 startValue, Vector3 endValue)
    {
        if (selectedJointIndex >= 0 && selectedMinFrame <= selectedMaxFrame)
        {
            EditSelectedRangePose(selectedJointIndex, startValue, endValue);
        }
        else
        {
            Debug.LogWarning("請先選擇一個關節");
        }
    }

    // 處理控制器輸入
    private void HandleControllerInput()
    {
        // 檢查是否有選中的關節
        if (selectedJointIndex < 0 || dataManager.IsPlaying)
            return;

        // 檢測按鈕按下狀態 - 使用 OVRInput 檢測握把按鈕
        bool leftGripPressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
        bool rightGripPressed = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

        // 如果按下握柄按鈕，開始控制關節
        if ((rightGripPressed || leftGripPressed) && !isControllingJoint)
        {
            // 決定使用哪個控制器
            if (rightGripPressed)
            {
                activeControllerTransform = rightControllerTransform;
                Debug.Log("使用右控制器控制關節");
            }
            else
            {
                activeControllerTransform = leftControllerTransform;
                Debug.Log("使用左控制器控制關節");
            }

            StartControllingJoint();
        }
        // 如果鬆開握柄按鈕，停止控制關節
        else if (!rightGripPressed && !leftGripPressed && isControllingJoint)
        {
            StopControllingJoint();
        }

        // 如果正在控制關節，處理控制器位置輸入
        if (isControllingJoint && activeControllerTransform != null)
        {
            UpdateJointPosition();

            // 檢測確認編輯按鈕 (A按鈕)
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                msg = "Click A check";
                // 獲取當前編輯後的關節位置
                Vector3 editedPosition = dataManager.TempFramesData[dataManager.CurrentFrameIndex][selectedJointIndex];

                if (isEditingStartFrame)
                {
                    // 記錄開始幀編輯的關節位置
                    startFrameEditedJoints[selectedJointIndex] = editedPosition;
                    Debug.Log($"確認開始幀編輯: 關節 {selectedJointName}, 位置 {editedPosition}");

                    // 切換到編輯結束幀
                    isEditingStartFrame = false;

                    // 跳轉到結束幀
                    dataManager.JumpToFrame(selectedMaxFrame);

                    UpdateDebug($"已記錄開始幀關節 {selectedJointName} 的編輯，請編輯結束幀");
                }
                else
                {
                    // 記錄結束幀編輯的關節位置
                    endFrameEditedJoints[selectedJointIndex] = editedPosition;
                    Debug.Log($"確認結束幀編輯: 關節 {selectedJointName}, 位置 {editedPosition}");

                    // 切換回編輯開始幀
                    isEditingStartFrame = true;

                    // 跳轉到開始幀
                    dataManager.JumpToFrame(selectedMinFrame);

                    UpdateDebug($"已記錄結束幀關節 {selectedJointName} 的編輯，可以繼續編輯其他關節或按 Save 保存");
                }

                // 重置控制狀態
                StopControllingJoint();
            }

            // 檢測取消編輯按鈕 (B按鈕)
            if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                msg = "Click B cancel";
                // 恢復原始位置
                dataManager.TempFramesData[dataManager.CurrentFrameIndex][selectedJointIndex] = originalJointPosition;

                // 更新顯示
                dataManager.UpdateCubeParentJointPositions(dataManager.CurrentFrameIndex);

                Debug.Log("取消編輯關節位置");

                // 重置控制狀態
                StopControllingJoint();
            }
        }
    }

    // 更新關節位置 - 根據控制器位置變化
    private void UpdateJointPosition()
    {
        if (activeControllerTransform == null)
        {
            Debug.LogWarning("活動控制器為空");
            return;
        }

        // 獲取控制器當前世界座標位置
        Vector3 currentControllerPosition = activeControllerTransform.position;

        // 計算控制器位置變化
        Vector3 controllerDelta = currentControllerPosition - initialControllerPosition;
        controllerDelta.x = controllerDelta.x * -1; // 反轉 X 軸方向，因為控制器的 X 軸方向與關節的 X 軸方向相反    
        controllerDelta.z = controllerDelta.z * -1; // 反轉 Z 軸方向，因為控制器的 Z 軸方向與關節的 Z 軸方向相反

        // 輸出調試訊息
        Debug.Log($"初始控制器位置: {initialControllerPosition}, 當前位置: {currentControllerPosition}, 偏移: {controllerDelta}");


        // 計算新的關節位置
        Vector3 newPosition = originalJointPosition + controllerDelta * controllerSensitivity;

        // 更新 TempFramesData 中的關節位置
        dataManager.TempFramesData[dataManager.CurrentFrameIndex][selectedJointIndex] = newPosition;

        // 更新顯示
        dataManager.UpdateCubeParentJointPositions(dataManager.CurrentFrameIndex);
    }

    // 開始控制關節
    private void StartControllingJoint()
    {
        if (selectedJointIndex >= 0 && activeControllerTransform != null)
        {
            isControllingJoint = true;

            // 獲取當前幀中關節的位置 (未縮放的原始數據)
            originalJointPosition = dataManager.TempFramesData[dataManager.CurrentFrameIndex][selectedJointIndex];

            // 儲存控制器初始位置
            initialControllerPosition = activeControllerTransform.position;

            // 改變球體顏色表示正在控制
            if (currentSphereRenderer != null)
            {
                currentSphereRenderer.material.color = Color.green; // 拖動時顏色
            }

            Debug.Log($"開始控制關節: {selectedJointName}, 控制器初始位置: {initialControllerPosition}, 關節初始位置: {originalJointPosition}");
        }
        else
        {
            Debug.LogWarning($"無法開始控制: 選中關節索引={selectedJointIndex}, 控制器={activeControllerTransform}");
        }
    }

    // 停止控制關節
    private void StopControllingJoint()
    {
        isControllingJoint = false;
        activeControllerTransform = null;

        // 恢復球體顏色
        if (currentSphereRenderer != null)
        {
            currentSphereRenderer.material.color = Color.white; // 恢復顏色
        }

        Debug.Log($"停止控制關節: {selectedJointName}");
    }

    private void OnSliderValueChanged(float minValue, float maxValue)
    {
        dataManager.PauseWithAudio();
        // 當滑桿值變化時，更新選定的幀範圍
        int newMinFrame = Mathf.RoundToInt(minValue);
        int newMaxFrame = Mathf.RoundToInt(maxValue);


        // 判斷哪個滑桿被移動
        if (newMinFrame != selectedMinFrame)
        {
            // 最小值滑桿被移動，跳轉到最小幀
            dataManager.JumpToFrame(newMinFrame);
            startFrameEditedJoints.Clear();
        }
        else if (newMaxFrame != selectedMaxFrame)
        {
            // 最大值滑桿被移動，跳轉到最大幀
            dataManager.JumpToFrame(newMaxFrame);
            endFrameEditedJoints.Clear();
        }

        // 更新記錄的範圍
        selectedMinFrame = newMinFrame;
        selectedMaxFrame = newMaxFrame;

        // 設置 DanceDataManager 的播放範圍
        dataManager.SetPlayRange(selectedMinFrame, selectedMaxFrame, true);
    }

    private void OnFrameChanged(int frameIndex)
    {
        if (timeSlider != null)
        {
            // 暫時移除監聽器，避免循環調用
            timeSlider.onValueChanged.RemoveListener(OnTimeSliderValueChanged);
            timeSlider.value = frameIndex;
            timeSlider.onValueChanged.AddListener(OnTimeSliderValueChanged);
        }
    }

    private void OnPlayStateChanged(bool isPlaying)
    {
        // 根據播放狀態更新 UI 元素
        play_button.interactable = !isPlaying;
        pause_button.interactable = isPlaying;

        // 如果開始播放，取消當前的編輯狀態
        if (isPlaying && isControllingJoint)
        {
            isControllingJoint = false;
            activeControllerTransform = null;
        }
    }

    private void OnDestroy()
    {
        if (dataManager != null)
        {
            dataManager.OnFrameChanged -= OnFrameChanged;
            dataManager.OnDataLoaded -= OnDataLoaded;
            dataManager.OnPlayStateChanged -= OnPlayStateChanged;
        }

        if (doubleSlider != null)
        {
            doubleSlider.OnValueChanged.RemoveListener(OnSliderValueChanged);
        }

        if (timeSlider != null)
        {
            timeSlider.onValueChanged.RemoveListener(OnTimeSliderValueChanged);
        }

        // 清除關節按鈕的事件監聽器
        foreach (var jointInfo in jointButtons)
        {
            if (jointInfo.button != null)
            {
                jointInfo.button.onClick.RemoveAllListeners();
            }
        }

        // 銷毀球體
        if (currentSphere != null)
        {
            Destroy(currentSphere);
        }

        ClearMusicVisualization();

    }
}