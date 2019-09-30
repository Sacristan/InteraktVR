using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetupTeleportation : MonoBehaviour
{
    [SerializeField] VRTeleporter rightTeleporter;
    [SerializeField] VRTeleporter leftTeleporter;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => InteraktVR.InteraktVRSetup.IsReady);
        InteraktVR.InteraktVRSetup.SetupTeleport(rightTeleporter, leftTeleporter);
    }

}
