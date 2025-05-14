using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class DanceDataManager : MonoBehaviour
{
  #region Public Properties
  // 從api獲得舞蹈資料夾清單
  public string baseUrl = "http://miislab.pagekite.me"; //"http://140.118.162.43:8443"
  public List<string> existingDanceIds = new List<string>();
  public List<List<Vector3>> FramesData { get; private set; } = new List<List<Vector3>>();
  public List<List<Vector3>> TempFramesData { get; private set; } = new List<List<Vector3>>();
  public int TotalFrames => FramesData.Count;
  public float Scale_x = 1f, Scale_y = 1f, Scale_z = 1f;
  public bool IsPlaying { get; private set; } = false;
  public int CurrentFrameIndex { get; private set; } = 0;
  public Transform CubeParent { get; set; } = null;
  public float FrameRate { get; set; } = 60f;
  public int PlayRangeStart { get; set; } = 0;
  public int PlayRangeEnd { get; set; } = 0;
  public bool UsePlayRange { get; set; } = false;

  // 新增音樂和節拍相關屬性
  public AudioClip DanceAudio { get; private set; } = null;
  public List<float> BeatTimes { get; private set; } = new List<float>();
  public List<float> RmsValues { get; private set; } = new List<float>();
  public string CurrentDanceId { get; private set; } = "";

  // 事件系統，用於通知框架變化
  public delegate void FrameChangedHandler(int frameIndex);
  public event FrameChangedHandler OnFrameChanged;

  // 播放狀態改變事件
  public delegate void PlayStateChangedHandler(bool isPlaying);
  public event PlayStateChangedHandler OnPlayStateChanged;

  // 資料載入完成事件
  public delegate void DataLoadedHandler(string danceId, bool success);
  public event DataLoadedHandler OnDataLoaded;

  // 下載進度事件
  public delegate void DownloadProgressHandler(string danceId, float progress, string status);
  public event DownloadProgressHandler OnDownloadProgress;

  #endregion

  #region Private Variables
  private Coroutine playbackCoroutine;
  private AudioSource audioSource;
  private HttpService httpService;
  #endregion

  #region Constants
  // 固定的檔案名稱
  private const string CSV_FILENAME = "music_joints.csv";
  private const string WAV_FILENAME = "music.wav";
  private const string JSON_FILENAME = "music_analysis.json";
  #endregion

  #region Unity Lifecycle
  private void Awake()
  {
    // 確保有AudioSource組件可以播放音樂
    audioSource = GetComponent<AudioSource>();
    if (audioSource == null)
    {
      audioSource = gameObject.AddComponent<AudioSource>();
    }
    // 設置 HttpService
    httpService = HttpService.Instance;
    httpService.SetBaseUrl(baseUrl);
  }
  #endregion

  #region Folder List Checking

  public void Start()
  {
    checkList();
  }

  // 手動調用以檢查資料夾列表
  public void checkList()
  {
    Debug.Log("開始檢查資料夾列表...");
    StartCoroutine(FetchFolderList());
  }

  // 發送請求並處理資料夾列表
  private IEnumerator FetchFolderList()
  {
    string listUrl = $"{httpService.baseUrl}/list_dances";
    using (UnityWebRequest request = UnityWebRequest.Get(listUrl))
    {
      Debug.Log($"發送請求到: {listUrl}");
      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
      {
        Debug.LogError("HTTP Error: " + request.error);
      }
      else
      {
        Debug.Log("API 回應：" + request.downloadHandler.text);

        try
        {
          // 解析 JSON
          FolderListResponse response = JsonUtility.FromJson<FolderListResponse>(request.downloadHandler.text);

          if (response.success)
          {
            Debug.Log("成功取得資料夾列表");
            ProcessFolderList(response.dances);
          }
          else
          {
            Debug.LogWarning("API 回應失敗");
          }
        }
        catch (Exception e)
        {
          Debug.LogError("JSON 解析錯誤：" + e.Message);
        }
      }
    }
  }

  // 處理資料夾列表
  private void ProcessFolderList(List<string> folderList)
  {
    Debug.Log($"收到 {folderList.Count} 個資料夾");

    StartCoroutine(ProcessFolderListSequentially(folderList));
  }

  private IEnumerator ProcessFolderListSequentially(List<string> folderList)
  {
    foreach (string danceId in folderList)
    {
      if (!existingDanceIds.Contains(danceId))
      {
        Debug.Log($"新增資料夾: {danceId}");
        existingDanceIds.Add(danceId);

        // 等待 LoadById 完成
        bool isCompleted = false;

        void OnDataLoaded(string id, bool success)
        {
          if (id == danceId)
          {
            Debug.Log($"資料夾 {id} 處理完成");
            isCompleted = true;
            this.OnDataLoaded -= OnDataLoaded;
          }
        }

        this.OnDataLoaded += OnDataLoaded;

        // 開始加載資料
        this.LoadById(danceId);

        // 等待資料加載完成，或超時 30 秒
        float timeout = 0f;
        while (!isCompleted && timeout < 30f)
        {
          timeout += Time.deltaTime;
          yield return null;
        }

        if (!isCompleted)
        {
          Debug.LogWarning($"資料夾 {danceId} 超時，跳過...");
        }
      }
      else
      {
        Debug.Log($"資料夾 {danceId} 已存在，跳過...");
      }
    }

    Debug.Log("所有資料夾處理完成");
  }


  #endregion

  #region Public Methods
  // 新方法：通過ID載入所有資料，如果本地沒有則嘗試下載
  public void LoadById(string danceId)
  {
    CurrentDanceId = danceId;

    // 檢查是否有本地資料
    if (CheckLocalDataExists(danceId))
    {
      Debug.Log($"本地已有 {danceId} 的資料，直接載入");
      StartCoroutine(LoadAllDataById(danceId));
    }
    else
    {
      Debug.Log($"本地沒有 {danceId} 的資料，嘗試下載");
      StartCoroutine(DownloadAndLoadData(danceId));
    }
  }

  // 檢查本地是否已有資料
  private bool CheckLocalDataExists(string danceId)
  {
    // 檢查 Resources 目錄下是否有此 ID 的資料夾和必要檔案
    string resourcesPath = $"{danceId}";
    TextAsset csvAsset = Resources.Load<TextAsset>($"{resourcesPath}/{CSV_FILENAME.Split(".csv")[0]}");

    return csvAsset != null;
  }

  // 下載並載入資料
  private IEnumerator DownloadAndLoadData(string danceId)
  {
    OnDownloadProgress?.Invoke(danceId, 0f, "開始下載資料...");

    // 下載 CSV 檔案
    bool csvDownloaded = false;
    httpService.DownloadCSV(danceId, (success) => { csvDownloaded = success; });

    // 等待 CSV 下載完成
    float timeoutCounter = 0;
    while (!csvDownloaded && timeoutCounter < 30f) // 30秒超時
    {
      timeoutCounter += Time.deltaTime;
      OnDownloadProgress?.Invoke(danceId, 0.3f, "下載舞姿資料中...");
      yield return null;
    }

    if (!csvDownloaded)
    {
      Debug.LogError($"下載 {danceId} 的 CSV 檔案失敗或超時");
      OnDownloadProgress?.Invoke(danceId, 0f, "下載失敗");
      OnDataLoaded?.Invoke(danceId, false);
      yield break;
    }

    OnDownloadProgress?.Invoke(danceId, 0.4f, "下載舞姿資料完成，開始下載音樂...");

    // 下載 WAV 檔案
    bool wavDownloaded = false;
    StartCoroutine(DownloadAudio(danceId, (success) => { wavDownloaded = success; }));

    // 等待 WAV 下載完成
    timeoutCounter = 0;
    while (!wavDownloaded && timeoutCounter < 30f)
    {
      timeoutCounter += Time.deltaTime;
      OnDownloadProgress?.Invoke(danceId, 0.6f, "下載音樂中...");
      yield return null;
    }

    if (!wavDownloaded)
    {
      Debug.LogError($"下載 {danceId} 的 WAV 檔案失敗或超時");
      OnDownloadProgress?.Invoke(danceId, 0f, "下載失敗");
      OnDataLoaded?.Invoke(danceId, false);
      yield break;
    }

    OnDownloadProgress?.Invoke(danceId, 0.8f, "下載音樂完成，開始下載分析資料...");

    // 下載 JSON 檔案
    bool jsonDownloaded = false;
    StartCoroutine(DownloadJson(danceId, (success) => { jsonDownloaded = success; }));

    // 等待 JSON 下載完成
    timeoutCounter = 0;
    while (!jsonDownloaded && timeoutCounter < 30f)
    {
      timeoutCounter += Time.deltaTime;
      OnDownloadProgress?.Invoke(danceId, 0.9f, "下載分析資料中...");
      yield return null;
    }

    if (!jsonDownloaded)
    {
      Debug.LogWarning($"下載 {danceId} 的 JSON 檔案失敗或超時，但仍繼續載入其他資料");
      // JSON 不是必須的，所以即使失敗也繼續
    }

    OnDownloadProgress?.Invoke(danceId, 1.0f, "下載完成，載入資料中...");

    // 刷新 Asset 資料庫，確保 Unity 能找到新下載的檔案
#if UNITY_EDITOR
    UnityEditor.AssetDatabase.Refresh();
#endif

    // 等待一幀，確保資源刷新
    yield return null;

    // 載入下載好的資料
    yield return StartCoroutine(LoadAllDataById(danceId));
  }

  // 載入所有資料的協程
  // 載入所有資料的協程
  private IEnumerator LoadAllDataById(string danceId)
  {
    bool success = true;

    // 載入CSV舞姿資料
    string csvPath = $"{danceId}/{CSV_FILENAME}";
    if (!LoadCSVData(csvPath))
    {
      Debug.LogError($"Failed to load CSV data for ID: {danceId}");
      success = false;
    }

    // 載入JSON分析資料
    yield return StartCoroutine(LoadJsonData(danceId));

    // 載入WAV音樂檔
    yield return StartCoroutine(LoadAudioData(danceId));

    // 觸發載入完成事件
    OnDataLoaded?.Invoke(danceId, success);

    // 如果成功載入，設置第一幀
    if (success)
    {
      SetFrame(0);

      // 設置播放範圍為全部幀
      PlayRangeStart = 0;
      PlayRangeEnd = TotalFrames - 1;
      UsePlayRange = true;
    }
  }
  // 載入JSON資料
  private IEnumerator LoadJsonData(string danceId)
  {
    string jsonPath = $"{danceId}/{JSON_FILENAME}";
    TextAsset jsonAsset = Resources.Load<TextAsset>(jsonPath.Split(".json")[0]);

    if (jsonAsset == null)
    {
      Debug.LogWarning($"Failed to load JSON data from: {jsonPath}");
      yield break;
    }

    string jsonText = jsonAsset.text;

    // 解析JSON資料
    try
    {
      JsonData danceData = JsonUtility.FromJson<JsonData>(jsonText);
      BeatTimes = new List<float>(danceData.beat_times);
      RmsValues = new List<float>(danceData.rms_values);
      Debug.Log($"JSON data loaded successfully. Beat times: {BeatTimes.Count}, RMS values: {RmsValues.Count}");
    }
    catch (System.Exception e)
    {
      Debug.LogError($"Error parsing JSON data: {e.Message}");
    }

    yield return null;
  }

  // 載入音樂資料
  private IEnumerator LoadAudioData(string danceId)
  {
    string audioPath = $"{danceId}/{WAV_FILENAME}";
    AudioClip clip = Resources.Load<AudioClip>(audioPath.Split(".wav")[0]);

    if (clip == null)
    {
      Debug.LogError($"Failed to load audio from: {audioPath}");
      yield break;
    }

    DanceAudio = clip;
    audioSource.clip = DanceAudio;
    Debug.Log($"Audio loaded successfully. Duration: {DanceAudio.length}s");

    yield return null;
  }

  // 下載音樂檔案
  private IEnumerator DownloadAudio(string danceId, System.Action<bool> onComplete)
  {
    string url = $"{httpService.baseUrl}/static/uploads/{danceId}/{WAV_FILENAME}";
    Debug.Log($"開始下載音樂: {url}");

    using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
    {
      yield return webRequest.SendWebRequest();

      if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
          webRequest.result == UnityWebRequest.Result.ProtocolError)
      {
        Debug.LogError($"下載音樂失敗: {webRequest.error}");
        onComplete?.Invoke(false);
        yield break;
      }

      try
      {
        // 獲取下載的音樂資料
        AudioClip clip = DownloadHandlerAudioClip.GetContent(webRequest);

        // 確保 Resources 目錄存在
        string resourcesPath = Application.dataPath + "/Resources";
        if (!System.IO.Directory.Exists(resourcesPath))
        {
          System.IO.Directory.CreateDirectory(resourcesPath);
        }

        // 確保 ID 資料夾存在
        string idFolderPath = $"{resourcesPath}/{danceId}";
        if (!System.IO.Directory.Exists(idFolderPath))
        {
          System.IO.Directory.CreateDirectory(idFolderPath);
        }

        // 保存音樂檔案 (這需要特殊處理，因為 AudioClip 不能直接寫入檔案)
        // 在這裡需要使用 WAV 檔案的原始資料
        byte[] audioData = webRequest.downloadHandler.data;
        string filePath = $"{idFolderPath}/{WAV_FILENAME}";
        System.IO.File.WriteAllBytes(filePath, audioData);

        Debug.Log($"音樂下載成功，保存至: {filePath}");

        onComplete?.Invoke(true);
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"保存音樂檔案失敗: {ex.Message}");
        onComplete?.Invoke(false);
      }
    }
  }

  // 下載 JSON 檔案
  private IEnumerator DownloadJson(string danceId, System.Action<bool> onComplete)
  {
    string url = $"{httpService.baseUrl}/static/uploads/{danceId}/{JSON_FILENAME}";
    Debug.Log($"開始下載 JSON 檔案: {url}");

    using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
    {
      yield return webRequest.SendWebRequest();

      if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
          webRequest.result == UnityWebRequest.Result.ProtocolError)
      {
        Debug.LogError($"下載 JSON 檔案失敗: {webRequest.error}");
        onComplete?.Invoke(false);
        yield break;
      }

      try
      {
        // 獲取下載的資料
        byte[] data = webRequest.downloadHandler.data;

        // 確保 Resources 目錄存在
        string resourcesPath = Application.dataPath + "/Resources";
        if (!System.IO.Directory.Exists(resourcesPath))
        {
          System.IO.Directory.CreateDirectory(resourcesPath);
        }

        // 確保 ID 資料夾存在
        string idFolderPath = $"{resourcesPath}/{danceId}";
        if (!System.IO.Directory.Exists(idFolderPath))
        {
          System.IO.Directory.CreateDirectory(idFolderPath);
        }

        // 保存文件
        string filePath = $"{idFolderPath}/{JSON_FILENAME}";
        System.IO.File.WriteAllBytes(filePath, data);

        Debug.Log($"JSON 檔案下載成功，保存至: {filePath}");

        onComplete?.Invoke(true);
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"保存 JSON 檔案失敗: {ex.Message}");
        onComplete?.Invoke(false);
      }
    }
  }

  // 修改後的CSV載入方法，返回是否成功
  public bool LoadCSVData(string csvFilePath)
  {
    FramesData.Clear();
    TextAsset _row_data = Resources.Load<TextAsset>(csvFilePath.Split(".csv")[0]);

    if (_row_data == null)
    {
      Debug.LogError($"Failed to load CSV data from: {csvFilePath}");
      return false;
    }

    string csv_row_data = _row_data.text;
    // 開始讀取CSV檔案
    string[] csvLines = csv_row_data.Split("\n");

    foreach (string line in csvLines)
    {
      // 解析每行CSV資料
      string[] values = line.Split(',');

      if (values.Length == 0 || values.Length == 1) { continue; }

      if (values.Length != 72)
      {
        Debug.LogError("Each line must contain exactly 72 values (24 joints * 3 coordinates). Line: " + line);
        continue;
      }

      List<Vector3> jointsPositions = new List<Vector3>();

      for (int i = 0; i < 72; i += 3)
      {
        // 轉換成三個float，作為x, y, z位置
        float x = float.Parse(values[i]);
        float y = float.Parse(values[i + 1]);
        float z = float.Parse(values[i + 2]);

        // 將每個關節的3D座標儲存為Vector3
        Vector3 jointPosition = new Vector3(x, y, z);
        jointsPositions.Add(jointPosition);
      }

      // 將這一個frame的所有關節位置儲存到framesData
      FramesData.Add(jointsPositions);
    }
    CopyFrameData2TempFrameData();
    Debug.Log("!!!! CSV data loaded successfully. Total frames: " + FramesData.Count);

    // 加載完資料後，更新到第一幀
    SetFrame(0);

    return true;
  }

  // 播放音樂和動作
  public void PlayWithAudio()
  {
    if (!IsPlaying && TotalFrames > 0 && DanceAudio != null)
    {
      float audioTime = (float)CurrentFrameIndex / FrameRate;
      audioSource.time = audioTime;

      Play();
      audioSource.Play();
    }
  }

  // 暫停音樂和動作
  public void PauseWithAudio()
  {
    if (IsPlaying)
    {
      Pause();
      audioSource.Pause();
    }
  }

  // 切換播放/暫停狀態（包括音樂）
  public void TogglePlayPauseWithAudio()
  {
    if (IsPlaying)
    {
      PauseWithAudio();
    }
    else
    {
      PlayWithAudio();
    }
  }

  public void CopyFrameData2TempFrameData()
  {
    TempFramesData.Clear(); // 清空 tempFramesData，避免覆蓋舊資料

    foreach (var frame in FramesData)
    {
      List<Vector3> frameCopy = new List<Vector3>(frame); // copy 每個 frame
      TempFramesData.Add(frameCopy); // 添加 copy 的 frame 到 tempFramesData
    }
  }

  public void ResetTempFrameData(int frameIndex)
  {
    if (frameIndex >= 0 && frameIndex < FramesData.Count)
    {
      TempFramesData[frameIndex] = new List<Vector3>(FramesData[frameIndex]);
    }
  }

  public void EditPose(int jointIndex, int startFrameIndex, Vector3 startValue, int endFrameIndex, Vector3 endValue)
  {
    if (startFrameIndex < 0 || endFrameIndex >= TempFramesData.Count || startFrameIndex > endFrameIndex)
    {
      Debug.LogError("Invalid frame range.");
      return;
    }

    for (int frame = startFrameIndex; frame <= endFrameIndex; frame++)
    {
      float t = (float)(frame - startFrameIndex) / (endFrameIndex - startFrameIndex);
      Vector3 interpolatedValue = Vector3.Lerp(startValue, endValue, t);
      TempFramesData[frame][jointIndex] = interpolatedValue;
    }

    // 如果當前幀在編輯範圍內，更新顯示
    if (CurrentFrameIndex >= startFrameIndex && CurrentFrameIndex <= endFrameIndex)
    {
      UpdateCubeParentJointPositions(CurrentFrameIndex);
    }
  }

  public void UpdateJointPosition(int frameIndex, string jointName, Vector3 newPosition)
  {
    int jointIndex = GetJointIndex(jointName);
    if (jointIndex != -1 && frameIndex >= 0 && frameIndex < TempFramesData.Count)
    {
      TempFramesData[frameIndex][jointIndex] = newPosition;

      // 如果更新的是當前幀，則立即更新顯示
      if (frameIndex == CurrentFrameIndex)
      {
        UpdateCubeParentJointPositions(CurrentFrameIndex);
      }
    }
  }

  public List<Vector3> GetScaledJointPositions(int frameIndex)
  {
    if (frameIndex < 0 || frameIndex >= TempFramesData.Count)
    {
      Debug.LogError("Invalid frame index: " + frameIndex);
      return new List<Vector3>();
    }

    List<Vector3> jointsPositions = new List<Vector3>();
    List<Vector3> originalPositions = TempFramesData[frameIndex];

    foreach (Vector3 pos in originalPositions)
    {
      Vector3 scaledPos = new Vector3(
          pos.x * Scale_x,
          pos.y * Scale_y,
          pos.z * Scale_z
      );
      jointsPositions.Add(scaledPos);
    }

    return jointsPositions;
  }

  public int GetJointIndex(string jointName)
  {
    // 根據 Joint 名稱回傳對應的index
    switch (jointName)
    {
      case "hip": return 0;
      case "lThighBend": return 2;
      case "rThighBend": return 1;
      case "abdomenUpper": return 3;
      case "lShin": return 5;
      case "rShin": return 4;
      case "spine": return 6;
      case "lFoot": return 8;
      case "rFoot": return 7;
      case "spine2": return 9;
      case "lToe": return 11;
      case "rToe": return 10;
      case "neck": return 12;
      case "lMid1": return 14;
      case "rMid1": return 13;
      case "head": return 15;
      case "lShldrBend": return 17;
      case "rShldrBend": return 16;
      case "lForearmBend": return 19;
      case "rForearmBend": return 18;
      case "lHand": return 21;
      case "rHand": return 20;
      case "lThumb2": return 23;
      case "rThumb2": return 22;
      default: return -1;
    }
  }
  public void Test() { Debug.Log("Test"); }

  // 播放控制方法
  public void Play()
  {
    Debug.Log("Play() called. IsPlaying: " + IsPlaying);
    if (!IsPlaying && TotalFrames > 0)
    {
      IsPlaying = true;
      if (playbackCoroutine != null)
      {
        StopCoroutine(playbackCoroutine);
      }
      playbackCoroutine = StartCoroutine(PlaybackCoroutine());
      OnPlayStateChanged?.Invoke(IsPlaying);
    }
  }

  public void Pause()
  {
    Debug.Log("Pause() called. IsPlaying: " + IsPlaying);
    if (IsPlaying)
    {
      IsPlaying = false;
      if (playbackCoroutine != null)
      {
        StopCoroutine(playbackCoroutine);
        playbackCoroutine = null;
      }
      OnPlayStateChanged?.Invoke(IsPlaying);
    }
  }

  public void TogglePlayPause()
  {
    if (IsPlaying)
    {
      Pause();
    }
    else
    {
      Play();
    }
  }

  // 設置當前幀
  public void SetFrame(int frameIndex)
  {
    frameIndex = Mathf.Clamp(frameIndex, 0, TotalFrames - 1);
    if (frameIndex != CurrentFrameIndex || CubeParent == null)
    {
      CurrentFrameIndex = frameIndex;
      UpdateCubeParentJointPositions(CurrentFrameIndex);
      OnFrameChanged?.Invoke(CurrentFrameIndex);
    }
  }

  // 前進一幀
  public void NextFrame()
  {
    int nextFrame = (CurrentFrameIndex + 1) % TotalFrames;
    SetFrame(nextFrame);
  }

  // 後退一幀
  public void PreviousFrame()
  {
    int prevFrame = (CurrentFrameIndex - 1 + TotalFrames) % TotalFrames;
    SetFrame(prevFrame);
  }

  // 跳到特定幀
  public void JumpToFrame(int frameIndex)
  {
    SetFrame(frameIndex);
    if (DanceAudio != null)
    {
      float audioTime = (float)frameIndex / FrameRate;
      audioSource.time = audioTime;
    }
  }

  // 更新 CubeParent 的關節位置
  public void UpdateCubeParentJointPositions(int frameIndex)
  {
    if (CubeParent == null)
    {
      Debug.LogWarning("CubeParent is not assigned!");
      return;
    }

    frameIndex = Mathf.Clamp(frameIndex, 0, TempFramesData.Count - 1);
    List<Vector3> jointsPositions = GetScaledJointPositions(frameIndex);

    // 從CubeParent取得所有子物件
    foreach (Transform child in CubeParent.transform)
    {
      switch (child.name)
      {
        case "hip":
          child.localPosition = jointsPositions[0];  // 是24個的第幾個
          break;
        case "lThighBend":
          child.localPosition = jointsPositions[2];
          break;
        case "rThighBend":
          child.localPosition = jointsPositions[1];
          break;
        case "abdomenUpper":
          child.localPosition = jointsPositions[3];
          break;
        case "lShin":
          child.localPosition = jointsPositions[5];
          break;
        case "rShin":
          child.localPosition = jointsPositions[4];
          break;
        case "spine":
          child.localPosition = jointsPositions[6];
          break;
        case "lFoot":
          child.localPosition = jointsPositions[8];
          break;
        case "rFoot":
          child.localPosition = jointsPositions[7];
          break;
        case "spine2":
          child.localPosition = jointsPositions[9];
          break;
        case "lToe":
          child.localPosition = jointsPositions[11];
          break;
        case "rToe":
          child.localPosition = jointsPositions[10];
          break;
        case "neck":
          child.localPosition = jointsPositions[12];
          break;
        case "lMid1":
          child.localPosition = jointsPositions[14];
          break;
        case "rMid1":
          child.localPosition = jointsPositions[13];
          break;
        case "head":
          child.localPosition = jointsPositions[15];
          break;
        case "lShldrBend":
          child.localPosition = jointsPositions[17];
          break;
        case "rShldrBend":
          child.localPosition = jointsPositions[16];
          break;
        case "lForearmBend":
          child.localPosition = jointsPositions[19];
          break;
        case "rForearmBend":
          child.localPosition = jointsPositions[18];
          break;
        case "lHand":
          child.localPosition = jointsPositions[21];
          break;
        case "rHand":
          child.localPosition = jointsPositions[20];
          break;
        case "lThumb2":
          child.localPosition = jointsPositions[23];
          break;
        case "rThumb2":
          child.localPosition = jointsPositions[22];
          break;
        default:
          // Debug.LogWarning("Unknown joint: " + child.name);
          break;
      }
    }
  }

  public void SetPlayRange(int startFrame, int endFrame, bool useRange = true)
  {
    PlayRangeStart = Mathf.Clamp(startFrame, 0, TotalFrames - 1);
    PlayRangeEnd = Mathf.Clamp(endFrame, PlayRangeStart, TotalFrames - 1);
    UsePlayRange = useRange;
    if (UsePlayRange && (CurrentFrameIndex < PlayRangeStart || CurrentFrameIndex > PlayRangeEnd))
    {
      JumpToFrame(PlayRangeStart);
    }
    Debug.Log($"設置播放範圍：{PlayRangeStart} 到 {PlayRangeEnd}，啟用：{UsePlayRange}");
  }
  #endregion

  #region Private Methods
  private IEnumerator PlaybackCoroutine()
  {
    float frameDuration = 1f / FrameRate;
    float timeAccumulator = 0f;

    while (IsPlaying)
    {
      timeAccumulator += Time.deltaTime;

      if (timeAccumulator >= frameDuration)
      {
        // 如果啟用了播放範圍限制
        if (UsePlayRange && PlayRangeStart <= PlayRangeEnd)
        {
          // 如果當前幀已經到達範圍結尾，跳回範圍開始
          if (CurrentFrameIndex >= PlayRangeEnd)
          {
            JumpToFrame(PlayRangeStart);
          }
          else
          {
            // 否則前進一幀
            int nextFrame = CurrentFrameIndex + 1;
            if (nextFrame > PlayRangeEnd)
            {
              nextFrame = PlayRangeStart;
              if (DanceAudio != null && audioSource.isPlaying)
              {
                float audioTime = (float)PlayRangeStart / FrameRate;
                audioSource.time = audioTime;
              }
            }
            SetFrame(nextFrame);
          }
        }
        else
        {
          NextFrame();
          if (DanceAudio != null && audioSource.isPlaying)
          {
            audioSource.time = 0;
          }
        }

        // 減去已經用掉的時間，防止跳幀
        timeAccumulator -= frameDuration;
      }

      yield return null;
    }
  }
  #endregion
}

// 用於解析資料夾列表的類別
[System.Serializable]
public class FolderListResponse
{
  public bool success;
  public List<string> dances;
}

// 用於解析 JSON 資料的類別
[System.Serializable]
public class JsonData
{
  public float[] beat_times;
  public float[] rms_values;
}