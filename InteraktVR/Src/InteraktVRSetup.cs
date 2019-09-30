using System;
using System.Collections;
using UnityEngine;

namespace InteraktVR
{
    public class InteraktVRSetup : MonoBehaviour
    {
        private static InteraktVRSetup instance;

        enum EnableRigMode { VR, StandaloneSimulator }

        [SerializeField] EnableRigMode enableRigMode;
        [SerializeField] GameObject vrRig;
        [SerializeField] GameObject vrSimulatorRig;

        private VRBodyAlias bodyAlias;
        private VRInteraction.VRInput leftController;
        private VRInteraction.VRInput rightController;

        public static bool IsVRSimulated => instance?.enableRigMode == EnableRigMode.StandaloneSimulator; //TODO: need this at editor time
        public static bool IsReady { get; private set; } = false;

        public static Transform Body => instance?.bodyAlias?.transform;
        public static VRInteraction.VRInput LeftController => instance?.leftController;
        public static VRInteraction.VRInput RightController => instance?.rightController;

        private void OnEnable()
        {
            instance = this;
        }

        IEnumerator Start()
        {
            vrRig.SetActive(enableRigMode == EnableRigMode.VR);
            vrSimulatorRig.SetActive(enableRigMode == EnableRigMode.StandaloneSimulator);

            yield return null;
            bodyAlias = GetComponentInChildren<VRBodyAlias>();

            VRInteraction.VRInput[] vrInputs = GetComponentsInChildren<VRInteraction.VRInput>();

            for (int i = 0; i < 2; i++)
            {
                if (vrInputs[i].IsLeftHand) leftController = vrInputs[i];
                else rightController = vrInputs[i];
            }

            yield return null;

            IsReady = true;
        }

        internal static void SetupTeleport(VRTeleporter rightTeleporter, VRTeleporter leftTeleporter)
        {
            leftTeleporter.bodyTransforn = Body;
            rightTeleporter.bodyTransforn = Body;

            leftTeleporter.transform.parent = LeftController.transform;
            leftTeleporter.transform.localPosition = Vector3.zero;

            rightTeleporter.transform.parent = RightController.transform;
            rightTeleporter.transform.localPosition = Vector3.zero;
        }

    }
}
