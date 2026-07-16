[h1]Connor's Localized Cleanliness![/h1]

Food poisoning, surgery success, and post-tend infection depend on how clean it is right where the work happens, not on the average over the whole room. A speck of dirt in the far corner of your hospital no longer raises the infection chance for a surgery across the room.

[h2]The problem it fixes[/h2]

Vanilla averages cleanliness over the entire room. That has two consequences:

[list]
[*][b]Room size matters more than dirt.[/b] Small kitchens are affected by small amounts of dirt, but even lots of dirt barely moves the average in a big one. This means a reasonably sized kitchen is penalized for no logical reason.
[*][b]Outdoors is not measured at all.[/b] A room-less tile has no room to average, so cleanliness stops counting.
[/list]

Because this mod measures only the area around the work spot, room size stops mattering and the calculation works outdoors too.

[h2]How it measures[/h2]

Cleanliness is sampled in a short radius around the exact spot the work happens. It weighs nearby tiles most and ignores tiles beyond the radius or behind a wall. It reuses vanilla's own cleanliness values, so a given amount of dirt poisons and infects at the same rate it always did; only [i]which[/i] dirt counts has changed.

It applies in three places, each toggleable:

[list]
[*][b]Cooking.[/b] Food poisoning rolls against the cleanliness around the cook.
[*][b]Surgery.[/b] The operating bed's success chance uses the cleanliness around the bed.
[*][b]Tending.[/b] Post-tend infection chance uses the cleanliness around the patient.
[/list]

[h2]Mod settings[/h2]

[list]
[*][b]Apply to cooking, surgery, and tending[/b] (all on by default). If you want this mod to only apply to one kind of cleanliness check, you can configure that.
[*][b]Radius[/b] (default 4 tiles). How far from the work spot cleanliness is sampled.
[*][b]Falloff strength[/b] (default 1). How heavily closer dirt or cleanliness outweighs farther tiles. 0 weights every tile in range equally; 1 is a straight linear falloff to the edge; higher values concentrate the weight on the tiles closest to the work spot.
[*][b]Remove cook skill from food poisoning[/b] (off). Separate from the above. Vanilla also rolls food poisoning against the cook's Cooking skill, so a low-skill cook poisons meals even in a spotless kitchen. Turn this on if you want only room cleanliness to affect food poisoning risk.
[/list]

[h2]Compatibility[/h2]

Requires Harmony. Safe to add to or remove from an existing save. It reuses the vanilla cleanliness stats and curves rather than replacing them, so it sits alongside mods that add filth, terrain, or medical content.

If you find a mod it conflicts with, say so in the comments and attach your Player.log.

[h2]Source[/h2]

Open source, MIT licensed. The code lives at [url=https://github.com/SeriousBug/rimworld-mods]github.com/SeriousBug/rimworld-mods[/url], along with my other RimWorld mods. Bug reports and pull requests are welcome; if you are reporting a bug, attach your Player.log.
