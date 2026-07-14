# Sidearms — design, gaps, limitations

Scope is deliberately **combat only**: carry spare weapons, swap between them, draw a melee weapon
when an enemy closes, optionally reach for a longer gun when the enemy is far.

Simple Sidearms (PeteTimesSix) already covers this ground and is maintained for 1.6. This mod is not
trying to replace it. It does less on purpose — no work-tool switching, no NPC sidearm generation, no
Combat Extended support — and diverges only where SS has known trouble.

## Where this differs from Simple Sidearms, and why

| | Simple Sidearms | Here |
|---|---|---|
| Melee trigger | reacts to being hit (`Verb_MeleeAttack.TryCastShot` postfix) | enemy **proximity**, checked on a timer |
| Ranged trigger | `Stance_Warmup.StanceTick` postfix, gated on warmup fraction | distance to `mindState.enemyTarget`, checked on a timer |
| Thrash control | none reported; users report "weapon juggling" | swap cooldown + hysteresis |
| Sidearm identity | `ThingDef` + `Stuff` pair | `Thing` reference |

Reacting to a hit means a pawn only draws a knife *after* being clubbed once, and never reacts to an
enemy who is closing but has not landed anything. Proximity catches both. SS's melee switch is
[reported unreliable](https://github.com/PeteTimesSix/SimpleSidearms/issues/37) and the warmup-fraction
gate is the suspected cause.

Hysteresis: entering melee is 2.9 cells, leaving it is 5. A single threshold makes a pawn flip weapons
while an enemy dances on the boundary.

## Known gaps

These are real and known, not oversights.

- **`applyToNonPlayerPawns` is currently inert.** NPC sidearm generation is out of scope, so raiders
  have no spare weapons to switch *to*. The setting is wired and the code runs for them; with an empty
  sidearm list it does nothing. It only bites if another mod puts weapons in enemy inventories. Either
  cut the setting or add generation.
- **A disarmed pawn will not draw a sidearm.** Auto-switch fires on melee proximity and range mismatch,
  never on "you are holding nothing." If a primary is destroyed or dropped mid-fight the pawn stands
  there unarmed with a knife still in the bag.
- **Violence- and shooting-disabled pawns** are only checked in the float menu, not in auto-switch.

## Known limitations

- **Forming a caravan or loading a pod packs sidearms as cargo.** `Pawn_InventoryTracker.FirstUnloadableThing`
  reports them as unloadable. A naive postfix returning `default` is *worse*: if a sidearm sits first in
  the container the pawn then stops unloading everything else too, so real cargo never loads. Fixing this
  needs the postfix to skip sidearms and continue to the next genuinely unloadable item.
- **Bonded and biocoded weapons** fall out of `EquipmentUtility.CanEquip`, so a pawn with a bonded persona
  weapon cannot use sidearms at all. This matches vanilla's restriction rather than working around it.

## The risky part

`SidearmsUtility.ResumeAttackJob` ends the pawn's current job and starts a fresh `AttackMelee` /
`AttackStatic` after a swap, because swapping invalidates the verb the old job was aiming with.

It is called from a `ThingComp` tick. **Vanilla does not mutate jobs from there** — it goes through
JobGivers and think nodes. If this turns out to be re-entrant or to fight the job driver, that is where
it will surface. Simple Sidearms carries an explicit stack-overflow guard around hunting jobs, which is
likely the same hazard; there is no equivalent guard here yet.

Nothing in this mod has been run in-game. It compiles. That is all that is currently established.

## Performance

`CompSidearms` ticks on every humanlike. The work is kept off the hot path three ways:

1. **Cheap gate first.** `AutoSwitch.Evaluate` bails unless the pawn is drafted, has an `enemyTarget`, or
   is in an attack job — and most pawns, most ticks, are none of those.
2. **Melee check is constant time.** `GenRadial.RadialDistinctThingsAround` reads the cells around the
   pawn out of the thing grid. Walking `attackTargetsCache.GetPotentialTargetsFor` instead would cost
   O(enemies) per pawn, worst exactly during a big raid, when the frame budget is already gone.
3. **Staggered.** `sinceLastCheck` is seeded from `thingIDNumber`, so pawns that spawned on the same tick
   (a raid) do not all evaluate on the same tick forever.
