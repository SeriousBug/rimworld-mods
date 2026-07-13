# Verified findings

Everything here was checked against the install on 2026-07-13, not copied from a guide. Game
**1.6.4871 rev595**. Re-check anything version-sensitive after a patch.

## Install layout (macOS)

| Thing | Path |
|---|---|
| App bundle | `~/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app` |
| Managed DLLs | `<app>/Contents/Resources/Data/Managed/` |
| Vanilla Defs | `<app>/Data/` — **not** `Contents/Resources/Data/` |
| DLC present | Core, Royalty, Ideology, Biotech, Odyssey. **No Anomaly.** |
| Mods | `<app>/Mods/` |
| Player.log | `~/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log` |

## Resolved flags from the bootstrap plan

- **Harmony `packageId` is `brrainz.harmony`.** Read from the subscribed Harmony mod's own
  `About/About.xml` (workshop id 2009463077). Its `modVersion` is 2.4.2.0.
- **`Lib.Harmony.Ref` 2.4.2** is the current release and matches the installed Harmony mod.
  The bootstrap plan guessed 2.4.1.
- **`Microsoft.NETFramework.ReferenceAssemblies` 1.0.3** is current.
- **`<url>` IS a valid `ModMetaData` tag**, contrary to the bootstrap plan, which omitted it
  after failing to find it. It is `ModMetaData.cs:33` (`public string url = ""`), exposed as
  `ModMetaData.Url` at line 493, and Harmony's own About.xml uses it.

## API anchors used by BootstrapCheck

Line numbers are into `~/rimref/src`, valid for this game build only.

| Symbol | Location |
|---|---|
| `ResearchProjectDef.tab` (type `ResearchTabDef`) | `Verse/ResearchProjectDef.cs:30` |
| `Game.FinalizeInit()` | `Verse/Game.cs:707` |
| `DefDatabase<T>.GetNamedSilentFail(string)` | `Verse/DefDatabase.cs:227` |
| `Mod(ModContentPack)` | `Verse/Mod.cs:13` |
| `Log.Message(string)` | `Verse/Log.cs:75` |

`Main` is the only `ResearchTabDef` in Core (`Data/Core/Defs/ResearchProjectDefs/ResearchTabs.xml`).

## Toolchain state

- `dotnet` 10.0.301, `rg` 15.1.0, `ilspycmd` 10.1.0 (`~/.dotnet/tools`, needs to be on PATH).
- The decompile produces 9,213 C# files; the Def index has 11,749 defNames.
- `Directory.Build.props` defaults `RimWorldManaged` to the standard Steam path, so no env var
  is needed on this machine. Override it with an env var or an uncommitted `Local.props` at
  the repo root if the install moves.
