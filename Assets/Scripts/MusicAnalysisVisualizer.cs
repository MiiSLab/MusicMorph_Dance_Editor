using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicAnalysisVisualizer : MonoBehaviour
{
  [Header("參考")]
  public DanceDataManager dataManager;
  public RectTransform visualizationContainer; // UI容器，用於放置視覺化元素

  [Header("設置")]
  public Color beatColor = Color.red;
  public Color rmsColor = Color.blue;
  public float beatMarkerHeight = 20f;
  public float rmsMarkerMaxHeight = 50f;
  public float markerWidth = 2f;

  [Header("Prefab")]
  public GameObject markerPrefab; // 一個簡單的UI圖像Prefab

  private List<GameObject> beatMarkers = new List<GameObject>();
  private List<GameObject> rmsMarkers = new List<GameObject>();
  private float containerWidth;
  private float totalDuration; // 音樂總時長

  private void Start()
  {
    if (dataManager != null)
    {
      dataManager.OnDataLoaded += OnDataLoaded;
    }

    if (visualizationContainer != null)
    {
      containerWidth = visualizationContainer.rect.width;
    }
  }

  private void OnDataLoaded(string danceId, bool success)
  {
    if (success)
    {
      ClearVisualization();
      CreateVisualization();
    }
  }

  private void ClearVisualization()
  {
    // 清除現有的標記
    foreach (var marker in beatMarkers)
    {
      Destroy(marker);
    }
    beatMarkers.Clear();

    foreach (var marker in rmsMarkers)
    {
      Destroy(marker);
    }
    rmsMarkers.Clear();
  }

  private void CreateVisualization()
  {
    if (dataManager == null || dataManager.DanceAudio == null ||
        visualizationContainer == null || markerPrefab == null)
    {
      Debug.LogWarning("無法創建音樂分析視覺化：缺少必要組件");
      return;
    }

    totalDuration = dataManager.DanceAudio.length;
    containerWidth = visualizationContainer.rect.width;

    // 創建節拍標記
    CreateBeatMarkers();

    // 創建音量強度標記
    CreateRmsMarkers();
  }

  private void CreateBeatMarkers()
  {
    if (dataManager.BeatTimes == null || dataManager.BeatTimes.Count == 0)
    {
      Debug.LogWarning("沒有節拍資料可視覺化");
      return;
    }

    foreach (float beatTime in dataManager.BeatTimes)
    {
      // 計算位置（基於時間在總時長中的比例）
      float xPosition = (beatTime / totalDuration) * containerWidth;

      // 創建 marker
      GameObject marker = Instantiate(markerPrefab, visualizationContainer);
      RectTransform rt = marker.GetComponent<RectTransform>();
      rt.anchoredPosition = new Vector2(xPosition, 0);
      rt.sizeDelta = new Vector2(markerWidth, beatMarkerHeight);

      // 設置顏色
      Image image = marker.GetComponent<Image>();
      if (image != null)
      {
        image.color = beatColor;
      }

      beatMarkers.Add(marker);
    }
  }

  private void CreateRmsMarkers()
  {
    if (dataManager.RmsValues == null || dataManager.RmsValues.Count == 0)
    {
      Debug.LogWarning("沒有RMS資料可視覺化");
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

    for (int i = 0; i < dataManager.RmsValues.Count; i++)
    {
      float rmsValue = dataManager.RmsValues[i];
      float normalizedRms = maxRms > 0 ? rmsValue / maxRms : 0;

      // 計算位置和高度
      float xPosition = i * markerSpacing;
      float height = normalizedRms * rmsMarkerMaxHeight;

      // 創建 marker
      GameObject marker = Instantiate(markerPrefab, visualizationContainer);
      RectTransform rt = marker.GetComponent<RectTransform>();
      rt.anchoredPosition = new Vector2(xPosition, -beatMarkerHeight - 5); // 放在節拍標記下方
      rt.sizeDelta = new Vector2(markerWidth, height);

      // 設置顏色
      Image image = marker.GetComponent<Image>();
      if (image != null)
      {
        image.color = rmsColor;
      }

      rmsMarkers.Add(marker);
    }
  }

  // 更新當前播放位置指示器
  public void UpdatePlaybackIndicator(float currentTime)
  {
    float position = (currentTime / totalDuration) * containerWidth;
  }

  private void OnDestroy()
  {
    if (dataManager != null)
    {
      dataManager.OnDataLoaded -= OnDataLoaded;
    }
  }
}