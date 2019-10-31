using System.Collections;
using System.Collections.Generic;
using InteraktVR.VRInteraction;
using UnityEngine;

namespace InteraktVR.Core
{
    public class Hand : MonoBehaviour
    {
        enum HandType { Right, Left }
        [SerializeField] HandType handType;

        Animator _animator;


        private void Start()
        {
            _animator = GetComponent<Animator>();
        }

        private void Update()
        {
            // Debug.Log(VRInput.Right.ActionPressed(VRInteraction.GlobalKeys.KEY_ACTION));
            SetAnimator((handType == HandType.Right ? VRInput.Right : VRInput.Left).ActionPressed(VRInteraction.GlobalKeys.KEY_ACTION));
        }

        private void SetAnimator(bool flag)
        {
            _animator.SetBool("Grab", flag);
        }
    }
}