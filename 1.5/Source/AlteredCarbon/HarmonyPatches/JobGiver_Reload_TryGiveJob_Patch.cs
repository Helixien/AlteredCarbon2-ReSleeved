using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using VFECore;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(JobGiver_Reload), "TryGiveJob")]
    public static class JobGiver_Reload_TryGiveJob_Patch
    {
        public static void Postfix(ref Job __result, Pawn pawn)
        {
            if (__result is null && pawn.Wears(AC_DefOf.AC_CuirassierBelt, out var apparel))
            {
                var comp = apparel.GetComp<CompShieldBubble>();
                if (comp.Energy < comp.EnergyMax / 2f)
                {
                    var nearbyPowerBuilding = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, 
                    TraverseParms.For(pawn, Danger.Deadly),
                    validator: (Thing x) => FloatMenuMakerMap_AddHumanlikeOrders_Patch.CanChargeAt(pawn, x) &&
                        JobDriver_ChargeCuirassierBelt.CanDoWork(pawn, apparel, x
                        as Building, JobDriver_ChargeCuirassierBelt.MakePowerComp(apparel)));
                    if (nearbyPowerBuilding != null)
                    {
                        JobDef jobDef = AC_DefOf.AC_ChargeCuirassierBelt;
                        Job job = JobMaker.MakeJob(jobDef, nearbyPowerBuilding, apparel);
                        pawn.jobs.TryTakeOrderedJob(job, 0);
                    }
                }
            }
        }
    }
}