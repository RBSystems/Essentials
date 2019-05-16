using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.SmartObjects;
using PepperDash.Essentials.Devices.Common.VideoCodec;
using PepperDash.Essentials.Devices.Common.Cameras;

namespace PepperDash.Essentials.UIDrivers.VC
{
    public enum eCameraMode
    {
        Manual,
        Auto,
        Off
    }

    /// <summary>
    /// Supports the available camera control modes and controls
    /// </summary>
    public class EssentialsVideoCodecCameraUiDriver : PanelDriverBase, IPanelSubdriver
    {
        EssentialsVideoCodecUiDriver Parent;

        IHasCodecCameras Codec;

        public uint SubpageVisibleJoin { get; private set; }

        SmartObjectDPad CameraDPad;

        protected Dictionary<string, Action> CameraModeJoins;

        JoinedSigInterlock CameraModeInterlock;

        SubpageReferenceList CameraModeSRL;

        SubpageReferenceList CameraListSRL;

        BoolFeedback CameraAutoModeIsOnFeedback;

        BoolFeedback CameraManualModeIsOnFeedback;

        BoolFeedback CameraOffModeIsOnFeedback;

        public EssentialsVideoCodecCameraUiDriver(EssentialsVideoCodecUiDriver parent, VideoCodecBase codec, uint subpageVisibleBase)
            : base(parent.TriList)
        {
            Parent = parent;

            Codec = codec as IHasCodecCameras;

            if (TriList.SmartObjects.Contains(UISmartObjectJoin.VCCameraDpad))
                CameraDPad = new SmartObjectDPad(TriList.SmartObjects[UISmartObjectJoin.VCCameraDpad], true);

            CameraModeInterlock = new JoinedSigInterlock(TriList);

            CameraModeInterlock.StatusChanged += new EventHandler<StatusChangedEventArgs>(CameraModeInterlock_StatusChanged);

            CameraModeInterlock.SetButDontShow(UIBoolJoin.VCCameraManualVisible);

            // If the codec is ready, then setup the cameras and presets, otherwise wait
            var videoCodec = codec as VideoCodecBase;

            if (videoCodec.IsReady)
                Codec_IsReady();
            else
                videoCodec.IsReadyChange += (o, a) => Codec_IsReady();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Codec_IsReady()
        {
            SetupCameraModes();

            SetupCamerasList();

            MapCameraControls();

            if (Codec is IHasCodecRoomPresets)
                SetupCameraPresets();
        }

        void CameraModeInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (CameraAutoModeIsOnFeedback != null)
                CameraAutoModeIsOnFeedback.FireUpdate();

            if (CameraManualModeIsOnFeedback != null)
                CameraManualModeIsOnFeedback.FireUpdate();

            if (CameraOffModeIsOnFeedback != null)
                CameraOffModeIsOnFeedback.FireUpdate();
        }

        /// <summary>
        /// Sets up the camera modes list based on the modes available on the codec
        /// </summary>
        void SetupCameraModes()
        {
            CameraModeSRL = new SubpageReferenceList(TriList, UISmartObjectJoin.VCCameraMode, 1, 1, 1);

            ushort i = 1;

            var autoCameraCodec = Codec as IHasCameraAutoMode;
            if (autoCameraCodec != null)
            {

                autoCameraCodec.CameraAutoModeIsOnFeedback.OutputChange += new EventHandler<FeedbackEventArgs>(CameraAutoModeIsOnFeedback_OutputChange);

                CameraModeSRL.AddItem(new SubpageReferenceListItem(i, CameraModeSRL));

                CameraModeSRL.StringInputSig(i, 1).StringValue = "Auto";
                CameraModeSRL.GetBoolFeedbackSig(i, 1).SetSigFalseAction(() => autoCameraCodec.CameraAutoModeOn());
                CameraAutoModeIsOnFeedback = new BoolFeedback(() => autoCameraCodec.CameraAutoModeIsOnFeedback.BoolValue);
                CameraAutoModeIsOnFeedback.LinkInputSig(CameraModeSRL.BoolInputSig(i, 1));

                i++;
            }

            CameraModeSRL.AddItem(new SubpageReferenceListItem(i, CameraModeSRL));

            CameraModeSRL.StringInputSig(i, 1).StringValue = "Manual";
            if (autoCameraCodec != null)
                CameraModeSRL.GetBoolFeedbackSig(i, 1).SetSigFalseAction(() => autoCameraCodec.CameraAutoModeOff());
            else
                CameraModeSRL.GetBoolFeedbackSig(i, 1).SetSigFalseAction(() => CameraModeInterlock.ShowInterlocked(UIBoolJoin.VCCameraManualVisible));

            CameraManualModeIsOnFeedback = new BoolFeedback(() => CameraModeInterlock.CurrentJoin == UIBoolJoin.VCCameraManualVisible);
            CameraManualModeIsOnFeedback.LinkInputSig(CameraModeSRL.BoolInputSig(i, 1));


            var offCameraCodec = Codec as IHasCameraOff;
            if (offCameraCodec != null)
            {
                i++;
                CameraModeSRL.AddItem(new SubpageReferenceListItem(i, CameraModeSRL));

                CameraModeSRL.StringInputSig(i, 1).StringValue = "Manual";
                CameraModeSRL.GetBoolFeedbackSig(i, 1).SetSigFalseAction(() => offCameraCodec.CameraOff());
                CameraOffModeIsOnFeedback = new BoolFeedback(() => CameraModeInterlock.CurrentJoin == UIBoolJoin.VCCameraOffModeVisible);
                CameraOffModeIsOnFeedback.LinkInputSig(CameraModeSRL.BoolInputSig(i, 1));
            }

            CameraModeSRL.Count = i;
        }

        /// <summary>
        /// Sets up the camera list based on the cameras available on the codec
        /// </summary>
        void SetupCamerasList()
        {
            CameraListSRL = new SubpageReferenceList(TriList, UISmartObjectJoin.VCCameraList, 1, 1, 1);

            Codec.CameraSelected += new EventHandler<CameraSelectedEventArgs>(Codec_CameraSelected);

            ushort i = 0;

            foreach (var camera in Codec.Cameras)
            {
                i++;
                var c = camera;

                if (i <= CameraListSRL.MaxDefinedItems) // Only maps cameras up to the max defined items
                {
                    CameraListSRL.AddItem(new SubpageReferenceListItem(i, CameraListSRL));

                    CameraListSRL.StringInputSig(i, 1).StringValue = c.Name;
                    CameraListSRL.GetBoolFeedbackSig(i, 1).SetSigFalseAction(() => Codec.SelectCamera(c.Key));

                    if (Codec.SelectedCamera.Key == c.Key)
                        CameraListSRL.BoolInputSig(i, 1).BoolValue = true;
                    else
                        CameraListSRL.BoolInputSig(i, 1).BoolValue = false;
                }
            }

            CameraListSRL.Count = i;
        }

        /// <summary>
        /// Sets button feedback for the selected camera and maps controls to that camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Codec_CameraSelected(object sender, CameraSelectedEventArgs e)
        {
            for ( uint i = 1; i <= CameraListSRL.Count; i++)
            {
                if (e.SelectedCamera.Name == CameraListSRL.StringInputSig(i, 1).StringValue)
                    CameraListSRL.BoolInputSig(i, 1).BoolValue = true;
                else
                    CameraListSRL.BoolInputSig(i, 1).BoolValue = false;
            }

            MapCameraControls();

            SendCameraPresetNames();
        }

        /// <summary>
        /// Maps the camera control buttons to the currently selected camera
        /// </summary>
        void MapCameraControls()
        {
            CameraDPad.SigCenter.ClearSigAction();
            CameraDPad.SigDown.ClearSigAction();
            CameraDPad.SigLeft.ClearSigAction();
            CameraDPad.SigRight.ClearSigAction();
            CameraDPad.SigUp.ClearSigAction();

            if (Codec.SelectedCamera != null)
            {
                var camera = Codec.SelectedCamera as IHasCameraPtzControl;
                if (camera != null)
                {
                    CameraDPad.SigUp.SetBoolSigAction(new Action<bool>(b => { if (b)camera.TiltUp(); else camera.TiltStop(); }));
                    CameraDPad.SigDown.SetBoolSigAction(new Action<bool>(b => { if (b)camera.TiltDown(); else camera.TiltStop(); }));
                    CameraDPad.SigLeft.SetBoolSigAction(new Action<bool>(b => { if (b)camera.PanLeft(); else camera.PanStop(); }));
                    CameraDPad.SigRight.SetBoolSigAction(new Action<bool>(b => { if (b)camera.PanRight(); else camera.PanStop(); }));
                    CameraDPad.SigCenter.SetSigFalseAction(new Action(camera.PositionHome));

                    TriList.SetBoolSigAction(UIBoolJoin.VCCameraZoomInPress, new Action<bool>(b => { if (b)camera.ZoomIn(); else camera.ZoomStop(); }));
                    TriList.SetBoolSigAction(UIBoolJoin.VCCameraZoomOutPress, new Action<bool>(b => { if (b)camera.ZoomOut(); else camera.ZoomStop(); }));

                    var focusCamera = camera as IHasCameraFocusControl;

                    TriList.ClearBoolSigAction(UIBoolJoin.VCCameraFocusNearPress);
                    TriList.ClearBoolSigAction(UIBoolJoin.VCCameraFocusFarPress);
                    TriList.ClearBoolSigAction(UIBoolJoin.VCCameraToggleAutoFocusPress);

                    if (focusCamera != null)
                    {
                        TriList.SetBoolSigAction(UIBoolJoin.VCCameraFocusNearPress, new Action<bool>(b => { if (b)focusCamera.FocusNear(); else focusCamera.FocusStop(); }));
                        TriList.SetBoolSigAction(UIBoolJoin.VCCameraFocusFarPress, new Action<bool>(b => { if (b)focusCamera.FocusFar(); else focusCamera.FocusStop(); }));

                        TriList.SetSigFalseAction(UIBoolJoin.VCCameraToggleAutoFocusPress, new Action(focusCamera.TriggerAutoFocus));
                    }
                }
            }
        }

        /// <summary>
        /// Setups up the camera preset button actions
        /// </summary>
        void SetupCameraPresets()
        {
            var presetsCodec = Codec as IHasCodecRoomPresets;

            uint holdTime = 2000;   // Hold time of 2s

            if (presetsCodec != null)
            {
                presetsCodec.CodecRoomPresetsListHasChanged += new EventHandler<EventArgs>(presetsCodec_CodecRoomPresetsListHasChanged);

                int presetNum = 1;
                for(uint join = UIBoolJoin.VCCameraPresetPressStart; join <= UIBoolJoin.VCCameraPresetPressEnd; join++)
                {
                    var preset = presetNum; // needs local scope for lamdas
                    TriList.SetSigHeldAction(join, holdTime, new Action(() => PresetStoreTriggered(preset)), new Action(() => presetsCodec.CodecRoomPresetSelect(preset)));
                    presetNum++;
                }

                SendCameraPresetNames();
            }

        }

        /// <summary>
        /// Handles when the room presets list has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void presetsCodec_CodecRoomPresetsListHasChanged(object sender, EventArgs e)
        {
            SendCameraPresetNames();
        }


        /// <summary>
        /// Sends the preset names to the appropriate string sigs
        /// </summary>
        void SendCameraPresetNames()
        {
            var presetsCodec = Codec as IHasCodecRoomPresets;

            if(presetsCodec != null)
            {
                List<CodecRoomPreset> currentPresets = new List<CodecRoomPreset>();

                if (Codec is IHasFarEndCameraControl && (Codec as IHasFarEndCameraControl).ControllingFarEndCameraFeedback.BoolValue)
                    currentPresets = presetsCodec.FarEndRoomPresets;
                else
                    currentPresets = presetsCodec.NearEndPresets;

                int presetindex = 0;
                for (uint join = UIBoolJoin.VCCameraPresetPressStart; join <= UIBoolJoin.VCCameraPresetPressEnd; join++)
                {
                    TriList.SetString(join, currentPresets[presetindex].Description);
                    presetindex++;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="presetNumber"></param>
        void PresetStoreTriggered(int presetNumber)
        {
            // TODO: Popup the keyboard for preset name editing
        }

        /// <summary>
        /// Respond to Camera Auto Mode state change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CameraAutoModeIsOnFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            uint cameraModeJoin;

            if(e.BoolValue)
                cameraModeJoin = UIBoolJoin.VCSelfViewLayoutVisible;
            else
                cameraModeJoin = UIBoolJoin.VCCameraManualVisible;

            // Either show or just set the join for the interlock based on the current mode
            if (IsVisible)
                CameraModeInterlock.ShowInterlocked(cameraModeJoin);
            else
                CameraModeInterlock.SetButDontShow(cameraModeJoin);

            CameraAutoModeIsOnFeedback.FireUpdate();
        }

        public override void Show()
        {
            CameraModeInterlock.Show();

            base.Show();
        }

        public override void Hide()
        {
            CameraModeInterlock.Hide();

            base.Hide();
        }
    }
}