using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.Avatar
{
    public class CameraMover : MonoBehaviour
    {
        [Serializable]
        struct Data
        {
            public Toggle toggle;
            public Transform location;
            public Animator animator;
            public ParticleSystem particle;
            public GameObject ui;
            public GameObject objectGroup;
        }

        [SerializeField] Button closeBtn;
        [SerializeField] GameObject controlGroup;
        [SerializeField] GameObject selectedParticle;
        [SerializeField] GameObject gridCanvas;
        [SerializeField] Camera mainRoomCamera;
        [SerializeField] Transform startLocaton;
        [SerializeField] ToggleGroup toggleGroup;
        [SerializeField] CanvasGroup canvasBlock;
        [SerializeField] Data[] data;

        private float cameraSpeed = 4f;
        private Transform currentLoation;
        private const string selection = "select";

        void Awake()
        {
            canvasBlock.alpha = 1f;
            canvasBlock.blocksRaycasts = true;

            mainRoomCamera.transform.position = startLocaton.position;
            mainRoomCamera.transform.rotation = startLocaton.rotation;

            currentLoation = startLocaton;

            foreach (var each in data)
            {
                each.toggle.onValueChanged.AddListener(ison =>
                {
                    if (ison)
                        SelectCharacter(each);

                    var canvasGroup = each.toggle.GetComponent<CanvasGroup>();
                    canvasGroup.blocksRaycasts = !ison;

                    SetLayerChilds(each.animator.transform.parent, ison);
                });

                if (each.ui)
                    each.ui.SetActive(false);
                if (each.objectGroup)
                    each.objectGroup.SetActive(false);
            }

            closeBtn.onClick.AddListener(ShowAllCharacters);

            ShowAllCharacters();
        }

        IEnumerator Start()
        {
            yield return null;
            canvasBlock.alpha = 1f;
            canvasBlock.blocksRaycasts = true;
            while (canvasBlock.alpha > 0f)
            {
                yield return null;
                canvasBlock.alpha -= Time.deltaTime * 0.75f;
            }

            canvasBlock.alpha = 0f;
            canvasBlock.blocksRaycasts = false;
        }

        private void ShowAllCharacters()
        {
            ClearUI();
            SetCharacters(true);
            ClearAnimations();
            toggleGroup.SetAllTogglesOff();

            gridCanvas.SetActive(false);
            controlGroup.SetActive(false);
            selectedParticle.SetActive(false);

            currentLoation = startLocaton;
        }

        void Update()
        {
            mainRoomCamera.transform.position = Vector3.Lerp(
                mainRoomCamera.transform.position,
                currentLoation.transform.position,
                Time.deltaTime * cameraSpeed);

            mainRoomCamera.transform.rotation = Quaternion.Slerp(
                mainRoomCamera.transform.rotation,
                currentLoation.transform.rotation,
                Time.deltaTime * cameraSpeed);
        }

        private void ClearAnimations()
        {
            foreach (var each in data)
                each.animator.SetBool(selection, false);
        }

        private void SelectCharacter(Data data)
        {
            gridCanvas.SetActive(true);
            controlGroup.SetActive(true);
            selectedParticle.SetActive(true);

            ClearAnimations();
            currentLoation = data.location;
            SetCharacters(false, data);
            data.animator.SetBool(selection, true);
        }

        private void SetCharacters(bool value, Data selectedData)
        {
            var selected = selectedData.animator.transform.parent.gameObject;
            SetCharacters(value, selected);
            SetUI(selectedData);

            if (selected)
                if (!selected.activeInHierarchy)
                {
                    selected.SetActive(true);
                    selectedData.particle.Play();
                }
        }

        private void SetCharacters(bool value, GameObject selected = null)
        {
            foreach (var each in data)
            {
                var character = each.animator.transform.parent.gameObject;
                if (character != selected)
                {
                    if (character.activeInHierarchy != value)
                        each.particle.Play();

                    character.SetActive(value);
                }
            }
        }

        private void SetLayerChilds(Transform character, bool value)
        {
            foreach (Transform trans in character.GetComponentsInChildren<Transform>(true))
            {
                trans.gameObject.layer = value ?
                    LayerMask.NameToLayer("TransparentFX") :
                    LayerMask.NameToLayer("Default");
            }
        }

        private void SetUI(Data selectedData)
        {
            ClearUI();
            if (selectedData.ui)
                selectedData.ui.SetActive(true);
            if (selectedData.objectGroup)
                selectedData.objectGroup.SetActive(true);
        }

        private void ClearUI()
        {
            foreach (var each in data)
            {
                if (each.ui)
                    each.ui.SetActive(false);
                if (each.objectGroup)
                    each.objectGroup.SetActive(false);
            }
        }
    }
}