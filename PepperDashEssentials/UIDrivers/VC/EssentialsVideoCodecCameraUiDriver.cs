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

        protected List<uint> CameraModeJoins;

        JoinedSigInterlock CameraModeInterlock;

        public EssentialsVideoCodecCameraUiDriver(EssentialsVideoCodecUiDriver parent, VideoCodecBase codec, uint subpageVisibleBase)
            : base(parent.TriList)
        {
            Parent = parent;

            Codec = codec as IHasCodecCameras;

            CameraModeInterlock = new JoinedSigInterlock(TriList);
            CameraModeInterlock.SetButDontShow(UIBoolJoin.VCCameraManualModeVisible);

            CameraModeJoins = new List<uint>();

            CameraModeJoins.Add(UIBoolJoin.VCCameraManualModeVisible);
            TriList.SetSigFalseAction(UIBoolJoin.VCCameraManualModePress, () => CameraModeInterlock.ShowInterlocked(UIBoolJoin.VCCameraManualModeVisible));


            if (Codec is IHasCameraAutoMode)
            {
                CameraModeJoins.Add(UIBoolJoin.VCCameraAutoModeVisible);
                TriList.SetSigFalseAction(UIBoolJoin.VCCameraManualModePress, () => CameraModeInterlock.ShowInterlocked(UIBoolJoin.VCCameraAutoModeVisible));
            }

            if (Codec is IHasCameraOff)
            {
                CameraModeJoins.Add(UIBoolJoin.VCCameraOffModeVisible);
                TriList.SetSigFalseAction(UIBoolJoin.VCCameraOffModePress, () => CameraModeInterlock.ShowInterlocked(UIBoolJoin.VCCameraOffModePress));
            }


        }
    }
}