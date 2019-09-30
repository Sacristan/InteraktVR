using UnityEngine;

namespace InteraktVR
{
    public class VRSimulatorRig : MonoBehaviour
    {
        internal static VRSimulatorRig instance;

        [SerializeField] private VRInteraction.VRInteractor rightHand;
        [SerializeField] private VRInteraction.VRInteractor leftHand;

        [SerializeField] private Transform bodyTransform;

        public Transform BodyTransform => bodyTransform;
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