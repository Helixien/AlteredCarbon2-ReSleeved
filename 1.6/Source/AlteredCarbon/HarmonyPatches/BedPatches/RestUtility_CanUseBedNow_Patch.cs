using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AlteredCarbon
{
    [HotSwappable]
    [HarmonyPatch(typeof(RestUtility), nameof(RestUtility.CanUseBedNow))]
    public static class RestUtility_CanUseBedNow_Patch
    {
        public static void Prefix(ref bool __result, Thing bedThing, ref GuestStatus? guestStatusOverride, ref bool allowMedBedEvenIfSetToNoCare)
        {
            if (bedThing is Building_SleeveCasket)
            {
                guestStatusOverride = GuestStatus.Guest;
                allowMedBedEvenIfSetToNoCare = true;
            }
        }
    }
}

