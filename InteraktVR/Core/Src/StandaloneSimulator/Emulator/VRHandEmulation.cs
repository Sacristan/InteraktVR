using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteraktVR.Core
{
    public class VRHandEmulation : MonoBehaviour
    {
        [SerializeField] private bool isRightHand = true;
        public bool IsRightHand => isRightHand;
    }
}

