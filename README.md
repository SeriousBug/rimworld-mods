# Connor's RimWorld mods

Mods for [RimWorld](https://rimworldgame.com/) 1.6. Each one is a small, self-contained quality-of-life
mod that fixes something the base game makes tedious. They can be used together or on their own.

All of them need [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077).

## The mods

### Connor's Sidearms!

Pawns can carry spare weapons in their inventory and swap between them.

Right-click a weapon on the ground to have a pawn pick it up as a sidearm. Draft the pawn and a button
appears for each weapon they are carrying; click it to swap. A pawn who is shooting and gets rushed will
automatically draw a melee weapon, then put it away once the enemy backs off.

Raiders and other non-player pawns play by the same rules, and a raider carrying a gun also spawns with a
knife, because the game will not let them shoot while someone is punching them. All of that is
configurable in the mod settings, including how many sidearms a pawn may carry and how much of their
carry capacity the weapons are allowed to take up.

Sidearms sit in the pawn's normal inventory: they have weight, they show in the gear tab, and they drop
when the pawn dies.

### Connor's Loadouts!

Swap a colonist into their combat gear with one button, and back out of it with another.

Select colonists and press **Gear up**. They stop what they are doing, take off what they are wearing,
and put on the best armour their combat apparel policy allows. Press **Stand down** and they change back
into the clothes they had on before. Each pawn's combat policy is picked by right-clicking the button.

Clothes taken off are stashed in the pawn's own inventory by default, so nobody can haul them away before
the pawn wants them back. Apparel you have force-equipped is never touched.

## Installing

Neither mod is on the Steam Workshop yet. To install by hand, copy the mod's folder from `mods/` into
your RimWorld `Mods/` directory, then enable it in the game's mod list.

The published copy of the folder contains the compiled `Assemblies/` directory, which is not checked into
this repository. Build it first (see below) or grab a release when one exists.

## Building from source

You need the [.NET SDK](https://dotnet.microsoft.com/download) and a copy of RimWorld 1.6 installed.

```sh
dotnet build mods/Sidearms/Source/Sidearms.csproj
./scripts/sync-mod.sh Sidearms
```

The build finds your game install and compiles the mod's DLL into `mods/<ModName>/Assemblies/`.
`sync-mod.sh` copies the whole mod folder into RimWorld's `Mods/` directory. Launch the game, enable the
mod, and it is live. `sync-mod.sh <ModName> --watch` re-syncs whenever a file changes.

The repository layout:

```
Directory.Build.props     shared build settings; locates the game install
scripts/                  build, sync, and log helpers
mods/<ModName>/           one folder per mod; this is what is copied into the game
```

## Contributing

Issues and pull requests are welcome. If you are reporting a bug, the game log is what makes it fixable:
on macOS it lives at `~/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log`, and on Windows
at `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log`.

## License

MIT. See [LICENSE](LICENSE).
