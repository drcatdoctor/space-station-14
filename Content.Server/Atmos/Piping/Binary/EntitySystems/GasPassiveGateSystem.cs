using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasPassiveGateSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPassiveGateComponent, AtmosDeviceUpdateEvent>(OnPassiveGateUpdated);
        }

        private void OnPassiveGateUpdated(EntityUid uid, GasPassiveGateComponent gate, AtmosDeviceUpdateEvent args)
        {
            if (!gate.Enabled)
                return;

            if (!EntityManager.TryGetComponent(uid, out NodeContainerComponent? nodeContainer))
                return;

            if (!nodeContainer.TryGetNode(gate.InletName, out PipeNode? inlet)
                || !nodeContainer.TryGetNode(gate.OutletName, out PipeNode? outlet))
                return;

            var outputStartingPressure = outlet.Air.Pressure;
            var inputStartingPressure = inlet.Air.Pressure;

            if (inputStartingPressure < gate.TargetPressure ||
                inputStartingPressure - outputStartingPressure < gate.FrictionPressureDifference)
                return;

            // Input Pressure would be 0 if there's no moles so let's not check for moles

            // We calculate the differences in moles using our good ol' friend PV=nRT.
            var deltaMoles = (inputStartingPressure - outputStartingPressure) * outlet.Air.Volume / (inlet.Air.Temperature * Atmospherics.R);

            // Transfer half to equalize the two sides.
            _atmosphereSystem.Merge(outlet.Air, inlet.Air.Remove(deltaMoles/2));
        }
    }
}
