using Robust.Client.GameObjects;
using Content.Shared.Lathe;
using Content.Shared.Power;
using Content.Client.Power;
using Content.Client.Wires.Visualizers;
using Content.Shared.Wires;

namespace Content.Client.Lathe
{
    public sealed class LatheSystem : VisualizerSystem<LatheVisualsComponent>
    {
        protected override void OnAppearanceChange(EntityUid uid, LatheVisualsComponent component, ref AppearanceChangeEvent args)
        {
            if (!TryComp(uid, out SpriteComponent? sprite)) return;

            if (args.Component.TryGetData(PowerDeviceVisuals.Powered, out bool powered) &&
                sprite.LayerMapTryGet(PowerDeviceVisualLayers.Powered, out _))
            {
                sprite.LayerSetVisible(PowerDeviceVisualLayers.Powered, powered);
            }

            if (args.Component.TryGetData(WiresVisuals.MaintenancePanelState, out bool panel)
                && sprite.LayerMapTryGet(WiresVisualizer.WiresVisualLayers.MaintenancePanel, out _))
            {
                sprite.LayerSetVisible(WiresVisualizer.WiresVisualLayers.MaintenancePanel, panel);
            }

            // Lathe specific stuff
            if (args.Component.TryGetData(LatheVisuals.IsRunning, out bool isRunning))
            {
                var state = isRunning ? component.RunningState : component.IdleState;
                sprite.LayerSetAnimationTime(LatheVisualLayers.IsRunning, 0f);
                sprite.LayerSetState(LatheVisualLayers.IsRunning, state);
            }

            if (args.Component.TryGetData(LatheVisuals.IsInserting, out bool isInserting)
                && sprite.LayerMapTryGet(LatheVisualLayers.IsInserting, out var isInsertingLayer))
            {
                if (args.Component.TryGetData(LatheVisuals.InsertingColor, out Color color)
                    && !component.IgnoreColor)
                {
                    sprite.LayerSetColor(isInsertingLayer, color);
                }

                sprite.LayerSetAnimationTime(isInsertingLayer, 0f);
                sprite.LayerSetVisible(isInsertingLayer, isInserting);
            }
        }
    }
}
public enum LatheVisualLayers : byte
{
    IsRunning,
    IsInserting
}
