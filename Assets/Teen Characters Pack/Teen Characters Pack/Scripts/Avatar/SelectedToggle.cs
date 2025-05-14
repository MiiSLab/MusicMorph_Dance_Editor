using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.Avatar
{
    public class SelectedToggle : MonoBehaviour
    {
        [SerializeField] GameObject[] unselects;
        [SerializeField] GameObject[] selects;

        void Awake()
        {
            var toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(SetSelect);
        }

        private void SetSelect(bool ison)
        {
            foreach (var each in unselects)
                each.SetActive(!ison);

            foreach (var each in selects)
                each.SetActive(ison);
        }
    }
}
