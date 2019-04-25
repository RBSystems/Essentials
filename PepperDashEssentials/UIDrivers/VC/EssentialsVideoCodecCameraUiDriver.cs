using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
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

        bool SupportsCameraAutoMode;

        bool SupportsCameraOffMode;

        protected Dictionary<string, Action> CameraModeJoins;

        JoinedSigInterlock CameraModeInterlock;

        SubpageReferenceList CameraModeSRL;

        SubpageReferenceList CameraListSRL;

        public EssentialsVideoCodecCameraUiDriver(EssentialsVideoCodecUiDriver parent, VideoCodecBase codec, uint subpageVisibleBase)
            : base(parent.TriList)
        {
            Parent = parent;

            Codec = codec as IHasCodecCameras;

            CameraModeInterlock = new JoinedSigInterlock(TriList);
            CameraModeInterlock.SetButDontShow(UIBoolJoin.VCCameraManualModeVisible);


        }

        /// <summary>
        /// Sets up the camera modes list based on the modes available on the codec
        /// </summary>
        void SetupCamerasModes()
        {
            CameraModeSRL = new SubpageReferenceList(TriList, UISmartObjectJoin.VCCameraList, 1, 1, 1);

            ushort i = 1;

            var autoCameraCodec = Codec as IHasCameraAutoMode;
            if (autoCameraCodec != null)
            {
                autoCameraCodec.CameraAutoModeIsOnFeedback.OutputChange += new EventHandler<FeedbackEventArgs>(CameraAutoModeIsOnFeedback_OutputChange);

                CameraModeSRL.StringInputSig(i, 1).StringValue = "Auto";
                CameraModeSRL.GetBoolFeedbackSig(1, 1).SetSigFalseAction(() => CameraModeInterlock.ShowInterlocked(UIBoolJoin.VCCameraAutoModeVisible));
                autoCameraCodec.CameraAutoModeIsOnFeedback.LinkInputSig(CameraModeSRL.BoolInputSig(i, 1));
                // TODO: Deal with feedback so that it matches the current interlocked CameraModeInterlock value

                i++;
            }

            CameraModeSRL.StringInputSig(i, 1).StringValue = "Manual";
            CameraModeSRL.GetBoolFeedbackSig(1, 1).SetSigFalseAction(() => CameraModeInterlock.ShowInterlocked(UIBoolJoin.VCCameraManualVisible));

            // TODO: Deal with feedback so that it matches the current interlocked CameraModeInterlock value


            var offCameraCodec = Codec as IHasCameraOff;
            if (offCameraCodec != null)
            {
                i++;

                CameraModeSRL.StringInputSig(i, 1).StringValue = "Manual";
                CameraModeSRL.GetBoolFeedbackSig(1, 1).SetSigFalseAction(() => CameraModeInterlock.ShowInterlocked(UIBoolJoin.VCCameraOffModeVisible));

                // TODO: Deal with feedback so that it matches the current interlocked CameraModeInterlock value
                
            }

            CameraModeSRL.Count = i;

            CameraListSRL.Refresh();
        }

        /// <summary>
        /// Sets up the camera list based on the cameras available on the codec
        /// </summary>
        void SetupCamerasList()
        {
            CameraListSRL = new SubpageReferenceList(TriList, UISmartObjectJoin.VCCameraList, 1, 1, 1);

            foreach (var mode in CameraModeJoins)
            {
                SubpageReferenceListItem item = new SubpageReferenceListItem(1, CameraModeSRL);

            }
        }

        /// <summary>
        /// Maps the camera control buttons to the currently selected camera
        /// </summary>
        void MapCameraControls()
        {

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
                cameraModeJoin = UIBoolJoin.VCCameraAutoModeVisible;
            else
                cameraModeJoin = UIBoolJoin.VCCameraManualModeVisible;

            // Either show or just set the join for the interlock based on the current mode
            if (IsVisible)
                CameraModeInterlock.ShowInterlocked(cameraModeJoin);
            else
                CameraModeInterlock.SetButDontShow(cameraModeJoin);
        }

        public override void Show()
        {
            TriList.SetBool(UIBoolJoin.VCCameraModeVisible, true);

            CameraModeInterlock.Show();

            base.Show();
        }

        public override void Hide()
        {
            TriList.SetBool(UIBoolJoin.VCCameraModeVisible, false);

            CameraModeInterlock.Hide();

            base.Hide();
        }
    }
}