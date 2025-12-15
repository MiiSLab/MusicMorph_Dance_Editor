using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TS.DoubleSlider;
using Microsoft.MixedReality.Toolkit.Experimental.UI;

public class TextBaseMotionEditorz : MonoBehaviour
{
    private TMP_InputField descriptionTextbox;
    private Button play_button, pause_button, stop_button, generation_button;
    public string csvFilePath = "demo";
    private DanceDataManager dataManager;
    private DoubleSlider doubleSlider;

    // 用於記錄目前選擇的幀範圍
    private int selectedMinFrame = 0;
    private int selectedMaxFrame = 0;

    public TextMeshProUGUI debug;



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
        LoadDanceData();
        SetupDoubleSlider();

        dataManager.PlayWithAudio();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateDebug($"frame: {dataManager.CurrentFrameIndex}\nrange from {selectedMinFrame} to {selectedMaxFrame}\nstatus: {(dataManager.IsPlaying ? "playing" : "pause")}");
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
        doubleSlider = GameObject.Find("DoubleSlider").GetComponent<DoubleSlider>();
        generation_button = GameObject.Find("GenerationButton").GetComponent<Button>();
    }

    private void GetComponents()
    {
        if (dataManager == null)
        {
            dataManager = GetComponent<DanceDataManager>();
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
        stop_button.onClick.AddListener(() => { dataManager.Test(); });  // 恢復原本的測試功能
        generation_button.onClick.AddListener(generationButtonClickHandler);


        // 訂閱 DanceDataManager 事件
        dataManager.OnFrameChanged += OnFrameChanged;
        dataManager.OnPlayStateChanged += OnPlayStateChanged;
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
            UpdateDebug("請輸入動作描述");
            return;
        }

        Debug.Log($"開始編輯動作: {prompt}, 範圍: {selectedMinFrame}-{selectedMaxFrame}");

        // 顯示載入中提示
        UpdateDebug("正在生成動作...");

        // 假設每秒30幀
        float fps = 30.0f;

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
                            Debug.Log("CSV 文件下載成功，正在加載數據...");
                            UpdateDebug("數據下載成功，正在加載...");

                            // 更新 csvFilePath
                            csvFilePath = newCsvId;

                            // 重新加載數據
                            LoadDanceData();

                            // 設置播放範圍並播放
                            dataManager.SetPlayRange(selectedMinFrame, selectedMaxFrame, true);
                            dataManager.JumpToFrame(selectedMinFrame);
                            dataManager.PlayWithAudio();

                            UpdateDebug($"動作編輯完成！新ID: {newCsvId}");
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
        }

        if (doubleSlider != null)
        {
            doubleSlider.OnValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }
}