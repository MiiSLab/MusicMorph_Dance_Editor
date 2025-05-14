using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Clicknext.Utils
{
    public class HighlightButton : MonoBehaviour, IPointerDownHandler, IPointerExitHandler, IPointerUpHandler
    {
        private bool isPointer;
        public void OnPointerDown(PointerEventData eventData) => isPointer = true;

        public void OnPointerExit(PointerEventData eventData) => isPointer = false;

        public void OnPointerUp(PointerEventData eventData) => isPointer = false;

        [SerializeField] private float _scale = 1.2f;

        private Vector3 _localScale;
        private Selectable selectable;
        protected virtual void Awake()
        {
            selectable = GetComponent<Selectable>();
            _localScale = transform.localScale;
        }

        void LateUpdate()
        {
            if (!selectable)
                return;

            if (!selectable.interactable)
                isPointer = false;

            transform.localScale = Vector3.Lerp(transform.localScale,
                isPointer ? _localScale * _scale : _localScale,
                Time.unscaledDeltaTime * 20f);
        }
    }
}
