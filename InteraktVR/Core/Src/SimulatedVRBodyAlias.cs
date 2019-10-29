using System;
using UnityEngine;

namespace InteraktVR.Core
{
    public class SimulatedVRBodyAlias : VRBodyAlias
    {
        private const float YOffset = 0.91f;

        internal override void Teleport(Vector3 loc, Vector3 surfaceOffset)
        {
            CharacterMotor characterMotor = GetComponent<CharacterMotor>();

            characterMotor.enabled = false;
            surfaceOffset.y = YOffset;

            base.Teleport(loc, surfaceOffset);

            characterMotor.enabled = true;

        }
    }
}
