using UnityEngine;

namespace InteraktVR
{
    public class VRSimulatorRig : MonoBehaviour
    {
        internal static VRSimulatorRig instance;

        [SerializeField] private VRInteraction.VRInteractor rightHand;
        [SerializeField] private VRInteraction.VRInteractor leftHand;

        public Vector3 Velocity => Vector3.zero;
        public Vector3 AngularVelocity => Vector3.zero;

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
    }
}