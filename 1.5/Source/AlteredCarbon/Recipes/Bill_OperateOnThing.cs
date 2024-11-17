using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AlteredCarbon
{
    public class Bill_OperateOnThing : Bill_Production
    {
        public Thing targetThing;
        public Bill_OperateOnThing()
        {

        }
        public Bill_OperateOnThing(Thing thing, RecipeDef recipe, Precept_ThingStyle precept = null)
            : base(recipe, precept)
        {
            this.targetThing = thing;
            targetThing.SetForbidden(false, warnOnFail: false);
        }

        public override string Label => this is not Bill_OperateOnStack 
            ?  base.Label + " (" + (targetThing?.LabelCap ?? "Destroyed".Translate()) + ")" : base.Label;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref targetThing, "targetThing");
        }

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            base.Notify_IterationCompleted(billDoer, ingredients);
            this.billStack.Bills.Remove(this);
        }

        public override Bill Clone()
        {
            var obj = base.Clone() as Bill_OperateOnThing;
            obj.targetThing = targetThing;
            return obj;
        }
    }
}

