using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(WorkGiver_FeedPatient), nameof(WorkGiver_FeedPatient.HasJobOnThing))]
    public static class WorkGiver_FeedPatient_HasJobOnThing_Patch
    {
        public static void Postfix(Pawn pawn, Thing t)
        {
            if (t is Pawn patient && patient.IsEmptySleeve())
            {
                Log.Message("WorkGiver_FeedPatient Postfix for empty sleeve: " + patient.Label);
                Log.Message("IsHungry: " + FeedPatientUtility.IsHungry(patient));
                Log.Message("ShouldBeFed: " + FeedPatientUtility.ShouldBeFed(patient));
                Log.Message("WardenFeedUtility.ShouldBeFed: " + WardenFeedUtility.ShouldBeFed(patient));
                Log.Message("CanReserve: " + pawn.CanReserve(t));
                bool foundFood = FoodUtility.TryFindBestFoodSourceFor(pawn, patient, patient.needs.food.CurCategory == HungerCategory.Starving, out var foodSource, out var foodDef, canRefillDispenser: false, canUseInventory: true, canUsePackAnimalInventory: true, allowForbidden: false, allowCorpse: true, allowSociallyImproper: false, allowHarvest: false, forceScanWholeMap: false, ignoreReservations: false, calculateWantedStackCount: false, allowVenerated: true);
                Log.Message("TryFindBestFoodSourceFor: " + foundFood + " - " + foodSource?.Label + " - " + foodDef?.LabelCap.ToString());
            }
            else
            {
                Log.Message("WorkGiver_FeedPatient Postfix for non-empty sleeve: " + t);
            }
        }
    }
}
