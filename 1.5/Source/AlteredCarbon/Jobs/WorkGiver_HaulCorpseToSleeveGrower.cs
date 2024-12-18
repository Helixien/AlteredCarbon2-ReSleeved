﻿using Verse;
using Verse.AI;
using RimWorld;
using System.Collections.Generic;

namespace AlteredCarbon
{
    [HotSwappable]
    public class WorkGiver_HaulCorpseToSleeveGrower : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (var thing in pawn.Map.listerThings.ThingsOfDef(AC_DefOf.AC_SleeveGestator))
            {
                yield return thing;
            }
        }
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var incubator = t as Building_SleeveGestator;
            if (incubator.corpseToRepurpose is null)
            {
                return false;
            }
            if (incubator.corpseToRepurpose.IsForbidden(pawn) || t.IsForbidden(pawn))
            {
                return false;
            }
            else if (pawn.CanReserveAndReach(incubator.corpseToRepurpose, PathEndMode.ClosestTouch, Danger.Deadly) is false)
            {
                return false;
            }
            else if (incubator.incubatorState != IncubatorState.ToBeActivated)
            {
                return false;
            }
            if (!pawn.CanReserveAndReach(t, PathEndMode, Danger.Deadly))
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(AC_DefOf.AC_HaulCorpseToGestation, t, (t as Building_SleeveGestator).corpseToRepurpose);
        }
    }
}


