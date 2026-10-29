using System;
using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace AlteredCarbon
{
    [HotSwappable]
    public class WorkGiver_HaulNeuralStacks : WorkGiver_Scanner
    {
        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerThings.ThingsOfDef(AC_DefOf.AC_ActiveNeuralStack)
                .Concat(pawn.Map.listerThings.ThingsOfDef(AC_DefOf.AC_ActiveArchotechStack))
                .OfType<NeuralStack>()
                .Where(x => x.autoLoad
                    && x.NeuralData.ContainsData
                    && pawn.CanReserveAndReach(x, PathEndMode.Touch, Danger.Deadly));
        }

        private Thing cachedStack;
        private Thing cachedCache;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            cachedStack = t;
            cachedCache = GetNeuralCaches(pawn, t).FirstOrDefault();
            return cachedCache is not null;
        }
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var neuralCache = cachedStack == t && cachedCache is not null ? cachedCache
                : GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, GetNeuralCaches(pawn, t), PathEndMode.Touch, TraverseParms.For(pawn));
            var job = JobMaker.MakeJob(AC_DefOf.AC_HaulThingsToContainer, t, neuralCache);
            job.count = 1;
            return job;
        }

        private static IEnumerable<Thing> GetNeuralCaches(Pawn hauler, Thing stack)
        {
            var storages = hauler.Map.GetAllStackCaches()
                .Where(x => x.IsForbidden(hauler) is false && x.TryGetComp<CompNeuralCache>() is CompNeuralCache comp
                && comp.Accepts(stack) && stack.IsForbidden(hauler) is false 
                && hauler.CanReserveAndReach(x, PathEndMode.Touch, Danger.Deadly));
            return storages;
        }
    }
}


