using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(FeedPatientUtility), "ShouldBeFed")]
    public static class FeedPatientUtility_ShouldBeFed_Patch
    {
        public static void Postfix(ref bool __result, Pawn p)
        {
            if (p.IsEmptySleeve())
            {
                if (__result && p.CurrentBed() is Building_SleeveCasket)
                {
                    __result = false;
                }
                else if (__result is false && HealthAIUtility.ShouldSeekMedicalRest(p) is false)
                {
                    __result = true;
                }
            }

        }
    }
}

