using System;
using System.Collections;
using UnityEngine;

namespace InteraktVR.Core
{
    public class InteraktVRSetup : MonoBehaviour
    {
        private static InteraktVRSetup instance;

        enum EnableRigMode { VR, StandaloneSimulator }

        [SerializeField] EnableRigMode enableRigMode;
        [SerializeField] GameObject vrRig;
        [SerializeField] GameObject vrSimulatorRig;

        private VRBodyModel bodyModel;
        private VRInteraction.VRInput leftController;
        private VRInteraction.VRInput rightController;

        [SerializeField] private BodyAlias aliasBody;
        [SerializeField] private RightHandAlias aliasHandR;
        [SerializeField] private LeftHandAlias aliasHandL;

        public static bool IsVRSimulated => instance?.enableRigMode == EnableRigMode.StandaloneSimulator; //TODO: need this at editor time
        public static bool IsReady { get; private set; } = false;

        public static VRBodyModel Body => instance?.bodyModel;
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
            bodyModel = GetComponentInChildren<VRBodyModel>();

            VRInteraction.VRInput[] vrInputs = GetComponentsInChildren<VRInteraction.VRInput>();

            for (int i = 0; i < 2; i++)
            {
                if (vrInputs[i].IsLeftHand) leftController = vrInputs[i];
                else rightController = vrInputs[i];
            }

            SetParent(aliasBody.transform, bodyModel.transform);
            SetParent(aliasHandR.transform, rightController.transform);
            SetParent(aliasHandL.transform, leftController.transform);

            aliasHandR.VRInput = rightController;
            aliasHandL.VRInput = leftController;

            yield return null;

            IsReady = true;
        }


        internal static void SetupTeleport(VRTeleporter rightTeleporter, VRTeleporter leftTeleporter)
        {
            SetupTeleporter(leftTeleporter, LeftController);
            SetupTeleporter(rightTeleporter, RightController);
        }

        private static void SetupTeleporter(VRTeleporter teleporter, VRInteraction.VRInput controller)
        {
            teleporter.BodyModel = Body;

            teleporter.VRInput = controller;
            teleporter.VRInteractor = controller.VRInteractor;

            SetParent(teleporter.transform, teleporter.VRInteractor?.AttachTransform);
        }
        private static void SetParent(Transform source, Transform target)
        {
            source.parent = target;
            source.localPosition = Vector3.zero;
            source.localRotation = Quaternion.identity;
        }
    }
}
