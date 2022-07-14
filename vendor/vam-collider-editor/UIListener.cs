using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ColliderEditor
{
    public class UIListener : MonoBehaviour, IPointerClickHandler
    {
        public readonly UnityEvent onDisabled = new UnityEvent();
        public readonly UnityEvent onEnabled = new UnityEvent();
        public readonly UnityEvent onClick = new UnityEvent();

        public void OnPointerClick(PointerEventData eventData)
        {
            onClick.Invoke();
        }

        public void OnDisable()
        {
            onDisabled.Invoke();
        }

        public void OnEnable()
        {
            onEnabled.Invoke();
        }

        private void OnDestroy()
        {
            onDisabled.RemoveAllListeners();
            onClick.RemoveAllListeners();
        }
    }
}
