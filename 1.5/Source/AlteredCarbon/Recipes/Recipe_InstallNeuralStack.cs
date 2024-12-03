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

                if (!pawn.IsEmptySleeve())
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
                if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer && !pawn.Faction.HostileTo(Faction.OfPlayer))
                {
                    ApplyInstalledOwnStackRelationshipChanges(pawn);
                }
            }
            else
            {
                pawn.health.AddHediff(hediff, part);
                if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer
                    && pawn.Faction.HostileTo(Faction.OfPlayer) is false)
                {
                    TryAffectGoodwillWith(pawn.Faction, Faction.OfPlayer, 5, canSendMessage: true, false, AC_DefOf.AC_InstalledEmptyStackEvent, pawn);
                }
            }

            if (ModsConfig.IdeologyActive)
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(AC_DefOf.AC_InstalledNeuralStack, pawn.Named(HistoryEventArgsNames.Doer)));
            }
            hediff.savedStyle = neuralStack.StyleDef;
        }

        public static bool TryAffectGoodwillWith(Faction inst, Faction other, int goodwillChange, bool canSendMessage = true, bool canSendHostilityLetter = true, HistoryEventDef reason = null, GlobalTargetInfo? lookTarget = null)
        {
            if (!inst.CanChangeGoodwillFor(other, goodwillChange))
            {
                return false;
            }
            if (goodwillChange == 0)
            {
                return true;
            }
            int num = inst.GoodwillWith(other);
            int num2 = inst.BaseGoodwillWith(other);
            int num3 = Mathf.Clamp(num2 + goodwillChange, -100, 100);
            if (num2 == num3)
            {
                return true;
            }
            if (reason != null && (inst.IsPlayer || other.IsPlayer))
            {
                Faction arg = (inst.IsPlayer ? other : inst);
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(reason, arg.Named(HistoryEventArgsNames.AffectedFaction), goodwillChange.Named(HistoryEventArgsNames.CustomGoodwill)));
            }
            FactionRelation factionRelation = inst.RelationWith(other);
            factionRelation.baseGoodwill = num3;
            factionRelation.CheckKindThresholds(inst, canSendHostilityLetter, reason?.LabelCap ?? ((TaggedString)null), lookTarget ?? GlobalTargetInfo.Invalid, out var sentLetter);
            FactionRelation factionRelation2 = other.RelationWith(inst);
            FactionRelationKind kind = factionRelation2.kind;
            factionRelation2.baseGoodwill = factionRelation.baseGoodwill;
            factionRelation2.kind = factionRelation.kind;
            bool sentLetter2;
            if (kind != factionRelation2.kind)
            {
                other.Notify_RelationKindChanged(inst, kind, canSendHostilityLetter, reason?.LabelCap ?? ((TaggedString)null), lookTarget ?? GlobalTargetInfo.Invalid, out sentLetter2);
            }
            else
            {
                sentLetter2 = false;
            }
            int num4 = inst.GoodwillWith(other);
            if (canSendMessage && num != num4 && !sentLetter && !sentLetter2 && Current.ProgramState == ProgramState.Playing && (inst.IsPlayer || other.IsPlayer))
            {
                Faction faction = (inst.IsPlayer ? other : inst);
                string text = ((reason == null) ? ((string)"MessageGoodwillChanged".Translate(faction.name, num.ToString("F0"), num4.ToString("F0"))) : ((string)"MessageGoodwillChangedWithReason".Translate(faction.name, num.ToString("F0"), num4.ToString("F0"), reason.label)));
                Messages.Message(text, lookTarget ?? GlobalTargetInfo.Invalid, ((float)goodwillChange > 0f) ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent);
            }
            return true;
        }

        private static void ApplyInstalledOwnStackRelationshipChanges(Pawn pawn)
        {
            int relationshipChange = 2;
            Dictionary<GeneDef, int> geneOffsets = new()
                    {
                        { AC_DefOf.AC_SleeveQuality_Awful, -10 },
                        { AC_DefOf.AC_SleeveQuality_Poor, -5 },
                        { AC_DefOf.AC_SleeveQuality_Normal, 0 },
                        { AC_DefOf.AC_SleeveQuality_Good, 2 },
                        { AC_DefOf.AC_SleeveQuality_Excellent, 5 },
                        { AC_DefOf.AC_SleeveQuality_Masterwork, 7 },
                        { AC_DefOf.AC_SleeveQuality_Legendary, 10 }
                    };

            Dictionary<FloatRange, int> ageOffsets = new()
                    {
                        { new FloatRange(18, 20), 5 },
                        { new FloatRange(20, 30), 4 },
                        { new FloatRange(30, 40), 3 },
                        { new FloatRange(40, 50), 2 },
                        { new FloatRange(60, 70), -2 },
                        { new FloatRange(70, 80), -4 },
                        { new FloatRange(80, float.MaxValue), -5 }
                    };
            if (pawn.genes != null)
            {
                GeneDef matchingGene = geneOffsets.Keys.FirstOrDefault(gene => pawn.genes.HasActiveGene(gene));
                if (matchingGene != null)
                {
                    relationshipChange += geneOffsets[matchingGene];
                }
            }

            float age = pawn.ageTracker.AgeBiologicalYearsFloat;
            FloatRange matchingRange = ageOffsets.Keys.FirstOrDefault(range => range.Includes(age));
            if (matchingRange != null)
            {
                relationshipChange += ageOffsets[matchingRange];
            }
            var eventDef = relationshipChange >= 0 ? AC_DefOf.AC_InstalledNeuralStackEvent : AC_DefOf.AC_InstalledNeuralStackLowQualitySleeveEvent;
            TryAffectGoodwillWith(pawn.Faction, Faction.OfPlayer, relationshipChange, true, false, eventDef, pawn);
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