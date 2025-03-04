﻿using RimWorld;
using Verse;

namespace AlteredCarbon
{
    public class Bill_OperateOnStack : Bill_OperateOnThing
    {
        public Bill_OperateOnStack()
        {

        }
        public Bill_OperateOnStack(Thing thing, RecipeDef recipe, Precept_ThingStyle precept = null)
            : base(thing, recipe, precept)
        {
        }
        public override bool ShouldDoNow()
        {
            if (targetThing is Pawn patient &&
                (patient.ParentHolder is not Building_NeuralConnector connector || connector.PowerOn is false))
            {
                return false;
            }
            return base.ShouldDoNow();
        }

        public override void Notify_PawnDidWork(Pawn p)
        {
            base.Notify_PawnDidWork(p);
            if (targetThing is Pawn patient && patient.ParentHolder is Building_NeuralConnector connector)
            {
                connector.lastBillWorkTicks = Find.TickManager.TicksGame;
            }
        }

        public override string Label => base.Label + " (" + (targetThing.GetNeuralData()?.PawnNameColored ?? "Destroyed".Translate()) + ")";
    }
}

