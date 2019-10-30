using System;
using UnityEngine;

namespace InteraktVR.Core
{
    public class VRBodyModel : MonoBehaviour
    {
        internal virtual void Teleport(Vector3 loc, Vector3 surfaceOffset)
        {
            transform.position = loc + surfaceOffset;
        }
    }
}
