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
    public class VRInput : MonoBehaviour
    {
        public enum HMDType
        {
            VIVE,
            OCULUS,
            STANDALONE
        }

        public string[] VRActions;

        //Used in the editor
        public bool mirrorControls = true;
        // Will display oculus buttons when false
        public bool displayViveButtons;

        public int triggerKey;
        public int padTop;
        public int padLeft;
        public int padRight;
        public int padBottom;
        public int padCentre;
        public int padTouch;
        public int gripKey;
        public int menuKey;
        public int AXKey;

        //Oculus alternative buttons
        public int triggerKeyOculus;
        public int padTopOculus;
        public int padLeftOculus;
        public int padRightOculus;
        public int padBottomOculus;
        public int padCentreOculus;
        public int padTouchOculus;
        public int gripKeyOculus;
        public int menuKeyOculus;
        public int AXKeyOculus;

#if Int_SteamVR2

        public SteamVR_Input_Sources handType;
        public List<SteamVR_Action_Boolean> booleanActions = new List<SteamVR_Action_Boolean>();
        public SteamVR_Action_Single triggerPressure = SteamVR_Input.GetAction<SteamVR_Action_Single>("TriggerPressure");
        public SteamVR_Action_Vector2 touchPosition = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("TouchPosition");
        public SteamVR_Action_Boolean padTouched = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PadTouched");
        public SteamVR_Action_Boolean padPressed = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("PadPressed");
        public SteamVR_Action_Boolean teleport = SteamVR_Input.GetAction<SteamVR_Action_Boolean>(GlobalKeys.KEY_TELEPORT);
        public SteamVR_Action_Vibration hapticAction = SteamVR_Input.GetAction<SteamVR_Action_Vibration>("Haptic");

#endif

        private bool _triggerPressedFlag = false;
        private bool _padPressedFlag = false;
        private bool _padTouchedFlag = false;
        private bool _grippedFlag = false;
        private bool _menuPressedFlag = false;
        private bool _AX_PressedFlag = false;

        private bool _stickLeftDown;
        private bool _stickTopDown;
        private bool _stickBottomDown;
        private bool _stickRightDown;

        public static VRInput Left { get; private set; }
        public static VRInput Right { get; private set; }

        VRInteractor _interactor;

        public VRInteractor VRInteractor
        {
            get
            {
                if (_interactor == null) _interactor = GetComponent<VRInteractor>();
                return _interactor;
            }
        }

        virtual protected void Start()
        {
#if Int_Oculus
            if (!isSteamVR())
            {
                bool leftHand = IsLeftHand; //Assigns LTouch and RTouch if unassigned
            }
#endif

            if (IsLeftHand) Left = this;
            else Right = this;
        }


        //TODO: MAP to trackpad. Isolate Standalone Logic
        virtual protected void Update()
        {
            if (hmdType == HMDType.STANDALONE)
            {
                if (Time.realtimeSinceStartup > 1f) StandaloneOrOculusControls();
                return;
            }

#if !(Int_SteamVR && !Int_SteamVR2)
            if (!isSteamVR())
            {
#endif
                StandaloneOrOculusControls();
#if !(Int_SteamVR && !Int_SteamVR2)
            }
#endif
#if Int_SteamVR2

            foreach (SteamVR_Action_Boolean boolAction in booleanActions)
            {
                if (boolAction == null)
                {
                    Debug.LogError("SteamVR Inputs have not been setup. Refer to the SteamVR 2.0 section of the Setup Guide. Found in Assets/VRInteraction/Docs.");
                    continue;
                }
                if (boolAction.GetStateDown(handType))
                {
                    SendMessageToInteractor(boolAction.GetShortName());
                }
                if (boolAction.GetStateUp(handType))
                {
                    SendMessageToInteractor(boolAction.GetShortName() + "Released");//TODO FIX
                }
            }

#endif
        }

        private void StandaloneOrOculusControls()
        {
            bool trigger = TriggerPressed;
            if (trigger && !_triggerPressedFlag)
            {
                _triggerPressedFlag = true;
                TriggerClicked();
            }
            else if (!trigger && _triggerPressedFlag)
            {
                _triggerPressedFlag = false;
                TriggerReleased();
            }

            bool thumbstick = PadPressed;
            if (thumbstick && !_padPressedFlag)
            {
                _padPressedFlag = true;
                TrackpadDown();
            }
            else if (!thumbstick && _padPressedFlag)
            {
                _padPressedFlag = false;
                TrackpadUp();
            }

            bool thumbstickTouch = PadTouched;
            if (thumbstickTouch && !_padTouchedFlag)
            {
                _padTouchedFlag = true;
                TrackpadTouch();
            }
            else if (!thumbstickTouch && _padTouchedFlag)
            {
                _padTouchedFlag = false;
                _stickLeftDown = false;
                _stickTopDown = false;
                _stickBottomDown = false;
                _stickRightDown = false;
                TrackpadUnTouch();
            }

            if (hmdType == HMDType.OCULUS && _padTouchedFlag)
            {
                if (PadLeftPressed && !_stickLeftDown)
                {
                    _stickLeftDown = true;
                    SendMessage(GlobalKeys.KEY_INPUT_RECEIVED, VRActions[padLeftOculus], SendMessageOptions.DontRequireReceiver);
                }
                else if (!PadLeftPressed && _stickLeftDown)
                    _stickLeftDown = false;

                if (PadRightPressed && !_stickRightDown)
                {
                    _stickRightDown = true;
                    SendMessage(GlobalKeys.KEY_INPUT_RECEIVED, VRActions[padRightOculus], SendMessageOptions.DontRequireReceiver);
                }
                else if (!PadRightPressed && _stickRightDown)
                    _stickRightDown = false;

                if (PadBottomPressed && !_stickBottomDown)
                {
                    _stickBottomDown = true;
                    SendMessage(GlobalKeys.KEY_INPUT_RECEIVED, VRActions[padBottomOculus], SendMessageOptions.DontRequireReceiver);
                }
                else if (!PadBottomPressed && _stickBottomDown)
                    _stickBottomDown = false;

                if (PadTopPressed && !_stickTopDown)
                {
                    _stickTopDown = true;
                    SendMessage(GlobalKeys.KEY_INPUT_RECEIVED, VRActions[padTopOculus], SendMessageOptions.DontRequireReceiver);
                }
                else if (!PadTopPressed && _stickTopDown)
                    _stickTopDown = false;
            }

            bool grip = GripPressed;
            if (grip && !_grippedFlag)
            {
                _grippedFlag = true;
                Gripped();
            }
            else if (!grip && _grippedFlag)
            {
                _grippedFlag = false;
                UnGripped();
            }

            bool menu = MenuPressed;
            if (menu && !_menuPressedFlag)
            {
                _menuPressedFlag = true;
                MenuClicked();
            }
            else if (!menu && _menuPressedFlag)
            {
                _menuPressedFlag = false;
                MenuReleased();
            }

            bool AX = AXPressed;
            if (AX && !_AX_PressedFlag)
            {
                _AX_PressedFlag = true;
                AXClicked();
            }
            else if (!AX && _AX_PressedFlag)
            {
                _AX_PressedFlag = false;
                AXReleased();
            }

        }


#if Int_SteamVR && !Int_SteamVR2

		//	If you are getting the error "The Type or namespace name 'SteamVR_TrackedController' could not be found."
		//	but you have SteamVR imported it is likely you imported the newest version of SteamVR which is not currently
		//	supported. The latest version that is supported is version 1.2.3 which you can download here:
		//	https://github.com/ValveSoftware/steamvr_unity_plugin/tree/fad02abee8ed45791993e92e420b340f63940aca
		//	Please delete the SteamVR folder and replace with the one from this repo.
		protected SteamVR_TrackedController _controller;

		public SteamVR_TrackedController controller
		{
			get 
			{
				if (_controller == null) _controller = GetComponent<SteamVR_TrackedController>();
				if (_controller == null) _controller = gameObject.AddComponent<SteamVR_TrackedController>();
				return _controller; 
			}
		}

#endif

#if Int_Oculus

        public OVRInput.Controller controllerHand;

#endif

        virtual public bool isSteamVR()
        {
#if Int_SteamVR
            if (GetComponent<SteamVR_TrackedObject>() != null || GetComponentInParent<SteamVR_PlayArea>() != null)
                return true;
            else
            {
#if Int_SteamVR2
                if (GetComponent<SteamVR_Behaviour_Pose>() != null) return true;
#endif
                return false;
            }
#elif Int_Oculus
			return false;
#else
			throw new System.Exception("Requires SteamVR or Oculus Integration asset. If one is already imported try re-importing, in the project window right click->Re-Import All.");
#endif
        }
        public string[] getVRActions { get { return VRActions; } set { VRActions = value; } }

        virtual public HMDType hmdType
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return HMDType.STANDALONE;
#if Int_SteamVR
                if ((GetComponent<SteamVR_TrackedObject>() != null || GetComponentInParent<SteamVR_PlayArea>() != null) || (SteamVR.active && SteamVR.instance != null && SteamVR.instance.hmd_TrackingSystemName != "oculus"))
                    return HMDType.VIVE;
                else
                    return HMDType.OCULUS;
#elif Int_Oculus
			return HMDType.OCULUS;
#else
			throw new System.Exception("Requires SteamVR or Oculus Integration asset. If one is already imported try re-importing, in the project window right click->Re-Import All.");
#endif
            }
        }
        virtual public bool IsLeftHand
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return !GetComponent<VRHandEmulation>().IsRightHand;

#if Int_SteamVR
                if (isSteamVR())
                {
#if !Int_SteamVR2
					SteamVR_ControllerManager controllerManager = null;
					if (transform.parent != null) controllerManager = transform.parent.GetComponent<SteamVR_ControllerManager>();
					else controllerManager = FindObjectOfType<SteamVR_ControllerManager>();
					if (controllerManager != null) return gameObject == controllerManager.left;
					else
					{
						Debug.LogError("Can't find SteamVR_ControllerManager in scene");
					}
#else
                    if (name.ToUpper().Contains("LEFT"))
                        handType = SteamVR_Input_Sources.LeftHand;
                    else handType = SteamVR_Input_Sources.RightHand;
                    return handType == SteamVR_Input_Sources.LeftHand;
#endif
                }
#endif

#if Int_Oculus
                if (!isSteamVR())
                {
                    OvrAvatar avatar = GetComponentInParent<OvrAvatar>();
                    if (avatar == null)
                    {
                        if (name.ToUpper().Contains("LEFT"))
                            controllerHand = OVRInput.Controller.LTouch;
                        else
                            controllerHand = OVRInput.Controller.RTouch;
                    }
                    else
                    {
                        if (avatar.ControllerLeft.transform == transform || avatar.HandLeft.transform == transform)
                            controllerHand = OVRInput.Controller.LTouch;
                        else if (avatar.ControllerRight.transform == transform || avatar.HandRight.transform == transform)
                            controllerHand = OVRInput.Controller.RTouch;
                    }
                    return controllerHand == OVRInput.Controller.LTouch;
                }
#endif
                return false;
            }
        }

        public bool ActionPressed(string action)
        {
#if Int_SteamVR && !Int_SteamVR2
			if (VRActions != null)
#else
            if (VRActions != null && !isSteamVR())
#endif
            {
                for (int i = 0; i < VRActions.Length; i++)
                {
                    if (action == VRActions[i])
                    {
                        return ActionPressed(i);
                    }
                }
            }
#if Int_SteamVR2
            foreach (SteamVR_Action_Boolean booleanAction in booleanActions)
            {
                if (booleanAction == null)
                {
                    Debug.LogError("SteamVR Inputs have not been setup for. Refer to the SteamVR 2.0 section of the Setup Guide. Found in Assets/VRInteraction/Docs.");
                    continue;
                }
                if (booleanAction.GetShortName() == action)
                {
                    return booleanAction.GetState(handType);
                }
            }
#endif
            return false;
        }


        //TODO: DEBUG VRActions
        public bool ActionPressed(int action)
        {
            if (hmdType == HMDType.VIVE)
            {
                if (triggerKey == action && TriggerPressed) return true;
                if (padTop == action && PadTopPressed) return true;
                if (padLeft == action && PadLeftPressed) return true;
                if (padRight == action && PadRightPressed) return true;
                if (padBottom == action && PadBottomPressed) return true;
                if (padCentre == action && PadCentrePressed) return true;
                if (padTouch == action && PadTouched) return true;
                if (menuKey == action && MenuPressed) return true;
                if (gripKey == action && GripPressed) return true;
                if (AXKey == action && AXPressed) return true;
            }
            else
            {
                if (triggerKeyOculus == action && TriggerPressed) return true;
                if (padTopOculus == action && PadTopPressed) return true;
                if (padLeftOculus == action && PadLeftPressed) return true;
                if (padRightOculus == action && PadRightPressed) return true;
                if (padBottomOculus == action && PadBottomPressed) return true;
                if (padCentreOculus == action && PadCentrePressed) return true;
                if (padTouchOculus == action && PadTouched) return true;
                if (menuKeyOculus == action && MenuPressed) return true;
                if (gripKeyOculus == action && GripPressed) return true;
                if (AXKeyOculus == action && AXPressed) return true;
            }
            return false;
        }

        virtual public bool TriggerPressed
        {
            get
            {
                return TriggerPressure > 0.5f;
            }
        }
        virtual public float TriggerPressure
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated && VRSimulatorRig.instance.IsTriggerPressed(IsLeftHand)) return 1f;

#if Int_SteamVR
                if (isSteamVR())
                {
#if !Int_SteamVR2

				var device = SteamVR_Controller.Input((int)controller.controllerIndex);
				return device.GetAxis(EVRButtonId.k_EButton_SteamVR_Trigger).x;
#else
                    if (triggerPressure != null) return triggerPressure.GetAxis(handType);
                    else Debug.LogError("SteamVR Inputs have not been setup. Refer to the SteamVR 2.0 section of the Setup Guide. Found in Assets/VRInteraction/Docs.");
#endif
                }
#endif

#if Int_Oculus
                if (!isSteamVR())
                {
                    return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controllerHand);
                }
#endif
                return 0f;
            }
        }

        virtual public bool PadTopPressed
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsPadTopPressed(IsLeftHand);

#if Int_SteamVR
                if (isSteamVR())
                {
                    if (PadPressed || (hmdType == HMDType.OCULUS && PadTouched))
                    {
                        Vector2 axis = PadPosition;
                        if (axis.y > (hmdType == HMDType.VIVE ? 0.4f : 0.8f) &&
                            axis.x < axis.y &&
                            axis.x > -axis.y)
                            return true;
                    }
                }
#endif

#if Int_Oculus
                if (!isSteamVR())
                {
                    Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controllerHand);
                    if (axis.y > 0.8f &&
                        axis.x < axis.y &&
                        axis.x > -axis.y)
                        return true;
                }
#endif
                return false;
            }
        }
        virtual public bool PadLeftPressed
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsPadLeftPressed(IsLeftHand);

#if Int_SteamVR
                if (isSteamVR())
                {
                    if (PadPressed || (hmdType == HMDType.OCULUS && PadTouched))
                    {
                        Vector2 axis = PadPosition;
                        if (axis.x < (hmdType == HMDType.VIVE ? -0.4f : -0.5f) &&
                            axis.y > axis.x &&
                            axis.y < -axis.x)
                            return true;
                    }
                }
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controllerHand);
                    if (axis.x < -0.5f &&
                        axis.y > axis.x &&
                        axis.y < -axis.x)
                        return true;
                }
#endif
                return false;
            }
        }
        virtual public bool PadRightPressed
        {
            get
            {

                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsPadRightPressed(IsLeftHand);

#if Int_SteamVR
                if (isSteamVR())
                {
                    if (PadPressed || (hmdType == HMDType.OCULUS && PadTouched))
                    {
                        Vector2 axis = PadPosition;
                        if (axis.x > (hmdType == HMDType.VIVE ? 0.4f : 0.5f) &&
                            axis.y < axis.x &&
                            axis.y > -axis.x)
                            return true;
                    }
                }
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controllerHand);
                    if (axis.x > 0.5f &&
                        axis.y < axis.x &&
                        axis.y > -axis.x)
                        return true;
                }
#endif
                return false;
            }
        }
        virtual public bool PadBottomPressed
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsPadBottomPressed(IsLeftHand);

#if Int_SteamVR
                if (isSteamVR())
                {
                    if (PadPressed || (hmdType == HMDType.OCULUS && PadTouched))
                    {
                        Vector2 axis = PadPosition;
                        if ((axis.y < (hmdType == HMDType.VIVE ? -0.4f : -0.8f) &&
                            axis.x > axis.y &&
                            axis.x < -axis.y))
                            return true;
                    }
                }
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controllerHand);
                    if ((axis.y < -0.8f &&
                        axis.x > axis.y &&
                        axis.x < -axis.y))
                        return true;
                }
#endif
                return false;
            }
        }
        virtual public bool PadCentrePressed
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsPadCentrePressed(IsLeftHand);

#if Int_SteamVR
                if (isSteamVR())
                {
                    if (PadPressed)
                    {
                        Vector2 axis = PadPosition;
                        if (axis.y >= -0.4f && axis.y <= 0.4f && axis.x >= -0.4f && axis.x <= 0.4f)
                            return true;
                    }
                }
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    if (OVRInput.Get(OVRInput.Button.DpadDown, controllerHand))
                    {
                        Vector2 axis = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controllerHand);
                        if (axis.y >= -0.4f && axis.y <= 0.4f && axis.x >= -0.4f && axis.x <= 0.4f)
                            return true;
                    }
                }
#endif
                return false;
            }
        }
        virtual public bool PadTouched
        {
            get
            {
                if (InteraktVR.Core.InteraktVRSetup.IsVRSimulated) return InteraktVR.Core.VRSimulatorRig.instance.IsPadTouched(IsLeftHand);

#if Int_SteamVR
                if (isSteamVR())
                {
#if !Int_SteamVR2
					return controller.padTouched;
#else
                    if (padTouched != null) return padTouched.GetState(handType);
                    else Debug.LogError("SteamVR Inputs have not been setup. Refer to the SteamVR 2.0 section of the Setup Guide. Found in Assets/VRInteraction/Docs.");
#endif
                }
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    return OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, controllerHand);
                }
#endif
                return false;
            }
        }
        virtual public bool PadPressed
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsPadPressed(IsLeftHand);

#if Int_SteamVR
                if (isSteamVR())
                {
#if !Int_SteamVR2
					return controller.padPressed;
#else
                    if (padPressed != null) return padPressed.GetState(handType);
                    else Debug.LogError("SteamVR Inputs have not been setup. Refer to the SteamVR 2.0 section of the Setup Guide. Found in Assets/VRInteraction/Docs.");
#endif
                }
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    return OVRInput.Get(OVRInput.Button.PrimaryThumbstick, controllerHand);
                }
#endif
                return false;
            }
        }
        virtual public Vector2 PadPosition
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.PadPosition(IsLeftHand);

#if Int_SteamVR
                if (isSteamVR())
                {
#if !Int_SteamVR2
					var device = SteamVR_Controller.Input((int)controller.controllerIndex);
					return device.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);
#else
                    if (touchPosition != null) return touchPosition.GetAxis(handType);
                    else Debug.LogError("SteamVR Inputs have not been setup. Refer to the SteamVR 2.0 section of the Setup Guide. Found in Assets/VRInteraction/Docs.");
#endif
                }
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controllerHand);
                }
#endif
                return Vector2.zero;
            }
        }
        virtual public bool GripPressed
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsGripPressed(IsLeftHand);

#if Int_SteamVR && !Int_SteamVR2
				if (isSteamVR())
				{
					return controller.gripped;
				}
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    return OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controllerHand) > 0.9f;
                }
#endif
                return false;
            }
        }
        virtual public bool MenuPressed
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsMenuPressed(IsLeftHand);

#if Int_SteamVR && !Int_SteamVR2
				if (isSteamVR())
				{
					return controller.menuPressed;
				}
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    return OVRInput.Get(OVRInput.Button.Two, controllerHand);
                }
#endif
                return false;
            }
        }
        virtual public bool AXPressed
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsAXPressed(IsLeftHand);

#if Int_SteamVR && !Int_SteamVR2
				if (isSteamVR())
				{
					var system = OpenVR.System;
					if (system != null && system.GetControllerState(controller.controllerIndex, ref controller.controllerState, (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t))))
					{
						ulong AButton = controller.controllerState.ulButtonPressed & (1UL << ((int)EVRButtonId.k_EButton_A));
						return AButton > 0L;
					}
				}
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    return OVRInput.Get(OVRInput.Button.One, controllerHand);
                }
#endif
                return false;
            }
        }

        virtual public bool BYPressed
        {
            get
            {
                if (InteraktVRSetup.IsVRSimulated) return VRSimulatorRig.instance.IsBYPressed(IsLeftHand);

#if Int_SteamVR && !Int_SteamVR2
				if (isSteamVR()) return false;
#endif
#if Int_Oculus
                if (!isSteamVR())
                {
                    return OVRInput.Get(OVRInput.Button.Two, controllerHand);
                }
#endif
                return false;
            }
        }


        public bool isTriggerPressed { get { return _triggerPressedFlag; } }
        public bool isPadPressed { get { return _padPressedFlag; } }
        public bool isPadTouched { get { return _padTouchedFlag; } }
        public bool isGripped { get { return _grippedFlag; } }
        public bool isBY_Pressed { get { return _menuPressedFlag; } }
        public bool isAX_Pressed { get { return _AX_PressedFlag; } }

        virtual public void SendMessageToInteractor(string message)
        {
            SendMessage(GlobalKeys.KEY_INPUT_RECEIVED, message, SendMessageOptions.DontRequireReceiver);
        }

        protected void TriggerClicked()
        {
            int triggerKey = this.triggerKey;
            if (hmdType == HMDType.OCULUS) triggerKey = this.triggerKeyOculus;
            if (triggerKey >= VRActions.Length)
            {
                Debug.LogWarning("Trigger key index (" + triggerKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[triggerKey]);
        }

        protected void TriggerReleased()
        {
            int triggerKey = this.triggerKey;
            if (hmdType == HMDType.OCULUS) triggerKey = this.triggerKeyOculus;
            if (triggerKey >= VRActions.Length)
            {
                Debug.LogWarning("Trigger key index (" + triggerKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[triggerKey] + "Released");
        }

        protected void TrackpadDown()
        {
            int action = 0;
            if (hmdType == HMDType.VIVE)
            {
                if (PadTopPressed) action = padTop;
                else if (PadLeftPressed) action = padLeft;
                else if (PadRightPressed) action = padRight;
                else if (PadBottomPressed) action = padBottom;
                else if (PadCentrePressed) action = padCentre;
            }
            else
            {
                action = padCentreOculus;
            }
            if (action >= VRActions.Length)
            {
                Debug.LogWarning("Pad key index (" + action + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[action]);
        }

        protected void TrackpadUp()
        {
            if (hmdType == HMDType.VIVE)
            {
                for (int i = 0; i < VRActions.Length; i++)
                {
                    if (padLeft == i || padTop == i || padRight == i || padBottom == i || padCentre == i)
                        SendMessageToInteractor(VRActions[i] + "Released");
                }
            }
            else
            {
                SendMessageToInteractor(VRActions[padCentreOculus] + "Released");
            }
        }

        protected void TrackpadTouch()
        {
            int touchKey = this.padTouch;
            if (hmdType == HMDType.OCULUS) touchKey = this.padTouchOculus;
            if (touchKey >= VRActions.Length)
            {
                Debug.LogWarning("Touch key index (" + touchKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[touchKey]);
        }

        protected void TrackpadUnTouch()
        {
            int touchKey = this.padTouch;
            if (hmdType == HMDType.OCULUS) touchKey = this.padTouchOculus;
            if (touchKey >= VRActions.Length)
            {
                Debug.LogWarning("Touch key index (" + touchKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[touchKey] + "Released");
        }

        protected void Gripped()
        {
            int gripKey = this.gripKey;
            if (hmdType == HMDType.OCULUS) gripKey = this.gripKeyOculus;
            if (gripKey >= VRActions.Length)
            {
                Debug.LogWarning("Gripped key index (" + gripKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[gripKey]);
        }

        protected void UnGripped()
        {
            int gripKey = this.gripKey;
            if (hmdType == HMDType.OCULUS) gripKey = this.gripKeyOculus;
            if (gripKey >= VRActions.Length)
            {
                Debug.LogWarning("Gripped key index (" + gripKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[gripKey] + "Released");
        }

        protected void MenuClicked()
        {
            int menuKey = this.menuKey;
            if (hmdType == HMDType.OCULUS) menuKey = this.menuKeyOculus;
            if (menuKey >= VRActions.Length)
            {
                Debug.LogWarning("Menu key index (" + menuKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[menuKey]);
        }

        protected void MenuReleased()
        {
            int menuKey = this.menuKey;
            if (hmdType == HMDType.OCULUS) menuKey = this.menuKeyOculus;
            if (menuKey >= VRActions.Length)
            {
                Debug.LogWarning("Menu key index (" + menuKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[menuKey] + "Released");
        }

        protected void AXClicked()
        {
            int aButtonKey = this.AXKey;
            if (hmdType == HMDType.OCULUS) aButtonKey = this.AXKeyOculus;
            if (aButtonKey >= VRActions.Length)
            {
                Debug.LogWarning("A Button key index (" + aButtonKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[aButtonKey]);
        }

        protected void AXReleased()
        {
            int aButtonKey = this.AXKey;
            if (hmdType == HMDType.OCULUS) aButtonKey = this.AXKeyOculus;
            if (aButtonKey >= VRActions.Length)
            {
                Debug.LogWarning("A Button key index (" + aButtonKey + ") out of range (" + VRActions.Length + ")");
                return;
            }
            SendMessageToInteractor(VRActions[aButtonKey] + "Released");
        }
    }

}
