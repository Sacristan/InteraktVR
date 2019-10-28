using UnityEngine;
using System.Collections.Generic;

namespace InteraktVR.Core
{
    public class VRTeleporter : MonoBehaviour
    {
        const uint MaxPathLookupTries = 30;
        const int MaxVertexcount = 100;
        const float VertexDelta = 0.08f;

        enum TeleportSurfaceMode
        {
            All,
            HorizontalOnly,
            VerticalOnly
        }

        [Header("General:")]
        [SerializeField] TeleportSurfaceMode teleportSurfaceMode = TeleportSurfaceMode.HorizontalOnly;
        [SerializeField] LayerMask excludeLayers;
        [SerializeField] LineRenderer arcRenderer;

        [Header("Position Marker:")]
        [SerializeField] GameObject positionMarkerRoot;
        [SerializeField] MeshRenderer positionMarkerBound;
        [SerializeField] MeshRenderer positionMarkerOK;
        [SerializeField] Material positionMarkerOKMaterial;
        [SerializeField] Gradient positionMarkerOKGradient;
        [SerializeField] MeshRenderer positionMarkerNOK;
        [SerializeField] Material positionMarkerNOKMaterial;
        [SerializeField] Gradient positionMarkerNOKGradient;

        [Header("Arc:")]
        [SerializeField] float arcAngle = 45f;
        [SerializeField] float arcLength = 10f;

        private Vector3 lastVertexVelocity;
        private Vector3 detectedGroundPos;
        private Vector3 lastDetectedSurfaceNormal;

        private bool groundDetected = false;
        private bool wasValidTeleporationSurface = false;
        private List<Vector3> vertexList = new List<Vector3>();
        private bool isBeingDisplayed = false;

        #region Properties
        public Transform BodyTransform { get; set; }
        public VRInteraction.VRInteractor VRInteractor { get; set; } = null;
        public VRInteraction.VRInput VRInput { get; set; } = null;

        #endregion

        private void Awake()
        {
            arcRenderer.enabled = false;
            positionMarkerRoot.SetActive(false);
        }

        private static bool IsHorizontalSurface(Vector3 normal)
        {
            // return ((int) normal.y) == 1;
            return Mathf.Approximately(normal.y, 1f);
        }

        private static bool IsVerticalSurface(Vector3 normal)
        {
            // return Mathf.Abs((int)normal.x) == 1 || Mathf.Abs((int)normal.z) == 1;
            return Mathf.Approximately(Mathf.Abs(normal.x), 1f) || Mathf.Approximately(Mathf.Abs(normal.z), 1f);
        }

        private bool IsValidTeleportationSurface()
        {
            // switch (teleportSurfaceMode)
            // {
            //     case TeleportSurfaceMode.HorizontalOnly:
            //         if (lastDetectedSurfaceNormal == Vector3.zero) return true;
            //         return IsHorizontalSurface(lastDetectedSurfaceNormal);
            //     case TeleportSurfaceMode.VerticalOnly:
            //         if (lastDetectedSurfaceNormal == Vector3.zero) return true;
            //         return IsVerticalSurface(lastDetectedSurfaceNormal);

            //     default:
            //         return true;
            // }

            if (lastDetectedSurfaceNormal == Vector3.zero) return true;
            return IsHorizontalSurface(lastDetectedSurfaceNormal);
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
                    if (wasValidTeleporationSurface) Teleport();
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
            positionMarkerRoot.SetActive(active);

            if (active)
            {
                positionMarkerOK.gameObject.SetActive(wasValidTeleporationSurface);
                positionMarkerNOK.gameObject.SetActive(!wasValidTeleporationSurface);

                positionMarkerBound.sharedMaterial = wasValidTeleporationSurface ? positionMarkerOKMaterial : positionMarkerNOKMaterial;
                arcRenderer.colorGradient = wasValidTeleporationSurface ? positionMarkerOKGradient : positionMarkerNOKGradient;
            }

            isBeingDisplayed = active;
        }

        private void UpdatePath()
        {
            groundDetected = false;
            wasValidTeleporationSurface = false;

            vertexList.Clear();

            lastVertexVelocity = Quaternion.AngleAxis(-arcAngle, transform.right) * transform.forward * arcLength;

            Vector3 pos = transform.position;

            vertexList.Add(pos);

            uint currentPathLookupTries = 0;
            while (!groundDetected && vertexList.Count < MaxVertexcount)
            {
                if (++currentPathLookupTries >= MaxPathLookupTries) break;

                Vector3 newPos = pos + lastVertexVelocity * VertexDelta + 0.5f * Physics.gravity * VertexDelta * VertexDelta;
                lastVertexVelocity += Physics.gravity * VertexDelta;

                vertexList.Add(newPos);

                if (Physics.Linecast(pos, newPos, out RaycastHit hit, ~excludeLayers))
                {
                    wasValidTeleporationSurface = IsValidTeleportationSurface();
                    groundDetected = true;
                    detectedGroundPos = hit.point;
                    lastDetectedSurfaceNormal = hit.normal;
                }
                pos = newPos;
            }

            positionMarkerRoot.SetActive(groundDetected);

            if (groundDetected)
            {
                positionMarkerRoot.transform.position = detectedGroundPos + lastDetectedSurfaceNormal * 0.1f;
                positionMarkerRoot.transform.LookAt(detectedGroundPos);
            }

            arcRenderer.positionCount = vertexList.Count;
            arcRenderer.SetPositions(vertexList.ToArray());
        }
    }
}