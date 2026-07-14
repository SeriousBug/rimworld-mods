# Development

Notes for working on the mods in this repo. See `README.md` for what the mods do and how to install them.

## Layout

```
Directory.Build.props     net472 + shared build settings; finds the game install
scripts/                  reference tree, sync, log
docs/                     notes
mods/<ModName>/           one folder per mod (this is what syncs to the game)
```

## Setup

Requires `dotnet`, `ripgrep`, and `ilspycmd` (`dotnet tool install -g ilspycmd`).

```sh
./scripts/refresh-ref.sh          # decompile Assembly-CSharp.dll -> ~/rimref
```

That produces `~/rimref/src` (the decompiled C#), `~/rimref/defnames.txt` and
`~/rimref/defparents.txt`. Both `~/rimref` and the game install are read-only; nothing is
written inside the install except `Mods/`.

The SessionStart hook in `.claude/settings.json` runs `scripts/check-ref.sh`, which compares
the decompiled tree's version against `Version.txt` and warns when the game has been patched
out from under it. Re-run `refresh-ref.sh` when it does.

## The verification rules

1. **The decompiled C# is the schema.** Whether a field exists on a Def, whether a class
   exists, the legal values of an enum — `~/rimref/src/**/*.cs` decides. Grep it first.
2. **The vanilla Defs are only examples.** They show what Ludeon happened to use. They never
   show what is *available*, and they say nothing about valid enum values. Grep `Data/` for a
   working precedent only after the C# has confirmed the field exists.

Grepping Defs alone is not verification. A validator built that way once rejected `<tab>` on
`ResearchProjectDef` as unknown; it is a real field, at `ResearchProjectDef.cs:30`.

## Build and test loop

```sh
dotnet build mods/<ModName>/Source/<ModName>.csproj   # -> mods/<ModName>/Assemblies/
./scripts/sync-mod.sh <ModName>                       # -> <app>/Mods/<ModName>
./scripts/sync-mod.sh <ModName> --watch               # re-sync on change (needs fswatch)
```

Then launch RimWorld by hand, enable the mod, and read the log:

```sh
./scripts/tail-log.sh '\[MyMod\]'
```

**A change is not working until `Player.log` says so.** Writing the file is not evidence.

## Rules baked into the build

- Target **net472**. Guides saying .NET 3.5 are for old versions.
- **Never ship `0Harmony.dll` in `Assemblies/`.** Harmony is declared as a mod dependency in
  `About.xml`; `Lib.Harmony.Ref` is reference-only.
- No HugsLib unless a specific HugsLib feature is actually used.
- A Def that references DLC content needs that DLC declared in `About.xml`.

## macOS notes

The app bundle has two `Data` directories. DLLs are in `Contents/Resources/Data/Managed`;
Defs and Textures are in `<app>/Data`. Guides pointing at
`Contents/Resources/Data/Core/Defs` are wrong here.

`~/Library/Logs/Unity/Player.log` exists on this machine but belongs to a different game. The
RimWorld log is `~/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`.

Scripts use BSD tools: `sed -i '' 's/…/…/' file` (the `''` is mandatory).

## mods/BootstrapCheck

A test mod that exercises Defs, Patches, DefInjected and a Harmony-patched assembly, and
reports each one to `Player.log` with a `[BootstrapCheck] PASS`/`FAIL` line. Use it to confirm
the toolchain works before blaming your own mod.
