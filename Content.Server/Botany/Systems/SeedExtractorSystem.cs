using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

public sealed class SeedExtractorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly BotanySystem _botanySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedExtractorComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, SeedExtractorComponent component, InteractUsingEvent args)
    {
        if (!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiverComponent) || !powerReceiverComponent.Powered)
            return;

        if (TryComp(args.Used, out ProduceComponent? produce))
        {
            if (!_botanySystem.TryGetSeed(produce, out var seed))
                return;

            _popupSystem.PopupCursor(Loc.GetString("seed-extractor-component-interact-message",("name", args.Used)),
                Filter.Entities(args.User));

            QueueDel(args.Used);

            var random = _random.Next(component.MinSeeds, component.MaxSeeds);
            var coords = Transform(uid).Coordinates;

            if (random > 1)
                seed.Unique = false;

            for (var i = 0; i < random; i++)
            {
                _botanySystem.SpawnSeedPacket(seed, coords);
            }
        }
    }
}
