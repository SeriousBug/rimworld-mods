# RimWorld modding monorepo

One repo, many mods. Each folder under `mods/` is self-contained. See `docs/development.md` for the
build and sync loop, and `docs/local/verified-facts.md` for API facts checked against the decompiled
source. `docs/local/` is not committed.

Folder names under `mods/` are the short names (`Sidearms`, `Loadout`); the player-facing names in
`About.xml` are "Connor's Sidearms!" and "Connor's Loadouts!". Commit tags use the folder name.

## Commit messages

A commit that touches a mod is tagged with that mod's folder name under `mods/`, in square brackets:

```
[Sidearms] carry spare weapons, swap between them, draw melee in melee
[Loadout] deposit into containers, so gear can land on an outfit stand
```

Anything else — docs, scripts, repo plumbing — takes no tag at all:

```
Sidearms design notes, and equipment/inventory facts from the source
tail-log: stop matching Unity's exit-time allocator table
```

Several mods live here at once and are often worked on in parallel, so the tag is what makes
`git log` readable.

**Keep each commit to one mod.** A commit that touches two mods means a `git add -A` swept up someone
else's in-progress work. Stage paths explicitly.

## Verification

A change is not working until `Player.log` says so. Writing the file is not evidence, and neither is a
clean build. State plainly what has and has not been run in-game.
