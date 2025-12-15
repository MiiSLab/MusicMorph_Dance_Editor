using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;

public class PromptConfirm : MonoBehaviour
{
    bool sendcheck = false;
    public Material pano;
    public TMP_InputField inputfield;

    UdpSocket udp;

    public Button saveButton;
    public GameObject waitMsg;

    // 添加對 DanceDataManager 的引用
    private DanceDataManager danceDataManager;

    private int skyboxOrder = 0;

    private void Start()
    {
        udp = GetComponent<UdpSocket>();
        if (!inputfield) Debug.Log("There is no such component on the object!");
        string cwd = System.IO.Directory.GetCurrentDirectory();
        int index = cwd.LastIndexOf("\\");
        if (index >= 0)
            cwd = cwd.Substring(0, index) + "\\pyServer\\External\\";
        PlayerPrefs.SetString("fileDir", cwd);
        Debug.Log(PlayerPrefs.GetString("fileDir") + " is file directory");

        // 獲取 DanceDataManager 的引用
        danceDataManager = FindObjectOfType<DanceDataManager>();
        if (!danceDataManager) Debug.LogWarning("找不到 DanceDataManager 元件!");
    }

    // Update is called once per frame    
    public void SceneGeneration()
    {
        if (!sendcheck)
        {
            string rawInput = inputfield.text;
            rawInput = rawInput.Replace("'", "");
            string sockMsg = "0," + rawInput;
            PlayerPrefs.SetString("prompt", rawInput.Replace(" ", "_"));
            PlayerPrefs.Save();
            Debug.Log(rawInput);
            Debug.Log(sockMsg);
            udp.SendData(sockMsg);
            waitMsg.SetActive(true);
            sendcheck = true;
        }
    }

    void Update()
    {
        if (udp.pano_state && sendcheck)
        {
            //SceneManager.LoadScene("Option");
            SkyboxPreview();
        }
    }

    public void SkyboxPreview()
    {
        //Debug.Log("You have clicked the button!");
        StartCoroutine(DelaySeconds(0.1f));
    }

    IEnumerator DelaySeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        string directory = PlayerPrefs.GetString("fileDir") + "\\pano\\" + PlayerPrefs.GetString("prompt");
        var rawData1 = System.IO.File.ReadAllBytes(directory + "_0.png");
        Texture2D tex1 = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex1.LoadImage(rawData1);
        pano.mainTexture = tex1;
        RenderSettings.skybox = pano;
        waitMsg.SetActive(false);
        saveButton.gameObject.SetActive(true);
        sendcheck = false;
    }

    // 新增 SaveScene 函數
    public void SaveScene()
    {
        if (pano.mainTexture == null)
        {
            Debug.LogError("無法儲存場景：天空盒貼圖尚未載入");
            return;
        }

        try
        {
            // 獲取當前的 DANCEID
            string currentDanceId = danceDataManager.CurrentDanceId;
            if (string.IsNullOrEmpty(currentDanceId))
            {
                Debug.LogError("無法儲存場景：找不到當前的 DANCEID");
                return;
            }

            // 獲取原始貼圖
            string sourceDirectory = PlayerPrefs.GetString("fileDir") + "\\pano\\" + PlayerPrefs.GetString("prompt");
            string sourceFilePath = sourceDirectory + "_0.png";

            if (!File.Exists(sourceFilePath))
            {
                Debug.LogError("無法儲存場景：找不到原始貼圖檔案 " + sourceFilePath);
                return;
            }

            // 確保 Resources 目錄下的 DANCEID 資料夾存在
            string resourcesPath = Application.dataPath + "/Resources";
            string danceIdFolderPath = $"{resourcesPath}/{currentDanceId}";

            if (!Directory.Exists(danceIdFolderPath))
            {
                Directory.CreateDirectory(danceIdFolderPath);
                Debug.Log($"建立資料夾: {danceIdFolderPath}");
            }

            // 複製並重新命名為 scene.png
            string destFilePath = $"{danceIdFolderPath}/scene.png";
            File.Copy(sourceFilePath, destFilePath, true); // true 表示如果檔案已存在則覆蓋

            Debug.Log($"場景已儲存至: {destFilePath}");

            // 刷新 Asset 資料庫，以便 Unity 可以找到新的檔案
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"儲存場景時發生錯誤: {e.Message}");
        }
    }

    public void CloseWin()
    {
        if (inputfield != null)
        {
            inputfield.text = "";
        }
        
        // 獲取當前的 DANCEID
        string currentDanceId = danceDataManager.CurrentDanceId;
        string scenePath = $"{Application.dataPath}/Resources/{currentDanceId}/scene.png";
        
        // 檢查是否存在已儲存的場景
        if (File.Exists(scenePath))
        {
            LoadTexture(scenePath);
        }
        else
        {
            // 使用預設天空盒
            string defaultPath = $"{Application.dataPath}/Resources/skybox/default.png";
            if (File.Exists(defaultPath))
            {
                LoadTexture(defaultPath);
            }
        }
    }

    private void LoadTexture(string filePath)
    {
        byte[] rawData = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(rawData);
        pano.mainTexture = tex;
        RenderSettings.skybox = pano;
    }
}