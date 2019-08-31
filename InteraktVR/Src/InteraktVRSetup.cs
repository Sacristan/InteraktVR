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

        public static bool IsVRSimulated => instance?.enableRigMode == EnableRigMode.StandaloneSimulator;

        void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(this);
        }

        void Start()
        {
            vrRig.SetActive(enableRigMode == EnableRigMode.VR);
            vrSimulatorRig.SetActive(enableRigMode == EnableRigMode.StandaloneSimulator);
        }
    }
}
