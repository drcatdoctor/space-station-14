using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos.Piping.Unary.Visuals;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Audio;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Unary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasVentScrubberSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetSystem = default!;

        private const float ScrubbingVolume = -12f;
        private const float ScrubbingSoundRange = 5;
        private const float SiphoningVolume = 12f;
        private const float SiphoningSoundRange = 15;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceUpdateEvent>(OnVentScrubberUpdated);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceEnabledEvent>(OnVentScrubberEnterAtmosphere);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosDeviceDisabledEvent>(OnVentScrubberLeaveAtmosphere);
            SubscribeLocalEvent<GasVentScrubberComponent, AtmosMonitorAlarmEvent>(OnAtmosAlarm);
            SubscribeLocalEvent<GasVentScrubberComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<GasVentScrubberComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
        }

        private void OnVentScrubberUpdated(EntityUid uid, GasVentScrubberComponent scrubber, AtmosDeviceUpdateEvent args)
        {
            if (scrubber.Welded)
            {
                return;
            }

            if (!scrubber.Enabled
                || !TryComp(uid, out AtmosDeviceComponent? device)
                || !TryComp(uid, out NodeContainerComponent? nodeContainer)
                || !nodeContainer.TryGetNode(scrubber.OutletName, out PipeNode? outlet)
                )
            {
                return;
            }

            var xform = Transform(uid);
            var environment = _atmosphereSystem.GetTileMixture(xform.Coordinates, true);
            if (environment == null)
            {
                return;
            }

            var timeDelta = (float) (_gameTiming.CurTime - device.LastProcess).TotalSeconds;

            var needsUpdate = Scrub(timeDelta, scrubber, environment, outlet);
            if (needsUpdate) UpdateState(uid, scrubber);
        }

        private void OnVentScrubberLeaveAtmosphere(EntityUid uid, GasVentScrubberComponent component,
            AtmosDeviceDisabledEvent args) => UpdateState(uid, component);

        private void OnVentScrubberEnterAtmosphere(EntityUid uid, GasVentScrubberComponent component,
            AtmosDeviceEnabledEvent args) => UpdateState(uid, component);

        /**
         * <returns>True if scrubber state is dirty (requires UpdateState).</returns>
         */
        private bool Scrub(float timeDelta, GasVentScrubberComponent scrubber, GasMixture tile, PipeNode outlet)
        {
            if (tile.Pressure >= GasVentScrubberComponent.CrumplePressure)
            {
                scrubber.Welded = true;
                return true;
            }

            var needsUpdate = false;

            // auto-siphon at high rates
            // TODO make this make a loud CHUNK noise as the vent snaps open
            if (tile.Pressure >= (scrubber.TargetPressure * 1.1f))
            {
                if (scrubber.ActualMode != VentOrScrubberMode.High)
                {
                    scrubber.ActualMode = VentOrScrubberMode.High;
                    needsUpdate = true;
                }
            }
            else
            {
                if (scrubber.ActualMode != scrubber.TargetMode)
                {
                    scrubber.ActualMode = scrubber.TargetMode;
                    needsUpdate = true;
                }
            }

            // don't check pressure until here, so the ActualMode updates correctly in Bad Situations
            if (outlet.Air.Pressure >= scrubber.MaxOutPressure)
            {
                return needsUpdate;
            }

            float transferRate = scrubber.ActualMode == VentOrScrubberMode.High
                ? scrubber.MaxTransferRate
                : scrubber.TransferRate;

            // Take a gas sample.
            var ratio = MathF.Min(1f, timeDelta * transferRate / tile.Volume);
            var removed = tile.RemoveRatio(ratio);

            // Nothing left to remove from the tile.
            if (MathHelper.CloseToPercent(removed.TotalMoles, 0f))
                return needsUpdate;

            _atmosphereSystem.Merge(outlet.Air, removed);
            return needsUpdate;
        }

        private void OnAtmosAlarm(EntityUid uid, GasVentScrubberComponent component, AtmosMonitorAlarmEvent args)
        {
            if (args.HighestNetworkType == AtmosMonitorAlarmType.Danger)
            {
                component.TargetMode = VentOrScrubberMode.High;
                component.Enabled = true;
            }
            else if (args.HighestNetworkType == AtmosMonitorAlarmType.Normal)
            {
                component.TargetMode = VentOrScrubberMode.Low;
                component.Enabled = true;
            }

            UpdateState(uid, component);
        }

        private void OnPowerChanged(EntityUid uid, GasVentScrubberComponent component, PowerChangedEvent args)
        {
            component.Enabled = args.Powered;
            UpdateState(uid, component);
        }

        private void OnPacketRecv(EntityUid uid, GasVentScrubberComponent component, DeviceNetworkPacketEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent netConn)
                || !args.Data.TryGetValue(DeviceNetworkConstants.Command, out var cmd))
                return;

            var payload = new NetworkPayload();

            switch (cmd)
            {
                case AirAlarmSystem.AirAlarmSyncCmd:
                    payload.Add(DeviceNetworkConstants.Command, AirAlarmSystem.AirAlarmSyncData);
                    payload.Add(AirAlarmSystem.AirAlarmSyncData, component.ToAirAlarmData());

                    _deviceNetSystem.QueuePacket(uid, args.SenderAddress, payload, device: netConn);

                    return;
                case AirAlarmSystem.AirAlarmSetData:
                    if (!args.Data.TryGetValue(AirAlarmSystem.AirAlarmSetData, out VentOrScrubberData? setData))
                        break;

                    component.FromAirAlarmData(setData.Value);
                    UpdateState(uid, component);
                    payload.Add(DeviceNetworkConstants.Command, AirAlarmSystem.AirAlarmSetDataStatus);
                    payload.Add(AirAlarmSystem.AirAlarmSetDataStatus, true);

                    _deviceNetSystem.QueuePacket(uid, null, payload, device: netConn);

                    return;
            }
        }

        /// <summary>
        ///     Updates a scrubber's appearance and ambience state.
        /// </summary>
        private void UpdateState(EntityUid uid, GasVentScrubberComponent scrubber,
            AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref appearance, false))
                return;

            EntityManager.TryGetComponent<AmbientSoundComponent>(uid, out var ambience);

            if (!scrubber.Enabled)
            {
                if (ambience.Enabled)
                {
                    ambience.Enabled = false;
                    Dirty(ambience);
                }
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Off);
            }
            else if (scrubber.ActualMode == VentOrScrubberMode.Low)
            {
                if (!ambience.Enabled || !ambience.Volume.Equals(ScrubbingVolume))
                {
                    ambience.Enabled = true;
                    ambience.Volume = ScrubbingVolume;
                    ambience.Range = ScrubbingSoundRange;
                    Dirty(ambience);
                }
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Scrub);
            }
            else if (scrubber.ActualMode == VentOrScrubberMode.High)
            {
                if (!ambience.Enabled || !ambience.Volume.Equals(SiphoningVolume))
                {
                    ambience.Enabled = true;
                    ambience.Volume = SiphoningVolume;
                    ambience.Range = SiphoningSoundRange;
                    Dirty(ambience);
                }
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Siphon);
            }
            else if (scrubber.Welded)
            {
                if (ambience.Enabled)
                {
                    ambience.Enabled = false;
                    Dirty(ambience);
                }
                appearance.SetData(ScrubberVisuals.State, ScrubberState.Welded);
            }
        }
    }
}
