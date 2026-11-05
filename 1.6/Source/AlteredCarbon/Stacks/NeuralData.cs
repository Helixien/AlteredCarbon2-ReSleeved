using HarmonyLib;
using PawnSnapshot;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AlteredCarbon
{
    [HotSwappable]
    public class NeuralData : PawnData
    {
        public NeuralData neuralDataRewritten;
        public Building_NeuralMatrix trackedToMatrix;
        public ThingDef sourceStack;
        public Pawn hostPawn;

        public Faction Faction
        {
            get
            {
                if (hostPawn != null && faction != hostPawn.Faction)
                {
                    faction = hostPawn.Faction;
                }
                return faction;
            }
            set
            {
                faction = value;
                if (hostPawn != null && hostPawn.Faction != value)
                {
                    hostPawn.SetFaction(value);
                }
            }
        }

        public bool ContainsData => hostPawn != null || name != null;

        public static readonly ConditionalWeakTable<Pawn, StrongBox<ThingWithNeuralData>> dummyPawns = new();

        private int pawnID;

        public bool? diedFromCombat;
        public bool isCopied = false;
        public int stackGroupID = -1;
        public int? lastTimeBackedUp;

        public int editTime;
        public float stackDegradation;
        public float stackDegradationToAdd;
        public Pawn dummyPawn;
        public NeuralData()
        {
            this.stackGroupID = AlteredCarbonManager.Instance.stacksRelationships.Count + 1;
            this.lastTimeBackedUp = GenTicks.TicksAbs;
        }

        public static Pawn lastDummyPawn;

        public Pawn DummyPawn
        {
            get
            {
                if (dummyPawn is null)
                {
                    RefreshDummyPawn();
                }
                lastDummyPawn = dummyPawn;
                return dummyPawn;
            }
        }
        public int PawnID => hostPawn != null ? hostPawn.thingIDNumber : pawnID;
        public ThingWithNeuralData host;

        public void RefreshDummyPawn()
        {
            var oldDebug = AC_Utils.debug;
            AC_Utils.debug = false;
            try
            {
                if (dummyPawn is null)
                {
                    var ticks = (long)Mathf.FloorToInt(18f * 3600000f);
                    if (ageBiologicalTicks != default)
                    {
                        ticks = ageBiologicalTicks;
                    }
                    else if (hostPawn is not null)
                    {
                        ticks = ageBiologicalTicks = hostPawn.ageTracker.AgeBiologicalTicks;
                    }
                    var pawn = AC_Utils.CreateEmptyPawn(hostPawn?.kindDef ?? kindDef ?? PawnKindDefOf.Colonist, Faction, OriginalRace ?? ThingDefOf.Human, ticks, OriginalXenotypeDef ?? XenotypeDefOf.Baseliner, OriginalGender);

                    pawn.gender = OriginalGender;
                    dummyPawn = pawn;
                }
                dummyPawns.Remove(dummyPawn);
                dummyPawns.Add(dummyPawn, new StrongBox<ThingWithNeuralData> { Value = host });

                OverwritePawn(dummyPawn, changeGlobalData: false);
                if (hostPawn is not null)
                {
                    AC_Utils.CopyBody(hostPawn, dummyPawn, copyAgeInfo: true, copyGenesFully: true);
                    dummyPawn.RefreshGraphic();
                }
                DummyPawnCleanup();
                AssignDummyPawnReferences();
            }
            finally
            {
                AC_Utils.debug = oldDebug;
            }
        }

        private void DummyPawnCleanup()
        {
            dummyPawn.genes.GenesListForReading.Where(x => x.def.aptitudes.NullOrEmpty() is false
            || x.def.forcedTraits.NullOrEmpty() is false || x.def.passionMod is not null).ToList()
                .ForEach(x => dummyPawn.genes.RemoveGene(x));
            dummyPawn.story.backstoriesCache = null;
            dummyPawn.Notify_DisabledWorkTypesChanged();
            if (this.skills != null)
            {
                foreach (var skill in this.skills)
                {
                    skill.cachedPermanentlyDisabled = BoolUnknown.Unknown;
                    skill.cachedTotallyDisabled = BoolUnknown.Unknown;
                }
            }
            if (hostPawn != null && (hostPawn.IsEmptySleeve() || hostPawn.Dead))
            {
                dummyPawn.health.healthState = PawnHealthState.Dead;
            }
        }

        public TaggedString PawnNameColored
        {
            get
            {
                var title = TitleShort;
                var pawnName = name?.ToStringShort.Colorize(PawnNameColorUtility.PawnNameColorOf(DummyPawn));
                return title.NullOrEmpty() ? pawnName : pawnName + ", " + title.CapitalizeFirst();
            }
        }

        public StackGroupData StackGroupData
        {
            get
            {
                if (!AlteredCarbonManager.Instance.stacksRelationships.TryGetValue(stackGroupID, out var stackData))
                {
                    AlteredCarbonManager.Instance.stacksRelationships[stackGroupID] = stackData = new StackGroupData();
                }
                return stackData;
            }
        }

        public bool Friendly
        {
            get
            {
                return Faction != null && (Faction != Faction.OfPlayer && Faction.HostileTo(Faction.OfPlayer) is false);
            }
        }
        public bool Colonist => Faction != null && Faction == Faction.OfPlayer;
        public bool Hostile => Faction != null && (Faction != Faction.OfPlayer && Faction.HostileTo(Faction.OfPlayer));

        public void AppendInfoStack(StringBuilder stringBuilder)
        {
            if (this.ContainsData)
            {
                if (this.Faction != null)
                {
                    stringBuilder.AppendLineTagged("AC.Faction".Translate() + ": " + this.Faction.NameColored);
                }
                if (ModCompatibility.AlienRacesIsActive && this.OriginalRace != null)
                {
                    stringBuilder.AppendLineTagged("AC.Race".Translate() + ": " + this.OriginalRace.LabelCap);
                }
                if (this.OriginalXenotypeName != null)
                {
                    stringBuilder.AppendLineTagged("AC.Xenotype".Translate() + ": " + this.OriginalXenotypeName);
                }
                else if (this.OriginalXenotypeDef != null)
                {
                    stringBuilder.AppendLineTagged("AC.Xenotype".Translate() + ": " + this.OriginalXenotypeDef.LabelCap);
                }

                if (this.childhood != null)
                {
                    stringBuilder.Append("AC.Childhood".Translate() + ": " + this.childhood.title.CapitalizeFirst() + "\n");
                }

                if (this.adulthood != null)
                {
                    stringBuilder.Append("AC.Adulthood".Translate() + ": " + this.adulthood.title.CapitalizeFirst() + "\n");
                }
                stringBuilder.Append("AC.AgeChronologicalTicks".Translate() + ": " + (int)(this.ageChronologicalTicks / 3600000) + "\n");
                if (this.stackDegradation > 0)
                {
                    stringBuilder.AppendLineTagged("AC.StackDegradation".Translate((TaggedString)(this.stackDegradation.ToStringPercent().Colorize(Color.red))));
                }
            }
        }

        public void AppendDebugData(StringBuilder stringBuilder, Pawn pawn)
        {
            if (AC_Utils.debug)
            {
                if (pawn != null)
                {
                    stringBuilder.AppendLine("Full name: ");
                    stringBuilder.AppendLine(pawn.GetFullName());
                    stringBuilder.AppendLine("pawn relations: ");
                    stringBuilder.AppendLine(pawn.relations.DirectRelations.Select(x => x.def + " - " + x.otherPawn.GetFullName()).ToStringSafeEnumerable());
                    stringBuilder.AppendLine("other relations: ");
                    foreach (var other in pawn.relations.RelatedPawns.ToList())
                    {
                        if (other.relations.DirectRelations.Any())
                        {
                            stringBuilder.AppendLine(other.GetFullName() + " - " + other.relations.DirectRelations.Select(x => x.def + " - " + x.otherPawn.GetFullName()).ToStringSafeEnumerable());
                        }
                    }
                }
                if (relations != null)
                {
                    stringBuilder.AppendLine("stack relations: ");
                    stringBuilder.AppendLine(relations.Select(x => x.def + " - " + x.otherPawn.GetFullName()).ToStringSafeEnumerable());
                }

                if (otherPawnRelations != null)
                {
                    stringBuilder.AppendLine("other pawn relations: ");
                    stringBuilder.AppendLine(otherPawnRelations.Select(x => x.Key.def + " - " + x.Value.GetFullName()).ToStringSafeEnumerable());
                }
                stringBuilder.AppendLine("stack group:");
                stringBuilder.AppendLine("originalPawn: " + StackGroupData.originalPawn.GetFullName()
                    + " - originalStack: " + StackGroupData.originalStack.ToStringSafe()
                    + " - copiedPawns: " + StackGroupData.copiedPawns.Select(x => x.GetFullName()).ToStringSafeEnumerable()
                    + " - copiedStacks: " + StackGroupData.copiedStacks.Select(x => x.ToStringSafe()).ToStringSafeEnumerable());
            }
        }

        public void CopyFromPawn(Pawn pawn, ThingDef sourceStack, bool copyRaceGenderInfo = false, bool canBackupPsychicStuff = true)
        {
            AC_Utils.DebugMessage("CopyFromPawn: -----------------------------------------");
            AC_Utils.DebugMessage("CopyFromPawn: " + pawn.GetFullName() + " - pawn id: " + pawnID);
            this.hostPawn = pawn;
            this.sourceStack = sourceStack ?? AC_DefOf.AC_ActiveNeuralStack;
            CaptureFrom(pawn, copyRaceGenderInfo, AC_DefOf.AC_StackSavingOptions.ignoresTraits);
            if (this.sourceStack == AC_DefOf.AC_ActiveArchotechStack && canBackupPsychicStuff)
            {
                if (pawn.HasPsylink)
                {
                    psylinkLevel = pawn.GetPsylinkLevel();
                }
                if (pawn.psychicEntropy != null)
                {
                    this.currentEntropy = pawn.psychicEntropy.currentEntropy;
                    this.currentPsyfocus = pawn.psychicEntropy.currentPsyfocus;
                    this.limitEntropyAmount = pawn.psychicEntropy.limitEntropyAmount;
                    this.targetPsyfocus = pawn.psychicEntropy.targetPsyfocus;
                }
                VPE_PsycastAbilityImplant = pawn.health.hediffSet.hediffs.FirstOrDefault(x => x.def == AC_DefOf.VPE_PsycastAbilityImplant);

                if (pawn.abilities?.abilities != null)
                {
                    abilities = new List<AbilityDef>();
                    foreach (var ability in pawn.abilities.abilities.Select(x => x.def).ToList())
                    {
                        if (CanStoreAbility(pawn, ability))
                        {
                            abilities.Add(ability);
                        }
                    }
                }

                var comp = pawn.GetComp<VEF.Abilities.CompAbilities>();
                if (comp != null && comp.LearnedAbilities != null)
                {
                    VEAbilities = comp.LearnedAbilities.Select(x => x.def).Where(x => CanStoreAbility(pawn, x)).ToList();
                }
            }

            pawnID = pawn.thingIDNumber;

            var stackDegradationHediff = pawn.GetHediff(AC_DefOf.AC_StackDegradation) as Hediff_StackDegradation;
            if (stackDegradationHediff != null)
            {
                this.stackDegradation = stackDegradationHediff.stackDegradation;
            }

            AC_Utils.DebugMessage("CopyFromPawn: -----------------------------------------");
        }

        public void CopyDataFrom(NeuralData other, bool isDuplicateOperation = false)
        {
            sourceStack = other.sourceStack;
            name = GetNameCopy(other.name);
            kindDef = other.kindDef;
            hostPawn = other.hostPawn;
            hostilityMode = other.hostilityMode;
            allowedAreas = other.allowedAreas.CopyDict();
            ageChronologicalTicks = other.ageChronologicalTicks;
            ageBiologicalTicks = other.ageBiologicalTicks;
            medicalCareCategory = other.medicalCareCategory;
            selfTend = other.selfTend;
            foodPolicy = other.foodPolicy;
            apparelPolicy = other.apparelPolicy;
            if (other.forcedHandler != null)
            {
                forcedHandler = new OutfitForcedHandler();
                forcedHandler.CopyFrom(other.forcedHandler);
            }
            drugPolicy = other.drugPolicy;
            times = other.times.CopyList();
            thoughts = other.thoughts.CopyList();
            faction = other.Faction;
            isFactionLeader = other.isFactionLeader;
            traits = new List<Trait>();
            if (other.traits != null)
            {
                foreach (var trait in other.traits)
                {
                    traits.Add(new Trait(trait.def, trait.degree));
                }
            }
            relations = other.relations.CopyList();
            otherPawnRelations = other.otherPawnRelations.CopyDict();
            everSeenByPlayer = other.everSeenByPlayer;
            canGetRescuedThought = other.canGetRescuedThought;
            relativeInvolvedInRescueQuest = other.relativeInvolvedInRescueQuest;
            nextMarriageNameChange = other.nextMarriageNameChange;
            hidePawnRelations = other.hidePawnRelations;
            relatedPawns = other.relatedPawns.CopyList();
            skills = new List<SkillRecord>();
            if (other.skills != null)
            {
                foreach (var skill in other.skills)
                {
                    skills.Add(new SkillRecord
                    {
                        def = skill.def,
                        levelInt = skill.levelInt,
                        xpSinceLastLevel = skill.xpSinceLastLevel,
                        xpSinceMidnight = skill.xpSinceMidnight,
                        passion = skill.passion,
                    });
                }
            }
            childhood = other.childhood;
            adulthood = other.adulthood;
            title = other.title;
            priorities = other.priorities.CopyDict();
            psylinkLevel = other.psylinkLevel;
            abilities = other.abilities.CopyList();
            VEAbilities = other.VEAbilities.CopyList();
            VPE_PsycastAbilityImplant = other.VPE_PsycastAbilityImplant;
            currentEntropy = other.currentEntropy;
            currentPsyfocus = other.currentPsyfocus;
            limitEntropyAmount = other.limitEntropyAmount;
            targetPsyfocus = other.targetPsyfocus;

            guestStatusInt = other.guestStatusInt;
            interactionMode = other.interactionMode;
            slaveInteractionMode = other.slaveInteractionMode;
            hostFactionInt = other.hostFactionInt;
            joinStatus = other.joinStatus;
            slaveFactionInt = other.slaveFactionInt;
            lastRecruiterName = other.lastRecruiterName;
            lastRecruiterOpinion = other.lastRecruiterOpinion;
            hasOpinionOfLastRecruiter = other.hasOpinionOfLastRecruiter;
            lastRecruiterResistanceReduce = other.lastRecruiterResistanceReduce;
            releasedInt = other.releasedInt;
            ticksWhenAllowedToEscapeAgain = other.ticksWhenAllowedToEscapeAgain;
            spotToWaitInsteadOfEscaping = other.spotToWaitInsteadOfEscaping;
            lastPrisonBreakTicks = other.lastPrisonBreakTicks;
            everParticipatedInPrisonBreak = other.everParticipatedInPrisonBreak;
            resistance = other.resistance;
            will = other.will;
            ideoForConversion = other.ideoForConversion;
            everEnslaved = other.everEnslaved;
            getRescuedThoughtOnUndownedBecauseOfPlayer = other.getRescuedThoughtOnUndownedBecauseOfPlayer;
            recruitable = other.recruitable;
            records = other.records.Clone();
            savedHediffs = new List<Hediff>();
            foreach (var otherHediff in other.savedHediffs)
            {
                savedHediffs.Add(MakeCopy(otherHediff, hostPawn ?? DummyPawn));
            }
            battleActive = other.battleActive;
            battleExitTick = other.battleExitTick;

            CopyOriginalData(other);

            pawnID = other.pawnID;

            if (ModsConfig.RoyaltyActive)
            {
                royalTitles = other.royalTitles.CopyList();
                favor = other.favor.CopyDict();
                heirs = other.heirs.CopyDict();
                bondedThings = other.bondedThings.CopyList();
                factionPermits = other.factionPermits.CopyList();
            }
            if (ModsConfig.IdeologyActive)
            {
                ideo = other.ideo;
                previousIdeos = other.previousIdeos.CopyList();
                joinTick = other.joinTick;
                certainty = other.certainty;

                precept_RoleSingle = other.precept_RoleSingle;

                precept_RoleMulti = other.precept_RoleMulti;

                if (other.favoriteColor != null)
                {
                    favoriteColor = other.favoriteColor;
                }
            }

            isCopied = isDuplicateOperation || other.isCopied;
            diedFromCombat = other.diedFromCombat;
            stackGroupID = other.stackGroupID;

            foreach (var otherEntry in other.ModCompatibilityEntries)
            {
                var entry = ModCompatibilityEntries.FirstOrDefault(x => x.modIdentifier == otherEntry.modIdentifier);
                if (entry != null)
                {
                    entry.CopyFrom(otherEntry);
                }
            }

            stackDegradation = other.stackDegradation;
            AssignDummyPawnReferences();
        }

        public void CopyOriginalData(NeuralData other)
        {
            OriginalGender = other.OriginalGender;
            OriginalRace = other.OriginalRace;
            OriginalXenotypeDef = other.OriginalXenotypeDef;
            OriginalXenotypeName = other.OriginalXenotypeName;
        }

        public static Pawn overwriting;
        public void OverwritePawn(Pawn pawn, bool changeGlobalData = true)
        {
            AC_Utils.DebugMessage("OverwritePawn: -----------------------------------------");
            AC_Utils.DebugMessage("OverwritePawn: " + pawn.GetFullName() + " - pawn id: " + pawnID + " - name: " + name?.ToStringShort);
            overwriting = pawn;
            try
            {
                OverwritePawnInternal(pawn, changeGlobalData);
            }
            finally
            {
                overwriting = null;
            }
            AC_Utils.DebugMessage("OverwritePawn: -----------------------------------------");
        }

        private void OverwritePawnInternal(Pawn pawn, bool changeGlobalData)
        {
            if (pawn.IsEmptySleeve())
            {
                pawn.UndoEmptySleeve();
            }
            var options = new PawnRestoreOptions
            {
                RestoreBody = false,
                RestoreRelations = false,
                RestoreFactionLeadership = changeGlobalData && pawn.IsCopy() is false,
                IgnoredTraitDefNames = AC_DefOf.AC_StackSavingOptions.ignoresTraits,
                RetainExtraAbility = ability => IsPsycastAbility(ability) && sourceStack != AC_DefOf.AC_ActiveArchotechStack,
                RetainLearnedAbility = def => IsPsycastAbility(def),
                RestorePsycasts = sourceStack == AC_DefOf.AC_ActiveArchotechStack
            };
            RestoreTo(pawn, options);
            if (changeGlobalData)
            {
                OverwriteRelationships(pawn);
                this.hostPawn = pawn;
                this.pawnID = this.hostPawn.thingIDNumber;
            }
        }

        [HotSwappable]
        [HarmonyPatch(typeof(PawnRelationWorker_Child), nameof(PawnRelationWorker_Child.InRelation))]
        public static class PawnRelationWorker_Child_InRelation_Patch
        {
            public static void Postfix(Pawn me, Pawn other, ref bool __result)
            {
                if (__result is false)
                {
                    if (me.IsMyOriginal(other.GetMother()) || me.IsMyOriginal(other.GetFather())
                        || ModsConfig.BiotechActive && me.IsMyOriginal(other.GetBirthParent()))
                    {
                        __result = true;
                    }
                }
            }
        }

        private void OverwriteRelationships(Pawn pawn)
        {
            AC_Utils.DebugMessage("OverwriteRelationships: " + pawn.GetFullName() + " - "
                + RelationshipsString(relations) + " - pawn id: " + pawnID);
            if (pawn.IsCopy())
            {
                AddCopyRelations(pawn);
            }

            GetAllRelatedPawns(pawn, out HashSet<Pawn> allPotentialRelatedPawns, out Pawn oldOrigPawn);
            AC_Utils.DebugMessage(pawn.GetFullName() + " found orig pawn: " + oldOrigPawn.GetFullName() + " - pawnID: " + pawnID);
            if (oldOrigPawn != pawn)
            {
                ReplaceOrigPawnReferences(pawn, allPotentialRelatedPawns, oldOrigPawn);
            }

            if (relations != null)
            {
                foreach (var relation in relations)
                {
                    if (pawn.relations.DirectRelationExists(relation.def, relation.otherPawn) is false)
                    {
                        pawn.relations.directRelations.Add(new DirectPawnRelation
                        {
                            def = relation.def,
                            otherPawn = relation.otherPawn,
                            startTicks = relation.startTicks,
                        });
                    }
                }
            }
            if (otherPawnRelations != null)
            {
                AC_Utils.DebugMessage("OverwriteRelationships: otherPawnRelations: " + pawn.GetFullName() + " - " + otherPawnRelations.Select(x => x.Key?.def + " : " + x.Value.GetFullName()).ToStringSafeEnumerable());
                foreach (var relation in otherPawnRelations)
                {
                    if (relation.Value.relations.DirectRelationExists(relation.Key.def, pawn) is false)
                    {
                        relation.Value.relations.directRelations.Add(new DirectPawnRelation
                        {
                            def = relation.Key.def,
                            otherPawn = pawn,
                            startTicks = relation.Key.startTicks,
                        });
                    }
                }
            }
            else
            {
                AC_Utils.DebugMessage(pawn.GetFullName() + " - " + "otherPawnRelations is null!");
            }

            foreach (var otherPawn in allPotentialRelatedPawns)
            {
                otherPawn.needs?.mood?.thoughts?.situational.Notify_SituationalThoughtsDirty();
            }
        }

        private void GetAllRelatedPawns(Pawn pawn, out HashSet<Pawn> allPotentialRelatedPawns, out Pawn oldOrigPawn)
        {
            allPotentialRelatedPawns = new HashSet<Pawn>();
            allPotentialRelatedPawns.AddRange(PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead);
            if (relations != null)
            {
                for (var i = relations.Count - 1; i >= 0; i--)
                {
                    var rel = relations[i];
                    if (rel != null && rel.otherPawn != null)
                    {
                        allPotentialRelatedPawns.Add(rel.otherPawn);
                    }
                }
            }

            if (relatedPawns != null)
            {
                for (var i = relatedPawns.Count - 1; i >= 0; i--)
                {
                    var relatedPawn = relatedPawns[i];
                    if (relatedPawn != null)
                    {
                        allPotentialRelatedPawns.Add(relatedPawn);
                    }
                }
            }

            allPotentialRelatedPawns = allPotentialRelatedPawns.Where(x => x.RaceProps.Humanlike).ToHashSet();
            oldOrigPawn = this.hostPawn;
            if (oldOrigPawn is null)
            {
                oldOrigPawn = allPotentialRelatedPawns.FirstOrDefault(x => x.Dead && x.thingIDNumber == pawnID);
                if (oldOrigPawn is null)
                {
                    oldOrigPawn = allPotentialRelatedPawns.FirstOrDefault(x => x.Dead && IsPresetPawn(x) && x != pawn);
                }
            }

            if (oldOrigPawn?.relations != null)
            {
                var potentiallyRelatedPawns = oldOrigPawn.relations.PotentiallyRelatedPawns.ToList();
                for (var i = potentiallyRelatedPawns.Count - 1; i >= 0; i--)
                {
                    var relatedPawn = potentiallyRelatedPawns[i];
                    if (relatedPawn != null)
                    {
                        allPotentialRelatedPawns.Add(relatedPawn);
                    }
                }
            }
        }

        private void AddCopyRelations(Pawn pawn)
        {
            var original = StackGroupData.originalPawn;
            if (original != null)
            {
                var otherRelations = original.relations.DirectRelations.Where(x => x.otherPawn != pawn
                && x.def != AC_DefOf.AC_Copy && x.def != AC_DefOf.AC_Original).ToList();
                AC_Utils.DebugMessage("original: " + original.GetFullName() + " - All relations: " + RelationshipsString(otherRelations));
                foreach (var rel in otherRelations)
                {
                    if (rel.def.familyByBloodRelation)
                    {
                        if (pawn.relations.DirectRelationExists(rel.def, rel.otherPawn) is false && pawn != rel.otherPawn)
                        {
                            pawn.relations.AddDirectRelation(rel.def, rel.otherPawn);
                            AC_Utils.DebugMessage(pawn.GetFullName() + " - 2 Added direct relation " + rel.def + " with " + rel.otherPawn.GetFullName());
                        }
                    }
                }

                var relatedPawns = original.relations.PotentiallyRelatedPawns.ToList();
                foreach (var relatedPawn in relatedPawns)
                {
                    if (relatedPawn != pawn)
                    {
                        otherRelations = relatedPawn.relations.DirectRelations.Where(x => x.otherPawn == original
                            && x.def != AC_DefOf.AC_Copy && x.def != AC_DefOf.AC_Original).ToList();
                        AC_Utils.DebugMessage("relatedPawn: " + relatedPawn.GetFullName() + " - All relations: " + RelationshipsString(otherRelations));
                        foreach (var rel in otherRelations)
                        {
                            if (rel.def.familyByBloodRelation)
                            {
                                if (relatedPawn.relations.DirectRelationExists(rel.def, pawn) is false)
                                {
                                    relatedPawn.relations.AddDirectRelation(rel.def, pawn);
                                    AC_Utils.DebugMessage(relatedPawn.GetFullName() + " - 3 Added direct relation " + rel.def + " with " + pawn.GetFullName());
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ReplaceOrigPawnReferences(Pawn pawn, HashSet<Pawn> allPotentialRelatedPawns, Pawn oldOrigPawn)
        {
            AC_Utils.DebugMessage("ReplaceOrigPawnReferences: pawn: " + pawn.GetFullName() + " - oldOrigPawn: "
                + oldOrigPawn.GetFullName());
            if (oldOrigPawn?.relations != null)
            {
                var directRelations = oldOrigPawn.relations.DirectRelations.ToList();
                foreach (var directRelation in directRelations)
                {
                    oldOrigPawn.relations.directRelations.Remove(directRelation);
                    oldOrigPawn.relations.pawnsWithDirectRelationsWithMe.Remove(directRelation.otherPawn);
                    pawn.relations.directRelations.Add(directRelation);
                    oldOrigPawn.relations.pawnsWithDirectRelationsWithMe.Add(directRelation.otherPawn);
                }
            }

            if (relatedPawns != null)
            {
                allPotentialRelatedPawns.AddRange(relatedPawns);
            }
            if (relations != null)
            {
                relations.RemoveAll(x => x.def is null || x.otherPawn is null);
                foreach (var relation in relations)
                {
                    allPotentialRelatedPawns.Add(relation.otherPawn);
                }
            }
            if (oldOrigPawn != null)
            {
                allPotentialRelatedPawns.Remove(oldOrigPawn);
            }
            allPotentialRelatedPawns.RemoveWhere(x => x is null);
            foreach (var potentiallyRelatedPawn in allPotentialRelatedPawns)
            {
                ReplaceSocialReferences(potentiallyRelatedPawn, pawn, oldOrigPawn);
            }
            if (oldOrigPawn != null)
            {
                ReplaceTales(pawn, oldOrigPawn);
            }
        }

        private void ReplaceSocialReferences(Pawn relatedPawn, Pawn newReference, Pawn oldOriginPawn)
        {
            if (relatedPawn.CanThink())
            {
                foreach (Thought_Memory thought in relatedPawn.needs.mood.thoughts.memories.Memories)
                {
                    if (oldOriginPawn != null && thought.otherPawn == oldOriginPawn)
                    {
                        AC_Utils.DebugMessage("1 ReplaceSocialReferences: relatedPawn: " + relatedPawn.GetFullName()
                            + " - " + thought.def + " - other: " + thought.otherPawn.GetFullName() + " is replaced by " + newReference.GetFullName());
                        thought.otherPawn = newReference;
                    }
                }
            }

            if (relatedPawn.relations != null)
            {
                var otherPawnRelations = relatedPawn.relations.DirectRelations;
                var oldString = RelationshipsString(otherPawnRelations);
                ReplacePawnRelations(relatedPawn, newReference, oldOriginPawn);
                if (otherPawnRelations.Any())
                {
                    var newString = RelationshipsString(otherPawnRelations);
                    if (newString != oldString)
                    {
                        AC_Utils.DebugMessage("ReplaceSocialReferences: " + relatedPawn.GetFullName() + " - BEFORE: "
                            + oldString);
                        AC_Utils.DebugMessage("ReplaceSocialReferences: " + relatedPawn.GetFullName() + " - AFTER: "
                            + newString);
                    }
                }
            }

            if (relatedPawn.needs?.mood?.thoughts != null)
            {
                relatedPawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
            }
        }

        public static string RelationshipsString(List<DirectPawnRelation> relations)
        {
            return relations.Select(x => x.def + " - " + x.otherPawn.GetFullName()).ToStringSafeEnumerable();
        }

        private static void ReplacePawnRelations(Pawn relatedPawn, Pawn newReference, Pawn otherPawn)
        {
            relatedPawn.relations.pawnsWithDirectRelationsWithMe.Remove(otherPawn);
            relatedPawn.relations.pawnsWithDirectRelationsWithMe.Add(newReference);
            foreach (var relation in relatedPawn.relations.DirectRelations)
            {
                if (relation.def != PawnRelationDefOf.Overseer)
                {
                    if (relation.otherPawn == otherPawn)
                    {
                        AC_Utils.DebugMessage("ReplacePawnRelations: Replacing: " + relation.def + " - old otherPawn: "
                            + relation.otherPawn.GetFullName() + " - new otherPawn: " + newReference.GetFullName());
                        relation.otherPawn = newReference;
                    }
                }
            }
        }

        private void ReplaceTales(Pawn pawn, Pawn oldOrigPawn)
        {
            foreach (var tale in Find.TaleManager.AllTalesListForReading)
            {
                if (tale is Tale_SinglePawn tale1)
                {
                    if (tale1.pawnData?.pawn == oldOrigPawn)
                        tale1.pawnData.pawn = pawn;
                }
                else if (tale is Tale_DoublePawn tale2)
                {
                    if (tale2.firstPawnData?.pawn == oldOrigPawn)
                        tale2.firstPawnData.pawn = pawn;
                    if (tale2.secondPawnData?.pawn == oldOrigPawn)
                        tale2.secondPawnData.pawn = pawn;
                }
            }
        }

        public static bool CanStoreAbility(Pawn pawn, Def def)
        {
            if (IsNaturalAbility(pawn, def))
            {
                return false;
            }
            if (IsPsycastAbility(def))
            {
                return true;
            }
            return false;
        }

        public static bool IsPsycastAbility(Def def)
        {
            if (def is AbilityDef abilityDef)
            {
                return typeof(Psycast).IsAssignableFrom(abilityDef.abilityClass);
            }
            else if (ModCompatibility.VEPsycastsIsActive && def.modExtensions != null && def is VEF.Abilities.AbilityDef abilityDef2)
            {
                foreach (var modExtension in def.modExtensions)
                {
                    if (ModCompatibility.VEPsycastModExtensionType.IsAssignableFrom(modExtension.GetType()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsPresetPawn(Pawn pawn)
        {
            if (pawn == null || pawn.Name == null) return false;
            return pawn != null && (hostPawn == pawn || pawn.thingIDNumber == pawnID || name != null && pawn.Name != null && name.ToStringFull == pawn.Name.ToStringFull);
        }
        public bool IsPresetPawn(NeuralData otherNeuralData)
        {
            if (PawnID != 0)
            {
                return PawnID == otherNeuralData.PawnID;
            }
            if (hostPawn != null)
            {
                return hostPawn == otherNeuralData.hostPawn;
            }
            else if (name != null)
            {
                return name == otherNeuralData.name;
            }
            return false;
        }

        protected override void FilterRestoredThoughts(Pawn pawn)
        {
            if (ideo?.HasPrecept(AC_DefOf.AC_CrossSleeving_DontCare) ?? false)
            {
                thoughts.RemoveAll(x => x.def == AC_DefOf.AC_WrongGender);
                thoughts.RemoveAll(x => x.def == AC_DefOf.AC_WrongGenderPregnant);
            }

            if (OriginalGender == pawn.gender)
            {
                thoughts.RemoveAll(x => x.def == AC_DefOf.AC_WrongGender);
                thoughts.RemoveAll(x => x.def == AC_DefOf.AC_WrongGenderPregnant);
            }
            if (ModCompatibility.AlienRacesIsActive && OriginalRace == pawn.kindDef.race)
            {
                thoughts.RemoveAll(x => x.def == AC_DefOf.AC_WrongRace);
            }
            if (OriginalXenotypeDef != null && OriginalXenotypeDef == pawn.genes.xenotype
                || OriginalXenotypeName.NullOrEmpty() is false && OriginalXenotypeName == pawn.genes.xenotypeName)
            {
                thoughts.RemoveAll(x => x.def == AC_DefOf.AC_WrongXenotype);
            }
        }

        protected override bool CanClaimIdeoRoles(Pawn pawn) => IsDummyPawn(pawn) is false;

        protected override bool CanClaimSingleRole(IdeoRoleInstance chosenPawn) => base.CanClaimSingleRole(chosenPawn) || (chosenPawn?.pawn?.IsEmptySleeve() ?? false);

        protected override bool TryCaptureExistingOriginal(Pawn pawn)
        {
            if (pawn.HasNeuralStack(out var hediff))
            {
                var neuralData = hediff.NeuralData;
                OriginalRace = neuralData.OriginalRace ?? pawn.def;
                OriginalGender = neuralData.OriginalGender != Gender.None ? neuralData.OriginalGender : pawn.gender;
                if (neuralData.OriginalXenotypeName.NullOrEmpty())
                {
                    if (pawn.genes.xenotypeName.NullOrEmpty())
                    {
                        OriginalXenotypeDef = neuralData.OriginalXenotypeDef != null ? neuralData.OriginalXenotypeDef : pawn.genes.xenotype;
                    }
                    else
                    {
                        OriginalXenotypeName = pawn.genes.xenotypeName;
                    }
                }
                else
                {
                    OriginalXenotypeName = neuralData.OriginalXenotypeName;
                }
                return true;
            }
            return false;
        }

        protected override bool CanCodeBondedWeapons(Pawn pawn) => IsDummyPawn(pawn) is false;

        public bool IsDummyPawn(Pawn pawn)
        {
            if (pawn != null)
            {
                return pawn == dummyPawn || dummyPawns.TryGetValue(pawn, out _);
            }
            return false;
        }

        private void AssignDummyPawnReferences()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                if (IsDummyPawn(hostPawn))
                {
                    var dummyPawn = DummyPawn;
                    if (skills != null)
                    {
                        foreach (var skill in skills)
                        {
                            skill.pawn = dummyPawn;
                        }
                    }
                    if (traits != null)
                    {
                        foreach (var trait in traits)
                        {
                            trait.pawn = dummyPawn;
                        }
                    }
                    dummyPawn.Notify_DisabledWorkTypesChanged();
                }
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref pawnID, "pawnID");
            Scribe_Values.Look(ref stackGroupID, "stackGroupID", 0);
            Scribe_Values.Look(ref isCopied, "isCopied", false, false);
            Scribe_Values.Look(ref diedFromCombat, "diedFromCombat");
            Scribe_References.Look(ref hostPawn, "hostPawn");
            Scribe_Defs.Look(ref sourceStack, "sourceStack");
            Scribe_Values.Look(ref lastTimeBackedUp, "lastTimeUpdated");
            Scribe_Values.Look(ref editTime, "editTime");
            Scribe_Values.Look(ref stackDegradation, "stackDegradation");
            Scribe_Values.Look(ref stackDegradationToAdd, "stackDegradationToAdd");
            Scribe_Deep.Look(ref neuralDataRewritten, "neuralDataRewritten");
            Scribe_References.Look(ref trackedToMatrix, "trackedToMatrix");
            if (VPE_PsycastAbilityImplant != null)
            {
                if (this.hostPawn != null)
                {
                    VPE_PsycastAbilityImplant.pawn = this.hostPawn;
                }
                VPE_PsycastAbilityImplant.loadID = Find.UniqueIDsManager.GetNextHediffID();
            }
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (pawnID == 0 && hostPawn != null)
                {
                    pawnID = hostPawn.thingIDNumber;
                }
                if (dummyPawn != null)
                {
                    dummyPawns.Remove(dummyPawn);
                    dummyPawns.Add(dummyPawn, new StrongBox<ThingWithNeuralData> { Value = host });
                }
            }
        }

        public override string ToString()
        {
            return name + " - " + Faction;
        }
    }
}
