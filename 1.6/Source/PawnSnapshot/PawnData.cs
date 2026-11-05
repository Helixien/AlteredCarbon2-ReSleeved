using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PawnSnapshot
{
    public class PawnData : IExposable
    {
        public Name name;
        public PawnKindDef kindDef;
        protected HostilityResponseMode hostilityMode;
        protected Dictionary<Map, Area> allowedAreas = new Dictionary<Map, Area>();
        protected MedicalCareCategory medicalCareCategory;
        protected bool selfTend;
        public long ageBiologicalTicks;
        public long ageChronologicalTicks;
        protected List<TimeAssignmentDef> times;
        protected FoodPolicy foodPolicy;
        protected ApparelPolicy apparelPolicy;
        protected OutfitForcedHandler forcedHandler;
        protected DrugPolicy drugPolicy;

        public Faction faction;
        public bool isFactionLeader;
        public List<Thought_Memory> thoughts;
        public List<Trait> traits = new List<Trait>();
        protected List<DirectPawnRelation> relations;
        protected Dictionary<DirectPawnRelation, Pawn> otherPawnRelations;
        protected List<Pawn> relatedPawns;
        public List<SkillRecord> skills = new List<SkillRecord>();

        public BackstoryDef childhood;
        public BackstoryDef adulthood;

        public string title;
        public bool everSeenByPlayer;
        public bool canGetRescuedThought = true;
        public Pawn relativeInvolvedInRescueQuest;
        public MarriageNameChange nextMarriageNameChange;
        public bool hidePawnRelations;

        protected Dictionary<WorkTypeDef, int> priorities;
        public GuestStatus guestStatusInt;
        protected PrisonerInteractionModeDef interactionMode;
        protected SlaveInteractionModeDef slaveInteractionMode;
        public Faction hostFactionInt;
        protected JoinStatus joinStatus;
        protected Faction slaveFactionInt;
        protected string lastRecruiterName;
        protected int lastRecruiterOpinion;
        protected bool hasOpinionOfLastRecruiter;
        protected float lastRecruiterResistanceReduce;
        protected bool releasedInt;
        protected int ticksWhenAllowedToEscapeAgain;
        public IntVec3 spotToWaitInsteadOfEscaping;
        public int lastPrisonBreakTicks = -1;
        public bool everParticipatedInPrisonBreak;
        public float resistance = -1f;
        public float will = -1f;
        public Ideo ideoForConversion;
        protected bool everEnslaved = false;
        public bool getRescuedThoughtOnUndownedBecauseOfPlayer;
        public bool recruitable;

        protected DefMap<RecordDef, float> records = new DefMap<RecordDef, float>();
        protected Battle battleActive;
        protected int battleExitTick;

        protected List<Hediff> savedHediffs = new List<Hediff>();
        protected List<AbilityDef> abilities = new List<AbilityDef>();

        // original pawn data before sleeving
        protected Gender originalGender;
        protected ThingDef originalRace;
        protected XenotypeDef originalXenotypeDef;
        protected string originalXenotypeName;

        public Gender OriginalGender { get => originalGender; set => originalGender = value; }
        public ThingDef OriginalRace { get => originalRace; set => originalRace = value; }
        public XenotypeDef OriginalXenotypeDef { get => originalXenotypeDef; set => originalXenotypeDef = value; }
        public string OriginalXenotypeName { get => originalXenotypeName; set => originalXenotypeName = value; }

        public List<RoyalTitle> royalTitles = new List<RoyalTitle>();
        public Dictionary<Faction, int> favor = new Dictionary<Faction, int>();
        protected Dictionary<Faction, Pawn> heirs = new Dictionary<Faction, Pawn>();
        protected List<Thing> bondedThings = new List<Thing>();
        protected List<FactionPermit> factionPermits = new List<FactionPermit>();

        protected int? psylinkLevel;
        protected float currentEntropy;
        public bool limitEntropyAmount = true;
        protected float currentPsyfocus = -1f;
        protected float targetPsyfocus = 0.5f;

        protected List<VEF.Abilities.AbilityDef> VEAbilities = new List<VEF.Abilities.AbilityDef>();
        protected Hediff VPE_PsycastAbilityImplant;

        // Ideology
        public Ideo ideo;
        public ColorDef favoriteColor;
        public int joinTick;
        public List<Ideo> previousIdeos;
        public float certainty;
        public Precept_RoleMulti precept_RoleMulti;
        public Precept_RoleSingle precept_RoleSingle;

        public List<ModCompatibilityEntry> modCompatibilityEntries = new List<ModCompatibilityEntry>();

        public HeadTypeDef headType;
        public BodyTypeDef bodyType;
        public HairDef hairDef;
        public BeardDef beardDef;
        public Color hairColor;
        public Color? skinColorOverride;
        public Color skinColorBase;
        public XenotypeIconDef xenotypeIcon;
        public List<GeneDef> endogenes = new List<GeneDef>();
        public List<GeneDef> xenogenes = new List<GeneDef>();

        public float BioAgeYears => (float)ageBiologicalTicks / GenDate.TicksPerYear;

        public string TitleShort
        {
            get
            {
                if (title != null)
                {
                    return title;
                }
                return adulthood != null ? adulthood.TitleShortFor(OriginalGender)
                    : childhood != null ? childhood.TitleShortFor(OriginalGender) : "";
            }
        }

        protected virtual bool TryCaptureExistingOriginal(Pawn pawn) => false;

        public void CaptureFrom(Pawn pawn, bool captureOriginalInfo, List<string> ignoredTraitDefNames = null)
        {
            name = GetNameCopy(pawn.Name);
            this.kindDef = pawn.kindDef;
            if (pawn.playerSettings != null)
            {
                hostilityMode = pawn.playerSettings.hostilityResponse;
                allowedAreas = pawn.playerSettings.allowedAreas.CopyDict();
                medicalCareCategory = pawn.playerSettings.medCare;
                selfTend = pawn.playerSettings.selfTend;
            }
            if (pawn.ageTracker != null)
            {
                ageChronologicalTicks = pawn.ageTracker.AgeChronologicalTicks;
                ageBiologicalTicks = pawn.ageTracker.AgeBiologicalTicks;
            }
            foodPolicy = pawn.foodRestriction?.CurrentFoodPolicy;
            apparelPolicy = pawn.outfits?.curApparelPolicy;
            if (pawn.outfits?.forcedHandler != null)
            {
                forcedHandler = new OutfitForcedHandler();
                forcedHandler.CopyFrom(pawn.outfits.forcedHandler);
            }
            drugPolicy = pawn.drugs?.CurrentPolicy;
            times = pawn.timetable?.times.CopyList();
            thoughts = pawn.needs?.mood?.thoughts?.memories?.Memories.Select(t => t.Clone()).ToList();
            faction = pawn.Faction;
            if (pawn.Faction?.leader == pawn)
            {
                isFactionLeader = true;
            }
            traits = new List<Trait>();
            if (pawn.story?.traits != null)
            {
                foreach (var trait in pawn.story.traits.allTraits)
                {
                    if (trait.sourceGene is null && trait.suppressedByGene is null)
                    {
                        if (ignoredTraitDefNames != null && ignoredTraitDefNames.Contains(trait.def.defName))
                        {
                            continue;
                        }
                        traits.Add(new Trait(trait.def, trait.degree));
                    }
                }
            }

            if (pawn.relations != null)
            {
                everSeenByPlayer = pawn.relations.everSeenByPlayer;
                canGetRescuedThought = pawn.relations.canGetRescuedThought;
                relativeInvolvedInRescueQuest = pawn.relations.relativeInvolvedInRescueQuest;
                nextMarriageNameChange = pawn.relations.nextMarriageNameChange;
                hidePawnRelations = pawn.relations.hidePawnRelations;
                relations = pawn.relations.DirectRelations.Where(x => x.def != PawnRelationDefOf.Overseer).ToList();
                otherPawnRelations = new Dictionary<DirectPawnRelation, Pawn>();
                foreach (var rel in pawn.relations.PotentiallyRelatedPawns.ToList())
                {
                    foreach (var otherRelation in rel.relations.directRelations.ToList())
                    {
                        if (otherRelation.otherPawn == pawn && otherRelation.def != PawnRelationDefOf.Overseer)
                        {
                            otherPawnRelations[otherRelation] = rel;
                        }
                    }
                }

                relatedPawns = pawn.relations.PotentiallyRelatedPawns.ToList();
                relatedPawns = relatedPawns.Distinct().ToList();
            }

            skills = new List<SkillRecord>();
            if (pawn.skills?.skills != null)
            {
                foreach (var skill in pawn.skills.skills)
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
            childhood = pawn.story?.Childhood;
            adulthood = pawn.story?.Adulthood;
            title = pawn.story?.title;

            priorities = new Dictionary<WorkTypeDef, int>();
            if (pawn.workSettings != null && pawn.workSettings.priorities != null)
            {
                foreach (WorkTypeDef w in DefDatabase<WorkTypeDef>.AllDefs)
                {
                    priorities[w] = pawn.workSettings.GetPriority(w);
                }
            }
            if (pawn.guest != null)
            {
                guestStatusInt = pawn.guest.GuestStatus;
                interactionMode = pawn.guest.interactionMode;
                slaveInteractionMode = pawn.guest.slaveInteractionMode;
                hostFactionInt = pawn.guest.HostFaction;
                joinStatus = pawn.guest.joinStatus;
                slaveFactionInt = pawn.guest.SlaveFaction;
                lastRecruiterName = pawn.guest.lastRecruiterName;
                lastRecruiterOpinion = pawn.guest.lastRecruiterOpinion;
                hasOpinionOfLastRecruiter = pawn.guest.hasOpinionOfLastRecruiter;
                releasedInt = pawn.guest.Released;
                ticksWhenAllowedToEscapeAgain = pawn.guest.ticksWhenAllowedToEscapeAgain;
                spotToWaitInsteadOfEscaping = pawn.guest.spotToWaitInsteadOfEscaping;
                lastPrisonBreakTicks = pawn.guest.lastPrisonBreakTicks;
                everParticipatedInPrisonBreak = pawn.guest.everParticipatedInPrisonBreak;
                resistance = pawn.guest.resistance;
                will = pawn.guest.will;
                ideoForConversion = pawn.guest.ideoForConversion;
                everEnslaved = pawn.guest.EverEnslaved;
                getRescuedThoughtOnUndownedBecauseOfPlayer = pawn.guest.getRescuedThoughtOnUndownedBecauseOfPlayer;
                recruitable = pawn.guest.recruitable;
            }

            if (pawn.records != null)
            {
                records = pawn.records.records.Clone();
                battleActive = pawn.records.BattleActive;
                battleExitTick = pawn.records.LastBattleTick;
            }
            savedHediffs = new List<Hediff>();
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (ModsConfig.AnomalyActive && hediff.def == HediffDefOf.Inhumanized)
                {
                    savedHediffs.Add(MakeCopy(hediff, pawn));
                }
            }

            if (captureOriginalInfo)
            {
                if (TryCaptureExistingOriginal(pawn) is false)
                {
                    OriginalRace = pawn.def;
                    OriginalGender = pawn.gender;
                    OriginalXenotypeDef = pawn.genes.xenotype;
                    OriginalXenotypeName = pawn.genes.xenotypeName;
                }
                if (pawn.story is not null)
                {
                    headType = pawn.story.headType;
                    bodyType = pawn.story.bodyType;
                    hairDef = pawn.story.hairDef;
                    hairColor = pawn.story.HairColor;
                    skinColorOverride = pawn.story.skinColorOverride;
                    skinColorBase = pawn.story.SkinColorBase;
                }
                if (pawn.style is not null)
                {
                    beardDef = pawn.style.beardDef;
                }
                if (pawn.genes is not null)
                {
                    xenotypeIcon = pawn.genes.iconDef;
                    endogenes = pawn.genes.Endogenes.Select(g => g.def).ToList();
                    xenogenes = pawn.genes.Xenogenes.Select(g => g.def).ToList();
                }
            }
            if (ModsConfig.RoyaltyActive && pawn.royalty != null)
            {
                royalTitles = pawn.royalty?.AllTitlesForReading.CopyList();
                favor = pawn.royalty.favor.CopyDict();
                heirs = pawn.royalty.heirs.CopyDict();
                bondedThings = new List<Thing>();
                foreach (Map map in Find.Maps)
                {
                    foreach (Thing thing in map.listerThings.AllThings)
                    {
                        CompBladelinkWeapon comp = thing.TryGetComp<CompBladelinkWeapon>();
                        if (comp != null && comp.CodedPawn == pawn)
                        {
                            bondedThings.Add(thing);
                        }
                    }
                    foreach (Apparel gear in pawn.apparel?.WornApparel)
                    {
                        CompBladelinkWeapon comp = gear.TryGetComp<CompBladelinkWeapon>();
                        if (comp != null && comp.CodedPawn == pawn)
                        {
                            bondedThings.Add(gear);
                        }
                    }
                    foreach (ThingWithComps gear in pawn.equipment?.AllEquipmentListForReading)
                    {
                        CompBladelinkWeapon comp = gear.TryGetComp<CompBladelinkWeapon>();
                        if (comp != null && comp.CodedPawn == pawn)
                        {
                            bondedThings.Add(gear);
                        }
                    }
                    foreach (Thing gear in pawn.inventory?.innerContainer)
                    {
                        CompBladelinkWeapon comp = gear.TryGetComp<CompBladelinkWeapon>();
                        if (comp != null && comp.CodedPawn == pawn)
                        {
                            bondedThings.Add(gear);
                        }
                    }
                }
                factionPermits = pawn.royalty.factionPermits.CopyList();
            }

            if (ModsConfig.IdeologyActive)
            {
                if (pawn.ideo != null && pawn.Ideo != null)
                {
                    ideo = pawn.Ideo;
                    certainty = pawn.ideo.Certainty;
                    previousIdeos = pawn.ideo.PreviousIdeos.CopyList();
                    joinTick = pawn.ideo.joinTick;

                    Precept_Role role = pawn.Ideo.GetRole(pawn);
                    if (role is Precept_RoleMulti multi)
                    {
                        precept_RoleMulti = multi;
                        precept_RoleSingle = null;
                    }
                    else if (role is Precept_RoleSingle single)
                    {
                        precept_RoleMulti = null;
                        precept_RoleSingle = single;
                    }
                }

                if (pawn.story?.favoriteColor != null)
                {
                    favoriteColor = pawn.story.favoriteColor;
                }
            }

            foreach (var modEntry in ModCompatibilityEntries)
            {
                modEntry.FetchData(pawn);
            }
        }

        protected static readonly List<string> activeModIdentifiers;

        private static readonly Dictionary<string, Type> modEntryTypeMap = new Dictionary<string, Type>
        {
            { "Mlie.SYRIndividuality", typeof(IndividualityCompatibilityEntry) },
            { "Community.Psychology.UnofficialUpdate", typeof(PsychologyCompatibilityEntry) },
            { "rim.job.world", typeof(RimJobWorldCompatibilityEntry) },
            { "vanillaexpanded.skills", typeof(VanillaSkillsExpandedCompatibilityEntry) },
            { "VanillaExpanded.VanillaAspirationsExpanded", typeof(AspirationsCompatibilityEntry) },
            { "VanillaExpanded.VAnomalyEInsanity", typeof(VanillaAnomalyInsanityCompatibilityEntry) }
        };

        static PawnData()
        {
            activeModIdentifiers = modEntryTypeMap
                .Where(entry => ModsConfig.IsActive(entry.Key))
                .Select(entry => entry.Key)
                .ToList();
        }

        protected bool initialized = false;
        public List<ModCompatibilityEntry> ModCompatibilityEntries
        {
            get
            {
                modCompatibilityEntries ??= new List<ModCompatibilityEntry>();
                if (!initialized)
                {
                    initialized = true;
                    foreach (var modIdentifier in activeModIdentifiers)
                    {
                        if (!modCompatibilityEntries.Any(existingEntry => existingEntry.modIdentifier == modIdentifier))
                        {
                            var entryType = modEntryTypeMap[modIdentifier];
                            var entry = (ModCompatibilityEntry)Activator.CreateInstance(entryType, modIdentifier);
                            modCompatibilityEntries.Add(entry);
                        }
                    }
                }
                return modCompatibilityEntries.Where(entry => entry.isActive).ToList();
            }
        }

        protected Hediff MakeCopy(Hediff hediff, Pawn pawn)
        {
            var newHediff = HediffMaker.MakeHediff(hediff.def, pawn, hediff.Part);
            newHediff.CopyFrom(hediff);
            return newHediff;
        }

        protected Name GetNameCopy(Name other)
        {
            var name = GetNameCopyInt(other);
            return name;
        }

        private Name GetNameCopyInt(Name other)
        {
            if (other is NameTriple nameTriple)
            {
                return new NameTriple(nameTriple.firstInt, nameTriple.nickInt, nameTriple.lastInt);
            }
            else if (other is NameSingle nameSingle)
            {
                return new NameSingle(nameSingle.nameInt);
            }
            if (other != null)
            {
                return other.Clone();
            }
            return null;
        }

        public void RestoreTo(Pawn pawn, PawnRestoreOptions options)
        {
            pawn.Name = GetNameCopy(name);
            if (kindDef != null)
            {
                pawn.kindDef = kindDef;
            }
            if (pawn.Faction != faction)
            {
                pawn.SetFaction(faction);
            }
            if (options.RestoreBody)
            {
                RestoreBody(pawn);
            }
            PawnComponentsUtility.CreateInitialComponents(pawn);
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn);
            if (isFactionLeader && options.RestoreFactionLeadership && pawn.Faction != null)
            {
                pawn.Faction.leader = pawn;
            }

            if (ModsConfig.RoyaltyActive && options.RestoreRoyalty)
            {
                RestoreRoyalty(pawn);
            }
            if (ModsConfig.IdeologyActive && options.RestoreIdeology)
            {
                RestoreIdeology(pawn);
            }

            RestoreThoughts(pawn);
            RestoreTraits(pawn, options);

            ResetRelationships(pawn);
            if (options.RestoreRelations)
            {
                ApplyRelations(pawn);
            }
            RestoreAbilities(pawn, options);
            AssignSkills(pawn);
            SetStoryData(pawn);
            SetGuestData(pawn);
            SetPawnSettings(pawn);
            SetHediffs(pawn);

            foreach (var modEntry in ModCompatibilityEntries)
            {
                modEntry.SetData(pawn);
            }
        }

        private void RestoreBody(Pawn pawn)
        {
            if (OriginalGender != Gender.None)
            {
                pawn.gender = OriginalGender;
            }
            pawn.ageTracker.AgeBiologicalTicks = ageBiologicalTicks;
            pawn.ageTracker.RecalculateLifeStageIndex();
            if (pawn.genes != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading.ToList())
                {
                    pawn.genes.RemoveGene(gene);
                }
                foreach (var endo in endogenes)
                {
                    pawn.genes.AddGene(endo, xenogene: false);
                }
                foreach (var xeno in xenogenes)
                {
                    pawn.genes.AddGene(xeno, xenogene: true);
                }
                if (OriginalXenotypeDef != null)
                {
                    pawn.genes.SetXenotypeDirect(OriginalXenotypeDef);
                }
                pawn.genes.xenotypeName = OriginalXenotypeName;
                if (xenotypeIcon != null)
                {
                    pawn.genes.iconDef = xenotypeIcon;
                }
            }
            if (headType != null)
            {
                pawn.story.headType = headType;
            }
            if (bodyType != null)
            {
                pawn.story.bodyType = bodyType;
            }
            if (hairDef != null)
            {
                pawn.story.hairDef = hairDef;
            }
            pawn.story.HairColor = hairColor;
            pawn.story.skinColorOverride = skinColorOverride;
            pawn.story.SkinColorBase = skinColorBase;
            if (beardDef != null)
            {
                pawn.style.beardDef = beardDef;
            }
        }

        private void SetHediffs(Pawn pawn)
        {
            foreach (var hediff in pawn.health.hediffSet.hediffs.ToList())
            {
                if (ModsConfig.AnomalyActive && hediff.def == HediffDefOf.Inhumanized)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }

            if (savedHediffs != null)
            {
                foreach (var hediff in savedHediffs)
                {
                    var newCopy = MakeCopy(hediff, pawn);
                    pawn.health.AddHediff(newCopy);
                }
            }
        }

        private void ResetRelationships(Pawn pawn)
        {
            foreach (var rel in pawn.relations.PotentiallyRelatedPawns.ToList())
            {
                foreach (var otherRelation in rel.relations.directRelations.ToList())
                {
                    if (otherRelation.otherPawn == pawn && otherRelation.def != PawnRelationDefOf.Overseer)
                    {
                        rel.relations.directRelations.Remove(otherRelation);
                    }
                }
            }

            var old = pawn.relations.directRelations.Where(x => x.def == PawnRelationDefOf.Overseer).ToList();
            pawn.relations = new Pawn_RelationsTracker(pawn)
            {
                everSeenByPlayer = everSeenByPlayer,
                canGetRescuedThought = canGetRescuedThought,
                relativeInvolvedInRescueQuest = relativeInvolvedInRescueQuest,
                nextMarriageNameChange = nextMarriageNameChange,
                hidePawnRelations = hidePawnRelations
            };
            pawn.relations.directRelations = old;
        }

        private void ApplyRelations(Pawn pawn)
        {
            if (relations != null)
            {
                foreach (var relation in relations)
                {
                    if (relation.otherPawn != null && pawn.relations.DirectRelationExists(relation.def, relation.otherPawn) is false)
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
                foreach (var relation in otherPawnRelations)
                {
                    if (relation.Value?.relations != null && relation.Value.relations.DirectRelationExists(relation.Key.def, pawn) is false)
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
            if (relatedPawns != null)
            {
                foreach (var otherPawn in relatedPawns)
                {
                    otherPawn?.needs?.mood?.thoughts?.situational.Notify_SituationalThoughtsDirty();
                }
            }
        }

        private void SetStoryData(Pawn pawn)
        {
            pawn.story.backstoriesCache = null;
            pawn.story.childhood = childhood;
            pawn.story.adulthood = adulthood;
            pawn.story.title = title;
            if (pawn.records is null)
            {
                pawn.records = new Pawn_RecordsTracker(pawn);
            }
            if (records != null)
            {
                pawn.records.records = records.Clone();
                pawn.records.battleActive = battleActive;
                pawn.records.battleExitTick = battleExitTick;
            }
        }

        private void SetPawnSettings(Pawn pawn)
        {
            if (pawn.playerSettings is null)
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }
            pawn.playerSettings.hostilityResponse = hostilityMode;
            pawn.playerSettings.allowedAreas = allowedAreas.CopyDict();
            pawn.playerSettings.medCare = medicalCareCategory;
            pawn.playerSettings.selfTend = selfTend;
            pawn.foodRestriction = new Pawn_FoodRestrictionTracker(pawn);
            pawn.foodRestriction.CurrentFoodPolicy = foodPolicy;
            pawn.outfits = new Pawn_OutfitTracker(pawn);
            pawn.outfits.curApparelPolicy = apparelPolicy;
            if (forcedHandler != null)
            {
                pawn.outfits.forcedHandler = new OutfitForcedHandler();
                pawn.outfits.forcedHandler.CopyFrom(forcedHandler);
            }
            pawn.drugs = new Pawn_DrugPolicyTracker(pawn);
            pawn.drugs.CurrentPolicy = drugPolicy;
            pawn.ageTracker.AgeChronologicalTicks = ageChronologicalTicks;
            pawn.timetable = new Pawn_TimetableTracker(pawn);

            if (times != null && times.Count == 24)
            {
                pawn.timetable.times = times.CopyList();
            }

            pawn.workSettings = new Pawn_WorkSettings(pawn);
            pawn.workSettings.priorities = new DefMap<WorkTypeDef, int>();
            pawn.Notify_DisabledWorkTypesChanged();
            if (priorities != null)
            {
                foreach (KeyValuePair<WorkTypeDef, int> priority in priorities)
                {
                    if (pawn.WorkTypeIsDisabled(priority.Key) is false)
                    {
                        pawn.workSettings.SetPriority(priority.Key, priority.Value);
                    }
                }
            }
        }

        private void SetGuestData(Pawn pawn)
        {
            if (pawn.guest is null)
            {
                pawn.guest = new Pawn_GuestTracker(pawn);
            }
            pawn.guest.guestStatusInt = guestStatusInt;
            pawn.guest.interactionMode = interactionMode;
            if (pawn.guest.interactionMode is null)
                pawn.guest.interactionMode = PrisonerInteractionModeDefOf.MaintainOnly;

            pawn.guest.slaveInteractionMode = slaveInteractionMode;
            pawn.guest.hostFactionInt = hostFactionInt;
            pawn.guest.joinStatus = joinStatus;
            pawn.guest.slaveFactionInt = slaveFactionInt;
            pawn.guest.lastRecruiterName = lastRecruiterName;
            pawn.guest.lastRecruiterOpinion = lastRecruiterOpinion;
            pawn.guest.hasOpinionOfLastRecruiter = hasOpinionOfLastRecruiter;
            pawn.guest.Released = releasedInt;
            pawn.guest.ticksWhenAllowedToEscapeAgain = ticksWhenAllowedToEscapeAgain;
            pawn.guest.spotToWaitInsteadOfEscaping = spotToWaitInsteadOfEscaping;
            pawn.guest.lastPrisonBreakTicks = lastPrisonBreakTicks;
            pawn.guest.everParticipatedInPrisonBreak = everParticipatedInPrisonBreak;
            pawn.guest.resistance = resistance;
            pawn.guest.will = will;
            pawn.guest.ideoForConversion = ideoForConversion;
            pawn.guest.everEnslaved = everEnslaved;
            pawn.guest.recruitable = recruitable;
            pawn.guest.getRescuedThoughtOnUndownedBecauseOfPlayer = getRescuedThoughtOnUndownedBecauseOfPlayer;
        }

        private void AssignSkills(Pawn pawn)
        {
            pawn.skills.skills.Clear();
            if (skills != null)
            {
                foreach (SkillRecord skill in skills)
                {
                    SkillRecord newSkill = new SkillRecord(pawn, skill.def)
                    {
                        passion = skill.passion,
                        levelInt = skill.levelInt,
                        xpSinceLastLevel = skill.xpSinceLastLevel,
                        xpSinceMidnight = skill.xpSinceMidnight
                    };
                    pawn.skills.skills.Add(newSkill);
                }
            }
        }

        private void RestoreAbilities(Pawn pawn, PawnRestoreOptions options)
        {
            var oldAbilities = pawn.abilities?.abilities.Select(x => x.def).ToList();
            pawn.abilities = new Pawn_AbilityTracker(pawn);
            if (oldAbilities != null)
            {
                foreach (var ability in oldAbilities)
                {
                    if (IsNaturalAbility(pawn, ability))
                    {
                        pawn.abilities.GainAbility(ability);
                    }
                    else if (options.RetainExtraAbility?.Invoke(ability) ?? false)
                    {
                        pawn.abilities.GainAbility(ability);
                    }
                }
            }
            var compAbilities = pawn.GetComp<VEF.Abilities.CompAbilities>();
            if (compAbilities?.LearnedAbilities != null)
            {
                compAbilities.LearnedAbilities.RemoveAll(x => IsNaturalAbility(pawn, x.def) is false && !(options.RetainLearnedAbility?.Invoke(x.def) ?? false));
            }

            if (options.RestorePsycasts)
            {
                if (ModsConfig.RoyaltyActive)
                {
                    var hediff_Psylink = pawn.GetMainPsylinkSource();
                    if (this.psylinkLevel.HasValue)
                    {
                        if (hediff_Psylink == null)
                        {
                            Hediff_Psylink_TryGiveAbilityOfLevel_Patch.suppress = true;
                            hediff_Psylink = HediffMaker.MakeHediff(HediffDefOf.PsychicAmplifier, pawn,
                                pawn.health.hediffSet.GetBrain()) as Hediff_Psylink;
                            pawn.health.AddHediff(hediff_Psylink);
                            Hediff_Psylink_TryGiveAbilityOfLevel_Patch.suppress = false;
                        }
                        var levelOffset = this.psylinkLevel.Value - hediff_Psylink.level;
                        hediff_Psylink.level = (int)Mathf.Clamp(hediff_Psylink.level + levelOffset, hediff_Psylink.def.minSeverity, hediff_Psylink.def.maxSeverity);
                    }
                    else if (hediff_Psylink != null)
                    {
                        pawn.health.RemoveHediff(hediff_Psylink);
                    }

                    pawn.psychicEntropy.currentEntropy = currentEntropy;
                    pawn.psychicEntropy.currentPsyfocus = currentPsyfocus;
                    pawn.psychicEntropy.limitEntropyAmount = limitEntropyAmount;
                    pawn.psychicEntropy.targetPsyfocus = targetPsyfocus;

                    if (VPE_PsycastAbilityImplant?.def != null)
                    {
                        pawn.health.hediffSet.hediffs.RemoveAll(x => x.def == VPE_PsycastAbilityImplant.def);
                        pawn.health.AddHediff(VPE_PsycastAbilityImplant);
                        Traverse.Create(VPE_PsycastAbilityImplant).Field("psylink").SetValue(hediff_Psylink);
                    }
                }

                if (abilities.NullOrEmpty() is false)
                {
                    foreach (var def in abilities)
                    {
                        pawn.abilities.GainAbility(def);
                    }
                }

                if (VEAbilities.NullOrEmpty() is false)
                {
                    if (compAbilities != null)
                    {
                        foreach (var ability in VEAbilities)
                        {
                            compAbilities.GiveAbility(ability);
                        }
                    }
                }
            }
        }

        private void RestoreTraits(Pawn pawn, PawnRestoreOptions options)
        {
            var traitsToRemove = pawn.story.traits.allTraits.Where(x => x.sourceGene is null).ToList();
            if (options.IgnoredTraitDefNames != null)
            {
                traitsToRemove.RemoveAll(x => options.IgnoredTraitDefNames.Contains(x.def.defName));
            }
            foreach (var trait in traitsToRemove)
            {
                pawn.story.traits.RemoveTrait(trait);
            }
            if (traits != null)
            {
                foreach (Trait trait in traits)
                {
                    if (options.IgnoredTraitDefNames != null && options.IgnoredTraitDefNames.Contains(trait.def.defName))
                    {
                        continue;
                    }
                    pawn.story.traits.GainTrait(new Trait(trait.def, trait.degree));
                }
            }
        }

        private void RestoreThoughts(Pawn pawn)
        {
            if (pawn.CanThink())
            {
                for (int num = pawn.needs.mood.thoughts.memories.Memories.Count - 1; num >= 0; num--)
                {
                    pawn.needs.mood.thoughts.memories.RemoveMemory(pawn.needs.mood.thoughts.memories.Memories[num]);
                }

                if (thoughts != null)
                {
                    FilterRestoredThoughts(pawn);
                    foreach (Thought_Memory thought in thoughts)
                    {
                        if (thought is Thought_MemorySocial && thought.otherPawn == null)
                        {
                            continue;
                        }
                        pawn.needs.mood.thoughts.memories.TryGainMemory(thought.Clone(), thought.otherPawn);
                    }
                }
                pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
            }
            pawn.mindState = new Pawn_MindState(pawn);
        }

        protected virtual void FilterRestoredThoughts(Pawn pawn)
        {
        }

        protected virtual bool CanClaimIdeoRoles(Pawn pawn) => true;

        protected virtual bool CanClaimSingleRole(IdeoRoleInstance chosenPawn) => chosenPawn?.pawn is null || chosenPawn.pawn.Dead;

        protected virtual bool CanCodeBondedWeapons(Pawn pawn) => true;

        private void RestoreIdeology(Pawn pawn)
        {
            pawn.ideo = new Pawn_IdeoTracker(pawn);
            if (ideo != null)
            {
                pawn.ideo.ideo = ideo;
                pawn.ideo.certaintyInt = certainty;
                pawn.ideo.previousIdeos = previousIdeos.CopyList();
                pawn.ideo.joinTick = joinTick;
                if (CanClaimIdeoRoles(pawn))
                {
                    if (precept_RoleMulti != null)
                    {
                        precept_RoleMulti.chosenPawns ??= new List<IdeoRoleInstance>();
                        precept_RoleMulti.chosenPawns.Add(new IdeoRoleInstance(precept_RoleMulti)
                        {
                            pawn = pawn
                        });
                        precept_RoleMulti.FillOrUpdateAbilities();
                    }
                    if (precept_RoleSingle != null && CanClaimSingleRole(precept_RoleSingle.chosenPawn))
                    {
                        precept_RoleSingle.chosenPawn = new IdeoRoleInstance(precept_RoleSingle)
                        {
                            pawn = pawn
                        };
                        precept_RoleSingle.FillOrUpdateAbilities();
                    }
                }
            }

            pawn.story.favoriteColor = favoriteColor;
        }

        private void RestoreRoyalty(Pawn pawn)
        {
            pawn.royalty = new Pawn_RoyaltyTracker(pawn);
            if (royalTitles != null)
            {
                pawn.royalty.titles = new List<RoyalTitle>();
                foreach (var title in royalTitles)
                {
                    var clone = title.Clone();
                    clone.pawn = pawn;
                    pawn.royalty.titles.Add(clone);
                }
            }
            if (heirs != null)
            {
                pawn.royalty.heirs = heirs.CopyDict();
            }

            if (favor != null)
            {
                pawn.royalty.favor = favor.CopyDict();
            }

            if (bondedThings != null && CanCodeBondedWeapons(pawn))
            {
                foreach (Thing bonded in bondedThings)
                {
                    CompBladelinkWeapon comp = bonded.TryGetComp<CompBladelinkWeapon>();
                    if (comp != null)
                    {
                        comp.CodeFor(pawn);
                    }
                }
            }

            if (factionPermits != null)
            {
                pawn.royalty.factionPermits = factionPermits.CopyList();
            }
        }

        public static bool IsNaturalAbility(Pawn pawn, Def def)
        {
            if (def is AbilityDef ability)
            {
                if (ModsConfig.BiotechActive && pawn.genes != null)
                {
                    foreach (var gene in pawn.genes.GenesListForReading)
                    {
                        if (gene.Active && gene.def.abilities.NullOrEmpty() is false && gene.def.abilities.Contains(ability))
                        {
                            return true;
                        }
                    }
                }
                if (pawn.IsCreepJoiner && pawn.creepjoiner.benefit != null
                    && pawn.creepjoiner.benefit.abilities.Contains(ability))
                {
                    return true;
                }
            }
            return false;
        }

        public void ChangeIdeo(Ideo newIdeo, float certainty)
        {
            if (ideo != null)
            {
                previousIdeos ??= new List<Ideo>();
                if (!previousIdeos.Contains(ideo))
                {
                    previousIdeos.Add(ideo);
                }
            }
            ideo = newIdeo;
            joinTick = Find.TickManager.TicksGame;
            this.certainty = certainty;
            precept_RoleSingle = null;
            precept_RoleMulti = null;
        }

        public virtual void ExposeData()
        {
            Scribe_Deep.Look(ref name, "name", new object[0]);
            Scribe_Values.Look(ref hostilityMode, "hostilityMode");
            Scribe_Collections.Look(ref allowedAreas, "allowedAreas", LookMode.Reference, LookMode.Reference, ref allowedAreasKeys, ref allowedAreasValues);
            if (Scribe.mode == LoadSaveMode.Saving && allowedAreas != null)
            {
                allowedAreas.RemoveAll((KeyValuePair<Map, Area> kvp) => kvp.Key == null || kvp.Value == null);
            }
            if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs && allowedAreas == null)
            {
                allowedAreas = new Dictionary<Map, Area>();
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs || Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Area refee = null;
                Scribe_References.Look(ref refee, "areaAllowed");
                if (refee != null && Find.AnyPlayerHomeMap != null)
                {
                    allowedAreas.Add(Find.AnyPlayerHomeMap, refee);
                }
            }
            Scribe_Values.Look(ref medicalCareCategory, "medicalCareCategory", MedicalCareCategory.NoCare, false);
            Scribe_Values.Look(ref selfTend, "selfTend", false, false);
            Scribe_Values.Look(ref ageBiologicalTicks, "ageBiologicalTicks", 0, false);
            Scribe_Values.Look(ref ageChronologicalTicks, "ageChronologicalTicks", 0, false);
            Scribe_Defs.Look(ref originalRace, "race");
            Scribe_Defs.Look(ref kindDef, "kindDef");
            Scribe_References.Look(ref apparelPolicy, "outfit", false);
            Scribe_Deep.Look(ref forcedHandler, "forcedHandler");
            Scribe_References.Look(ref foodPolicy, "foodPolicy", false);
            Scribe_References.Look(ref drugPolicy, "drugPolicy", false);

            Scribe_Collections.Look(ref times, "times", LookMode.Def);
            Scribe_Collections.Look(ref thoughts, "thoughts", LookMode.Deep);
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref isFactionLeader, "isFactionLeader", false, false);

            Scribe_Collections.Look(ref skills, "skills", LookMode.Deep);
            Scribe_Defs.Look(ref childhood, "childhood");
            Scribe_Defs.Look(ref adulthood, "adulthood");
            Scribe_Values.Look(ref title, "title", null, false);
            Scribe_Defs.Look(ref originalXenotypeDef, "originalXenotypeDef");
            Scribe_Values.Look(ref originalXenotypeName, "originalXenotypeName");
            Scribe_Defs.Look(ref headType, "headType");
            Scribe_Defs.Look(ref bodyType, "bodyType");
            Scribe_Defs.Look(ref hairDef, "hairDef");
            Scribe_Defs.Look(ref beardDef, "beardDef");
            Scribe_Values.Look(ref hairColor, "hairColor");
            Scribe_Values.Look(ref skinColorOverride, "skinColorOverride");
            Scribe_Values.Look(ref skinColorBase, "skinColorBase");
            Scribe_Defs.Look(ref xenotypeIcon, "xenotypeIcon");
            Scribe_Collections.Look(ref endogenes, "endogenes", LookMode.Def);
            Scribe_Collections.Look(ref xenogenes, "xenogenes", LookMode.Def);

            Scribe_Collections.Look(ref traits, "traits", LookMode.Deep);
            Scribe_Collections.Look(ref skills, "skills", LookMode.Deep);
            Scribe_Collections.Look(ref relations, "otherPawnRelations", LookMode.Deep);
            Scribe_Collections.Look(ref otherPawnRelations, "otherPawnRelations2", LookMode.Deep, LookMode.Reference, ref relationKeys, ref pawnValues);

            Scribe_Values.Look(ref everSeenByPlayer, "everSeenByPlayer");
            Scribe_Values.Look(ref canGetRescuedThought, "canGetRescuedThought", true);
            Scribe_References.Look(ref relativeInvolvedInRescueQuest, "relativeInvolvedInRescueQuest");
            Scribe_Values.Look(ref nextMarriageNameChange, "nextMarriageNameChange");
            Scribe_Values.Look(ref hidePawnRelations, "hidePawnRelations");
            Scribe_Collections.Look(ref relatedPawns, "relatedPawns", LookMode.Reference);
            Scribe_Collections.Look(ref priorities, "priorities", LookMode.Def, LookMode.Value);
            Scribe_Values.Look(ref guestStatusInt, "guestStatusInt");
            Scribe_Defs.Look(ref interactionMode, "interactionMode");
            Scribe_Defs.Look(ref slaveInteractionMode, "slaveInteractionMode");
            Scribe_References.Look(ref hostFactionInt, "hostFactionInt");
            Scribe_References.Look(ref slaveFactionInt, "slaveFactionInt");
            Scribe_Values.Look(ref joinStatus, "joinStatus");
            Scribe_Values.Look(ref lastRecruiterName, "lastRecruiterName");
            Scribe_Values.Look(ref lastRecruiterOpinion, "lastRecruiterOpinion");
            Scribe_Values.Look(ref hasOpinionOfLastRecruiter, "hasOpinionOfLastRecruiter");
            Scribe_Values.Look(ref lastRecruiterResistanceReduce, "lastRecruiterResistanceReduce");
            Scribe_Values.Look(ref releasedInt, "releasedInt");
            Scribe_Values.Look(ref ticksWhenAllowedToEscapeAgain, "ticksWhenAllowedToEscapeAgain");
            Scribe_Values.Look(ref spotToWaitInsteadOfEscaping, "spotToWaitInsteadOfEscaping");
            Scribe_Values.Look(ref lastPrisonBreakTicks, "lastPrisonBreakTicks");
            Scribe_Values.Look(ref everParticipatedInPrisonBreak, "everParticipatedInPrisonBreak");
            Scribe_Values.Look(ref resistance, "resistance");
            Scribe_Values.Look(ref will, "will");
            Scribe_References.Look(ref ideoForConversion, "ideoForConversion");
            Scribe_Values.Look(ref everEnslaved, "everEnslaved");
            Scribe_Values.Look(ref recruitable, "recruitable");
            Scribe_Values.Look(ref getRescuedThoughtOnUndownedBecauseOfPlayer, "getRescuedThoughtOnUndownedBecauseOfPlayer");

            Scribe_Deep.Look(ref records, "records");
            Scribe_References.Look(ref battleActive, "battleActive");
            Scribe_Values.Look(ref battleExitTick, "battleExitTick", 0);
            Scribe_Values.Look(ref originalGender, "gender");
            Scribe_Collections.Look(ref savedHediffs, "savedHediffs", LookMode.Deep);
            if (ModsConfig.RoyaltyActive)
            {
                Scribe_Collections.Look(ref favor, "favor", LookMode.Reference, LookMode.Value, ref favorKeys, ref favorValues);
                Scribe_Collections.Look(ref heirs, "heirs", LookMode.Reference, LookMode.Reference, ref heirsKeys, ref heirsValues);
                Scribe_Collections.Look(ref bondedThings, "bondedThings", LookMode.Reference);
                Scribe_Collections.Look(ref royalTitles, "royalTitles", LookMode.Deep);
                Scribe_Collections.Look(ref factionPermits, "permits", LookMode.Deep);
            }
            if (ModsConfig.IdeologyActive)
            {
                Scribe_References.Look(ref ideo, "ideo");
                Scribe_Collections.Look(ref previousIdeos, "previousIdeos", LookMode.Reference);
                Scribe_Defs.Look(ref favoriteColor, "favoriteColor");
                Scribe_Values.Look(ref joinTick, "joinTick");
                Scribe_Values.Look(ref certainty, "certainty");
                Scribe_References.Look(ref precept_RoleSingle, "precept_RoleSingle");
                Scribe_References.Look(ref precept_RoleMulti, "precept_RoleMulti");
            }

            Scribe_Values.Look(ref psylinkLevel, "psylinkLevel");
            Scribe_Collections.Look(ref abilities, "abilities", LookMode.Def);
            Scribe_Collections.Look(ref VEAbilities, "VEAbilities", LookMode.Def);
            Scribe_Deep.Look(ref VPE_PsycastAbilityImplant, "VPE_PsycastAbilityImplant");

            Scribe_Values.Look(ref currentEntropy, "currentEntropy");
            Scribe_Values.Look(ref currentPsyfocus, "currentPsyfocus");
            Scribe_Values.Look(ref limitEntropyAmount, "limitEntropyAmount");
            Scribe_Values.Look(ref targetPsyfocus, "targetPsyfocus");

            Scribe_Collections.Look(ref modCompatibilityEntries, "modCompatibilityEntries", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                times.CleanupList();
                thoughts.CleanupList();
                traits.CleanupList();
                relations.CleanupList();
                otherPawnRelations.CleanupDict();
                relatedPawns.CleanupList();
                skills.CleanupList();
                royalTitles.CleanupList();
                bondedThings.CleanupList();
                factionPermits.CleanupList();
                abilities.CleanupList();
                VEAbilities.CleanupList();
                previousIdeos.CleanupList();
                priorities.CleanupDict();
                favor.CleanupDict();
                heirs.CleanupDict();
                savedHediffs.CleanupList();
                modCompatibilityEntries.CleanupList();
                endogenes.CleanupList();
                xenogenes.CleanupList();
            }
        }

        protected List<Map> allowedAreasKeys;

        protected List<Area> allowedAreasValues;

        protected List<Faction> favorKeys = new List<Faction>();
        protected List<int> favorValues = new List<int>();

        protected List<Faction> heirsKeys = new List<Faction>();
        protected List<Pawn> heirsValues = new List<Pawn>();

        protected List<DirectPawnRelation> relationKeys = new List<DirectPawnRelation>();
        protected List<Pawn> pawnValues = new List<Pawn>();

        public override string ToString()
        {
            return name + " - " + faction;
        }
    }
}
