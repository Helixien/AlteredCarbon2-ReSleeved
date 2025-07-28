using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace AlteredCarbon
{
    public class FloatMenuOptionProvider_ExtractStack : FloatMenuOptionProvider
    {
        public override bool Drafted => true;

        public override bool Undrafted => true;

        public override bool Multiselect => false;

        public override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            var pawn = context.FirstSelectedPawn;
            if (clickedThing is Corpse corpse && corpse.InnerPawn.HasNeuralStack(out var stack))
            {
                JobDef jobDef = AC_DefOf.AC_ExtractStack;
                Action action = delegate ()
                {
                    Job job = JobMaker.MakeJob(jobDef, corpse);
                    pawn.jobs.TryTakeOrderedJob(job, 0);
                };
                string text = "AC.ExtractStack".Translate(corpse.LabelCap, corpse);
                FloatMenuOption opt = new FloatMenuOption
                    (text, action, MenuOptionPriority.RescueOrCapture, null, corpse, 0f, null, null);
                return opt;
            }
            return null;
        }
    }
}
