using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MenuUI : MonoBehaviour
{
    [Header("Dance Data Manager")]
    public DanceDataManager danceDataManager;

    [Header("Character References")]
    [Tooltip("多個角色骨架的父物件陣列")]
    public Transform[] skeletonCubeParents; // 改為陣列以支援多角色
    
    [Tooltip("是否保持所有角色在相同位置")]
    public bool keepAllCharactersInSamePosition = true;

    [Header("UI References")]
    public Button checkFoldersButton;
    public Button previousButton;
    public Button nextButton;
    public Button playPauseButton;
    public TextMeshProUGUI logText;

    [Header("播放/暫停按鈕圖示")]
    public Sprite playSprite;
    public Sprite pauseSprite;

    // 記錄當前在清單中的位置
    private int currentIndex = 0;
    private Image playPauseImage;
    private bool isSetupComplete = false;
    private Transform primarySkeleton; // 保存給 DanceDataManager 的主骨架
    
    // 效能優化：快取幀資料和最後更新的幀索引
    private int lastUpdatedFrameIndex = -1;
    
    // 用於儲存角色的原始位置
    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();

    public Material pano;
    private static string DEFAULT_SKYBOX_PATH = "skybox/default";

    private void Start()
    {
        // 檢查必要組件
        if (danceDataManager == null)
        {
            Debug.LogWarning("MenuUI: danceDataManager 未設置，請在 Inspector 中指定");
            return;
        }

        // 初始化角色
        InitializeCharacters();

        // 初始設定
        InitializeUI();

        // 自動檢查資料夾清單
        danceDataManager.checkList();

        // 訂閱事件
        SubscribeEvents();

        SetDefaultSkybox();

        isSetupComplete = true;
    }

    // 初始化角色
    private void InitializeCharacters()
    {
        // 檢查角色陣列
        if (skeletonCubeParents == null || skeletonCubeParents.Length == 0)
        {
            // 向下兼容：嘗試查找單一骨架
            Transform singleSkeleton = GameObject.Find("SkeletonCubeParent")?.transform;
            if (singleSkeleton != null)
            {
                skeletonCubeParents = new Transform[] { singleSkeleton };
                Debug.Log("未設置角色骨架陣列，已自動找到並使用 SkeletonCubeParent");
            }
            else
            {
                Debug.LogWarning("未找到任何角色骨架，請在 Inspector 中設置 skeletonCubeParents 陣列");
                skeletonCubeParents = new Transform[0];
            }
        }
        
        // 儲存所有角色的原始位置
        originalPositions.Clear();
        foreach (Transform character in skeletonCubeParents)
        {
            if (character != null)
            {
                originalPositions[character] = character.position;
            }
        }

        // 設置主要角色 (給 DanceDataManager 使用)
        if (skeletonCubeParents.Length > 0 && skeletonCubeParents[0] != null)
        {
            primarySkeleton = skeletonCubeParents[0];
            
            // 設置 DanceDataManager 的 CubeParent
            if (danceDataManager != null)
            {
                danceDataManager.CubeParent = primarySkeleton;
                Debug.Log($"已設置 DanceDataManager 的 CubeParent 為第一個角色");
            }
        }
    }

    // 效能優化：更新所有角色的關節位置
    private void UpdateAllCharactersJointPositions(int frameIndex)
    {
        // 效能優化：如果這一幀已經更新過，跳過不必要的更新
        if (frameIndex == lastUpdatedFrameIndex) return;
        lastUpdatedFrameIndex = frameIndex;
        
        // 如果只有一個角色，使用原始方法即可
        if (skeletonCubeParents.Length <= 1) return;
        
        // 獲取已縮放的關節位置
        var jointPositions = danceDataManager.GetScaledJointPositions(frameIndex);
        if (jointPositions.Count == 0) return;
        
        // 暫存當前的 CubeParent
        Transform originalParent = danceDataManager.CubeParent;
        
        try
        {
            // 如果需要保持所有角色在相同位置，先記錄主角色的位置
            Vector3 primaryPosition = Vector3.zero;
            if (keepAllCharactersInSamePosition && primarySkeleton != null)
            {
                primaryPosition = primarySkeleton.position;
            }
            
            // 更新每個角色
            for (int i = 0; i < skeletonCubeParents.Length; i++)
            {
                Transform character = skeletonCubeParents[i];
                if (character == null) continue;
                
                // 跳過第一個角色（主角色），因為它會被 DanceDataManager 自動更新
                if (i == 0 && character == primarySkeleton) continue;
                
                // 如果需要保持所有角色在相同位置
                if (keepAllCharactersInSamePosition)
                {
                    // 先保存角色原始位置
                    Vector3 originalPos = Vector3.zero;
                    if (originalPositions.ContainsKey(character))
                    {
                        originalPos = originalPositions[character];
                    }
                    
                    // 將角色移動到與主角色相同的位置
                    character.position = primaryPosition;
                    
                    // 暫時設置當前角色為 CubeParent 並更新關節位置
                    danceDataManager.CubeParent = character;
                    danceDataManager.UpdateCubeParentJointPositions(frameIndex);
                    
                    // 恢復角色位置
                    character.position = originalPos;
                }
                else
                {
                    // 直接設置當前角色為 CubeParent 並更新關節位置
                    danceDataManager.CubeParent = character;
                    danceDataManager.UpdateCubeParentJointPositions(frameIndex);
                }
            }
        }
        finally
        {
            // 恢復原始的 CubeParent
            danceDataManager.CubeParent = originalParent;
        }
    }

    // 手動更新所有角色的位置
    public void ResetAllCharacterPositions()
    {
        foreach (Transform character in skeletonCubeParents)
        {
            if (character != null && originalPositions.ContainsKey(character))
            {
                character.position = originalPositions[character];
            }
        }
    }

    private void UpdateSkybox(string danceId)
    {
        if (pano == null) return;

        Texture2D skyboxTexture = Resources.Load<Texture2D>($"{danceId}/scene");
        if (skyboxTexture == null)
        {
            skyboxTexture = Resources.Load<Texture2D>(DEFAULT_SKYBOX_PATH);
            if (skyboxTexture == null) return;
        }

        pano.mainTexture = skyboxTexture;
        RenderSettings.skybox = pano;
        DynamicGI.UpdateEnvironment();
    }

    private void SetDefaultSkybox()
    {
        if (pano == null) return;

        Texture2D defaultTexture = Resources.Load<Texture2D>(DEFAULT_SKYBOX_PATH);
        if (defaultTexture != null)
        {
            pano.mainTexture = defaultTexture;
            RenderSettings.skybox = pano;
            DynamicGI.UpdateEnvironment();
        }
        else
        {
            Debug.LogWarning($"未找到預設天空盒貼圖，路徑：{DEFAULT_SKYBOX_PATH}");
        }
    }
    
    private void SubscribeEvents()
    {
        if (danceDataManager != null)
        {
            danceDataManager.OnDataLoaded += OnDanceDataLoaded;
            danceDataManager.OnPlayStateChanged += OnPlayStateChanged;
            danceDataManager.OnFrameChanged += OnFrameChanged;
        }
    }

    // 當幀變化時更新所有角色
    private void OnFrameChanged(int frameIndex)
    {
        // 只有在擁有多個角色時才需要特別處理
        if (skeletonCubeParents.Length > 1)
        {
            UpdateAllCharactersJointPositions(frameIndex);
        }
    }

    private void InitializeUI()
    {
        if (checkFoldersButton != null && danceDataManager != null)
        {
            checkFoldersButton.onClick.AddListener(() => danceDataManager.checkList());
        }

        if (previousButton != null)
        {
            previousButton.onClick.AddListener(LoadPreviousDance);
            previousButton.interactable = false;
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(LoadNextDance);
            nextButton.interactable = false;
        }

        if (playPauseButton != null)
        {
            playPauseImage = playPauseButton.GetComponent<Image>();
            if (playPauseImage != null && playSprite != null)
            {
                playPauseImage.sprite = playSprite;
            }
            playPauseButton.onClick.AddListener(TogglePlayPauseWithAudio);
            playPauseButton.interactable = false;
        }

        UpdateUI();
    }

    // 切換播放/暫停狀態（支持多角色）
    private void TogglePlayPauseWithAudio()
    {
        if (danceDataManager == null) return;

        try
        {
            if (danceDataManager.IsPlaying)
            {
                danceDataManager.PauseWithAudio();
            }
            else
            {
                // 確保 CubeParent 已設置
                if (danceDataManager.CubeParent == null && primarySkeleton != null)
                {
                    danceDataManager.CubeParent = primarySkeleton;
                }

                int startFrame = 0;
                int endFrame = danceDataManager.TotalFrames - 1;
                danceDataManager.SetPlayRange(startFrame, endFrame, true);
                danceDataManager.JumpToFrame(startFrame);
                
                // 確保角色位置正確
                if (keepAllCharactersInSamePosition)
                {
                    ResetAllCharacterPositions();
                }
                
                // 確保初始幀顯示正確 (更新所有角色)
                UpdateAllCharactersJointPositions(startFrame);

                danceDataManager.PlayWithAudio();
            }

            UpdatePlayPauseButton(danceDataManager.IsPlaying);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"播放/暫停操作發生錯誤: {ex.Message}");

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

                UpdatePlayPauseButton(danceDataManager.IsPlaying);
            }
            catch (System.Exception backupEx)
            {
                Debug.LogWarning($"備用播放方式也失敗: {backupEx.Message}");
            }
        }
    }

    private void OnPlayStateChanged(bool isPlaying)
    {
        UpdatePlayPauseButton(isPlaying);
    }

    private void UpdatePlayPauseButton(bool isPlaying)
    {
        if (playPauseImage != null)
        {
            playPauseImage.sprite = isPlaying && pauseSprite != null ? pauseSprite : playSprite;
        }
    }

    private void LoadPreviousDance()
    {
        if (danceDataManager == null || danceDataManager.existingDanceIds.Count == 0)
            return;

        if (danceDataManager.IsPlaying)
        {
            danceDataManager.PauseWithAudio();
        }

        currentIndex--;
        if (currentIndex < 0)
            currentIndex = danceDataManager.existingDanceIds.Count - 1;

        LoadDanceAtCurrentIndex();
    }

    private void LoadNextDance()
    {
        if (danceDataManager == null || danceDataManager.existingDanceIds.Count == 0)
            return;

        if (danceDataManager.IsPlaying)
        {
            danceDataManager.PauseWithAudio();
        }

        currentIndex++;
        if (currentIndex >= danceDataManager.existingDanceIds.Count)
            currentIndex = 0;

        LoadDanceAtCurrentIndex();
    }

    private void LoadDanceAtCurrentIndex()
    {
        if (danceDataManager != null && currentIndex >= 0 && currentIndex < danceDataManager.existingDanceIds.Count)
        {
            string danceId = danceDataManager.existingDanceIds[currentIndex];

            SetButtonsInteractable(false);

            if (danceDataManager.CubeParent == null && primarySkeleton != null)
            {
                danceDataManager.CubeParent = primarySkeleton;
            }

            danceDataManager.LoadById(danceId);
            UpdateUI();
        }
    }

    private void OnDanceDataLoaded(string danceId, bool success)
    {
        if (success)
        {
            int index = danceDataManager.existingDanceIds.IndexOf(danceId);
            if (index >= 0)
            {
                currentIndex = index;
            }

            SetButtonsInteractable(true);
            UpdateUI();
            UpdatePlayPauseButton(danceDataManager.IsPlaying);

            if (danceDataManager.TotalFrames > 0)
            {
                // 確保角色位置正確
                if (keepAllCharactersInSamePosition)
                {
                    ResetAllCharacterPositions();
                }
                
                danceDataManager.JumpToFrame(0);
                
                // 重置幀更新狀態，確保下次幀更新可以執行
                lastUpdatedFrameIndex = -1;
                
                // 更新所有角色的初始幀
                UpdateAllCharactersJointPositions(0);
            }
            
            UpdateSkybox(danceId);
            Debug.Log($"舞蹈 {danceId} 載入成功，可以播放");
        }
        else
        {
            Debug.LogWarning($"舞蹈 {danceId} 載入失敗");
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (previousButton != null)
            previousButton.interactable = interactable && danceDataManager.existingDanceIds.Count > 1;

        if (nextButton != null)
            nextButton.interactable = interactable && danceDataManager.existingDanceIds.Count > 1;

        if (playPauseButton != null)
            playPauseButton.interactable = interactable;
    }

    private void UpdateUI()
    {
        if (logText != null && danceDataManager != null)
        {
            if (danceDataManager.existingDanceIds.Count > 0 && currentIndex >= 0 && currentIndex < danceDataManager.existingDanceIds.Count)
            {
                int total = danceDataManager.existingDanceIds.Count;
                logText.text = $" Playlist: {currentIndex + 1}/{total} ";
            }
            else
            {
                logText.text = "None";
            }
        }

        SetButtonsInteractable(danceDataManager != null && danceDataManager.existingDanceIds.Count > 0);
    }

    private void Update()
    {
        if (!isSetupComplete) return;

        if (danceDataManager != null && danceDataManager.IsPlaying)
        {
            AudioSource audioSource = danceDataManager.GetComponent<AudioSource>();
            if (audioSource != null && !audioSource.isPlaying)
            {
                UpdatePlayPauseButton(false);
                danceDataManager.PauseWithAudio();
            }
        }
    }

    private void OnDestroy()
    {
        if (checkFoldersButton != null)
            checkFoldersButton.onClick.RemoveAllListeners();

        if (previousButton != null)
            previousButton.onClick.RemoveAllListeners();

        if (nextButton != null)
            nextButton.onClick.RemoveAllListeners();

        if (playPauseButton != null)
            playPauseButton.onClick.RemoveAllListeners();

        if (danceDataManager != null)
        {
            danceDataManager.OnDataLoaded -= OnDanceDataLoaded;
            danceDataManager.OnPlayStateChanged -= OnPlayStateChanged;
            danceDataManager.OnFrameChanged -= OnFrameChanged;
        }
    }
}