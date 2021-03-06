﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using InteraktVR.Core;

#if Int_SteamVR
using Valve.VR;
#endif

namespace InteraktVR.VRInteraction
{
    public class VRInteractor : MonoBehaviour
    {
        public const string AnchorOffsetName = "ControllerAnchorOffset";

        [SerializeField] public GameObject objectReference;
        [SerializeField] private bool hideControllersWhileHolding = true;
        [SerializeField] public Transform controllerAnchor;
        [SerializeField] public Transform controllerAnchorOffset;
        [SerializeField] public Transform ikTarget;
        [SerializeField] private Vector3 forceGrabDirection = Vector3.right;
        [SerializeField] public float forceGrabDistance = 0f;
        [SerializeField] private bool useHoverLine;
        [SerializeField] private Material hoverLineMat;
        [SerializeField] private Transform _vrRigRoot;
        [SerializeField] private bool triggerHapticPulse = true;
        [SerializeField] private bool checkItemAngle = false;
        [SerializeField] private float itemCheckHalfAngle = 90;

        protected VRInteractableItem _hoverItem;
        protected VRInteractableItem _heldItem;
        protected bool beingDestroyed;
        protected VRInput _vrInput;
        protected bool _highlighting;
        protected bool _ikTargetInitialized;
        protected Vector3 _ikTargetPosition;
        protected Quaternion _ikTargetRotation;
        protected Transform _ikTargetParent;
        protected float _lastDropped;
        protected LineRenderer _hoverLine;

        public bool objectReferenceIsPrefab; //EDITOR VAR



#if Int_Oculus
        private Quaternion _currentRotation;
        private Quaternion _lastRotation;
#endif

        public bool IsHoldingSomething => heldItem != null;


        virtual public VRInput vrInput
        {
            get
            {
                if (_vrInput == null) _vrInput = GetComponent<VRInput>();
                if (_vrInput == null) Debug.LogError("VRInteractor needs an input component", gameObject);
                return _vrInput;
            }
        }

        virtual public VRInteractableItem hoverItem
        {
            get { return _hoverItem; }
            set { _hoverItem = value; }
        }

        virtual public VRInteractableItem heldItem
        {
            get { return _heldItem; }
        }

        private bool highlighting
        {
            set
            {
                if (_highlighting == value) return;
                _highlighting = value;
                if (_highlighting) TriggerHapticPulse(500);
            }
        }

        public Transform AttachTransform => controllerAnchorOffset ?? transform;

        virtual public Transform GetVRRigRoot
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated)
                {
                    //WORKAROUND
                    if (_vrRigRoot == null) _vrRigRoot = VRSimulatorRig.instance.transform;
                    return _vrRigRoot;
                }

#if Int_SteamVR
                if (vrInput.isSteamVR())
                {
                    if (_vrRigRoot == null)
                    {
                        SteamVR_PlayArea playArea = GetComponentInParent<SteamVR_PlayArea>();
                        if (playArea != null) _vrRigRoot = playArea.transform;
                        else _vrRigRoot = transform.root;
                    }
                }
#endif
#if Int_Oculus
                if (!vrInput.isSteamVR())
                {
                    if (_vrRigRoot == null)
                    {
                        OvrAvatar ovrAvatar = GetComponentInParent<OvrAvatar>();
                        if (ovrAvatar != null)
                        {
                            if (ovrAvatar.transform.parent != null) _vrRigRoot = ovrAvatar.transform.parent;
                            else _vrRigRoot = ovrAvatar.transform;
                        }
                        else _vrRigRoot = transform.root;
                    }
                }
#endif
                return _vrRigRoot;
            }
        }
        virtual public Vector3 Velocity
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated)
                {
                    Vector3 velocity = GetComponent<Zinnia.Tracking.Velocity.AverageVelocityEstimator>().GetVelocity();
                    // Debug.Log("Velocity: " + velocity);
                    return velocity;
                }

#if Int_SteamVR
                if (vrInput.isSteamVR())
                {
                    Vector3 deviceVel = Vector3.zero;
#if !Int_SteamVR2
					var device = SteamVR_Controller.Input((int)vrInput.controller.controllerIndex);
					deviceVel = device.velocity;
#else
                    SteamVR_Behaviour_Pose poseComp = GetComponent<SteamVR_Behaviour_Pose>();
                    if (poseComp != null) deviceVel = poseComp.GetVelocity();
#endif
                    Rigidbody body = transform.parent.GetComponent<Rigidbody>();
                    Vector3 bodyVel = Vector3.zero;
                    if (body != null) bodyVel = body.velocity;
                    return bodyVel + (transform.parent.rotation * deviceVel);
                }
#endif
#if Int_Oculus
                if (!vrInput.isSteamVR())
                {
                    return GetVRRigRoot.rotation * OVRInput.GetLocalControllerVelocity(vrInput.controllerHand);
                }
#endif
                return Vector3.zero;
            }
        }
        virtual public Vector3 AngularVelocity
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated)
                {
                    Vector3 velocity = GetComponent<Zinnia.Tracking.Velocity.AverageVelocityEstimator>().GetAngularVelocity();
                    // Debug.Log("AngularVelocity: " + velocity);
                    return velocity;
                }

#if Int_SteamVR
                if (vrInput.isSteamVR())
                {
                    Vector3 deviceAngularVel = Vector3.zero;
#if !Int_SteamVR2
					var device = SteamVR_Controller.Input((int)vrInput.controller.controllerIndex);
					deviceAngularVel = device.angularVelocity;
#else
                    SteamVR_Behaviour_Pose poseComp = GetComponent<SteamVR_Behaviour_Pose>();
                    if (poseComp != null) deviceAngularVel = poseComp.GetAngularVelocity();
#endif
                    return transform.parent.TransformVector(deviceAngularVel);
                }
#endif
#if Int_Oculus
                if (!vrInput.isSteamVR())
                {
                    Quaternion deltaRotation = (_currentRotation * Quaternion.Inverse(_lastRotation));
                    return new Vector3(Mathf.DeltaAngle(0, deltaRotation.eulerAngles.x), Mathf.DeltaAngle(0, deltaRotation.eulerAngles.y), Mathf.DeltaAngle(0, deltaRotation.eulerAngles.z));
                }
#endif
                return Vector3.zero;
            }
        }
        virtual public void TriggerHapticPulse(int frames)
        {
            if (!triggerHapticPulse) return;

#if Int_SteamVR && !Int_SteamVR2
			if (vrInput.isSteamVR())
			{
				var device = SteamVR_Controller.Input((int)vrInput.controller.controllerIndex);
				device.TriggerHapticPulse((ushort)(frames*50));
			}
#endif
#if Int_SteamVR2
            vrInput.hapticAction.Execute(0f, 0.15f, 160, 0.5f, vrInput.handType);
#endif
#if Int_Oculus
            if (!vrInput.isSteamVR())
            {
                var pulse = new byte[frames];
                for (int i = 0; i < frames; i++) pulse[i] = (byte)255;
                if (vrInput.controllerHand == OVRInput.Controller.LTouch)
                    OVRHaptics.LeftChannel.Mix(new OVRHapticsClip(pulse, 1));
                else
                    OVRHaptics.RightChannel.Mix(new OVRHapticsClip(pulse, 1));
            }
#endif
        }

        virtual public Transform getControllerAnchor
        {
            get
            {
                if (controllerAnchor == null)
                    controllerAnchor = transform;
                return controllerAnchor;
            }
        }

        virtual public Transform getControllerAnchorOffset
        {
            get
            {
                if (controllerAnchorOffset == null)
                {
                    GameObject controllerAnchorObject = new GameObject(VRInteractor.AnchorOffsetName);
                    controllerAnchorOffset = controllerAnchorObject.transform;
                    controllerAnchorOffset.parent = getControllerAnchor;
                    if (vrInput.isSteamVR())
                    {
                        controllerAnchorOffset.localPosition = new Vector3(0f, -0.0046f, -0.0343f);
                        controllerAnchorOffset.localRotation = Quaternion.Euler(50f, 0f, 0f);
                    }
                    else
                    {
                        controllerAnchorOffset.localPosition = Vector3.zero;
                        controllerAnchorOffset.localRotation = Quaternion.identity;
                    }
                }
                return controllerAnchorOffset;
            }
        }

        virtual public void SetIKTarget(Transform newAnchor)
        {
            if (ikTarget == null) return;
            if (!_ikTargetInitialized)
            {
                _ikTargetInitialized = true;
                _ikTargetPosition = ikTarget.localPosition;
                _ikTargetRotation = ikTarget.localRotation;
                _ikTargetParent = ikTarget.parent;
            }
            if (newAnchor == null)
            {
                ikTarget.parent = _ikTargetParent;
                ikTarget.localPosition = _ikTargetPosition;
                ikTarget.localRotation = _ikTargetRotation;
            }
            else
            {
                ikTarget.parent = newAnchor;
                ikTarget.localPosition = Vector3.zero;
                ikTarget.localRotation = Quaternion.identity;
            }
        }

        virtual public VRInteractor GetOtherController()
        {
            VRInteractor[] interactors = GetVRRigRoot.GetComponentsInChildren<VRInteractor>();
            foreach (VRInteractor interactor in interactors)
            {
                if (interactor != this) return interactor;
            }
            return null;
        }

        virtual protected void Start()
        {
#if Int_Oculus
            if (!vrInput.isSteamVR()) Time.fixedDeltaTime = 0.006f;
#endif

            if (objectReference != null) StartCoroutine(LockObjectToController());
        }
        virtual protected void Update()
        {
            if (!enabled) return;
            CheckHover();
            PositionHoverLine();

#if Int_Oculus
            if (_heldItem != null)
            {
                _lastRotation = _currentRotation;
                _currentRotation = _heldItem.transform.rotation;
            }
#endif
        }

        virtual protected void OnDestroy()
        {
            beingDestroyed = true;
        }

        virtual protected IEnumerator LockObjectToController()
        {
            yield return new WaitForSeconds(0.5f);
            VRInteractableItem interactableItem = null;
            if (objectReferenceIsPrefab)
            {
                GameObject newObjectReference = (GameObject)Instantiate(objectReference);
                interactableItem = newObjectReference.GetComponentInChildren<VRInteractableItem>();
            }
            else interactableItem = objectReference.GetComponentInChildren<VRInteractableItem>();
            if (interactableItem != null)
            {
                hoverItem = interactableItem;
                interactableItem.item.position = transform.position;
                TryPickup();
            }
            else Debug.LogWarning("Couldn't find VRInteractableItem on object reference");
        }

        virtual protected void CheckHover()
        {
            if (_heldItem != null) //If were holding something
            {
                if (_highlighting) //If were holding something and still highligting then stop
                {
                    highlighting = false;
                }
                return; //End of update (We're holding something so no need to carry on)
            }

            VRInteractableItem closestItem = null;
            float closestDist = float.MaxValue;
            bool forceGrab = false;

            foreach (VRInteractableItem item in VRInteractableItem.items)
            {
                if (item == null || !item.CanInteract()) continue;

                Vector3 controllerPosition = getControllerAnchorOffset.position;
                Vector3 targetPosition = item.GetWorldHeldPosition(this);

                if (checkItemAngle)
                {
                    float angle = Vector3.Angle(getControllerAnchorOffset.forward, (targetPosition - controllerPosition).normalized);
                    if (angle > itemCheckHalfAngle) continue;
                }

                float dist = Vector3.Distance(controllerPosition, targetPosition);

                if (dist > closestDist) continue;
                bool canGrab = false;
                bool isForceGrab = false;
                if (dist < item.interactionDistance || ItemWithinColliderBounds(item))
                    canGrab = true;

                if ((item.interactionDistance < forceGrabDistance &&
                    VRUtils.PositionWithinCone(controllerPosition,
                            getControllerAnchorOffset.TransformVector(new Vector3(vrInput.IsLeftHand ? forceGrabDirection.x : -forceGrabDirection.x, forceGrabDirection.y, forceGrabDirection.z)),
                            targetPosition, 20f, forceGrabDistance)))
                {
                    canGrab = true;
                    isForceGrab = true;
                }

                if (canGrab)
                {
                    forceGrab = isForceGrab;
                    closestDist = dist;
                    closestItem = item;
                }
            }

            if (hoverItem != null && hoverItem != closestItem)
            {
                highlighting = false;
                hoverItem.DisableHover(this);
            }
            if (closestItem != null && (hoverItem != closestItem))
            {
                ForceGrabToggle forceToggle = GetComponent<ForceGrabToggle>();
                bool forceToggleAllowed = true;
                if (forceToggle != null) forceToggleAllowed = !forceGrab || !vrInput.ActionPressed(forceToggle.actionName);

                if (_lastDropped + 0.5f < Time.time && forceToggleAllowed && (vrInput.ActionPressed(GlobalKeys.KEY_ACTION) || vrInput.ActionPressed(GlobalKeys.KEY_PICKUP_DROP) || vrInput.ActionPressed(GlobalKeys.KEY_PICKUP)))
                {
                    hoverItem = closestItem;
                    string actionDown = GlobalKeys.KEY_PICKUP;
                    if (vrInput.ActionPressed(GlobalKeys.KEY_PICKUP_DROP)) actionDown = GlobalKeys.KEY_PICKUP_DROP;
                    else if (vrInput.ActionPressed(GlobalKeys.KEY_ACTION)) actionDown = GlobalKeys.KEY_ACTION;

                    SendMessage(GlobalKeys.KEY_INPUT_RECEIVED, actionDown, SendMessageOptions.DontRequireReceiver);
                    return;
                }
                else if (hoverItem != closestItem)
                {
                    highlighting = true;
                    closestItem.EnableHover(this);
                }
            }

            hoverItem = closestItem;
        }

        virtual protected void PositionHoverLine()
        {
            if (!useHoverLine) return;
            if (!(heldItem == null || _hoverLine == null || !_hoverLine.enabled)) _hoverLine.enabled = false;
            if (hoverItem != null)
            {
                if (_hoverLine == null)
                {
                    GameObject hoverLineObj = new GameObject("InteraktVR Hover Line");
                    _hoverLine = hoverLineObj.AddComponent<LineRenderer>();
                    _hoverLine.startWidth = _hoverLine.endWidth = 0.01f;
                    _hoverLine.positionCount = 2;
                    _hoverLine.material = hoverLineMat;
                }
                if (!_hoverLine.enabled) _hoverLine.enabled = true;
                _hoverLine.SetPosition(0, getControllerAnchorOffset.position);
                _hoverLine.SetPosition(1, hoverItem.GetWorldHeldPosition(this));
            }
            else if (_hoverLine != null && _hoverLine.enabled) _hoverLine.enabled = false;
        }

        virtual protected bool ItemWithinColliderBounds(VRInteractableItem item)
        {
            if (item.triggerColliders.Count == 0) return false;

            foreach (Collider col in item.triggerColliders)
            {
                if (col == null)
                {
                    Debug.LogError("Item has an empty collider in trigger colliders list: " + item.name, item.gameObject);
                    continue;
                }
                //Is the controller anchor offset within the bounds of this collider
                if (col.bounds.Contains(getControllerAnchorOffset.position))
                    return true;
            }
            return false;
        }

        virtual public bool TryPickup()
        {
            if (_heldItem != null || /*already holding something*/
                hoverItem == null || /*have something were hovering over*/
                (hoverItem.holdType != VRInteractableItem.HoldType.SPRING_JOINT && hoverItem.holdType != VRInteractableItem.HoldType.FIXED_JOINT && hoverItem.HeldBy != null) /*Thing were hovering over is not a joint hold and is already being held*/)
                return false;
            _heldItem = hoverItem;
            _heldItem.DisableHover(this);
            if (!_heldItem.Pickup(this)) /*Is the item alright with us picking it up*/
            {
                _heldItem = null;
                return false;
            }
            hoverItem = null;

            if (hideControllersWhileHolding) ToggleControllers(false);

            VREvent.Send(GlobalKeys.KEY_PICKUP, new object[] { _heldItem });
            return true;
        }

        virtual public void Drop()
        {
            if (_heldItem == null || beingDestroyed) return;
            VREvent.Send(GlobalKeys.KEY_DROP, new object[] { _heldItem });
            if (hoverItem != null)
            {
                hoverItem.DisableHover(this);
                hoverItem = null;
            }
            _lastDropped = Time.time;
            _heldItem.Drop(true, this);
            _heldItem = null;

            if (hideControllersWhileHolding) ToggleControllers(true);
        }

        private void ToggleControllers(bool enable)
        {
            if (vrInput.isSteamVR())
            {
                ToggleAllChildRenderers(gameObject, enable);
            }
            else
            {
#if Int_Oculus
                OvrAvatar avatar = GetComponentInParent<OvrAvatar>();
                if (avatar == null)
                {
                    avatar = FindObjectOfType<OvrAvatar>();
                    //Debug.LogWarning("Can't find OVRAvatar as parent of controller, using FindObjectOfType, warning this is slow and may result in a long frame");
                }
                if (avatar == null) return;
                if (vrInput.IsLeftHand)
                {
                    ToggleAllChildRenderers(avatar.ControllerLeft.gameObject, enable);
                    ToggleAllChildRenderers(avatar.HandLeft.gameObject, enable);
                }
                else
                {
                    ToggleAllChildRenderers(avatar.ControllerRight.gameObject, enable);
                    ToggleAllChildRenderers(avatar.HandRight.gameObject, enable);
                }
#endif
            }
        }

        private void ToggleAllChildRenderers(GameObject target, bool enable)
        {
            MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer.GetComponent<DontHide>() != null) continue;
                renderer.enabled = enable;
            }
            SkinnedMeshRenderer[] skinnedRenderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
            {
                if (skinnedRenderer.GetComponent<DontHide>() != null) continue;
                skinnedRenderer.enabled = enable;
            }
        }

        /// <summary>
        /// Called by VRInput using a SendMessage.
        /// </summary>
        /// <param name="message">Method name for receiving item</param>
        public void InputReceived(string method)
        {
            SendFocusItemMethod(method);
        }

        /// <summary>
        /// Calls method on the focus item, either the hover item or held item.
        /// </summary>
        /// <param name="method">Method Name.</param>
        public void SendFocusItemMethod(string method)
        {
            VRInteractableItem item = null;
            if (heldItem != null) item = heldItem;
            else if (hoverItem != null) item = hoverItem;
            if (item != null && item.CanAcceptMethod(method))
            {
                item.gameObject.SendMessage(method, this, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

}
