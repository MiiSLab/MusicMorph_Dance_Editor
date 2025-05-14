using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;
using System.IO;

public class DanceEditor : MonoBehaviour
{
  #region public variables

  static bool demo = true;

  public float Scale_x = 1f, Scale_y = 1f, Scale_z = 1f;
  public Transform CubeParent = null;
  public bool Playing = true;
  public Slider StartSlider, EndSlider;
  public Button SaveButton;
  public float PinchThreshold = 0.9f; // 手勢 Pinch 強度門檻
  public float Sensitivity = 10f; // 拖動靈敏度
  public float slideDuration = .6f; // 動畫持續時間

  public float JointScaleSize = 2f;
  public float rayLength = 10f;
  public Image ActiveJointImage;
  public Color JointColor = new Color(255f / 255f, 255f / 160f, 0);
  public GameObject spherePrefab; // 用於拖動 Joint 的 Sphere 預製體

  [SerializeField] private string csvFilePath = "Dodoro_clip";
  [SerializeField] private DanceDataManager dataManager;

  #endregion

  // ------------------------------------------------------------------

  #region private variables

  private int currentFrameIndex = 0, startFrameIndex = 0, endFrameIndex = 0;
  private OVRHand leftHand, rightHand, activeHand;
  private Transform currentJoint; // 當前正在編輯的 Joint
  private GameObject currentSphere; // 當前 Joint 上的 Sphere
  private Renderer currentSphereRenderer; // Sphere 的 Renderer
  private bool isDragging = false;
  private Vector3 initialHandPosition, initialJointPosition;
  private string selectedJoint = "";
  private string editMode = "";

  #endregion

  private void Start()
  {
    if (dataManager == null)
    {
      dataManager = GetComponent<DanceDataManager>();
      if (dataManager == null)
      {
        dataManager = gameObject.AddComponent<DanceDataManager>();
      }
    }

    // 將 Scale 值傳遞給 DanceDataManager
    dataManager.Scale_x = Scale_x;
    dataManager.Scale_y = Scale_y;
    dataManager.Scale_z = Scale_z;

    InitSettings();
    dataManager.LoadCSVData(csvFilePath);
    StartCoroutine(AutoPlayFrames());
    UpdateJointPositions((currentFrameIndex + 1) % dataManager.TotalFrames);
  }

  private void Update()
  {
    HandleKeyboardInput();
    HandleJointDragging();
  }

  private void InitSettings()
  {
    leftHand = GameObject.Find("LeftHandAnchor").GetComponent<OVRHand>();
    rightHand = GameObject.Find("RightHandAnchor").GetComponent<OVRHand>();

    if (SaveButton != null) { SaveButton.onClick.AddListener(OnSaveButtonClick); }

    StartSlider.onValueChanged.AddListener(OnStartSliderValueChanged);
    EndSlider.onValueChanged.AddListener(OnEndSliderValueChanged);
    StartSlider.wholeNumbers = true;
    EndSlider.wholeNumbers = true;
    ActiveJointImage.color = new Color(0, 0, 0, 0);
  }

  private IEnumerator AutoPlayFrames()
  {
    // 每幀的播放時間，60FPS
    float frameDuration = 1f / 60f;
    float timeAccumulator = 0f;

    while (true)
    {
      if (Playing) // 只有當 playing 為 true 時才進行播放
      {
        // 使用 Time.deltaTime 進行累計，確保在不同設備上播放速度一致
        timeAccumulator += Time.deltaTime;

        // 當累積時間大於或等於一幀所需時間時，更新動畫
        if (timeAccumulator >= frameDuration)
        {
          // 切換到下一幀
          currentFrameIndex = (currentFrameIndex + 1) % dataManager.TotalFrames;
          UpdateJointPositions(currentFrameIndex);

          // 減去已經用掉的時間，防止跳幀
          timeAccumulator -= frameDuration;
        }
      }

      // 等待下一幀
      yield return null;
    }
  }

  private void UpdateJointPositions(int frameIndex)
  {
    frameIndex %= dataManager.TotalFrames;

    if (frameIndex < 0 || frameIndex >= dataManager.TotalFrames)
    {
      Debug.LogError("Invalid frame index." + frameIndex);
      return;
    }

    List<Vector3> jointsPositions = dataManager.GetScaledJointPositions(frameIndex);

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

  private void HandleKeyboardInput()
  {
    // 切換frame的簡單方式，按左右方向鍵切換
    if (Input.GetKeyDown(KeyCode.RightArrow))
    {
      currentFrameIndex += 1;
      UpdateJointPositions(currentFrameIndex);
    }
    if (Input.GetKeyDown(KeyCode.LeftArrow))
    {
      currentFrameIndex -= 1;
      UpdateJointPositions(currentFrameIndex);
    }

    if (Input.GetKeyDown(KeyCode.Space)) { Playing = !Playing; }
    if (Input.GetKeyDown(KeyCode.Keypad1)) { OnEditStartFrameButtonClick(); }
    if (Input.GetKeyDown(KeyCode.Keypad2))
    {
      StartCoroutine(TriggerButtonAnimation(SaveButton));
      ActiveJointImage.color = Color.clear;
      SetCurrentJoint("-");
    }
    if (Input.GetKeyDown(KeyCode.Keypad3)) { OnEditEndFrameButtonClick(); }

    if (Input.GetKeyDown(KeyCode.Keypad4))
    {
      StartCoroutine(SmoothSlideTo(180f, StartSlider));
    }
    if (Input.GetKeyDown(KeyCode.Keypad5)) { StartCoroutine(SmoothSlideTo(185f, StartSlider)); }
    if (Input.GetKeyDown(KeyCode.Keypad6)) { StartCoroutine(SmoothSlideTo(360f, EndSlider)); }

    if (Input.GetKeyDown(KeyCode.Keypad7))
    {
      ActiveJointImage.transform.localPosition = new Vector3(403, -77, 0);
      ActiveJointImage.color = JointColor;
      SetCurrentJoint("lHand");
    }
    if (Input.GetKeyDown(KeyCode.Keypad8))
    {
      ActiveJointImage.color = JointColor;
      ActiveJointImage.transform.localPosition = new Vector3(403, -194, 0);
      SetCurrentJoint("lFoot");
    }
    if (Input.GetKeyDown(KeyCode.Keypad0))
    {
      ActiveJointImage.color = Color.clear;
      SetCurrentJoint("-");
    }
    if (Input.GetKeyDown(KeyCode.KeypadEnter))
    {
      currentFrameIndex = 0;
      Playing = true;
    }
  }

  private void OnEditStartFrameButtonClick()
  {
    editMode = "start";
    UpdateJointPositions(startFrameIndex);
    currentFrameIndex = startFrameIndex;
  }

  private void OnEditEndFrameButtonClick()
  {
    editMode = "end";
    UpdateJointPositions(endFrameIndex);
    currentFrameIndex = endFrameIndex;
  }

  private void OnSaveButtonClick()
  {
    for (int i = 0; i < dataManager.TempFramesData[startFrameIndex].Count; i++)
    {
      dataManager.EditPose(i, startFrameIndex, dataManager.TempFramesData[startFrameIndex][i],
                          endFrameIndex, dataManager.TempFramesData[endFrameIndex][i]);
    }
  }

  private void OnStartSliderValueChanged(float value)
  {
    editMode = "start";
    int _value = (int)value;
    StartSlider.value = _value;
    if (startFrameIndex == _value) { return; }
    dataManager.ResetTempFrameData(startFrameIndex);
    startFrameIndex = _value;
    UpdateJointPositions(startFrameIndex);
    currentFrameIndex = startFrameIndex;
  }

  private void OnEndSliderValueChanged(float value)
  {
    editMode = "end";
    int _value = (int)value;
    EndSlider.value = _value;
    if (endFrameIndex == _value) { return; }
    dataManager.ResetTempFrameData(endFrameIndex);
    endFrameIndex = _value;
    UpdateJointPositions(endFrameIndex);
    currentFrameIndex = endFrameIndex;
  }

  private void HandleJointDragging()
  {
    if (currentJoint == null) return;

    float leftPinchStrength = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
    float rightPinchStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

    if (leftPinchStrength > PinchThreshold)
    {
      activeHand = leftHand;
      if (!isDragging) { StartDragging(); }
    }
    else if (rightPinchStrength > PinchThreshold)
    {
      activeHand = rightHand;
      if (!isDragging) { StartDragging(); }
    }
    else if (isDragging) { StopDragging(); }

    if (isDragging && activeHand != null) { UpdateJointPosition(); }
  }

  private void StartDragging()
  {
    isDragging = true;
    currentSphereRenderer.material.color = Color.green; // 拖動時顏色
    initialHandPosition = activeHand.transform.position;
    initialJointPosition = currentJoint.position;
  }

  private void StopDragging()
  {
    isDragging = false;
    activeHand = null;
    currentSphereRenderer.material.color = Color.white; // 恢復顏色
  }

  private void UpdateJointPosition()
  {
    Vector3 handDelta = activeHand.transform.position - initialHandPosition;
    currentJoint.position = initialJointPosition + handDelta * Sensitivity;

    if (editMode == "start")
    {
      dataManager.UpdateJointPosition(startFrameIndex, selectedJoint, currentJoint.localPosition);
    }
    else if (editMode == "end")
    {
      dataManager.UpdateJointPosition(endFrameIndex, selectedJoint, currentJoint.localPosition);
    }
  }

  private void SetCurrentJoint(string jointName)
  {
    selectedJoint = jointName;

    if (currentSphere != null) { Destroy(currentSphere); }

    currentJoint = CubeParent.Find(jointName);
    if (currentJoint == null)
    {
      Debug.Log($"找不到節點：{jointName}");
      return;
    }

    currentSphere = Instantiate(spherePrefab, currentJoint);
    currentSphere.transform.localPosition = Vector3.zero;
    currentSphere.transform.localScale = Vector3.one * JointScaleSize;
    currentSphereRenderer = currentSphere.GetComponent<Renderer>();

    if (currentSphere.GetComponent<Collider>() == null)
    {
      currentSphere.AddComponent<SphereCollider>();
    }
    if (currentSphere.GetComponent<Rigidbody>() == null)
    {
      Rigidbody rb = currentSphere.AddComponent<Rigidbody>();
      rb.isKinematic = true;
    }
    if (currentSphere.GetComponent<OVRGrabbable>() == null)
    {
      currentSphere.AddComponent<OVRGrabbable>();
    }

    Debug.Log($"當前編輯的關節已切換為：{jointName}");
  }

  private IEnumerator TriggerButtonAnimation(Button button)
  {
    PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
    // 模擬按下狀態
    button.OnPointerDown(pointerEventData);
    yield return new WaitForSeconds(0.2f); // 按下的時間間隔

    // 模擬釋放狀態
    button.OnPointerUp(pointerEventData);

    // 手動觸發 onClick 事件
    button.onClick.Invoke();
  }

  public IEnumerator SmoothSlideTo(float targetValue, Slider slider)
  {
    // 確保目標值在範圍內
    targetValue = Mathf.Clamp(targetValue, slider.minValue, slider.maxValue);

    // 獲取當前值
    float startValue = slider.value;

    // 計算滑動過程
    float elapsedTime = 0f;
    while (elapsedTime < slideDuration)
    {
      elapsedTime += Time.deltaTime;
      slider.value = Mathf.Lerp(startValue, targetValue, elapsedTime / slideDuration);

      // 觸發 onValueChanged
      slider.onValueChanged.Invoke(slider.value);

      yield return null;
    }

    // 最後設置到準確的目標值
    slider.value = targetValue;
    slider.onValueChanged.Invoke(targetValue);
  }
}