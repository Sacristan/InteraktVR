using UnityEngine;
using System.Collections;
using System;

namespace InteraktVR
{
    public class FPSController : MonoBehaviour
    {
        private const float FloatTolerance = 0.0001f;

        #region Settings Classes

        [System.Serializable]
        public class Bobbing
        {
            public float bobSpeed;
            public float bobAmount;
            public static float midpoint = 0.0f;

            public Bobbing(float p1, float p2)
            {
                bobSpeed = p1;
                bobAmount = p2;
            }
        }
        [System.Serializable]
        public class MovementSpeed
        {
            public float maxForwardSpeed;
            public float maxSidewaysSpeed;
            public float maxBackwardsSpeed;

            public MovementSpeed(float p1, float p2, float p3)
            {
                maxForwardSpeed = p1;
                maxSidewaysSpeed = p2;
                maxBackwardsSpeed = p3;
            }
        }
        #endregion

        #region Serialized Settings

        [Header("General")]
        [SerializeField] private Transform pivot;
        [SerializeField] private bool canCrouch = true;

        [Header("Movement Settings")]
        [SerializeField] private MovementSpeed walkSpeed = new MovementSpeed(3.0f, 4.0f, 2.0f);
        [SerializeField] private MovementSpeed runSpeed = new MovementSpeed(6.0f, 4.0f, 3.0f);
        private MovementSpeed crouchSpeed = new MovementSpeed(1.0f, 1.0f, 1.0f);

        [Header("Lean Settings")]
        [SerializeField] private float speed = 50f;
        [SerializeField] private float maxAngle = 20f;
        [SerializeField] private float leanPosAmount = 0.5f;
        [SerializeField] private float leanPosSpeed = 5f;

        [Header("Bobbing Settings")]
        [SerializeField] private bool bobEnabled = false;
        [SerializeField] private Bobbing walkBobbing = new Bobbing(.1f, .04f);
        [SerializeField] private Bobbing runBobbing = new Bobbing(.3f, .085f);
        [SerializeField] private Bobbing crouchBobbing = new Bobbing(.05f, .065f);
        #endregion

        #region Privates

        private float timer = 0f;
        private float bobSpeed;
        private float bobAmount;

        private bool isCrouching = false;
        private float dist;

        private CharacterMotor _characterMotor;
        private CharacterController _characterController;

        private Transform _characterTransform;
        private float crouchScale = 0.15f;
        private float standingScale = 1f;

        private float vScale;
        private float currentLeanAngle = 0f;
        private Vector3 currentLeanPos;

        private float horizontalInput;
        private float verticalInput;
        private float encumberance = 0f;
        #endregion

        private float EncumberanceModifier
        {
            get { return 1f - encumberance; }
        }

        #region Mono

        private void Start()
        {
            _characterMotor = GetComponent<CharacterMotor>();
            _characterController = GetComponent<CharacterController>();

            _characterTransform = pivot;
            dist = _characterController.height / 2;

            vScale = standingScale;
        }

        private void Update()
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalInput = Input.GetAxis("Vertical");

            HandleMovement();
            HandleLeaning();
            HandleBobbing();
        }

        #endregion

        public void ApplyEncumberance(float encumberance)
        {
            this.encumberance = encumberance;
        }

        public void RemoveEncumberance()
        {
            this.encumberance = 0f;
        }

        #region FPS Logic

        private void HandleMovement()
        {
            Vector3 directionVector;

            directionVector = new Vector3(horizontalInput, 0, verticalInput);

            if (directionVector != Vector3.zero)
            {
                float directionLength = directionVector.magnitude;
                directionVector = directionVector / directionLength;

                directionLength = Mathf.Min(1, directionLength);

                directionLength = directionLength * directionLength;

                directionVector = directionVector * directionLength;
            }

            _characterMotor.inputMoveDirection = transform.rotation * directionVector;
            _characterMotor.inputJump = Input.GetKey(KeyCode.Space);
        }

        private void HandleLeaning()
        {
            if (Input.GetKey(KeyCode.Q))
            {
                currentLeanAngle = Mathf.MoveTowardsAngle(currentLeanAngle, maxAngle, speed * Time.deltaTime);
                currentLeanPos = Vector3.MoveTowards(currentLeanPos, -Vector3.right * leanPosAmount, leanPosSpeed * Time.deltaTime);
            }

            else if (Input.GetKey(KeyCode.E))
            {
                currentLeanAngle = Mathf.MoveTowardsAngle(currentLeanAngle, -maxAngle, speed * Time.deltaTime);
                currentLeanPos = Vector3.MoveTowards(currentLeanPos, Vector3.right * leanPosAmount, leanPosSpeed * Time.deltaTime);
            }

            else
            {
                currentLeanAngle = Mathf.MoveTowardsAngle(currentLeanAngle, 0f, speed * Time.deltaTime);
                currentLeanPos = Vector3.MoveTowards(currentLeanPos, Vector3.zero, leanPosSpeed * Time.deltaTime);
            }

            pivot.localRotation = Quaternion.AngleAxis(currentLeanAngle, Vector3.forward);
            pivot.localPosition = currentLeanPos;
        }

        private void HandleBobbing()
        {
            Vector3 newVec;

            if (canCrouch)
            {
                if (Input.GetKeyDown(KeyCode.LeftControl) && !_characterMotor.inputJump && _characterController.isGrounded)
                {
                    isCrouching = !isCrouching;
                    vScale = isCrouching ? crouchScale : standingScale;
                }
            }
            else
            {
                if (isCrouching) isCrouching = false;
            }

            if (!isCrouching)
            {
                _characterMotor.jumping.enabled = true;
                if ((Mathf.Abs(horizontalInput) > FloatTolerance || Mathf.Abs(verticalInput) > FloatTolerance) && (Input.GetKey(KeyCode.LeftShift)))
                {
                    ApplyBobbing(runBobbing);
                    ApplySpeed(runSpeed);
                }
                else
                {
                    ApplyBobbing(walkBobbing);
                    ApplySpeed(walkSpeed);
                }
            }
            else
            {
                _characterMotor.jumping.enabled = false;
                ApplyBobbing(crouchBobbing);
                ApplySpeed(crouchSpeed);
            }

            if ((isCrouching || !_characterMotor.inputJump) && _characterController.isGrounded && bobEnabled)
            {
                float waveslice = 0.0f;

                if (Mathf.Abs(horizontalInput) <= FloatTolerance && Mathf.Abs(verticalInput) <= FloatTolerance)
                {
                    timer = 0.0f;
                }
                else
                {
                    waveslice = Mathf.Sin(timer);
                    timer = timer + bobSpeed;
                    if (timer > Mathf.PI * 2) timer = timer - (Mathf.PI * 2);
                }

                if (waveslice != 0)
                {
                    float translateChange = waveslice * bobAmount;
                    float totalAxes = Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput);
                    totalAxes = Mathf.Clamp(totalAxes, 0.0f, 1.0f);
                    translateChange = totalAxes * translateChange;

                    newVec = _characterTransform.localPosition;
                    newVec.y = Bobbing.midpoint + translateChange;
                    _characterTransform.localPosition = newVec;
                }
                else
                {
                    newVec = _characterTransform.localPosition;
                    newVec.y = Bobbing.midpoint;
                    _characterTransform.localPosition = newVec;
                }
            }

            float ultScale = _characterTransform.localScale.y;

            newVec = _characterTransform.localScale;
            newVec.y = Mathf.Lerp(_characterTransform.localScale.y, vScale, 5f * Time.deltaTime);
            _characterTransform.localScale = newVec;

            newVec = _characterTransform.position;
            newVec.y += dist * (_characterTransform.localScale.y - ultScale);
            _characterTransform.position = newVec;
        }

        private void ApplyBobbing(Bobbing bobbing)
        {
            bobSpeed = bobbing.bobSpeed * EncumberanceModifier;
            bobAmount = bobbing.bobAmount * EncumberanceModifier;
        }

        private void ApplySpeed(MovementSpeed movementSpeed)
        {
            _characterMotor.movement.maxForwardSpeed = movementSpeed.maxForwardSpeed * EncumberanceModifier;
            _characterMotor.movement.maxSidewaysSpeed = movementSpeed.maxSidewaysSpeed * EncumberanceModifier;
            _characterMotor.movement.maxBackwardsSpeed = movementSpeed.maxBackwardsSpeed * EncumberanceModifier;
        }
        #endregion
    }
}