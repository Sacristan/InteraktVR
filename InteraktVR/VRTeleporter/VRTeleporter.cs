using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRTeleporter : MonoBehaviour
{
    [SerializeField] GameObject positionMarker; // marker for display ground position
    [SerializeField] LineRenderer arcRenderer;

    [SerializeField] LayerMask excludeLayers; // excluding for performance

    [SerializeField] float angle = 45f; // Arc take off angle

    [SerializeField] float strength = 10f; // Increasing this value will increase overall arc length

    int maxVertexcount = 100; // limitation of vertices for performance. 

    internal Transform bodyTransform; // target transferred by teleport
    private float vertexDelta = 0.08f; // Delta between each Vertex on arc. Decresing this value may cause performance problem.

    private Vector3 velocity; // Velocity of latest vertex

    private Vector3 groundPos; // detected ground position

    private Vector3 lastNormal; // detected surface normal

    private bool groundDetected = false;
    private List<Vector3> vertexList = new List<Vector3>(); // vertex on arc

    private bool displayActive = false; // don't update path when it's false.

    public VRInteraction.VRInput VRInput { get; set; } = null;

    // Teleport target transform to ground position
    public void Teleport()
    {
        if (groundDetected)
        {
            bodyTransform.position = groundPos + lastNormal * 0.1f;
        }
        else
        {
            Debug.Log("Ground wasn't detected");
        }
    }

    // Active Teleporter Arc Path
    public void ToggleDisplay(bool active)
    {
        arcRenderer.enabled = active;
        positionMarker.SetActive(active);
        displayActive = active;
    }

    private void Awake()
    {
        // arcRenderer = GetComponentInChildren<LineRenderer>();
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
            if (displayActive)
            {
                Teleport();
                ToggleDisplay(false);
            }
        }
    }

    private void FixedUpdate()
    {
        if (displayActive)
        {
            UpdatePath();
        }
    }


    private void UpdatePath()
    {
        groundDetected = false;

        vertexList.Clear(); // delete all previouse vertices


        velocity = Quaternion.AngleAxis(-angle, transform.right) * transform.forward * strength;

        RaycastHit hit;


        Vector3 pos = transform.position; // take off position

        vertexList.Add(pos);

        while (!groundDetected && vertexList.Count < maxVertexcount)
        {
            Vector3 newPos = pos + velocity * vertexDelta
                + 0.5f * Physics.gravity * vertexDelta * vertexDelta;

            velocity += Physics.gravity * vertexDelta;

            vertexList.Add(newPos); // add new calculated vertex

            // linecast between last vertex and current vertex
            if (Physics.Linecast(pos, newPos, out hit, ~excludeLayers))
            {
                groundDetected = true;
                groundPos = hit.point;
                lastNormal = hit.normal;
            }
            pos = newPos; // update current vertex as last vertex
        }


        positionMarker.SetActive(groundDetected);

        if (groundDetected)
        {
            positionMarker.transform.position = groundPos + lastNormal * 0.1f;
            positionMarker.transform.LookAt(groundPos);
        }

        // Update Line Renderer

        arcRenderer.positionCount = vertexList.Count;
        arcRenderer.SetPositions(vertexList.ToArray());
    }


}