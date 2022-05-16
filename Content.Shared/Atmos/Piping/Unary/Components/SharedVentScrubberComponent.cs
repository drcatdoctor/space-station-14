using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components
{
    [Serializable, NetSerializable]
    public struct GasVentScrubberData : IAtmosDeviceData
    {
        public bool Enabled { get; set; }
        public bool Dirty { get; set; }
        public bool IgnoreAlarms { get; set; }
        public ScrubberMode Mode { get; set; }
        public float VolumeRate { get; set; }
        public float TargetPressure { get; set; }
    }

    [Serializable, NetSerializable]
    public enum ScrubberMode : sbyte
    {
        Siphoning = 0,
        Scrubbing = 1,
    }
}
