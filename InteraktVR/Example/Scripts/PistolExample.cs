using UnityEngine;
using VRInteraction;

namespace InteraktVR
{
    public class PistolExample : MonoBehaviour
    {
        private bool _enabled;
        private VRInteractableItem _item;
        private AudioSource _audioSource;

        void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            _item = GetComponent<VRInteractableItem>();
            if (_item == null)
            {
                Debug.LogError("This script requires an VRInteracableItem script on the same object", gameObject);
                return;
            }
        }

        void ACTION(VRInteractor hand)
        {
            if (_item == null || hand.heldItem != _item) return;
            _audioSource.Play();
        }
    }
}