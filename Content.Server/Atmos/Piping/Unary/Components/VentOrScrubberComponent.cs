using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;

namespace Content.Server.Atmos.Piping.Unary.Components;

public class VentOrScrubberComponent : Component
{
    private const float DefaultTransferRate = 5f; // L/s
    public const float CrumplePressure = Atmospherics.OneAtmosphere * 50f;

    [ViewVariables(VVAccess.ReadWrite)] public bool Enabled { get; set; } = true;

    [ViewVariables] public bool IsDirty { get; set; } = false;

    [ViewVariables(VVAccess.ReadWrite)] public bool Welded { get; set; } = false;

    [ViewVariables(VVAccess.ReadWrite)] public VentOrScrubberMode TargetMode { get; set; } = VentOrScrubberMode.Low;

    [ViewVariables(VVAccess.ReadOnly)] public VentOrScrubberMode ActualMode { get; set; } = VentOrScrubberMode.Low;

    [ViewVariables(VVAccess.ReadWrite)]
    public float TransferRate
    {
        get => _transferRate;
        set => _transferRate = Math.Clamp(value, 0f, MaxTransferRate);
    }

    private float _transferRate = DefaultTransferRate;

    [ViewVariables(VVAccess.ReadWrite)] [DataField("maxTransferRate")]
    public float MaxTransferRate = Atmospherics.MaxTransferRate;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("backpressureTolerance")]
    public float BackpressureTolerance = Atmospherics.OneAtmosphere * 2f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("targetPressure")]
    public float TargetPressure = Atmospherics.OneAtmosphere;

    public VentOrScrubberData ToAirAlarmData()
    {
        return new VentOrScrubberData
        {
            Enabled = Enabled,
            Dirty = IsDirty,
            Mode = TargetMode,
            VolumeRate = TransferRate,
            TargetPressure = TargetPressure
        };
    }

    public void FromAirAlarmData(VentOrScrubberData data)
    {
        Enabled = data.Enabled;
        IsDirty = data.Dirty;
        TargetMode = data.Mode;
        TransferRate = data.VolumeRate;
        TargetPressure = data.TargetPressure;
    }
}
