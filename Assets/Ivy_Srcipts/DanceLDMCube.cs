using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class DanceLDMCube : MonoBehaviour
{
    private string csvFilePath;

    public List<List<Vector3>> framesData = new List<List<Vector3>>();  // (900, 24)

    public GameObject CubeParent = null;

    private int currentFrame = 0;

    public float scale_x = 1f;
    public float scale_y = 1f;
    public float scale_z = 1f;

    public bool autoplay = true;

    public TMP_Text text1;

    private float playbackSpeed = 1f / 60f;

    // Start is called before the first frame update
    void Start()
    {
        TextAsset _row_data = Resources.Load<TextAsset>("Dodoro_clip");
        string csv_row_data = _row_data.text;
        LoadCSVData(csv_row_data);

        if (autoplay)
        {
            StartCoroutine(AutoPlayFrames());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!autoplay)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                currentFrame = (currentFrame + 1) % framesData.Count;
                UpdateJointPositions(currentFrame);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                currentFrame = (currentFrame - 1 + framesData.Count) % framesData.Count;
                UpdateJointPositions(currentFrame);
            }
        }
    }

    IEnumerator AutoPlayFrames()
    {
        while (autoplay)
        {
            // ?C?V?????????A60FPS
            float frameDuration = 1f / 60f;
            float timeAccumulator = 0f;

            while (true)
            {
                // ???Time.deltaTime?i???p?A?T?O?b???P?]??W????t??@?P
                timeAccumulator += Time.deltaTime;

                if (timeAccumulator >= frameDuration)
                {
                    currentFrame = (currentFrame + 1) % framesData.Count;
                    UpdateJointPositions(currentFrame);

                    timeAccumulator -= frameDuration;
                }

                yield return null;
            }
        }
    }


    void LoadCSVData(string data)
    {
        string[] csvLines = data.Split("\n");

        foreach (string line in csvLines)
        {
            string[] values = line.Split(',');

            if (values.Length == 0 || values.Length == 1)
            {
                continue;
            }

            if (values.Length != 72)
            {
                Debug.LogError("Each line must contain exactly 72 values (24 joints * 3 coordinates). Line: " + line);
                continue;
            }

            List<Vector3> jointsPositions = new List<Vector3>();

            for (int i = 0; i < 72; i += 3)
            {
                float x = float.Parse(values[i]);
                float y = float.Parse(values[i + 1]);
                float z = float.Parse(values[i + 2]);

                Vector3 jointPosition = new Vector3(x, y, z);
                jointsPositions.Add(jointPosition);
            }

            framesData.Add(jointsPositions);
        }

        Debug.Log("CSV data loaded successfully. Total frames: " + framesData.Count);
    }

    void UpdateJointPositions(int frameIndex)
    {
        if (frameIndex < 0 || frameIndex >= framesData.Count)
        {
            Debug.LogError("Invalid frame index.");
            return;
        }

        List<Vector3> _jointsPositions = framesData[frameIndex];
        List<Vector3> jointsPositions = new List<Vector3>();

        for (int i = 0; i < _jointsPositions.Count; i++)
        {
            Vector3 temp = _jointsPositions[i];
            temp.x = temp.x * scale_x;
            temp.y = temp.y * scale_y;
            temp.z = temp.z * scale_z;
            jointsPositions.Add(temp);
        }

        foreach (Transform child in CubeParent.transform)
        {
            switch (child.name)
            {
                case "hip":
                    child.localPosition = jointsPositions[0];
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
                    Debug.LogWarning("Unknown joint: " + child.name);
                    break;
            }
        }
    }
}

