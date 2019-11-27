using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace InteraktVR.VRInteraction
{
    public class VRInteractableItem : MonoBehaviour
    {
        public enum HoverMode
        {
            MATERIAL,
            SHADER
        }
        public enum HoldType
        {
            FIXED_POSITION,
            PICKUP_POSITION,
            SPRING_JOINT,
            FIXED_JOINT,
        }

        static private List<VRInteractableItem> _items;
        static public List<VRInteractableItem> items
        {
            get
            {
                if (_items == null) _items = new List<VRInteractableItem>();
                return _items;
            }
        }

        [System.Serializable]
        public class HoverItem
        {
            [SerializeField] private MeshRenderer renderer;
            [SerializeField] private HoverMode hoverMode;
            [SerializeField] private Shader defaultShader;
            [SerializeField] private Shader hoverShader;
            [SerializeField] private Material[] defaultMaterials;
            [SerializeField] private Material hoverMaterial;

            public Renderer Renderer => renderer;
            public HoverMode HoverMode => hoverMode;

            public Shader DefaultShader
            {
                get => defaultShader;
                internal set => defaultShader = value;
            }

            public Shader HoverShader
            {
                get => hoverShader;
                internal set => hoverShader = value;
            }

            public Material[] DefaultMaterials
            {
                get => defaultMaterials;
                internal set => defaultMaterials = value;
            }

            public Material HoverMaterial
            {
                get => hoverMaterial;
                internal set => hoverMaterial = value;
            }
        }


        //References
        public Transform item;
        [SerializeField] public List<Collider> triggerColliders = new List<Collider>(); //TODO: Property
        [SerializeField] private Transform leftHandIKAnchor;
        [SerializeField] private Transform rightHandIKAnchor;
        [SerializeField] private string leftHandIKPoseName;
        [SerializeField] private string rightHandIkPoseName;

        //Set parent if this item can't be interacted with unless the parent is being held
        [SerializeField] private List<VRInteractableItem> parents = new List<VRInteractableItem>();

        //Variables
        [SerializeField] private string itemId;
        [SerializeField] private bool canBeHeld = true;
        [SerializeField] private bool interactionDisabled = false;
        [SerializeField] public HoldType holdType = HoldType.FIXED_POSITION; //TODO: Property

        [SerializeField] private bool lerpToOffsetAnchor = false;
        [SerializeField] private Transform fixedJointHoldOffsetAnchor;

        [SerializeField] private float fixedJointMinDistance = 1f;

        [SerializeField] private bool useBreakDistance = false;
        [SerializeField] private float breakDistance = 0.1f;

        [SerializeField] private float throwBoost = 1f;
        [SerializeField] private float followForce = 1f;
        [SerializeField] public float interactionDistance = 0.1f; //TODO: Property

        [SerializeField] private HoverItem[] hovers;
        [SerializeField] private bool toggleToPickup;
        [SerializeField] public UnityEvent pickupEvent;
        [SerializeField] public UnityEvent dropEvent;
        [SerializeField] public UnityEvent enableHoverEvent;
        [SerializeField] public UnityEvent disableHoverEvent;

        //Sounds
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip enterHover;
        [SerializeField] private AudioClip exitHover;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip dropSound;
        [SerializeField] private AudioClip forceGrabSound;

        // EDITOR CALCULATED
        public bool linkedLeftAndRightHeldPositions = true;
        public bool limitAcceptedAction;
        public List<string> acceptedActions = new List<string>();
        public Vector3 heldPositionRightHand = Vector3.zero;
        public Quaternion heldRotationRightHand = Quaternion.identity;
        public Vector3 heldPosition = Vector3.zero;
        public Quaternion heldRotation = Quaternion.identity;


#if UNITY_EDITOR
        //Editor Vars
        public bool hoverFoldout;
        public bool triggersFoldout;
        public bool soundsFoldout;
        public bool ikFoldout;
#endif

        //protected string originalShaderName;
        protected Rigidbody _selfBody;
        protected Collider itemCollider;
        protected List<Joint> _springJoints = new List<Joint>();
        protected List<Joint> _fixedJoints = new List<Joint>();

        protected bool activeHover = false;
        protected float currentFollowForce = -1f;
        protected bool _pickingUp;
        protected object[] selfParam;
        protected static int itemIdIndex;

        protected VRInteractor _heldBy;
        protected List<VRInteractor> _heldBys = new List<VRInteractor>();

        public VRInteractor HeldBy
        {
            get { return _heldBy; }
            set { _heldBy = value; }
        }

        public Rigidbody SelfBody
        {
            get
            {
                if (_selfBody == null) _selfBody = item.GetComponent<Rigidbody>();
                return _selfBody;
            }
        }

        public bool InteractionDisabled
        {
            get { return interactionDisabled; }
            set { interactionDisabled = value; }
        }

        protected object[] GetSelfParam
        {
            get
            {
                if (selfParam == null) selfParam = new object[] { this };
                return selfParam;
            }
        }

        // private Vector3 LocalOffsetVector => fixedJointHoldOffsetAnchor == null ? Vector3.zero : fixedJointHoldOffsetAnchor.localPosition;
        private Vector3 LocalOffsetVector => Vector3.zero;

        void Start()
        {
            Init();
        }

        virtual protected void OnEnable()
        {
            VRInteractableItem.items.Add(this);
        }

        virtual protected void OnDisable()
        {
            if (HeldBy != null)
            {
                var oldCanBeHeld = canBeHeld;
                canBeHeld = false;
                HeldBy.Drop();
                canBeHeld = oldCanBeHeld;
            }
            VRInteractableItem.items.Remove(this);
        }

        void FixedUpdate()
        {
            Step();
        }

        virtual protected void Init()
        {
            if (item == null)
            {
                Debug.LogError("Item object not set for Interactable Item", gameObject);
                return;
            }

            //Initialize self param (This is so it's not being made when the object is being destroyed)
            selfParam = GetSelfParam;

            if (string.IsNullOrEmpty(itemId)) itemId = (1000 + (itemIdIndex++)).ToString();

            if (item.GetComponent<Rigidbody>() != null)
            {
                Collider[] colliders = item.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    if (col.isTrigger || col.GetComponent<VRItemCollider>() != null || col.GetComponent<VRInteractableItem>() != null) continue;

                    Debug.LogWarning("Non trigger collider without VRItemCollider script. " +
                        "if the item is going crazy when you pick it up, this is why. Remove this collider or " +
                        "add a VRItemCollider script and reference the interactable item or set is trigger to true.", col.gameObject);
                }
            }

            if (Camera.main != null && Camera.main.transform.parent != null)
            {
                Collider playerRigCollider = Camera.main.transform.parent.GetComponent<Collider>();
                if (playerRigCollider != null && !playerRigCollider.isTrigger)
                {
                    Collider[] itemColliders = item.GetComponentsInChildren<Collider>();
                    foreach (Collider itemCol in itemColliders)
                    {
                        if (itemCol.isTrigger) continue;
                        Physics.IgnoreCollision(playerRigCollider, itemCol);
                    }
                }
            }
            itemCollider = item.GetComponent<Collider>();
            for (int i = 0; i < hovers.Length; i++)
            {
                HoverItem hoverItem = hovers[i];
                Renderer hoverRenderer = hoverItem?.Renderer;

                if (hoverRenderer == null)
                {
                    Debug.LogError(name + " has a missing renderer. Check the Hover section of the editor", gameObject);
                    continue;
                }
                switch (hovers[i].HoverMode)
                {
                    case HoverMode.SHADER:
                        if (hoverRenderer.material == null) break;
                        if (hoverItem.HoverShader == null) hoverItem.HoverShader = Shader.Find("Unlit/Texture");
                        if (hoverItem.DefaultShader == null) hoverItem.DefaultShader = hoverRenderer.material.shader;
                        else hoverRenderer.material.shader = hoverItem.DefaultShader;
                        break;
                    case HoverMode.MATERIAL:
                        if (hoverItem.HoverMaterial == null)
                        {
                            hoverItem.HoverMaterial = new Material(hoverRenderer.material);
                            hoverItem.HoverMaterial.shader = Shader.Find("Unlit/Texture");
                        }
                        if (hoverItem.DefaultMaterials == null || hoverItem.DefaultMaterials.Length == 0)
                        {
                            hoverItem.DefaultMaterials = hoverRenderer.sharedMaterials;
                        }

                        else hoverRenderer.sharedMaterials = hoverItem.DefaultMaterials;
                        break;
                }
            }
        }

        virtual protected void Step()
        {
            if (item == null || HeldBy == null || interactionDisabled || holdType == HoldType.SPRING_JOINT || holdType == HoldType.FIXED_JOINT) return;

            if (useBreakDistance && Vector3.Distance(HeldBy.getControllerAnchorOffset.position, GetWorldHeldPosition(HeldBy)) > breakDistance)
            {
                HeldBy.Drop();
                return;
            }

            if (!canBeHeld) return;

            if (SelfBody == null)
            {
                item.position = GetControllerPosition(HeldBy);
                item.rotation = GetControllerRotation(HeldBy);
                return;
            }
            SelfBody.maxAngularVelocity = float.MaxValue;

            Quaternion rotationDelta = GetHeldRotationDelta();
            Vector3 positionDelta = GetHeldPositionDelta();

            float angle;
            Vector3 axis;
            rotationDelta.ToAngleAxis(out angle, out axis);

            if (angle > 180)
                angle -= 360;

            if (currentFollowForce < 0f) currentFollowForce = followForce;

            if (angle != 0)
            {
                Vector3 angularTarget = (angle * axis) * (currentFollowForce * (Time.fixedDeltaTime * 100f));
                SelfBody.angularVelocity = angularTarget;
            }

            Vector3 velocityTarget = (positionDelta / Time.fixedDeltaTime) * currentFollowForce;

            if (float.IsInfinity(velocityTarget.x) || float.IsNaN(velocityTarget.x))
                velocityTarget = Vector3.zero;
            SelfBody.velocity = velocityTarget;
        }

        virtual public bool CanInteract()
        {
            return HeldBy == null && !interactionDisabled && (parents.Count == 0 || IsParentItemHeld());
        }

        virtual public bool IsParentItemHeld()
        {
            bool held = false;
            foreach (VRInteractableItem parent in parents)
            {
                if (parent == null) continue;
                if (parent.HeldBy != null)
                {
                    held = true;
                    break;
                }
            }
            return held;
        }

        virtual public bool Pickup(VRInteractor hand)
        {
            // Debug.Log("PICKUP");

            if (canBeHeld && item != null)
            {
                //TODO: check this
                Rigidbody rigidbody = _selfBody;
                if (rigidbody == null) rigidbody = transform.parent.GetComponent<Rigidbody>();
                if (rigidbody != null && rigidbody.isKinematic) rigidbody.isKinematic = false;

                switch (holdType)
                {
                    case HoldType.FIXED_POSITION:
                        item.SetParent(hand.GetVRRigRoot);
                        StartCoroutine(PickingUp(hand));
                        VRInteractableItem.HeldFreezeItem(item.gameObject);
                        break;
                    case HoldType.PICKUP_POSITION:
                        if (Vector3.Distance(hand.getControllerAnchorOffset.position, item.position) < interactionDistance)
                            heldPosition = hand.getControllerAnchorOffset.InverseTransformPoint(item.position);
                        else
                            heldPosition = Vector3.zero;
                        heldRotation = Quaternion.Inverse(hand.getControllerAnchorOffset.rotation) * item.rotation;
                        item.SetParent(hand.GetVRRigRoot);
                        StartCoroutine(PickingUp(hand));
                        VRInteractableItem.HeldFreezeItem(item.gameObject);
                        break;
                    case HoldType.FIXED_JOINT:

                        // FixedJoint fixedJoint = item.gameObject.AddComponent<FixedJoint>();
                        // Rigidbody controllerBodyFixed = hand.getControllerAnchorOffset.GetComponent<Rigidbody>();
                        // if (controllerBodyFixed == null) controllerBodyFixed = hand.getControllerAnchorOffset.gameObject.AddComponent<Rigidbody>();

                        // ConfigureJoint(fixedJoint, controllerBodyFixed, hand);

                        // _fixedJoints.Add(fixedJoint);
                        // _heldBys.Add(hand);

                        StartCoroutine(PickingUpFixedJoint(hand, () =>
                        {
                            FixedJoint fixedJoint = item.gameObject.AddComponent<FixedJoint>();
                            Rigidbody controllerBodyFixed = hand.getControllerAnchorOffset.GetComponent<Rigidbody>();
                            if (controllerBodyFixed == null) controllerBodyFixed = hand.getControllerAnchorOffset.gameObject.AddComponent<Rigidbody>();

                            ConfigureJoint(fixedJoint, controllerBodyFixed, hand);

                            _fixedJoints.Add(fixedJoint);
                            _heldBys.Add(hand);
                        }));

                        break;
                    case HoldType.SPRING_JOINT:

                        SpringJoint springJoint = item.gameObject.AddComponent<SpringJoint>();
                        Rigidbody controllerBodySpring = hand.getControllerAnchorOffset.GetComponent<Rigidbody>();
                        if (controllerBodySpring == null) controllerBodySpring = hand.getControllerAnchorOffset.gameObject.AddComponent<Rigidbody>();

                        ConfigureJoint(springJoint, controllerBodySpring, hand);

                        springJoint.spring = followForce * 100f;
                        springJoint.damper = 100f;
                        _springJoints.Add(springJoint);
                        _heldBys.Add(hand);
                        break;
                }

                if (Vector3.Distance(hand.getControllerAnchorOffset.position, item.position) < interactionDistance)
                    PlaySound(pickupSound);
                else PlaySound(forceGrabSound, hand.getControllerAnchorOffset.position);
            }
            else CheckIK(true, hand);
            HeldBy = hand;
            if (pickupEvent != null) pickupEvent.Invoke();
            return true;
        }

        private void ConfigureJoint(Joint joint, Rigidbody rigidbody, VRInteractor hand)
        {
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            joint.connectedBody = rigidbody;
            joint.anchor = item.InverseTransformPoint(fixedJointHoldOffsetAnchor == null ? hand.getControllerAnchorOffset.position : fixedJointHoldOffsetAnchor.position);
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = LocalOffsetVector;
        }

        virtual protected IEnumerator PickingUpFixedJoint(VRInteractor heldBy, System.Action action = null)
        {

            if (lerpToOffsetAnchor)
            {

                yield return null;

                float dist = Vector3.Distance(GetControllerPosition(heldBy), item.position);
                // Transform = fixedJointHoldOffsetAnchor != null;
                float t = 0f;

                Vector3 pos = item.position;
                Quaternion rot = item.rotation;

                while (t < 1f)
                {
                    if (HeldBy == null) yield break;
                    t += Time.deltaTime;

                    item.position = Vector3.Lerp(pos, GetControllerPosition(heldBy), t);
                    item.rotation = Quaternion.Slerp(rot, heldBy.getControllerAnchorOffset.rotation, t);
                }

                // while (dist > fixedJointMinDistance)
                // {
                //     if (HeldBy == null) yield break;

                //     item.position = Vector3.MoveTowards(item.position, GetControllerPosition(heldBy, 1f);
                //     // item.rotation = Quaternion.RotateTowards(item.rotation, fixedJointHoldOffsetAnchor.rotation, 1f);

                //     yield return null;

                //     dist = Vector3.Distance(GetControllerPosition(heldBy), item.position);
                // }
            }

                yield return null;
 
            if (action != null) action.Invoke();
        }

        virtual protected IEnumerator PickingUp(VRInteractor heldBy, System.Action action = null)
        {
            currentFollowForce = 0.05f;
            _pickingUp = true;
            float baseDist = Vector3.Distance(GetControllerPosition(heldBy), item.position);
            if (baseDist < 0.1f) baseDist = 0.1f;
            float elapsedTime = 0;
            while (currentFollowForce < 0.99f)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime > baseDist * 0.25f) break; //Maximum time safety
                float dist = Vector3.Distance(GetControllerPosition(heldBy), item.position);
                float percent = -((dist / baseDist) - 1f);
                if (baseDist > 1f && percent < 0.3f) percent *= 0.2f;
                if (percent < 0.05f) percent = 0.05f;
                currentFollowForce = followForce * percent;
                yield return null;
                if (this.HeldBy != heldBy)
                {
                    currentFollowForce = followForce;
                    _pickingUp = false;
                    break;
                }
            }
            CheckIK(true, heldBy);
            _pickingUp = false;
            currentFollowForce = followForce;

            if (action != null) action.Invoke();
        }

        virtual public void Drop(bool addControllerVelocity, VRInteractor hand = null)
        {
            if (canBeHeld && item != null)
            {
                item.parent = null;
                switch (holdType)
                {
                    case HoldType.FIXED_POSITION:
                    case HoldType.PICKUP_POSITION:
                        VRInteractableItem.UnFreezeItem(item.gameObject);
                        ApplyThrowForce(addControllerVelocity, hand);
                        break;

                    case HoldType.FIXED_JOINT:
                        DropJointConfig(hand, _fixedJoints);
                        ApplyThrowForce(addControllerVelocity, hand);
                        break;

                    case HoldType.SPRING_JOINT:
                        DropJointConfig(hand, _springJoints);
                        break;
                }
                PlaySound(dropSound);
            }
            CheckIK(false, hand);
            dropEvent?.Invoke();
            HeldBy = null;
        }

        private void ApplyThrowForce(bool addControllerVelocity, VRInteractor hand)
        {
            if (SelfBody != null)
            {
                if (hand != null && addControllerVelocity)
                {
                    bool useBoost = hand.Velocity.magnitude > 1f;
                    SelfBody.velocity = hand.Velocity * (useBoost ? throwBoost : 1f);
                    SelfBody.angularVelocity = hand.AngularVelocity;
                    SelfBody.maxAngularVelocity = SelfBody.angularVelocity.magnitude;
                }
            }
        }

        private void DropJointConfig(VRInteractor hand, List<Joint> joints)
        {
            for (int i = _heldBys.Count - 1; i >= 0; i--)
            {
                if (_heldBys[i] != hand) continue;
                _heldBys.RemoveAt(i);
                Destroy(joints[i]);
                joints.RemoveAt(i);
            }

            Rigidbody controllerBody = hand.getControllerAnchorOffset.GetComponent<Rigidbody>();
            if (controllerBody != null) Destroy(controllerBody);
        }

        virtual protected void PICKUP_DROP(VRInteractor hand)
        {
            if (hand.heldItem == null) hand.TryPickup();
            else if (toggleToPickup) hand.Drop();
        }

        virtual protected void PICKUP_DROPReleased(VRInteractor hand)
        {
            if (hand.heldItem == null || toggleToPickup || hand.vrInput.ActionPressed(GlobalKeys.KEY_PICKUP) || hand.vrInput.ActionPressed(GlobalKeys.KEY_ACTION) || hand.vrInput.ActionPressed(GlobalKeys.KEY_PICKUP_DROP)) return;

            hand.Drop();
        }

        virtual protected void PICKUP(VRInteractor hand)
        {
            if (hand.heldItem != null) return;
            hand.TryPickup();
        }

        virtual protected void PICKUPReleased(VRInteractor hand)
        {
            PICKUP_DROPReleased(hand);
        }

        virtual protected void DROP(VRInteractor hand)
        {
            if (hand.heldItem == null) return;

            hand.Drop();
        }

        virtual protected void DROPReleased(VRInteractor hand)
        { }

        virtual protected void ACTION(VRInteractor hand)
        {
            if (hand.heldItem != null) return;

            PICKUP_DROP(hand);
        }

        virtual protected void ACTIONReleased(VRInteractor hand)
        {
            PICKUP_DROPReleased(hand);
        }

        virtual protected void CheckIK(bool pickingUp, VRInteractor hand)
        {
            if (hand == null || hand.ikTarget == null) return;
            if (pickingUp)
            {
                Transform handIKAnchor = hand.vrInput.IsLeftHand ? leftHandIKAnchor : rightHandIKAnchor;
                if (handIKAnchor != null) hand.SetIKTarget(handIKAnchor);
                if ((hand.vrInput.IsLeftHand && leftHandIKPoseName != "") ||
                    (!hand.vrInput.IsLeftHand && rightHandIkPoseName != ""))
                {
                    //	Method is in HandPoseController.cs, found in the FinalIK integrations folder (make sure to open the FinalIK package in VRInteraction first).
                    hand.GetVRRigRoot.BroadcastMessage(hand.vrInput.IsLeftHand ? "ApplyPoseLeftHand" : "ApplyPoseRightHand", hand.vrInput.IsLeftHand ? leftHandIKPoseName : rightHandIkPoseName, SendMessageOptions.DontRequireReceiver);
                }
            }
            else
            {
                hand.SetIKTarget(null);
                if ((hand.vrInput.IsLeftHand && leftHandIKPoseName != "") ||
                    (!hand.vrInput.IsLeftHand && rightHandIkPoseName != ""))
                {
                    //	Method is in HandPoseController.cs, found in the FinalIK integrations folder (make sure to open the FinalIK package in VRInteraction first).
                    hand.GetVRRigRoot.BroadcastMessage("ClearPose", hand.vrInput.IsLeftHand, SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        virtual public Vector3 GetControllerPosition(VRInteractor hand)
        {
            return hand.getControllerAnchorOffset.TransformPoint(GetLocalHeldPosition(hand));
        }

        virtual public Quaternion GetControllerRotation(VRInteractor hand)
        {
            return HeldBy.getControllerAnchorOffset.rotation * GetLocalHeldRotation(HeldBy);
        }

        /// <summary>
        /// Get item held position in world space
        /// </summary>
        /// <returns>The world held position.</returns>
        /// <param name="hand">Hand.</param>
        virtual public Vector3 GetWorldHeldPosition(VRInteractor hand)
        {
            if (item == null) return Vector3.zero;
            switch (holdType)
            {
                case HoldType.FIXED_POSITION:
                    return item.position - (item.rotation * (Quaternion.Inverse(GetLocalHeldRotation(hand)) * GetLocalHeldPosition(hand)));
                case HoldType.PICKUP_POSITION:
                case HoldType.SPRING_JOINT:
                case HoldType.FIXED_JOINT:
                    return item.position;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Get item held position in parent tranform local space.
        /// Used on the gun slide to get the controller position as a child of the gunhandler item transform
        /// </summary>
        /// <returns>The local controller position to parent transform.</returns>
        /// <param name="hand">Hand.</param>
        /// <param name="item">Item.</param>
        /// <param name="parent">Parent.</param>
        public static Vector3 GetLocalControllerPositionToParentTransform(VRInteractor hand, VRInteractableItem item, Transform parent)
        {
            Vector3 controllerPosition = item.GetControllerPosition(hand);
            return parent.InverseTransformPoint(controllerPosition);
        }

        /*public Vector3 ControllerPositionToLocal()
		{
			if (heldBy == null || currentGun == null) return Vector3.zero;
			Vector3 localPosition =  currentGun.item.InverseTransformPoint(heldBy.getControllerAnchorOffset.position);
			Vector3 rotatedOffset = Quaternion.Inverse(GetLocalHeldRotation(heldBy)) * defaultRotation * GetLocalHeldPosition(heldBy);
			Vector3 scaledOffset = new Vector3(rotatedOffset.x/currentGun.item.localScale.x, rotatedOffset.y/currentGun.item.localScale.y, rotatedOffset.z/currentGun.item.localScale.z);
			return localPosition + scaledOffset;
		}*/

        //virtual public Vector3 GetControllerPositionItemAligned(VRInteractor hand)
        //{
        //	return VRUtils.TransformPoint(hand.getControllerAnchorOffset.position, item.rotation, hand.getControllerAnchorOffset.lossyScale, GetLocalHeldPosition(hand));
        //}

        virtual public Vector3 GetLocalHeldPosition(VRInteractor hand)
        {
            if (linkedLeftAndRightHeldPositions || hand.vrInput.IsLeftHand)
                return heldPosition;
            else if (!linkedLeftAndRightHeldPositions && !hand.vrInput.IsLeftHand)
                return heldPositionRightHand;

            Debug.LogError("No held position. LinkedLeftAndRightHeldPositions: " + linkedLeftAndRightHeldPositions + " hand.LeftHand: " + hand.vrInput.IsLeftHand);
            return Vector3.zero;
        }

        virtual public Quaternion GetLocalHeldRotation(VRInteractor hand)
        {
            if (linkedLeftAndRightHeldPositions || hand.vrInput.IsLeftHand)
                return heldRotation;
            else if (!linkedLeftAndRightHeldPositions && !hand.vrInput.IsLeftHand)
                return heldRotationRightHand;

            Debug.LogError("No held rotation. LinkedLeftAndRightHeldPositions: " + linkedLeftAndRightHeldPositions + " hand.LeftHand: " + hand.vrInput.IsLeftHand);
            return Quaternion.identity;
        }

        virtual protected Vector3 GetHeldPositionDelta()
        {
            Transform heldByTransform = HeldBy.getControllerAnchorOffset;
            return (heldByTransform.TransformPoint(GetLocalHeldPosition(HeldBy))) - item.position;
        }

        virtual protected Quaternion GetHeldRotationDelta()
        {
            Transform heldByTransform = HeldBy.getControllerAnchorOffset;
            return (heldByTransform.rotation * GetLocalHeldRotation(HeldBy)) * Quaternion.Inverse(item.rotation);
        }

        virtual public void EnableHover(VRInteractor hand = null)
        {
            if (activeHover || interactionDisabled) return;
            activeHover = true;
            PlaySound(enterHover);
            if (enableHoverEvent != null) enableHoverEvent.Invoke();
            if (hovers.Length == 0) return;

            for (int i = 0; i < hovers.Length; i++)
            {
                Renderer hoverRenderer = hovers[i].Renderer;

                if (hoverRenderer == null) continue;
                switch (hovers[i].HoverMode)
                {
                    case HoverMode.SHADER:
                        if (hoverRenderer.material != null)
                            hoverRenderer.material.shader = hovers[i].HoverShader;
                        break;
                    case HoverMode.MATERIAL:
                        Material[] mats = new Material[hovers[i].DefaultMaterials.Length];

                        for (int j = 0; j < hovers[i].DefaultMaterials.Length; j++) mats[j] = hovers[i].HoverMaterial;

                        hoverRenderer.sharedMaterials = mats;
                        break;
                }
            }
        }

        virtual public void DisableHover(VRInteractor hand = null)
        {
            if (!activeHover || interactionDisabled) return;
            activeHover = false;
            PlaySound(exitHover);
            if (disableHoverEvent != null) disableHoverEvent.Invoke();
            if (hovers.Length == 0) return;
            for (int i = 0; i < hovers.Length; i++)
            {
                Renderer hoverRenderer = hovers[i].Renderer;

                if (hoverRenderer == null) continue;
                switch (hovers[i].HoverMode)
                {
                    case HoverMode.SHADER:
                        if (hoverRenderer.material != null)
                            hoverRenderer.material.shader = hovers[i].DefaultShader;
                        break;
                    case HoverMode.MATERIAL:
                        hoverRenderer.sharedMaterials = hovers[i].DefaultMaterials;
                        break;
                }
            }
        }

        //Set item up to be held correctly
        static public void HeldFreezeItem(GameObject item)
        {
            Collider[] itemColliders = item.GetComponentsInChildren<Collider>();
            foreach (Collider col in itemColliders)
            {
                VRInteractableItem ii = null;
                VRItemCollider ic = col.GetComponent<VRItemCollider>();
                if (ic != null) ii = ic.item;
                if (ii == null) ii = col.GetComponent<VRInteractableItem>();
                if (ii != null && (ii.parents.Count != 0 || !ii.enabled || ii.interactionDisabled)) continue;
                col.enabled = true;
            }
            RigidbodyMarker bodyMarker = item.GetComponent<RigidbodyMarker>();
            if (bodyMarker != null)
            {
                Rigidbody body = bodyMarker.ReplaceMarkerWithRigidbody();
                body.isKinematic = false;
            }
            else
            {
                Rigidbody body = item.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = false;
            }
        }

        //Disable unity physics and collision. For moving item through code
        static public void FreezeItem(GameObject item, bool disableAllColliders = false, bool disableTriggerColliders = false, bool disableNonTriggerColliders = false)
        {
            VRInteractableItem.DisableObjectColliders(item, disableAllColliders, disableTriggerColliders, disableNonTriggerColliders);
            Rigidbody itemBody = item.GetComponent<Rigidbody>();
            if (itemBody != null) RigidbodyMarker.ReplaceRigidbodyWithMarker(itemBody);
        }

        //Enable unity physics
        static public void UnFreezeItem(GameObject item)
        {
            Collider[] itemColliders = item.GetComponentsInChildren<Collider>();
            foreach (Collider col in itemColliders)
            {
                VRInteractableItem ii = null;
                VRItemCollider ic = col.GetComponent<VRItemCollider>();
                if (ic != null) ii = ic.item;
                if (ii == null) ii = col.GetComponent<VRInteractableItem>();
                if (ii != null && (ii.parents.Count != 0 || !ii.enabled || ii.interactionDisabled)) continue;
                col.enabled = true;
            }
            RigidbodyMarker bodyMarker = item.GetComponent<RigidbodyMarker>();
            if (bodyMarker != null)
            {
                Rigidbody body = bodyMarker.ReplaceMarkerWithRigidbody();
                body.isKinematic = false;
            }
            else
            {
                Rigidbody body = item.GetComponent<Rigidbody>();
                if (body != null) body.isKinematic = false;
            }
            /*Rigidbody itemBody = item.GetComponentInChildren<Rigidbody>();
			if (itemBody != null)
			{
				itemBody.useGravity = true;
				itemBody.isKinematic = false;
				itemBody.constraints = RigidbodyConstraints.None;
				itemBody.interpolation = RigidbodyInterpolation.Interpolate;
			}*/
        }

        static public void DisableObjectColliders(GameObject item, bool disableAllColliders = false, bool disableTriggerColliders = false, bool disableNonTriggerColliders = false)
        {
            Collider[] itemColliders = GetCollidersOf(item, disableAllColliders, disableTriggerColliders, disableNonTriggerColliders);
            ToggleColliders(itemColliders, false);
        }

        static public void EnableObjectColliders(GameObject item, bool enableAllColliders = false, bool enableTriggerColliders = false, bool enableNonTriggerColliders = false)
        {
            Collider[] itemColliders = GetCollidersOf(item, enableAllColliders, enableTriggerColliders, enableNonTriggerColliders);
            ToggleColliders(itemColliders, true);
        }

        static private Collider[] GetCollidersOf(GameObject item, bool all, bool triggers, bool nonTriggers)
        {
            Collider[] itemColliders = item.GetComponentsInChildren<Collider>();
            if (!all)
            {
                itemColliders = item.GetComponentsInChildren<Collider>();
                for (int i = itemColliders.Length - 1; i >= 0; i--)
                {
                    if (itemColliders[i].isTrigger && !triggers ||
                        !itemColliders[i].isTrigger && !nonTriggers)
                    {
                        itemColliders[i] = null;
                    }
                }
            }
            return itemColliders;
        }

        static private void ToggleColliders(Collider[] cols, bool toggle)
        {
            if (cols == null) return;
            foreach (Collider col in cols)
            {
                if (col == null) continue;
                col.enabled = toggle;
            }
        }

        public bool CanAcceptMethod(string method)
        {
            if (!limitAcceptedAction) return true;

            foreach (string acceptedAction in acceptedActions)
            {
                if (method != acceptedAction) continue;
                return true;
            }
            return false;
        }

        virtual public void Reset()
        {
            interactionDisabled = false;
            if (item != null) VRInteractableItem.UnFreezeItem(item.gameObject);
        }

        /// <summary>
        /// Gets any connected VRInteractableItem.
        /// </summary>
        /// <returns>The item.</returns>
        /// <param name="target">Target.</param>
        static public VRInteractableItem GetItem(GameObject target)
        {
            if (target == null) return null;
            VRInteractableItem ii = target.GetComponent<VRInteractableItem>();
            if (ii != null) return ii;

            //If the item isn't on the target it could have an itemCollider that would reference the item
            VRItemCollider itemCollider = target.GetComponent<VRItemCollider>();
            if (itemCollider != null && itemCollider.col.isTrigger) ii = itemCollider.item;
            if (ii != null) return ii;

            return null;
        }

        public void PlaySound(AudioClip clip)
        {
            if (clip == null) return;
            PlaySound(clip, item.position);
        }

        public void PlaySound(AudioClip clip, Vector3 worldPosition)
        {
            if (clip == null) return;
            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
                AudioSource.PlayClipAtPoint(clip, worldPosition);
        }
    }

}
