using HarmonyLib;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(PawnUtility), "ShouldSendNotificationAbout")]
    public static class PawnUtility_ShouldSendNotificationAbout_Patch
    {
        public static bool Prefix(Pawn p)
        {
            if (p.IsEmptySleeve() || p.IsDummyPawn() || NeuralData.overwriting == p)
            {
                return false;
            }
            return true;
        }
    }
}

