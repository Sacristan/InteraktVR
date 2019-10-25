using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InteraktVR.VRInteraction
{
    public class VRItemCollider : MonoBehaviour
    {
        [SerializeField] public VRInteractableItem item;

        private Collider _col;

        public Collider col
        {
            get
            {
                if (_col == null) _col = GetComponent<Collider>();
                return _col;
            }
        }
    }
}