using UnityEngine;

namespace InteraktVR
{
    public class EnableRig : MonoBehaviour
    {
        enum EnableRigMode { VR, Desktop }

        [SerializeField] EnableRigMode enableRigMode;
        [SerializeField] GameObject vrRig;
        [SerializeField] GameObject desktopRig;

        void Start()
        {
            vrRig.SetActive(enableRigMode == EnableRigMode.VR);
            desktopRig.SetActive(enableRigMode == EnableRigMode.Desktop);
        }
    }
}
