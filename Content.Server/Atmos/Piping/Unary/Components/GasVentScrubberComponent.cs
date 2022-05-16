using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    [Friend(typeof(GasVentScrubberSystem))]
    public sealed class GasVentScrubberComponent : Component
    {
        private const float DefaultTransferRate = 5f; // L/s
        public const float CrumplePressure = Atmospherics.OneAtmosphere * 50f;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables]
        public bool IsDirty { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Welded { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string OutletName { get; set; } = "pipe";


        [ViewVariables(VVAccess.ReadWrite)]
        public ScrubberMode TargetMode { get; set; } = ScrubberMode.Scrubbing;

        [ViewVariables(VVAccess.ReadOnly)]
        public ScrubberMode ActualMode { get; set; }= ScrubberMode.Scrubbing;

        [ViewVariables(VVAccess.ReadWrite)]
        public float TransferRate
        {
            get => _transferRate;
            set => _transferRate = Math.Clamp(value, 0f, MaxTransferRate);
        }

        private float _transferRate = DefaultTransferRate;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxTransferRate")]
        public float MaxTransferRate = Atmospherics.MaxTransferRate;

        /// <summary>
        ///     As pressure difference approaches this number, the effective volume rate may be smaller than <see
        ///     cref="TransferRate"/>
        /// </summary>
        [DataField("maxPressure")]
        public float MaxOutPressure = Atmospherics.OneAtmosphere * 2f;

        [DataField("targetPressure")]
        public float TargetPressure = Atmospherics.OneAtmosphere;

        public GasVentScrubberData ToAirAlarmData()
        {
            return new GasVentScrubberData
            {
                Enabled = Enabled,
                Dirty = IsDirty,
                Mode = TargetMode,
                VolumeRate = TransferRate,
                TargetPressure = TargetPressure
            };
        }

        public void FromAirAlarmData(GasVentScrubberData data)
        {
            Enabled = data.Enabled;
            IsDirty = data.Dirty;
            TargetMode = data.Mode;
            TransferRate = data.VolumeRate;
            TargetPressure = data.TargetPressure;
        }

        public static GasVentScrubberData ScrubbingModePreset = new GasVentScrubberData
        {
            Enabled = true,
            Mode = ScrubberMode.Scrubbing,
            VolumeRate = DefaultTransferRate,
        };

        public static GasVentScrubberData SiphonModePreset = new GasVentScrubberData
        {
            Enabled = true,
            Dirty = true,
            Mode = ScrubberMode.Siphoning,
        };
    }
}
