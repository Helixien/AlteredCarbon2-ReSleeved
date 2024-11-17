using HarmonyLib;
using RimWorld;
using System;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "AddHediff", new Type[]
    {
        typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult)
    })]
    public static class Pawn_HealthTracker_AddHediff_Patch
    {
        [HarmonyPriority(int.MaxValue)]
        private static bool Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, ref Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
        {
            return HandleHediff(___pawn, hediff);
        }

        public static bool HandleHediff(Pawn ___pawn, Hediff hediff)
        {
            if (___pawn.HasHediff(AC_DefOf.AC_CryptoStasis))
            {
                if (hediff.def == HediffDefOf.Hypothermia)
                {
                    return false;
                }
                else if (hediff.def == HediffDefOf.Heatstroke)
                {
                    return false;
                }
            }
            if (___pawn.IsEmptySleeve())
            {
                if (hediff.def == HediffDefOf.CatatonicBreakdown || hediff.def == HediffDefOf.Anesthetic)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
