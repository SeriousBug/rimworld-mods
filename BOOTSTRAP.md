# RimWorld 1.6 modding monorepo — bootstrap plan

Status: **draft for your review**. Nothing here has been executed. Read it, then tell me to
proceed (or amend). Steps that modify your machine (`brew`, `dotnet tool install`) are called
out explicitly and are your call to run.

Every path, tag, and field below was verified against your actual install on 2026-07-13, not
copied from a guide. Where I could not verify something, it says so.

---

## 0. Verified ground truth (this machine)

| Thing | Value |
|---|---|
| Game build | **1.6.4871 rev595** (`$RIMWORLD/Version.txt`) |
| App bundle | `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app` |
| Game assemblies | `<app>/Contents/Resources/Data/Managed/` (`Assembly-CSharp.dll`, `UnityEngine*.dll`) |
| Vanilla Defs root | `<app>/Data/Core/Defs` — **note: `<app>/Data`, NOT `Contents/Resources/Data`** |
| DLC Defs present | Core, Royalty, Ideology, Biotech, **Odyssey**. **No Anomaly** on this machine. |
| Mods dir | `<app>/Mods` |
| Workshop content | `~/Library/Application Support/Steam/steamapps/workshop/content/294100` |
| Player.log | `~/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log` |

macOS gotchas baked into the plan:
- The app bundle has **two** `Data` directories. Managed **DLLs** live under
  `Contents/Resources/Data/Managed`; game **Defs/Textures** live under `<app>/Data`. The old
  guides' single `Contents/Resources/Data/Core/Defs` path does **not** exist here.
- `~/Library/Logs/Unity/Player.log` exists on this machine but belongs to a **different game**.
  Do not read it. Use the Ludeon path above.
- Any shell script we write uses BSD `sed`: `sed -i '' 's/…/…/' file` (the `''` is mandatory
  on macOS; GNU-style `sed -i 's/…/…/' file` fails here).

### 2a

```sh
brew install ripgrep                 # the Defs tree is large; plain grep is painful
dotnet tool install -g ilspycmd      # cross-platform decompiler (ILSpy GUI is Windows-centric)
```

`dotnet` is already at `/opt/homebrew/bin/dotnet`. `ilspycmd` installs to `~/.dotnet/tools`
(ensure that's on PATH).

### 2b. Decompile our own DLL

```sh
RIMWORLD="$HOME/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app"
DLL="$RIMWORLD/Contents/Resources/Data/Managed/Assembly-CSharp.dll"
DATA="$RIMWORLD/Data"
REF="$HOME/rimref"

mkdir -p "$REF"
ilspycmd -p -o "$REF/src" "$DLL"     # grep-able C# tree; this is the SCHEMA
```

**Trap 1 — version match.** Re-run this decompile after every game patch or it silently goes
stale. Before trusting the tree, confirm it matches the running game:

```sh
cat "$RIMWORLD/Version.txt"          # expect 1.6.4871 rev595 today; re-decompile if it changed
```

Implement this as an automated hook that runs at the start of a session, or something equivalent to that.

### 2c. Def indexes (examples, not schema)

```sh
# defName -> file
rg --no-heading -H -o '<defName>[^<]+</defName>' "$DATA" -g '*.xml' > "$REF/defnames.txt"
# abstract parents (Name="..." is what ParentName points at)
rg --no-heading -H -o 'Name="[^"]+"' "$DATA" -g '*.xml' > "$REF/defparents.txt"
```

Both `$DATA` and `$REF/src` are **read-only**. Nothing writes inside the RimWorld install
except `Mods/`.

### 2d. The verification rules

1. **The decompiled C# is the schema.** Whether `<tab>` is a real field on `ResearchProjectDef`,
   whether `HediffCompProperties_Foo` exists as a class, the legal values of an enum like
   `tickerType` — all decided by `$REF/src/…/*.cs`. Grep the C# first.
2. **The vanilla Defs are only examples.** They show what Ludeon happened to use. They never
   show what is *available*, and they say nothing about valid enum values. Grep Defs for a
   working precedent after the C# confirms the field exists.

> Why this matters, concretely: the reviewed skill's validator hard-errored on `<tab>` in
> `ResearchProjectDef`. But `tab` is field row 4080 of
> `ResearchProjectDef` in your DLL — a real field. The validator's "schema" listed 27 fields;
> the C# class has 38. It had been harvested from Defs (examples), not C# (schema). Grepping
> Defs alone is not verification.

---

## 3. Monorepo layout

One repo, multiple mods. Shared reference and docs at the root; each mod self-contained so it
can be synced to `Mods/` independently.

```
rimworld-modding/                 (this git repo)
├── README.md
├── .gitignore                    (bin/ obj/ *.user .DS_Store)
├── Directory.Build.props         (shared net472 + build settings for all mods' Source/)
├── docs/                         (our own notes; verified findings)
└── mods/
    └── <ModName>/                (one folder per mod; this is what syncs to Mods/)
        ├── About/
        │   ├── About.xml
        │   └── Preview.png        (optional)
        ├── Defs/                  (new defs)
        ├── Patches/               (PatchOperations against vanilla)
        ├── Languages/
        │   └── English/
        │       └── DefInjected/   (folder names must match Def type exactly, e.g. ThingDef)
        ├── Textures/
        ├── Sounds/
        ├── Assemblies/            (build output; empty until milestone 2; NO 0Harmony.dll)
        └── Source/                (C#; only if the mod needs it)
            └── <ModName>.csproj
```

Folder-name rules (enforced by RimWorld, verified against vanilla): `Defs`, `Patches`,
`Languages`, `Textures`, `Sounds`, `Assemblies` — plural where shown. `DefInjected`
subfolders must exactly match the Def type name (`ThingDef`, not `ThingDefs`).

### About.xml template (every tag below verified present in Assembly-CSharp.dll)

Tags confirmed from vanilla `About.xml` files and/or the DLL string heap: `packageId`, `name`,
`author`, `description`, `supportedVersions`, `modDependencies` (+ child `packageId`,
`displayName`, `steamWorkshopUrl`), `loadAfter`. I did **not** find a `url` tag; omitted.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ModMetaData>
  <packageId>yourname.modname</packageId>       <!-- lowercase, globally unique -->
  <name>Mod Name</name>
  <author>yourname</author>
  <description>One or two sentences.</description>
  <supportedVersions>
    <li>1.6</li>
  </supportedVersions>
  <modDependencies>
    <li>
      <packageId>brrainz.harmony</packageId>
      <displayName>Harmony</displayName>
      <steamWorkshopUrl>steam://url/CommunityFilePage/2009463077</steamWorkshopUrl>
    </li>
  </modDependencies>
  <loadAfter>
    <li>brrainz.harmony</li>
  </loadAfter>
</ModMetaData>
```

**Flag:** the Harmony `packageId` `brrainz.harmony` is declared by the Harmony mod itself, not
by the game DLL, so I could not verify it from your install. Confirm it after subscribing to
Harmony by reading that mod's own `About/About.xml` `<packageId>`. 

**Trap 2 — DLC content.** If a Def references DLC content (e.g. an Odyssey-only def), that DLC
must be owned *and* declared as a dependency in `About.xml`. 

---

## 4. Build setup (for milestone 2+; not needed for milestone 1)

Target **net472** (1.6's target; ignore any guide saying .NET 3.5). SDK-style project, no Mono
install needed.

`Directory.Build.props` (repo root):

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>latest</LangVersion>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Per-mod `Source/<ModName>.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- build output goes to the mod's Assemblies/ folder -->
    <OutputPath>..\Assemblies\</OutputPath>
    <EnableDefaultCompileItems>true</EnableDefaultCompileItems>
  </PropertyGroup>

  <!-- Harmony as a REFERENCE only. Do NOT ship 0Harmony.dll; it comes from the Harmony mod. -->
  <ItemGroup>
    <PackageReference Include="Lib.Harmony.Ref" Version="2.4.1" PrivateAssets="all" />
  </ItemGroup>

  <!-- Game assemblies referenced from the local install; never copied to output. -->
  <ItemGroup>
    <Reference Include="Assembly-CSharp">
      <HintPath>$(RimWorldManaged)\Assembly-CSharp.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>$(RimWorldManaged)\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

Set `RimWorldManaged` (env var or a local props file, not committed) to
`<app>/Contents/Resources/Data/Managed`.

Rules baked in per the handoff:
- **Do not ship `0Harmony.dll` in `Assemblies/`.** Harmony is declared as a mod dependency in
  `About.xml` (section 3). `Lib.Harmony.Ref` is reference-only (`PrivateAssets="all"`), so it is
  not copied to output.
- **No HugsLib** unless a specific HugsLib feature is actually used.
- **Flags I could not fully verify:** exact latest versions of `Microsoft.NETFramework.Reference-
  Assemblies` (1.0.3 shown), `Lib.Harmony.Ref` (2.4.1 shown — 2.4 is the release that added
  Apple Silicon support), and whether official Harmony's `packageId` is `brrainz.harmony`.
  Confirm all three before the first C# build.

---

## 5. Test loop

The agent cannot see the game. **A change is not "working" until `Player.log` or you confirm
it. Do not report success from having written the file.**

```sh
# sync one mod into the game (a small script we'll write; uses rsync, excludes Source/ and .git)
mods/<ModName>  ->  <app>/Mods/<ModName>

# then, by hand:
#   1. launch RimWorld, enable the mod, restart if prompted
#   2. read the log:
rg -n 'yourname|modname|<ModName>|Exception|Error' \
   "$HOME/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log"
```

Any sync script we write will guard its target (must be inside `<app>/Mods/` and contain
`About/About.xml` before any delete) and use macOS `rsync`/`sed` syntax.

