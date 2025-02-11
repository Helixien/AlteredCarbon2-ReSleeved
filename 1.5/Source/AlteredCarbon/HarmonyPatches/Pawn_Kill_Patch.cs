﻿using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace AlteredCarbon
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Pawn_Kill_Patch
    {
        public static Pawn pawnWithStackBeingKilled;
        public static void Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            if (__instance.HasStackInsideOrOutside() || __instance.IsEmptySleeve())
            {
                __instance.DisableKillEffects();
            }
            if (__instance.HasNeuralStack())
            {
                pawnWithStackBeingKilled = __instance;
            }

            if (AlteredCarbonManager.Instance.StacksIndex.TryGetValue(__instance.thingIDNumber, out var neuralStack))
            {
                if (LookTargets_Patch.targets.TryGetValue(__instance, out var targets))
                {
                    foreach (var target in targets)
                    {
                        target.targets.Remove(__instance);
                        target.targets.Add(neuralStack);
                    }
                }
            }
        }

        public static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit = null)
        {
            pawnWithStackBeingKilled = null;
            if (__instance.Dead)
            {
                if (__instance.HasNeuralStack(out var stackHediff))
                {
                    var stackData = stackHediff.NeuralData.StackGroupData;
                    stackData.deadPawns.Add(__instance);

                    var caravan = __instance.GetCaravan();
                    bool isArchotechStack = stackHediff.def == AC_DefOf.AC_ArchotechStack;
                    if (dinfo.HasValue && dinfo.Value.Def == DamageDefOf.Crush
                        && dinfo.Value.Category == DamageInfo.SourceCategory.Collapse)
                    {
                        if (isArchotechStack)
                        {
                            stackHediff.SpawnStack(caravan: caravan);
                        }
                    }
                    else
                    {
                        AlteredCarbonManager.Instance.deadPawns.Add(__instance);
                        if (dinfo.HasValue && dinfo.Value.Def.ExternalViolenceFor(__instance))
                        {
                            stackHediff.NeuralData.diedFromCombat = true;
                        }

                        if (isArchotechStack && caravan is null && __instance.GetNeck() is null)
                        {
                            stackHediff.SpawnStack();
                        }
                    }

                    if (caravan != null)
                    {
                        stackHediff.SpawnStack(caravan: caravan);
                        var head = __instance.health.hediffSet.GetNotMissingParts().FirstOrDefault((BodyPartRecord x) => x.def == BodyPartDefOf.Head);
                        if (head != null)
                        {
                            Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, __instance, head);
                            hediff_MissingPart.lastInjury = HediffDefOf.SurgicalCut;
                            hediff_MissingPart.IsFresh = true;
                            __instance.health.AddHediff(hediff_MissingPart);
                        }
                    }
                }
            }

            __instance.EnableKillEffects();
        }

    }
}

