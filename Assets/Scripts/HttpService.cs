using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class HttpService : MonoBehaviour
{
  // 單例模式實現
  private static HttpService _instance;
  public static HttpService Instance
  {
    get
    {
      if (_instance == null)
      {
        GameObject go = new GameObject("HttpService");
        _instance = go.AddComponent<HttpService>();
        DontDestroyOnLoad(go);
      }
      return _instance;
    }
  }

  // 伺服器基礎 URL
  public string baseUrl = "http://140.118.162.43:8443"; // 預設值，可以在初始化時更改

  // 設置基礎 URL
  public void SetBaseUrl(string url)
  {
    baseUrl = url;
    Debug.Log($"HttpService: Base URL set to {baseUrl}");
  }

  // 發送 GET 請求
  public void Get(string endpoint, Action<string> onSuccess, Action<string> onError = null)
  {
    StartCoroutine(GetRequest(endpoint, onSuccess, onError));
  }

  // 發送 POST 請求
  public void Post(string endpoint, string jsonData, Action<string> onSuccess, Action<string> onError = null)
  {
    StartCoroutine(PostRequest(endpoint, jsonData, onSuccess, onError));
  }

  // 測試 API 連線
  public void TestConnection(Action<bool, string> onComplete)
  {
    Get("/", (response) =>
    {
      Debug.Log($"API 連線測試成功: {response}");
      onComplete?.Invoke(true, response);
    }, (error) =>
    {
      Debug.LogError($"API 連線測試失敗: {error}");
      onComplete?.Invoke(false, error);
    });
  }

  // 發送動作生成請求
  public void GenerateDance(string prompt, int startFrame, int endFrame, Action<List<List<Vector3>>> onSuccess, Action<string> onError = null)
  {
    // 建立請求資料
    DanceGenerationRequest request = new DanceGenerationRequest
    {
      prompt = prompt,
      startFrame = startFrame,
      endFrame = endFrame
    };

    string jsonData = JsonUtility.ToJson(request);

    // 發送請求
    Post("/generate", jsonData, (response) =>
    {
      try
      {
        // 解析回應
        DanceGenerationResponse danceResponse = JsonUtility.FromJson<DanceGenerationResponse>(response);

        // 將回應轉換為 Vector3 列表
        List<List<Vector3>> framesData = ConvertToFramesData(danceResponse.frames);

        // 呼叫成功回調
        onSuccess?.Invoke(framesData);
      }
      catch (Exception ex)
      {
        Debug.LogError($"Error parsing dance generation response: {ex.Message}");
        onError?.Invoke($"解析回應失敗: {ex.Message}");
      }
    }, onError);
  }

  // 發送編輯請求
  public void EditMotion(string csvId, string prompt, int startFrame, int endFrame, float fps, Action<bool, string> onComplete)
  {
    // 將幀數轉換為秒數
    float startTime = startFrame / fps;
    float endTime = endFrame / fps;

    // 建立請求資料
    EditMotionRequest request = new EditMotionRequest
    {
      preId = csvId,
      settings = new EditMotionSettings
      {
        // startTime = 1,
        // endTime = 10,
        startTime = startTime,
        endTime = endTime,
        description = prompt
      }
    };

    string jsonData = JsonUtility.ToJson(request);
    Debug.Log($"發送編輯請求: {jsonData}");

    // 發送請求
    Post("/edit", jsonData, (response) =>
    {
      try
      {
        // 解析回應
        EditMotionResponse editResponse = JsonUtility.FromJson<EditMotionResponse>(response);

        if (editResponse.success)
        {
          Debug.Log($"編輯成功，新ID: {editResponse.id}");
          onComplete?.Invoke(true, editResponse.id);
        }
        else
        {
          Debug.LogError($"編輯失敗: {editResponse.message}");
          onComplete?.Invoke(false, editResponse.message);
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"解析編輯回應失敗: {ex.Message}");
        onComplete?.Invoke(false, $"解析回應失敗: {ex.Message}");
      }
    }, (error) =>
    {
      Debug.LogError($"編輯請求失敗: {error}");
      onComplete?.Invoke(false, error);
    });
  }

  // 將伺服器回傳的幀資料轉換為 Vector3 列表
  private List<List<Vector3>> ConvertToFramesData(List<FrameData> framesData)
  {
    List<List<Vector3>> result = new List<List<Vector3>>();

    foreach (var frame in framesData)
    {
      List<Vector3> jointPositions = new List<Vector3>();

      // 假設 joints 是一個包含所有關節位置的列表
      foreach (var joint in frame.joints)
      {
        Vector3 position = new Vector3(joint.x, joint.y, joint.z);
        jointPositions.Add(position);
      }

      result.Add(jointPositions);
    }

    return result;
  }

  // GET 請求協程
  private IEnumerator GetRequest(string endpoint, Action<string> onSuccess, Action<string> onError)
  {
    string url = baseUrl + endpoint;
    using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
    {
      // 發送請求
      yield return webRequest.SendWebRequest();

      if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
          webRequest.result == UnityWebRequest.Result.ProtocolError)
      {
        Debug.LogError($"HTTP Error: {webRequest.error}");
        onError?.Invoke(webRequest.error);
      }
      else
      {
        string response = webRequest.downloadHandler.text;
        onSuccess?.Invoke(response);
      }
    }
  }

  // POST 請求協程
  private IEnumerator PostRequest(string endpoint, string jsonData, Action<string> onSuccess, Action<string> onError)
  {
    string url = baseUrl + endpoint;
    using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
    {
      byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
      webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
      webRequest.downloadHandler = new DownloadHandlerBuffer();
      webRequest.SetRequestHeader("Content-Type", "application/json");

      // 發送請求
      yield return webRequest.SendWebRequest();

      if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
          webRequest.result == UnityWebRequest.Result.ProtocolError)
      {
        Debug.LogError($"HTTP Error: {webRequest.error}");
        onError?.Invoke(webRequest.error);
      }
      else
      {
        string response = webRequest.downloadHandler.text;
        onSuccess?.Invoke(response);
      }
    }
  }

  // 下載 CSV 文件 (修改為使用固定檔名)
  public void DownloadCSV(string danceId, Action<bool> onComplete)
  {
    string url = $"{baseUrl}/static/uploads/{danceId}/music_joints.csv";
    StartCoroutine(DownloadFile(url, danceId, "music_joints.csv", onComplete));
  }
  // 獲取所有可用的舞蹈ID
  public void ListDances(Action<List<string>> onSuccess, Action<string> onError = null)
  {
    Get("/list_dances", (response) =>
    {
      try
      {
        // 解析回應
        DanceListResponse listResponse = JsonUtility.FromJson<DanceListResponse>(response);

        if (listResponse.success)
        {
          Debug.Log($"成功獲取舞蹈列表，共有 {listResponse.dances.Count} 個舞蹈");
          onSuccess?.Invoke(listResponse.dances);
        }
        else
        {
          Debug.LogError($"獲取舞蹈列表失敗: {listResponse.message}");
          onError?.Invoke(listResponse.message);
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"解析舞蹈列表回應失敗: {ex.Message}");
        onError?.Invoke($"解析回應失敗: {ex.Message}");
      }
    }, (error) =>
    {
      Debug.LogError($"獲取舞蹈列表請求失敗: {error}");
      onError?.Invoke(error);
    });
  }

  // 下載文件協程 (修改為支援固定檔名)
  private IEnumerator DownloadFile(string url, string danceId, string fileName, Action<bool> onComplete)
  {
    Debug.Log($"開始下載檔案: {url}");

    using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
    {
      // 發送請求
      yield return webRequest.SendWebRequest();

      if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
          webRequest.result == UnityWebRequest.Result.ProtocolError)
      {
        Debug.LogError($"下載檔案失敗: {webRequest.error}");
        onComplete?.Invoke(false);
      }
      else
      {
        try
        {
          // 獲取下載的數據
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

          // 保存文件到 ID 資料夾下，使用固定檔名
          string filePath = $"{idFolderPath}/{fileName}";
          System.IO.File.WriteAllBytes(filePath, data);

          Debug.Log($"檔案下載成功，保存至: {filePath}");

          // 如果在編輯器模式下，刷新資源資料庫
#if UNITY_EDITOR
          UnityEditor.AssetDatabase.Refresh();
#endif

          onComplete?.Invoke(true);
        }
        catch (System.Exception ex)
        {
          Debug.LogError($"保存檔案失敗: {ex.Message}");
          onComplete?.Invoke(false);
        }
      }
    }
  }
}

// 請求和回應的資料結構
[Serializable]
public class DanceGenerationRequest
{
  public string prompt;
  public int startFrame;
  public int endFrame;
}

[Serializable]
public class DanceGenerationResponse
{
  public string status;
  public string message;
  public List<FrameData> frames;
}

[Serializable]
public class FrameData
{
  public List<JointData> joints;
}

[Serializable]
public class JointData
{
  public float x;
  public float y;
  public float z;
}

// 新增編輯動作的請求和回應結構
[Serializable]
public class EditMotionRequest
{
  public string preId;
  public EditMotionSettings settings;
}

[Serializable]
public class EditMotionSettings
{
  public float startTime;
  public float endTime;
  public string description;
}
[Serializable]
public class EditMotionResponse
{
  public bool success;
  public string message;
  public string id;
  public string error_code;
}

[Serializable]
public class DanceListResponse
{
  public bool success;
  public string message;
  public List<string> dances;
  public string error_code;
}