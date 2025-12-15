using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DanceLDM : MonoBehaviour
{
    public string baseUrl = "http://140.118.162.43:8443";
    public string danceId = "demo";
    public Transform SkeletonCube;
    private DanceDataManager dataManager;
    // Start is called before the first frame update
    void Start()
    {
        GetComponents();
        Debug.Log("!!!!! hi");
        LoadDanceData();
        if (dataManager != null)
        {
            dataManager.OnDataLoaded += OnDataLoaded;
        }
        dataManager.PlayWithAudio();
    }

    // Update is called once per frame
    void Update()
    {

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
    private void LoadDanceData()
    {
        dataManager.CubeParent = SkeletonCube;
        // dataManager.CubeParent = SkeletonCube;
        dataManager.LoadById(danceId);
        Debug.Log("你好");
    }

    private void OnDataLoaded(string danceId, bool success)
    {
        if (success)
        {
            Debug.Log($"資料 {danceId} 載入成功，更新 UI");
            dataManager.PlayWithAudio();

        }
    }
}
