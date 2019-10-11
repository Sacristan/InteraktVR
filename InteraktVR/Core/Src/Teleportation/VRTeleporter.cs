using UnityEngine;
using System.Collections.Generic;

namespace InteraktVR
{
    public class VRTeleporter : MonoBehaviour
    {
        const int MaxVertexcount = 100;
        const float VertexDelta = 0.08f;

        [SerializeField] GameObject positionMarker;
        [SerializeField] LineRenderer arcRenderer;
        [SerializeField] LayerMask excludeLayers;
        [SerializeField] float arcAngle = 45f;
        [SerializeField] float arcLength = 10f;

        private Vector3 lastVertexVelocity;
        private Vector3 detectedGroundPos;
        private Vector3 lastDetectedSurfaceNormal;

        private bool groundDetected = false;
        private List<Vector3> vertexList = new List<Vector3>();
        private bool isBeingDisplayed = false;

        #region Properties
        public Transform BodyTransform { get; set; }
        public VRInteraction.VRInput VRInput { get; set; } = null;
        #endregion

        private void Awake()
        {
            arcRenderer.enabled = false;
            positionMarker.SetActive(false);
        }

        private void Update()
        {
            if (!VRInput) return;

            if (VRInput.ActionPressed(VRInteraction.GlobalKeys.KEY_TELEPORT))
            {
                ToggleDisplay(true);
            }
            else
            {
                if (isBeingDisplayed)
                {
                    Teleport();
                    ToggleDisplay(false);
                }
            }
        }

        private void FixedUpdate()
        {
            if (isBeingDisplayed)
            {
                UpdatePath();
            }
        }

        public void Teleport()
        {
            if (groundDetected)
            {
                BodyTransform.position = detectedGroundPos + lastDetectedSurfaceNormal * 0.1f;
            }
            else
            {
                Debug.Log("Ground wasn't detected");
            }
        }

        public void ToggleDisplay(bool active)
        {
            arcRenderer.enabled = active;
            positionMarker.SetActive(active);
            isBeingDisplayed = active;
        }


        private void UpdatePath()
        {
            groundDetected = false;

            vertexList.Clear();

            lastVertexVelocity = Quaternion.AngleAxis(-arcAngle, transform.right) * transform.forward * arcLength;

            Vector3 pos = transform.position;

            vertexList.Add(pos);

            while (!groundDetected && vertexList.Count < MaxVertexcount)
            {
                Vector3 newPos = pos + lastVertexVelocity * VertexDelta + 0.5f * Physics.gravity * VertexDelta * VertexDelta;
                lastVertexVelocity += Physics.gravity * VertexDelta;

                vertexList.Add(newPos);

                if (Physics.Linecast(pos, newPos, out RaycastHit hit, ~excludeLayers))
                {
                    groundDetected = true;
                    detectedGroundPos = hit.point;
                    lastDetectedSurfaceNormal = hit.normal;
                }
                pos = newPos;
            }


            positionMarker.SetActive(groundDetected);

            if (groundDetected)
            {
                positionMarker.transform.position = detectedGroundPos + lastDetectedSurfaceNormal * 0.1f;
                positionMarker.transform.LookAt(detectedGroundPos);
            }

            arcRenderer.positionCount = vertexList.Count;
            arcRenderer.SetPositions(vertexList.ToArray());
        }
    }
}