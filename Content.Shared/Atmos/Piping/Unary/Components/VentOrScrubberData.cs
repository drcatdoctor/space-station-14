using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components
{
    [Serializable, NetSerializable]
    public struct VentOrScrubberData : IAtmosDeviceData
    {
        public AtmosDeviceType DeviceType;
        public bool Enabled { get; set; }
        public bool Dirty { get; set; }
        public bool IgnoreAlarms { get; set; }
        public VentOrScrubberMode Mode { get; set; }
        public float VolumeRate { get; set; }
        public float TargetPressure { get; set; }
    }

    [Serializable, NetSerializable]
    public enum VentOrScrubberMode : sbyte
    {
        High = 0,
        Low = 1,
    }

    [Serializable, NetSerializable]
    public enum AtmosDeviceType : sbyte
    {
        Vent,
        Scrubber,
    }

}
