using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dancing : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
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

    // Update is called once per frame
    void Update()
    {

    }
}
