using UnityEngine;
using System.Collections;

namespace InteraktVR
{
    public class MyDragRigidbody : MonoBehaviour
    {
        [SerializeField] private float spring = 50.0f;
        [SerializeField] private float damper = 5.0f;
        [SerializeField] private float drag = 10.0f;
        [SerializeField] private float angularDrag = 5.0f;
        [SerializeField] private float maxDistance = 0.2f;
        [SerializeField] private float pushForce = 0.2f;
        [SerializeField] private bool attachToCenterOfMass = false;
        [SerializeField] private float screenY = 0.5f;
        [SerializeField] private bool removeKinematic = false;

        GameObject hightlightObject;

        SpringJoint springJoint;
        InteractionCursorController _interactionCursorController;

        float raycastDistance = 100.0f;

        Camera _camera;

        void Start()
        {
            _interactionCursorController = GetComponent<InteractionCursorController>();
            _camera = Camera.main;
        }

        void Update()
        {
            hightlightObject = null;
            if (springJoint != null && springJoint.connectedBody != null)
            {
                hightlightObject = springJoint.connectedBody.gameObject;
            }
            else
            {
                RaycastHit hitt;
                if (Physics.Raycast(_camera.ScreenPointToRay(_interactionCursorController.cursorPos), out hitt, raycastDistance))
                {
                    if (hitt.rigidbody && !hitt.rigidbody.isKinematic)
                    {
                        hightlightObject = hitt.rigidbody.gameObject;
                    }
                }
            }

            if (!Input.GetMouseButtonDown(0)) return;

            RaycastHit hit;

            if (!Physics.Raycast(_camera.ScreenPointToRay(_interactionCursorController.cursorPos), out hit, raycastDistance)) return;
            if (!hit.rigidbody) return;

            if (hit.rigidbody.isKinematic)
            {
                if (removeKinematic) hit.rigidbody.isKinematic = false;
                else return;
            }

            if (!springJoint)
            {
                GameObject go = new GameObject("Rigidbody dragger") as GameObject;
                Rigidbody body = go.AddComponent<Rigidbody>();
                springJoint = go.AddComponent<SpringJoint>();
                body.isKinematic = true;
            }
            springJoint.transform.position = hit.point;

            if (attachToCenterOfMass)
            {
                Vector3 anchor = transform.TransformDirection(hit.rigidbody.centerOfMass) + hit.rigidbody.transform.position;
                anchor = springJoint.transform.InverseTransformPoint(anchor);
                springJoint.anchor = anchor;
                springJoint.connectedBody = hit.rigidbody;
            }
            else
            {
                springJoint.spring = spring;
                springJoint.damper = damper;
                springJoint.maxDistance = maxDistance;
                springJoint.connectedBody = hit.rigidbody;
            }
            if (!dragging) StartCoroutine(DragObject(hit.distance, hit.point, _camera.ScreenPointToRay(_interactionCursorController.cursorPos).direction));
        }

        private bool dragging = false;
        IEnumerator DragObject(float distance, Vector3 hitpoint, Vector3 dir)
        {
            dragging = true;

            float startTime = Time.time;
            Vector2 mousePos = Input.mousePosition;

            float oldDrag = springJoint.connectedBody.drag;
            float oldAngularDrag = springJoint.connectedBody.angularDrag;

            springJoint.connectedBody.drag = drag;
            springJoint.connectedBody.angularDrag = angularDrag;

            // while (true)
            while (Input.GetMouseButton(0))
            {
                Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * screenY, 0f));
                springJoint.transform.position = ray.GetPoint(distance);

                yield return null;

                if (Input.GetMouseButtonUp(1))
                {
                    //print("MouseDown");
                    if (hightlightObject.GetComponent<Rigidbody>())
                    {
                        Vector3 vect = Vector3.Normalize(GameObject.FindWithTag("Player").transform.forward);
                        hightlightObject.GetComponent<Rigidbody>().AddForce(vect * 10, ForceMode.Impulse);
                        break;
                    }
                }
            }
            if (Mathf.Abs(mousePos.x - _interactionCursorController.cursorPos.x) <= 2 && Mathf.Abs(mousePos.y - _interactionCursorController.cursorPos.y) <= 2f && Time.time - startTime < .2f && springJoint.connectedBody)
            {
                dir.y = 0;
                dir.Normalize();
                springJoint.connectedBody.AddForceAtPosition(dir * pushForce, hitpoint, ForceMode.VelocityChange);
            }
            if (springJoint.connectedBody)
            {
                springJoint.connectedBody.drag = oldDrag;
                springJoint.connectedBody.angularDrag = oldAngularDrag;
                springJoint.connectedBody = null;
            }

            dragging = false;
        }
    }
}
