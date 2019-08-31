using UnityEngine;
using System.Collections;

public enum CursorState { None = 0, Idle, Over, Clicked }

namespace InteraktVR
{
    public class InteractionCursorController : MonoBehaviour
    {
        [SerializeField] private Texture defCursor;
        [SerializeField] private Texture idleCursor;
        [SerializeField] private Texture overCursor;
        [SerializeField] private Texture clickedCursor;

        Texture cursorImage;
        private CursorState cursorState = CursorState.None;
        [HideInInspector] public Vector2 cursorPos;

        private bool isMouseDown = false;
        private float maxDistance = 4.0f;
        private Vector2 cursorSize = new Vector2(25, 25);

        UIInteractableObject objectItemMenuController;

        public CursorState CursorState
        {
            get => cursorState;

            set
            {
                if (value != cursorState)
                {
                    cursorState = value;
                    switch (cursorState)
                    {
                        case CursorState.Over:
                            cursorImage = overCursor;
                            break;

                        case CursorState.Clicked:
                            cursorImage = clickedCursor;
                            break;
                        default:
                            cursorImage = idleCursor;
                            break;
                    }
                }
            }
        }

        IEnumerator Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
            yield return null;
            CursorState = CursorState.Idle;
        }

        private void OnGUI()
        {
            GUI.depth = -1;
            Rect pos;

            pos = new Rect(Screen.width / 2 - cursorSize.x / 2, Screen.height / 2 - cursorSize.y / 2, cursorSize.x, cursorSize.y);
            cursorPos = new Vector2(pos.x, pos.y);
            GUI.Label(pos, cursorImage);
        }

        //TODO: REMOVE ENTER / EXIT / CLICK MAGIC
        void Update()
        {
            Ray ray = Camera.main.ScreenPointToRay(cursorPos);
            RaycastHit hit;

            if (Input.GetMouseButtonUp(0)) isMouseDown = false;
            if (isMouseDown) CursorState = CursorState.Clicked;

            if (Physics.Raycast(ray, out hit, 100.0f))
            {
                UIInteractableObject newObjectItemMenuController = hit.collider.gameObject.GetComponent<UIInteractableObject>();

                if (newObjectItemMenuController != null)
                {
                    if (newObjectItemMenuController != objectItemMenuController)
                    {
                        if (objectItemMenuController != null) objectItemMenuController.Exit();
                        objectItemMenuController = newObjectItemMenuController;
                    }

                    objectItemMenuController.Enter();
                }
            }
            else
            {
                if (objectItemMenuController != null)
                {
                    objectItemMenuController.Exit();
                    objectItemMenuController = null;
                }
            }

            if (objectItemMenuController != null && Input.GetMouseButtonDown(0)) objectItemMenuController.Click();

            if (Physics.Raycast(ray, out hit, 100.0f))
            {
                if (hit.collider.gameObject.LookupComponent<VRInteraction.VRInteractableItem>() && hit.distance < maxDistance)
                {
                    if (!isMouseDown) CursorState = CursorState.Over;

                    if (Input.GetMouseButtonDown(0))
                    {
                        isMouseDown = true;
                        CursorState = CursorState.Clicked;
                    }
                }
                else
                {
                    if (!isMouseDown) CursorState = CursorState.Idle;
                }
            }
        }

    }
}
