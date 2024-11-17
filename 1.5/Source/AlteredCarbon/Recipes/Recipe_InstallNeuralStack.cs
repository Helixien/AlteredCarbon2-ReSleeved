using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AlteredCarbon
{
    public class Recipe_InstallActiveNeuralStack : Recipe_InstallNeuralStack
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            return false;
        }
    }

    [HotSwappable]
    public class Recipe_InstallNeuralStack : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            var pawn = thing as Pawn;
            if (AC_Utils.CanImplantStackTo(this.recipe.addsHediff, pawn))
            {
                return base.AvailableOnNow(thing, part);
            }
            return false;
        }

        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            return MedicalRecipesUtility.GetFixedPartsToApplyOn(recipe, pawn, delegate (BodyPartRecord record)
            {
                if (!pawn.health.hediffSet.GetNotMissingParts().Contains(record))
                {
                    return false;
                }
                if (pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(record))
                {
                    return false;
                }
                return (!pawn.health.hediffSet.hediffs.Any((Hediff x) => x.Part == record && (x.def == recipe.addsHediff || !recipe.CompatibleWithHediff(x.def)))) ? true : false;
            });
        }

        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient is not NeuralStack)
            {
                base.ConsumeIngredient(ingredient, recipe, map);
            }
        }

        public static Pawn pawnToInstallStack;
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            var neuralStack = ingredients.OfType<NeuralStack>().FirstOrDefault();
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
                if (pawn.HasNeuralStack(out var stackHediff))
                {
                    var emptyStack = AC_Utils.stacksPairs[stackHediff.SourceStack];
                    var stack = ThingMaker.MakeThing(emptyStack);
                    GenPlace.TryPlaceThing(stack, billDoer.Position, billDoer.Map, ThingPlaceMode.Near);
                    stackHediff.preventKill = true;
                    pawn.health.RemoveHediff(stackHediff);
                }

                ApplyNeuralStack(recipe.addsHediff, pawn, part, neuralStack);
                neuralStack.DestroyNoKill();
            }
        }

        public static void ApplyNeuralStack(HediffDef stackHediff, Pawn pawn, BodyPartRecord part, NeuralStack neuralStack)
        {
            pawnToInstallStack = pawn;
            var hediff = HediffMaker.MakeHediff(stackHediff, pawn) as Hediff_NeuralStack;
            if (neuralStack.NeuralData.ContainsData)
            {
                neuralStack.NeuralData.hostPawn = null;
                var data = hediff.NeuralData = neuralStack.NeuralData;
                if (pawn.IsEmptySleeve() is false)
                {
                    Messages.Message("AC.PawnDiedOfImplantingStack".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.PawnDeath);
                    var copy = new NeuralData();
                    copy.CopyFromPawn(pawn, data.sourceStack);
                    var dummyPawn = copy.DummyPawn;
                    GenSpawn.Spawn(dummyPawn, pawn.Position, pawn.Map);
                    Pawn_HealthTracker_NotifyPlayerOfKilled_Patch.pawnToSkip = dummyPawn;
                    dummyPawn.Kill(null, hediff);
                    dummyPawn.Corpse.DeSpawn();
                }
                else
                {
                    pawn.UndoEmptySleeve();
                }

                AlteredCarbonManager.Instance.StacksIndex.Remove(data.PawnID);
                AlteredCarbonManager.Instance.ReplaceStackWithPawn(neuralStack, pawn);
                data.OverwritePawn(pawn);
                pawn.health.AddHediff(hediff, part);
                ApplyMindEffects(pawn, hediff);
            }
            else
            {
                pawn.health.AddHediff(hediff, part);
            }

            if (ModsConfig.IdeologyActive)
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(AC_DefOf.AC_InstalledNeuralStack, pawn.Named(HistoryEventArgsNames.Doer)));
            }
            hediff.savedStyle = neuralStack.StyleDef;
        }

        public static void ApplyMindEffects(Pawn pawn, Hediff_NeuralStack hediff)
        {
            ApplyStackDegradation(pawn, hediff);

            ApplyThoughts(pawn, hediff.NeuralData);

            if (hediff.NeuralData.diedFromCombat.HasValue && hediff.NeuralData.diedFromCombat.Value)
            {
                hediff.NeuralData.diedFromCombat = null;
            }

            if (pawn.story.traits.HasTrait(AC_DefOf.AC_Sleever) is false)
            {
                var sleeveShock = HediffMaker.MakeHediff(AC_DefOf.AC_SleeveShock, pawn);
                sleeveShock.Severity = Rand.Range(0.2f, 1f);
                pawn.health.AddHediff(sleeveShock);
            }
            pawn.needs.AddOrRemoveNeedsAsAppropriate();
            if (pawn.Faction?.HostileTo(Faction.OfPlayer) ?? false && pawn.IsPrisoner is false)
            {
                pawn.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            }
        }

        public static void ApplyStackDegradation(Pawn pawn, Hediff_NeuralStack hediff)
        {
            if (AC_Utils.editStacksSettings.enableStackDegradation && hediff.NeuralData.stackDegradation > 0)
            {
                var stackDegradationHediff = pawn.GetHediff(AC_DefOf.AC_StackDegradation) as Hediff_StackDegradation;
                if (stackDegradationHediff is null)
                {
                    BodyPartRecord neckRecord = pawn.GetNeck();
                    stackDegradationHediff = HediffMaker.MakeHediff(AC_DefOf.AC_StackDegradation, pawn, neckRecord) as Hediff_StackDegradation;
                    pawn.health.AddHediff(stackDegradationHediff, neckRecord);
                }
                stackDegradationHediff.stackDegradation = hediff.NeuralData.stackDegradation;

                var brainTraumaChance = (hediff.NeuralData.stackDegradation - 0.8f) * 5f;
                if (brainTraumaChance > 0 && Rand.Chance(brainTraumaChance))
                {
                    pawn.health.AddHediff(AC_DefOf.AC_BrainTrauma, pawn.health.hediffSet.GetBrain());
                }
                pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.AC_StackDegradationThought);
            }
        }

        public static void ApplyThoughts(Pawn pawn, NeuralData neuralData)
        {
            bool isAndroid = pawn.IsAndroid();
            var ideo = pawn.Ideo;
            if (ideo is null || ideo.HasPrecept(AC_DefOf.AC_CrossSleeving_DontCare) is false)
            {
                if (pawn.gender != neuralData.OriginalGender)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(isAndroid ? AC_DefOf.AC_WrongShellGender : AC_DefOf.AC_WrongGender);
                }

                if (ModCompatibility.AlienRacesIsActive && neuralData.OriginalRace != null && pawn.kindDef.race != neuralData.OriginalRace)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.AC_WrongRace);
                }
                if (pawn.SleeveMatchesOriginalXenotype(neuralData))
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.AC_WrongXenotype);
                }
            }

            pawn.needs.mood.thoughts.memories.TryGainMemory(isAndroid ? AC_DefOf.AC_NewShell : AC_DefOf.AC_NewSleeve);
            if (ModCompatibility.VanillaRacesExpandedAndroidIsActive)
            {
                if (pawn.story.traits.HasTrait(AC_DefOf.AC_Shellwalker) && isAndroid is false)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(AC_DefOf.AC_WantsShell);
                }
            }
        }
    }
}