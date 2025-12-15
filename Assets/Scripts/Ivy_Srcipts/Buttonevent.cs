using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Buttonevent : MonoBehaviour
{
    public List<DropSlot> allSlots;

    public Button ApplyButton;
    public void ClearAllSlots()
    {
        Debug.Log("Cancel==========");
        foreach (var slot in allSlots)
        {
            slot.ClearSlot();
        }
    }

    public void Apply()
    {
        Debug.Log("APPly2");
        for (int i = 0; i < allSlots.Count; i++)
        {
            PlayerPrefs.SetString($"Slot{i + 1}_ImageName", allSlots[i].imageName);
            Debug.Log($"Slot {i + 1} Image: {allSlots[i].imageName}");
        }
        PlayerPrefs.Save();

        string left = PlayerPrefs.GetString($"Slot{1}_ImageName", "None");
        string center = PlayerPrefs.GetString($"Slot{2}_ImageName", "None");
        string right = PlayerPrefs.GetString($"Slot{3}_ImageName", "None");

        GameObject leftObj = null;
        GameObject centerObj = null;
        GameObject rightObj = null;

        GameObject leftskeleton = null;
        GameObject centerskeleton = null;
        GameObject rightskeleton = null;
        GameObject camera = null;


        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == left)
            {
                leftObj = obj;
            }
            else if (obj.name == center)
            {
                centerObj = obj;
            }
            else if (obj.name == right)
            {
                rightObj = obj;
            }

            if (obj.name == "SkeletonCube_" + left)
            {
                leftskeleton = obj;
            }
            else if (obj.name == "SkeletonCube_" + center)
            {
                centerskeleton = obj;
            }
            else if (obj.name == "SkeletonCube_" + right)
            {
                rightskeleton = obj;
            }
            else if (obj.name == "OVRCameraRig")
            {
                camera = obj;
            }
        }


        if (left != "None" && center != "None" && right != "None") //三個人
        {
            Debug.Log(left + " " + center + " " + right);
            Debug.Log(leftskeleton + " " + centerskeleton + " " + rightskeleton);
            leftObj.SetActive(true);
            centerObj.SetActive(true);
            rightObj.SetActive(true);

            leftskeleton.SetActive(true);
            centerskeleton.SetActive(true);
            rightskeleton.SetActive(true);

            Vector3 cameraPos = camera.transform.position;
            leftskeleton.transform.position = new Vector3(cameraPos.x - 1f, cameraPos.y, cameraPos.z + 5f);
            centerskeleton.transform.position = new Vector3(cameraPos.x, cameraPos.y, cameraPos.z + 4f);
            rightskeleton.transform.position = new Vector3(cameraPos.x + 1f, cameraPos.y, cameraPos.z + 5f);

        }

    }
    public void Backtoselect()
    {
        SceneManager.LoadScene("character_selection");
    }
    void Start()
    {
        Debug.Log("!!!ssss");
        ApplyButton.onClick.AddListener(Apply);
    }
}

