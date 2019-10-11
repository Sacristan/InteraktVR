using System;
using UnityEngine;

namespace InteraktVR
{
    public class VRSimulatorRig : MonoBehaviour
    {
        internal static VRSimulatorRig instance;

        [SerializeField] private VRInteraction.VRInteractor rightHand;
        [SerializeField] private VRInteraction.VRInteractor leftHand;

        public VRInteraction.VRInteractor RightHand => rightHand;
        public VRInteraction.VRInteractor LeftHand => leftHand;

        void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        internal bool IsTriggerPressed(bool isLeftHand) => !Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(isLeftHand ? 0 : 1);
        internal bool IsGripPressed(bool isLeftHand) => Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(isLeftHand ? 0 : 1);
        internal bool IsPadPressed(bool isLeftHand) => false;
        internal bool IsPadTouched(bool isLeftHand) => false;
        internal bool IsMenuPressed(bool isLeftHand) => false;
        internal bool IsAXPressed(bool isLeftHand) => false;
        internal bool IsBYPressed(bool isLeftHand) => false;

        internal bool IsPadCentrePressed(bool isLeftHand) => false;
        // internal bool IsPadTopPressed(bool isLeftHand) => (isLeftHand ^ Input.GetKey(KeyCode.LeftAlt)) && Input.GetMouseButton(2);
        internal bool IsPadTopPressed(bool isLeftHand) => Input.GetKey(isLeftHand ? KeyCode.Q : KeyCode.E);
        internal bool IsPadLeftPressed(bool isLeftHand) => false;
        internal bool IsPadRightPressed(bool isLeftHand) => false;
        internal bool IsPadBottomPressed(bool isLeftHand) => false;

        internal Vector2 PadPosition(bool isLeftHand) => Vector2.zero;
    }
}