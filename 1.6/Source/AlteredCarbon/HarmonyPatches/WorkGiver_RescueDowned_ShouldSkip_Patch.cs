using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(WorkGiver_RescueDowned), nameof(WorkGiver_RescueDowned.ShouldSkip))]
    public static class WorkGiver_RescueDowned_ShouldSkip_Patch
    {
        public static void Postfix(ref bool __result, Pawn pawn, bool forced)
        {
            if (__result)
            {
                List<Pawn> list = pawn.Map.mapPawns.AllHumanlikeSpawned;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Downed && list[i].IsEmptySleeve() && !list[i].InBed())
                    {
                        __result = false;
                        break;
                    }
                }
            }
        }
    }
}