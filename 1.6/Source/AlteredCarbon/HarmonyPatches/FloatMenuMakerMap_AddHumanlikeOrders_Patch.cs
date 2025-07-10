using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_CapturePawn), "GetSingleOptionFor")]
    public static class FloatMenuOptionProvider_CapturePawn_GetSingleOptionFor_Patch
    {
        public static void Postfix(Pawn clickedPawn, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (clickedPawn.IsEmptySleeve())
            {
                __result = null;
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuOptionProvider_RescuePawn), "GetSingleOptionFor")]
    public static class FloatMenuOptionProvider_RescuePawn_GetSingleOptionFor_Patch
    {
        public static void Postfix(Pawn clickedPawn, FloatMenuContext context, ref FloatMenuOption __result)
        {
            if (clickedPawn.IsEmptySleeve())
            {
                __result.Label = "AC.TakeToSleeveCasketOrMedicalBed".Translate();
                __result.action = delegate
                {
                    AC_Utils.AddTakeEmptySleeveJob(context.FirstSelectedPawn, clickedPawn, true);
                };
            }
        }
    }

    public class FloatMenuOptionProvider_ChargeCuirassierBelt : FloatMenuOptionProvider
    {
        public override bool Drafted => true;

        public override bool Undrafted => true;

        public override bool Multiselect => false;

        public override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            var pawn = context.FirstSelectedPawn;
            if (pawn.Wears(AC_DefOf.AC_CuirassierBelt, out var apparel))
            {
                if (CanChargeAt(pawn, clickedThing))
                {
                    if (JobDriver_ChargeCuirassierBelt.CanDoWork(pawn, apparel, clickedThing
                        as Building, JobDriver_ChargeCuirassierBelt.MakePowerComp(apparel)))
                    {
                        JobDef jobDef = AC_DefOf.AC_ChargeCuirassierBelt;
                        Action action = delegate ()
                        {
                            Job job = JobMaker.MakeJob(jobDef, clickedThing, apparel);
                            pawn.jobs.TryTakeOrderedJob(job, 0);
                        };
                        string text = TranslatorFormattedStringExtensions.Translate("AC.ChargeCuirassierBelt",
                            clickedThing.LabelCap, clickedThing);
                        FloatMenuOption opt = new FloatMenuOption
                            (text, action, MenuOptionPriority.RescueOrCapture, null, clickedThing, 0f, null, null);
                        return opt;
                    }
                }
            }
            return null;
        }

        public static bool CanChargeAt(Pawn pawn, TargetInfo targ)
        {
            if (!targ.HasThing || targ.Thing.Faction != pawn.Faction)
            {
                return false;
            }
            if (targ.Thing.def == ThingDefOf.HiddenConduit || targ.Thing.def == AC_DefOf.WaterproofConduit)
            {
                return false;
            }
            if (targ.Thing is Building_MechCharger)
            {
                return true;
            }
            return targ.Thing.TryGetComp<CompPower>() is CompPowerTransmitter or CompPowerBattery;
        }
    }

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
