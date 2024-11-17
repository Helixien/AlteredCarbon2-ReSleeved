using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    [HotSwappable]
    [HarmonyPatch(typeof(PawnRenderer), "GetBodyPos")]
    public static class PawnRenderer_GetBodyPos_Patch
    {
        public static void Postfix(Pawn ___pawn, ref Vector3 __result)
        {
            if (___pawn.CurrentBed() is Building_SleeveCasket bed)
            {
                __result.y -= 1f;
                if (bed.Rotation == Rot4.North)
                {
                    __result.z += 0.3f;
                }
            }
        }
    }
}