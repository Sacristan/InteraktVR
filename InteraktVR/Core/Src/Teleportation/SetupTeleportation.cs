using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteraktVR.Core
{
    public class SetupTeleportation : MonoBehaviour
    {
        [SerializeField] VRTeleporter rightTeleporter;
        [SerializeField] VRTeleporter leftTeleporter;

        IEnumerator Start()
        {
            yield return new WaitUntil(() => InteraktVRSetup.IsReady);
            InteraktVRSetup.SetupTeleport(rightTeleporter, leftTeleporter);
        }

    }
}
