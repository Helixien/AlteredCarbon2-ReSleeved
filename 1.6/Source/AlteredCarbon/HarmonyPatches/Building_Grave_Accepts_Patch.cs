using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Building_Grave), "Accepts")]
    public static class Building_Grave_Accepts_Patch
    {
        public static void Postfix(ref bool __result, Thing thing)
        {
            if (!__result)
            {
                return;
            }

            if (thing is Corpse corpse && corpse.Map != null && corpse.Map.designationManager.DesignationOn(corpse, AC_DefOf.AC_ExtractStackDesignation) != null)
            {
                __result = false;
            }
        }
    }
}