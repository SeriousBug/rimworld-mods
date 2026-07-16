using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace LocalizedCleanliness;

/// <summary>
/// A localized version of the vanilla room <c>Cleanliness</c> stat. Instead of a flat average over
/// every cell in a room, it is a distance-weighted average over the cells within a short radius of a
/// point, clamped to that point's room so walls block it. Same inputs (terrain plus filth/items) and
/// same scale as vanilla cleanliness, so the result feeds straight into the vanilla room-stat curves.
/// </summary>
public static class LocalCleanliness
{
    public static float At(Map map, IntVec3 center, float radius, float falloffPower)
    {
        var centerRoom = center.GetRoom(map);
        var num = 0f;
        var den = 0f;

        foreach (var cell in GenRadial.RadialCellsAround(center, radius, useCenter: true))
        {
            if (!cell.InBounds(map) || cell.GetRoom(map) != centerRoom)
            {
                continue;
            }

            var t = 1f - center.DistanceTo(cell) / radius;
            if (t <= 0f)
            {
                continue;
            }
            var weight = falloffPower == 1f ? t : Mathf.Pow(t, falloffPower);

            num += weight * CellCleanliness(map, cell);
            den += weight;
        }

        if (den <= 0f)
        {
            return centerRoom?.GetStat(RoomStatDefOf.Cleanliness) ?? RoomStatDefOf.Cleanliness.roomlessScore;
        }
        return num / den;
    }

    private static float CellCleanliness(Map map, IntVec3 cell)
    {
        var score = cell.GetTerrain(map).GetStatValueAbstract(StatDefOf.Cleanliness);

        List<Thing> things = cell.GetThingList(map);
        for (var i = 0; i < things.Count; i++)
        {
            var thing = things[i];
            // A multi-cell thing appears in every cell it occupies; count it once, at its root cell.
            if (thing.Position != cell)
            {
                continue;
            }
            var category = thing.def.category;
            if (category == ThingCategory.Building || category == ThingCategory.Item
                || category == ThingCategory.Filth || category == ThingCategory.Plant)
            {
                score += thing.stackCount * thing.GetStatValue(StatDefOf.Cleanliness);
            }
        }
        return score;
    }
}
