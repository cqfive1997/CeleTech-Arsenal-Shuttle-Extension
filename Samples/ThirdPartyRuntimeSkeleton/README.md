# Third-Party Runtime Skeleton Sample

This sample is a compact SDK v1 candidate reference for a third-party shuttle module.
It uses only public APIs from the main mod assembly.

## Files

- `AlienScannerProfileContributor.cs`
  - Registers `my.cool.mod/alien-scanner-profile`.
  - Implements `IShuttleProfileContributor`.
  - Adds a small static range bonus through `context.Contributions`.
  - Reads module data through `context.ModuleView`.

- `AlienScannerRuntimeSystem.cs`
  - Registers `my.cool.mod/alien-scanner-runtime`.
  - Passes runtime label and description translation keys for the Declared
    Runtimes UI.
  - Implements map-side runtime hooks, launch hooks, lifecycle hooks, state migration,
    power demand, and stored-energy consumption.

- `AlienScannerPanelProvider.cs`
  - Registers `my.cool.mod/alien-scanner-panel`.
  - Draws a read-only right-side panel inside the External Modules page.
  - Declares a host-owned command button through `CollectCommands`.

- `SetScanModeCommandHandler.cs`
  - Registers `my.cool.mod/set-scan-mode`.
  - Handles a `ShuttleExternalCommand` and writes the external key-value state store.

- `Defs_AlienScannerModule.xml`
  - Declares `runtimeSystemKey`.
  - Declares `contributorKey`.
  - Does not instantiate C# runtime classes from XML.

## Load Order And References

Your mod should load after the main shuttle mod:

```xml
<loadAfter>
  <li>LongRange.CeleTech.ShuttleExtension</li>
</loadAfter>
```

Compile your mod against:

```text
Assemblies/CeleTech_Shuttle.dll
```

Use public SDK namespaces only:

```csharp
using CeleTech.ShuttleExtension.ModularShuttle.API.Profile;
using CeleTech.ShuttleExtension.ModularShuttle.API.Runtime;
using CeleTech.ShuttleExtension.ModularShuttle.API.Commands;
using CeleTech.ShuttleExtension.ModularShuttle.API.UI;
using CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions;
```

Do not reference internal runtime interfaces, controller classes, runtime state classes, or
UI host internals.

## Key Rules

All extension keys use the same namespaced format:

```text
ownerPackageId/local-key
```

This sample registers:

```text
profile contributor: my.cool.mod/alien-scanner-profile
runtime system:      my.cool.mod/alien-scanner-runtime
panel provider:      my.cool.mod/alien-scanner-panel
command handler:     my.cool.mod/set-scan-mode
```

XML must bind the full keys exactly:

```xml
<li Class="CeleTech.ShuttleExtension.ModularShuttle.Extensions.ShuttleRuntimeSystemDefExtension">
  <runtimeSystemKey>my.cool.mod/alien-scanner-runtime</runtimeSystemKey>
</li>
<li Class="CeleTech.ShuttleExtension.ModularShuttle.Extensions.ShuttleProfileContributorDefExtension">
  <contributorKey>my.cool.mod/alien-scanner-profile</contributorKey>
</li>
```

Keys are trimmed but not lower-cased. Duplicate registration is first-wins.

## Registration Order

Register from `[StaticConstructorOnStartup]`. XML only contains keys; the main mod resolves
those keys after your static constructors have registered the implementations.

The profile contributor, runtime system, panel provider, and command handler may be
registered from one bootstrap class or separate bootstrap classes. They must use the same
owner package id when they operate on the same runtime state.

## Runtime State

External runtime state is saved by the main mod as a key-value envelope. Your mod should
store simple values through:

```csharp
context.State.SetInt("scanCount", value);
context.State.SetString("scanMode", mode);
```

Do not save your own runtime state class with `Scribe_Deep`. This keeps saves loadable when
your mod is temporarily disabled.

Map-side `ShuttleExternalRuntimeContext.State` is writable. Power-demand and launch contexts
expose only `IShuttleExternalRuntimeStateReader`.

## Occupant And Host Reads

Map-side runtime contexts also expose optional read-only host and occupant DTOs:

```csharp
if (context.SupportsHostRead)
{
    ShuttleExternalHostInfo hostInfo;
    context.TryGetHostInfo(out hostInfo);
}

IReadOnlyList<ShuttleExternalOccupantInfo> occupants =
    context.GetOccupants(new ShuttleExternalOccupantQuery
    {
        HumanlikeOnly = true,
        ServiceableOnly = true
    });
```

These APIs return DTO copies with read-only `Pawn` references for service
integration. They do not expose holders, controllers, cargo backends, runtime
buckets, or write access to main-mod occupant state.

During the current development-stage API, `ShuttleExternalOccupantInfo.Pawn` and
`ShuttleExternalHostInfo.ShuttleHost` / `Map` are exposed for integrations that
must call their own map-side service logic. Treat these as read-only integration
references: do not move, unload, destroy, re-parent, or otherwise mutate shuttle
occupant containment. Use the role flags and `CanReceiveExternalService` when
choosing pawns for external service work.

## Runtime Hooks

Map-side hooks:

- `Initialize`
- `Migrate`
- `Reconcile`
- `CollectPowerDemand`
- `Tick`
- `CanRemove`
- `OnInstalled`
- `OnRemoved`
- `OnArrived`

Launch-side hooks:

- `PreLaunchValidate`
- `OnLaunchSucceeded`

Launch contexts are read-only. If you need to change state before launch, do it during
`Initialize`, `Reconcile`, `Tick`, or a command handler.

## UI Panel And Commands

Panel providers draw only inside the External Modules page right-side host. They must not:

- open independent RimWorld windows,
- write runtime state,
- mutate command state directly.

To expose a button, implement `CollectCommands` and add a
`ShuttleExternalPanelCommandContribution`. The main mod draws the button and executes
`ShuttleExternalCommand`. The registered `IShuttleExternalCommandHandler` receives writable
state.

If the host supplies `context.CommandExecutor`, a provider that draws its own compact
control may pass the same `ShuttleExternalPanelCommandContribution` to that executor. This
still uses the main mod command boundary, owner/runtime validation, and registered command
handler. Treat a missing executor as "host-rendered commands only" and fall back to
`CollectCommands`.

The first SDK version forbids cross-owner panel and command operation. A provider registered
as `my.cool.mod/...` can target `my.cool.mod/...` runtime and command keys, not another
mod's keys.

## Metrics

The main mod measures external runtime cost. Third-party runtimes do not report their own
timings. The External Modules page and main-page EXT tooltip may show:

- latest external power demand watts,
- tick rolling average ms,
- tick lifetime peak ms,
- cumulative stored-energy consumption.

DevMode may show reconcile and power-demand pass timings.
