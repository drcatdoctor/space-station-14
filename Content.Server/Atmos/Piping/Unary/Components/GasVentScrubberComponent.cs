using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    [Friend(typeof(GasVentScrubberSystem))]
    public sealed class GasVentScrubberComponent : VentOrScrubberComponent
    {
    }
}
