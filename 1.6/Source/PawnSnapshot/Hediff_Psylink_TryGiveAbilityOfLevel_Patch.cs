using HarmonyLib;
using Verse;

namespace PawnSnapshot
{
    [StaticConstructorOnStartup]
    public static class PawnSnapshotPatches
    {
        static PawnSnapshotPatches()
        {
            new Harmony("PawnSnapshot.Library").PatchAll(typeof(PawnSnapshotPatches).Assembly);
        }
    }

    [HarmonyPatch(typeof(Hediff_Psylink), nameof(Hediff_Psylink.TryGiveAbilityOfLevel))]
    public static class Hediff_Psylink_TryGiveAbilityOfLevel_Patch
    {
        public static bool suppress;

        public static bool Prefix()
        {
            return !suppress;
        }
    }
}
