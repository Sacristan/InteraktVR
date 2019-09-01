using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace InteraktVR
{

    public class UIInteractableObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private UnityEvent onClickEvent;

        private Color originalColor;
        private Texture originalTexture;
        private Renderer _renderer;
        private Color highlightColor = Color.yellow;

        private void Start()
        {
            _renderer = GetComponent<Renderer>();
            originalColor = _renderer.material.color;
            originalColor = _renderer.material.color;
            originalTexture = _renderer.material.mainTexture;
        }

        public void Enter()
        {
            _renderer.material.color = highlightColor;
            _renderer.material.mainTexture = null;
        }

        public void Exit()
        {
            _renderer.material.color = originalColor;
            _renderer.material.mainTexture = originalTexture;
        }

        public void Click()
        {
            onClickEvent?.Invoke();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Enter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Exit();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Click();
        }
    }
}
