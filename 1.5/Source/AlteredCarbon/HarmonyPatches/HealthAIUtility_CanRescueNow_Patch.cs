using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(HealthAIUtility), "CanRescueNow")]
    public static class HealthAIUtility_CanRescueNow_Patch
    {
        public static void Prefix(Pawn rescuer, Pawn patient, out Faction __state)
        {
            __state = patient.factionInt;
            if (patient.IsEmptySleeve())
            {
                patient.factionInt = rescuer.factionInt;
            }
        }

        public static void Postfix(Pawn patient, Faction __state)
        {
            patient.factionInt = __state;
        }
    }
}