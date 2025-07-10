﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(CompAffectedByFacilities), "CanLinkTo")]
    public static class CompAffectedByFacilities_CanLinkTo_Patch
    {
        public static bool Prefix(CompAffectedByFacilities __instance, Thing facility, ref bool __result)
        {
            if (__instance.parent.def == AC_DefOf.AC_NeuralEditor)
            {
                return CheckForFacilities(facility, ref __result, AC_DefOf.AC_NeuralEditor, 1);
            }
            return true;
        }

        private static bool CheckForFacilities(Thing facility, ref bool __result, ThingDef thingDef, int maxCount)
        {
            int count = facility.TryGetComp<CompFacility>().LinkedBuildings
                .Count(linkedFacility => linkedFacility.def == thingDef);
            if (count >= maxCount)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
