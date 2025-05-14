using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuUI : MonoBehaviour
{
    [Header("Dance Data Manager")]
    public DanceDataManager danceDataManager;

    [Header("Character References")]
    public Transform skeletonCubeParent; // 角色骨架的父物件

    [Header("UI References")]
    public Button checkFoldersButton;    // 檢查資料夾清單按鈕
    public Button previousButton;        // 上一個舞蹈按鈕
    public Button nextButton;            // 下一個舞蹈按鈕
    public Button playPauseButton;       // 播放/暫停按鈕
    public TextMeshProUGUI logText; // 顯示當前 ID 的文字

    [Header("播放/暫停按鈕圖示")]
    public Sprite playSprite;            // 播放圖示
    public Sprite pauseSprite;           // 暫停圖示

    // 記錄當前在清單中的位置
    private int currentIndex = 0;
    private Image playPauseImage;        // 播放/暫停按鈕的圖像組件

    // 用於存儲組件檢查結果
    private bool isSetupComplete = false;

    private void Start()
    {
        // 檢查必要組件
        if (danceDataManager == null)
        {
            Debug.LogError("MenuUI: danceDataManager 未設置，請在 Inspector 中指定");
            return;
        }

        // 查找骨架父物件（如果未指定）
        if (skeletonCubeParent == null)
        {
            skeletonCubeParent = GameObject.Find("SkeletonCubeParent")?.transform;

            if (skeletonCubeParent == null)
            {
                Debug.LogWarning("未找到 SkeletonCubeParent，角色動作將無法顯示");
            }
        }

        // 設置 DanceDataManager 的 CubeParent
        if (skeletonCubeParent != null && danceDataManager != null)
        {
            danceDataManager.CubeParent = skeletonCubeParent;
            Debug.Log("已設置 DanceDataManager 的 CubeParent");
        }

        // 初始設定
        InitializeUI();

        // 自動檢查資料夾清單
        danceDataManager.checkList();

        // 訂閱事件
        SubscribeEvents();

        isSetupComplete = true;
    }

    private void SubscribeEvents()
    {
        if (danceDataManager != null)
        {
            // 訂閱資料載入完成事件
            danceDataManager.OnDataLoaded += OnDanceDataLoaded;

            // 訂閱播放狀態改變事件
            danceDataManager.OnPlayStateChanged += OnPlayStateChanged;
        }
    }

    private void InitializeUI()
    {
        // 檢查資料夾按鈕
        if (checkFoldersButton != null && danceDataManager != null)
        {
            checkFoldersButton.onClick.AddListener(() => danceDataManager.checkList());
        }

        // 上一個舞蹈按鈕
        if (previousButton != null)
        {
            previousButton.onClick.AddListener(LoadPreviousDance);
            // 一開始禁用按鈕，直到資料載入完成
            previousButton.interactable = false;
        }

        // 下一個舞蹈按鈕
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(LoadNextDance);
            // 一開始禁用按鈕，直到資料載入完成
            nextButton.interactable = false;
        }

        // 獲取播放/暫停按鈕的圖像組件
        if (playPauseButton != null)
        {
            playPauseImage = playPauseButton.GetComponent<Image>();

            // 設置初始圖示為播放圖示
            if (playPauseImage != null && playSprite != null)
            {
                playPauseImage.sprite = playSprite;
            }

            // 設置按鈕點擊事件
            playPauseButton.onClick.AddListener(TogglePlayPauseWithAudio);

            // 一開始禁用按鈕，直到資料載入完成
            playPauseButton.interactable = false;
        }

        // 更新 UI 顯示
        UpdateUI();
    }

    // 切換播放/暫停狀態（包括角色動作）
    private void TogglePlayPauseWithAudio()
    {
        if (danceDataManager == null) return;

        try
        {
            if (danceDataManager.IsPlaying)
            {
                // 如果正在播放，則暫停音樂和動作
                danceDataManager.PauseWithAudio();
            }
            else
            {
                // 確保 CubeParent 已設置
                if (danceDataManager.CubeParent == null && skeletonCubeParent != null)
                {
                    danceDataManager.CubeParent = skeletonCubeParent;
                }

                // 直接使用 danceDataManager 播放全部範圍
                int startFrame = 0;
                int endFrame = danceDataManager.TotalFrames - 1;

                // 設置播放範圍
                danceDataManager.SetPlayRange(startFrame, endFrame, true);

                // 跳轉到起始幀
                danceDataManager.JumpToFrame(startFrame);

                // 確保初始幀顯示正確
                danceDataManager.UpdateCubeParentJointPositions(startFrame);

                // 開始播放
                danceDataManager.PlayWithAudio();
            }

            // 立即更新按鈕狀態
            UpdatePlayPauseButton(danceDataManager.IsPlaying);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"播放/暫停操作發生錯誤: {ex.Message}");

            // 嘗試最簡單的播放方式
            try
            {
                if (danceDataManager.IsPlaying)
                {
                    danceDataManager.PauseWithAudio();
                }
                else
                {
                    danceDataManager.PlayWithAudio();
                }

                // 更新按鈕狀態
                UpdatePlayPauseButton(danceDataManager.IsPlaying);
            }
            catch (System.Exception backupEx)
            {
                Debug.LogError($"備用播放方式也失敗: {backupEx.Message}");
            }
        }
    }

    // 當播放狀態改變時的回調
    private void OnPlayStateChanged(bool isPlaying)
    {
        UpdatePlayPauseButton(isPlaying);
    }

    // 更新播放/暫停按鈕圖示
    private void UpdatePlayPauseButton(bool isPlaying)
    {
        if (playPauseImage != null)
        {
            if (isPlaying && pauseSprite != null)
            {
                // 如果正在播放，顯示暫停圖示
                playPauseImage.sprite = pauseSprite;
            }
            else if (!isPlaying && playSprite != null)
            {
                // 如果已暫停，顯示播放圖示
                playPauseImage.sprite = playSprite;
            }
        }
    }

    // 載入上一個舞蹈
    private void LoadPreviousDance()
    {
        if (danceDataManager == null || danceDataManager.existingDanceIds.Count == 0)
            return;

        // 如果正在播放，先暫停音樂和動作
        if (danceDataManager.IsPlaying)
        {
            danceDataManager.PauseWithAudio();
        }

        // 計算上一個索引 (循環到最後一個)
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = danceDataManager.existingDanceIds.Count - 1;

        // 載入舞蹈
        LoadDanceAtCurrentIndex();
    }

    // 載入下一個舞蹈
    private void LoadNextDance()
    {
        if (danceDataManager == null || danceDataManager.existingDanceIds.Count == 0)
            return;

        // 如果正在播放，先暫停音樂和動作
        if (danceDataManager.IsPlaying)
        {
            danceDataManager.PauseWithAudio();
        }

        // 計算下一個索引 (循環到第一個)
        currentIndex++;
        if (currentIndex >= danceDataManager.existingDanceIds.Count)
            currentIndex = 0;

        // 載入舞蹈
        LoadDanceAtCurrentIndex();
    }

    // 載入當前索引的舞蹈
    private void LoadDanceAtCurrentIndex()
    {
        if (danceDataManager != null && currentIndex >= 0 && currentIndex < danceDataManager.existingDanceIds.Count)
        {
            string danceId = danceDataManager.existingDanceIds[currentIndex];

            // 禁用按鈕，直到資料載入完成
            SetButtonsInteractable(false);

            // 確保 CubeParent 已設置
            if (danceDataManager.CubeParent == null && skeletonCubeParent != null)
            {
                danceDataManager.CubeParent = skeletonCubeParent;
            }

            // 載入新的舞蹈 ID
            danceDataManager.LoadById(danceId);

            // 更新 UI 顯示
            UpdateUI();
        }
    }

    // 當舞蹈資料載入完成時的回調
    private void OnDanceDataLoaded(string danceId, bool success)
    {
        if (success)
        {
            // 更新當前索引為載入完成的 danceId
            int index = danceDataManager.existingDanceIds.IndexOf(danceId);
            if (index >= 0)
            {
                currentIndex = index;
            }

            // 啟用按鈕
            SetButtonsInteractable(true);

            // 更新 UI 顯示
            UpdateUI();

            // 確保播放/暫停按鈕顯示正確的圖示
            UpdatePlayPauseButton(danceDataManager.IsPlaying);

            // 確保顯示第一幀的角色姿勢
            if (danceDataManager.TotalFrames > 0)
            {
                danceDataManager.JumpToFrame(0);
                danceDataManager.UpdateCubeParentJointPositions(0);
            }

            Debug.Log($"舞蹈 {danceId} 載入成功，可以播放");
        }
        else
        {
            Debug.LogWarning($"舞蹈 {danceId} 載入失敗");
        }
    }

    // 設置所有按鈕的交互狀態
    private void SetButtonsInteractable(bool interactable)
    {
        if (previousButton != null)
            previousButton.interactable = interactable && danceDataManager.existingDanceIds.Count > 1;

        if (nextButton != null)
            nextButton.interactable = interactable && danceDataManager.existingDanceIds.Count > 1;

        if (playPauseButton != null)
            playPauseButton.interactable = interactable;
    }

    // 更新 UI 顯示
    private void UpdateUI()
    {
        // 更新當前 ID 文字
        if (logText != null && danceDataManager != null)
        {
            if (danceDataManager.existingDanceIds.Count > 0 && currentIndex >= 0 && currentIndex < danceDataManager.existingDanceIds.Count)
            {
                string currentId = danceDataManager.existingDanceIds[currentIndex];
                int total = danceDataManager.existingDanceIds.Count;
                //logText.text = $"舞蹈 ID: {currentId} ({currentIndex + 1}/{total})";
                logText.text = $" Playlist: {currentIndex + 1}/{total} ";

            }
            else
            {
                logText.text = "None";
            }
        }

        // 設置按鈕狀態
        SetButtonsInteractable(danceDataManager != null && danceDataManager.existingDanceIds.Count > 0);
    }

    // 在每幀更新檢查，確保音樂播放完畢後更新按鈕狀態
    private void Update()
    {
        if (!isSetupComplete) return;

        // 檢查音樂是否播放完畢但狀態沒更新
        if (danceDataManager != null && danceDataManager.IsPlaying)
        {
            // 檢查 AudioSource 是否還在播放
            AudioSource audioSource = danceDataManager.GetComponent<AudioSource>();
            if (audioSource != null && !audioSource.isPlaying)
            {
                // 音樂播放完畢，但 IsPlaying 狀態還沒更新
                // 這裡可以手動更新 UI
                UpdatePlayPauseButton(false);

                // 可能還需要停止動作播放
                danceDataManager.PauseWithAudio();
            }
        }
    }

    private void OnDestroy()
    {
        // 移除所有監聽器
        if (checkFoldersButton != null)
            checkFoldersButton.onClick.RemoveAllListeners();

        if (previousButton != null)
            previousButton.onClick.RemoveAllListeners();

        if (nextButton != null)
            nextButton.onClick.RemoveAllListeners();

        if (playPauseButton != null)
            playPauseButton.onClick.RemoveAllListeners();

        // 取消訂閱事件
        if (danceDataManager != null)
        {
            danceDataManager.OnDataLoaded -= OnDanceDataLoaded;
            danceDataManager.OnPlayStateChanged -= OnPlayStateChanged;
        }
    }
}