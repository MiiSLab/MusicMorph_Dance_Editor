using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TS.DoubleSlider;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class TextBaseMotionEditor : MonoBehaviour
{
    private TMP_InputField descriptionTextbox;
    private Button play_button, pause_button, stop_button, generation_button;
    public string csvFilePath = "";
    private DanceDataManager dataManager;
    private DoubleSlider doubleSlider;

    // 用於記錄目前選擇的幀範圍
    private int selectedMinFrame = 0;
    private int selectedMaxFrame = 0;

    public TextMeshProUGUI debug;
    private Slider timeSlider;
    // Audio visualization components
    private RectTransform timelineContainer;
    public GameObject beatMarkerPrefab;
    public GameObject rmsMarkerPrefab;
    private List<GameObject> beatMarkers = new List<GameObject>();
    private List<GameObject> rmsMarkers = new List<GameObject>();

    // Colors for visualization
    public Color beatColor = new Color(0.2f, 0.8f, 0.2f, 0.7f);
    public Color rmsColor = new Color(0.2f, 0.2f, 0.8f, 0.5f);

    public void EditSelectedRangePose(int jointIndex, Vector3 startValue, Vector3 endValue)
    {
        if (selectedMinFrame <= selectedMaxFrame)
        {
            dataManager.EditPose(jointIndex, selectedMinFrame, startValue, selectedMaxFrame, endValue);
            Debug.Log($"已編輯關節 {jointIndex} 在幀範圍 {selectedMinFrame}-{selectedMaxFrame} 的姿勢");
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
        InitializeAudioVisualization();
        LoadDanceData();

        // Subscribe to data loaded event to create music visualization
        dataManager.OnDataLoaded += OnDataLoaded;
    }

    // Update is called once per frame
    void Update()
    {
        if (dataManager.IsPlaying)
        {
            UpdateDebug($"frame: {dataManager.CurrentFrameIndex}\nrange from {selectedMinFrame} to {selectedMaxFrame}\nstatus: {(dataManager.IsPlaying ? "playing" : "pause")}");
        }

        // Debug.Log("hi");
    }

    void InitDebug()
    {
        // debug = GameObject.Find("Debug").GetComponent<TextMeshPro>();
    }

    private void InitSettings()
    {
        HttpService.Instance.SetBaseUrl("http://140.118.162.43:8443"); //"miislab.pagekite.me"
    }

    void UpdateDebug(string text)
    {
        if (debug != null) { debug.text = text; }
    }

    private void GetGameObject()
    {
        descriptionTextbox = GameObject.Find("DescriptionTextbox").GetComponent<TMP_InputField>();
        play_button = GameObject.Find("PlayButton").GetComponent<Button>();
        pause_button = GameObject.Find("PauseButton").GetComponent<Button>();
        stop_button = GameObject.Find("StopButton").GetComponent<Button>();
        timeSlider = GameObject.Find("TimeSlider").GetComponent<Slider>();
        doubleSlider = GameObject.Find("DoubleSlider").GetComponent<DoubleSlider>();
        generation_button = GameObject.Find("GenerationButton").GetComponent<Button>();

        // Get timeline container for audio visualization
        timelineContainer = GameObject.Find("TimelineContainer")?.GetComponent<RectTransform>();

        // Find or create beat marker prefab
        // beatMarkerPrefab = Resources.Load<GameObject>("BeatMarker");
        // if (beatMarkerPrefab == null)
        // {
        //     beatMarkerPrefab = CreateBeatMarkerPrefab();
        // }

        // Find or create RMS marker prefab
        // rmsMarkerPrefab = Resources.Load<GameObject>("RmsMarker");
        // if (rmsMarkerPrefab == null)
        // {
        //     rmsMarkerPrefab = CreateRmsMarkerPrefab();
        // }
    }

    // private GameObject CreateBeatMarkerPrefab()
    // {
    //     GameObject prefab = new GameObject("BeatMarker");
    //     prefab.AddComponent<RectTransform>().sizeDelta = new Vector2(2, 20);
    //     Image image = prefab.AddComponent<Image>();
    //     image.color = beatColor;
    //     prefab.SetActive(false);
    //     return prefab;
    // }

    // private GameObject CreateRmsMarkerPrefab()
    // {
    //     GameObject prefab = new GameObject("RmsMarker");
    //     prefab.AddComponent<RectTransform>().sizeDelta = new Vector2(5, 10);
    //     Image image = prefab.AddComponent<Image>();
    //     image.color = rmsColor;
    //     prefab.SetActive(false);
    //     return prefab;
    // }

    private void InitializeAudioVisualization()
    {
        if (timelineContainer == null)
        {
            Debug.LogWarning("找不到時間軸容器，無法初始化音樂視覺化");
            return;
        }

        // Clear any existing markers
        ClearMusicVisualization();
    }

    private void GetComponents()
    {
        dataManager = FindObjectOfType<DanceDataManager>();
        if (dataManager == null)
        {
            dataManager = GetComponent<DanceDataManager>();
            if (dataManager == null)
            {
                dataManager = gameObject.AddComponent<DanceDataManager>();
                Debug.Log("DanceDataManager component added2");
            }
            Debug.Log("DanceDataManager component added1");
        }
        Debug.Log("DanceDataManager component added0");

        // Make sure we're using the current dance ID
        if (!string.IsNullOrEmpty(dataManager.CurrentDanceId))
        {
            csvFilePath = dataManager.CurrentDanceId;
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
        stop_button.onClick.AddListener(() => { dataManager.Test(); });  // 恢復原本的測試功能
        generation_button.onClick.AddListener(generationButtonClickHandler);

        // 訂閱 DanceDataManager 事件
        dataManager.OnFrameChanged += OnFrameChanged;
        dataManager.OnPlayStateChanged += OnPlayStateChanged;
    }

    private void OnDataLoaded(string danceId, bool success)
    {
        if (success)
        {
            Debug.Log($"資料 {danceId} 載入成功，更新 UI");
            // Update csvFilePath with the current dance ID
            csvFilePath = danceId;
            SetupDoubleSlider();
            SetupTimeSlider();
            // 更新範圍滑桿
            if (doubleSlider != null)
            {
                doubleSlider.Setup(0, dataManager.TotalFrames - 1, 0, dataManager.TotalFrames - 1);
                selectedMinFrame = 0;
                selectedMaxFrame = dataManager.TotalFrames - 1;
            }
            // // 更新時間滑塊
            if (timeSlider != null)
            {
                timeSlider.minValue = 0;
                timeSlider.maxValue = dataManager.TotalFrames - 1;
                timeSlider.value = 0;
            }

            // 創建音樂視覺化
            CreateMusicVisualization();

            // 播放音樂
            dataManager.PlayWithAudio();
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

            marker.SetActive(true);
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

            marker.SetActive(true);
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

    private void LoadDanceData()
    {
        dataManager.CubeParent = GameObject.Find("SkeletonCubeParent").transform;
        dataManager.LoadById(dataManager.CurrentDanceId);
        Debug.Log("PlayID_Text: " + dataManager.CurrentDanceId);
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
        }
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
        }
        else if (newMaxFrame != selectedMaxFrame)
        {
            // 最大值滑桿被移動，跳轉到最大幀
            dataManager.JumpToFrame(newMaxFrame);
        }

        // 更新記錄的範圍
        selectedMinFrame = newMinFrame;
        selectedMaxFrame = newMaxFrame;

        // 設置 DanceDataManager 的播放範圍
        dataManager.SetPlayRange(selectedMinFrame, selectedMaxFrame, true);
    }

    private void OnFrameChanged(int frameIndex)
    {
        // 不做幀範圍限制，讓播放可以正常循環所有幀
        // 不更新 descriptionTextbox，保留其原有功能
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
    }

    private void generationButtonClickHandler()
    {
        // 確保 HttpService 已經初始化
        HttpService httpService = HttpService.Instance;

        // 暫停當前播放
        dataManager.PauseWithAudio();

        // 獲取描述文字
        string prompt = descriptionTextbox.text;
        if (string.IsNullOrEmpty(prompt))
        {
            Debug.LogWarning("請輸入動作描述");
            UpdateDebug("Enter Action Prompt");
            return;
        }

        Debug.Log($"開始編輯動作: {prompt}, 範圍: {selectedMinFrame}-{selectedMaxFrame}");

        // 顯示載入中提示
        UpdateDebug("Generating Motion...");

        // 假設每秒30幀
        float fps = 30.0f;

        // 保存當前的CSV文件ID
        string originalCsvId = csvFilePath;

        // 發送編輯請求
        httpService.EditMotion(
            csvFilePath,       // 當前使用的CSV檔案ID
            prompt,            // 描述文字
            selectedMinFrame,  // 開始幀
            selectedMaxFrame,  // 結束幀
            fps,               // 每秒幀數
            (success, result) =>
            {
                if (success)
                {
                    // 編輯成功，result 為新的 CSV ID
                    Debug.Log($"動作編輯成功，新ID: {result}");
                    UpdateDebug($"動作編輯成功！正在下載數據...");

                    // 更新當前使用的 CSV ID
                    string newCsvId = result;

                    // 先下載 CSV 文件
                    httpService.DownloadCSV(newCsvId, (downloadSuccess) =>
                    {
                        if (downloadSuccess)
                        {
                            Debug.Log("CSV 文件下載成功，正在處理數據...");
                            UpdateDebug("數據下載成功，正在處理...");

                            // 只替換選定範圍的幀
                            bool rangeReplaceSuccess = dataManager.ReplaceCSVFileRange(
                                originalCsvId,  // 目標CSV ID（原始文件）
                                newCsvId,       // 源CSV ID（新生成的文件）
                                selectedMinFrame, // 開始幀
                                selectedMaxFrame  // 結束幀
                            );

                            if (rangeReplaceSuccess)
                            {
                                Debug.Log($"成功替換 {originalCsvId} 中從 {selectedMinFrame} 到 {selectedMaxFrame} 的幀數據");
                                UpdateDebug("成功更新選定範圍的動作數據");

                                // 重新加載原始CSV文件的數據
                                dataManager.LoadCSVData($"{originalCsvId}");

                                // 設置播放範圍並播放
                                dataManager.SetPlayRange(selectedMinFrame, selectedMaxFrame, true);
                                dataManager.JumpToFrame(selectedMinFrame);
                                dataManager.PlayWithAudio();
                            }
                            else
                            {
                                Debug.LogError("替換選定範圍的幀數據失敗");
                                UpdateDebug("更新動作數據失敗");
                            }
                        }
                        else
                        {
                            Debug.LogError("CSV 文件下載失敗");
                            UpdateDebug("數據下載失敗，請重試");
                        }
                    });
                    NonNativeKeyboard.Instance.Close();
                }
                else
                {
                    // 編輯失敗，result 為錯誤訊息
                    Debug.LogError($"動作編輯失敗: {result}");
                    UpdateDebug($"生成失敗: {result}");
                }
            }
        );
    }
    private void OnDestroy()
    {
        if (dataManager != null)
        {
            dataManager.OnFrameChanged -= OnFrameChanged;
            dataManager.OnPlayStateChanged -= OnPlayStateChanged;
            dataManager.OnDataLoaded -= OnDataLoaded;
        }

        if (doubleSlider != null)
        {
            doubleSlider.OnValueChanged.RemoveListener(OnSliderValueChanged);
        }
        if (timeSlider != null)
        {
            timeSlider.onValueChanged.RemoveListener(OnTimeSliderValueChanged);
        }
        // Clean up audio visualization
        ClearMusicVisualization();
    }
}