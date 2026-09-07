# Third-Party Profile-Only Sample

This sample shows the first supported third-party shuttle SDK surface: static profile
contributors.

## Load Order

Your mod should declare `loadAfter` for the main shuttle mod:

```xml
<loadAfter>
  <li>LongRange.CeleTech.ShuttleExtension</li>
</loadAfter>
```

Compile your assembly against the main mod assembly that exposes:

- `CeleTech.ShuttleExtension.ModularShuttle.API.Profile.ShuttleProfileAPI`
- `CeleTech.ShuttleExtension.ModularShuttle.Profile.Extensions.IShuttleProfileContributor`

## Key Contract

The XML `contributorKey` must exactly match the full key built by registration:

```csharp
ShuttleProfileAPI.RegisterProfileContributor(
    "my.cool.mod",
    "alien-reactor-profile",
    new AlienReactorProfileContributor());
```

Full XML key:

```xml
<contributorKey>my.cool.mod/alien-reactor-profile</contributorKey>
```

Keys are trimmed but not lower-cased. Use the recommended format:

```text
package.id/local-key
```

When an XML `contributorKey` resolves to a registered contributor, that contributor
replaces the built-in profile contributor for that module. If your module still needs
built-in battery, cargo, reactor, or similar behavior, add the same profile contributions
explicitly in your contributor. A future SDK may add an `appendBuiltInContributor` style
option, but this first stage does not.

## Current Limits

This stage only supports static profile contributions during profile rebuild. It does not
support runtime ticks, runtime state, launch lifecycle hooks, commands, UI zones, custom
save data, or controller access.

Use `context.ModuleView` and `context.ParentSegmentView` for read-only installed-module
facts. Do not mutate shuttle assembly state from a profile contributor.
