[h1]Connor's Sidearms![/h1]

Pawns carry spare weapons in their inventory and swap to them. A sniper with a knife on their belt is not helpless when a scyther walks up to them.

[h2]What it does[/h2]

[list]
[*][b]Carry spare weapons.[/b] Right-click a weapon on the ground with a pawn selected and pick "Carry {weapon} as sidearm". The pawn walks over, picks it up, and keeps it in their inventory. You can toggle a setting to treat all weapons in a pawn's inventory as weapons. All weapons will get a button to switch to them manually, and auto-switch logic will apply to all weapons.
[*][b]Swap by hand.[/b] Select a colonist and every sidearm they carry shows up as a button on the command bar. Click it to draw that weapon; whatever they were holding goes back into the inventory.
[*][b]Swap automatically.[/b] When an enemy closes to melee range, a pawn holding a gun draws their best melee sidearm, and puts the gun back once the enemy is off them.
[*][b]Raiders do it too.[/b] Enemy pawns play by the same rules, and gunners spawn with a cheap melee weapon so they fight back with a knife instead of the butt of their rifle. Both of these can be turned off.
[/list]

Sidearms live in the pawn's normal inventory. They weigh something, they show up in the gear tab, and they drop on death, same as anything else a pawn is carrying.

[h2]When it auto-switches[/h2]

[b]To melee (on by default).[/b] A hostile pawn coming within about 3 tiles counts as melee range. Your pawn draws the melee sidearm with the highest DPS they can use. Once no enemy is within 5 tiles, they put it away and go back to the weapon they were holding before. A pawn does not flip weapons while an enemy moves along the edge of that range.

For raiders and other non-player pawns the trigger is different, because the engine already refuses to let them fire while a melee attacker is next to them. They draw melee exactly while that block is in effect, and re-draw the gun the moment they are allowed to shoot again.

[b]To longer range (off by default).[/b] Switch on "Reach for a longer-ranged weapon" in mod settings. When your pawn's target is further away than their current weapon can shoot, they swap to the [i]shortest[/i] sidearm that still reaches, keeping the better close-range weapon in reserve for when the fight comes back to them. Melee takes priority: if an enemy is within melee range, the pawn draws melee instead.

A swap costs the pawn a moment of stance delay, so there is a cooldown between automatic swaps to keep a pawn from flip-flopping. Anything you equip by hand sticks; the auto-switch will not undo a choice you made.

[h2]Mod settings[/h2]

[list]
[*]Every carried weapon is a sidearm (default off). Turn on if you want everything a pawn is carrying to be a sidearm, or if you are using an incompatible mod that makes your pawn pick up weapons to use as sidearms.
[*]Maximum sidearms per pawn (default 2)
[*]Share of carry capacity sidearms may use (default 50%)
[*]Draw a melee weapon in melee, and whether to put it away afterwards (both on)
[*]Reach for a longer-ranged weapon (off)
[*]Minimum ticks between automatic swaps (default 120)
[*]Apply to non-player pawns (on). Turn this off to make auto-switching a player-only advantage.
[*]Give raiders a melee sidearm (on), and the most valuable one they may spawn with (default 60 silver, so you get knives and clubs, not free longswords)
[/list]

[h2]Compatibility[/h2]

Requires Harmony. Safe to add to an existing save. Removing it mid-save leaves the weapons in your pawns' inventories; drop them from the gear tab first if you want them back.

[b][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3735900296]Manage Sidearm Policies[/url][/b] is supported: weapons a sidearm policy puts in a colonist's inventory get a command-bar button and are used by the auto-switch, same as ones you picked out by hand.

[b][url=https://steamcommunity.com/sharedfiles/filedetails/?id=2947023388]Grab Your Tool![/url][/b] is supported: a weapon it draws to work with keeps its button and comes back as a sidearm when the job is done.

Should not conflict with other mods that touch equipment, but if you find one that does, say so in the comments.

[h2]Source[/h2]

Open source, MIT licensed. The code lives at [url=https://github.com/SeriousBug/rimworld-mods]github.com/SeriousBug/rimworld-mods[/url], along with my other RimWorld mods. Bug reports and pull requests are welcome; if you are reporting a bug, attach your Player.log.
