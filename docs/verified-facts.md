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

## Equipment, inventory and float menus (checked for the Sidearms mod)

Line numbers are into `~/rimref/src`, valid for this game build only.

- **`ThingOwner` fires the equip notifies itself.** `NotifyAdded`/`NotifyRemoved` check whether the
  owner is a `Pawn_EquipmentTracker` and call `Notify_EquipmentAdded`/`Notify_EquipmentRemoved`
  (`Verse/ThingOwner.cs:992` and `:1022`). So moving a weapon between `pawn.inventory.innerContainer`
  and `pawn.equipment.GetDirectlyHeldThings()` sets up the verbs correctly without going through
  `AddEquipment`. `Pawn_EquipmentTracker.equipment` is private, but `GetDirectlyHeldThings()` is public.
- **`MakeRoomFor` drops, it does not stow** (`Verse/Pawn_EquipmentTracker.cs:125`). Not what a swap wants.
- **`Notify_PrimaryWeaponChanged` only sets `fireAtWill = true`** (`RimWorld/Pawn_DraftController.cs:215`)
  and is `internal`. Skipping it on an automatic swap is correct: a swap should not silently override
  the player's hold-fire.
- **`JobGiver_DropUnusedInventory` does NOT drop weapons.** Its loop is gated on
  `ShouldKeepDrugInInventory`, which returns `true` for anything that is not a drug
  (`RimWorld/JobGiver_DropUnusedInventory.cs`). It only drops disallowed drugs and stale raw food.
  Widely-repeated modding advice says you must patch this to stop pawns dumping inventory weapons.
  That is **wrong for 1.6**.
- **`FirstUnloadableThing` is only consumed behind `UnloadEverything`** — caravan forming, transport
  pods, portals, shuttles (`Verse/Pawn_InventoryTracker.cs:58`). It is not a routine "pawn tidies up"
  path.
- **`FloatMenuOptionProvider` subclasses auto-register.** `FloatMenuMakerMap` builds its provider list
  by reflecting over `typeof(FloatMenuOptionProvider).AllSubclassesNonAbstract()`
  (`RimWorld/FloatMenuMakerMap.cs:21`). Adding a right-click option needs no Def and no Harmony patch —
  just subclass it. This replaces the old `AddHumanlikeOrders` patching.
- **A `ThingComp` injected into pawn defs at `[StaticConstructorOnStartup]` works on existing saves.**
  `ThingWithComps.ExposeData` calls `InitializeComps()` on `LoadingVars`
  (`Verse/ThingWithComps.cs:240`), so the comp is constructed and `PostExposeData`'d on load. No
  migration step needed.
- **Pawns tick comps through two different paths in 1.6.** `Pawn.Tick()` (`Verse/Pawn.cs:1538`) reaches
  `CompTick`, while `Pawn.TickInterval(delta)` (`:1618`) reaches `CompTickInterval(delta)`. A comp that
  only implements `CompTick` silently stops running for pawns on the interval path. Implement both.
- **`AttackTargetsCache.GetPotentialTargetsFor` returns a shared list** that it clears on every call
  (`Verse.AI/AttackTargetsCache.cs:53`). Consume it immediately; never hold onto it.

## Toolchain state

- `dotnet` 10.0.301, `rg` 15.1.0, `ilspycmd` 10.1.0 (`~/.dotnet/tools`, needs to be on PATH).
- The decompile produces 9,213 C# files; the Def index has 11,749 defNames.
- `Directory.Build.props` defaults `RimWorldManaged` to the standard Steam path, so no env var
  is needed on this machine. Override it with an env var or an uncommitted `Local.props` at
  the repo root if the install moves.
