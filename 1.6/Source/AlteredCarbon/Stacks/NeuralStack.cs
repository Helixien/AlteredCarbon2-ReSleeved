using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    [HotSwappable]
    public class NeuralStack : ThingWithNeuralData, INeedlecastable
    {
        public override Graphic Graphic
        {
            get
            {
                NeuralData neuralData = NeuralData;
                if (neuralData.ContainsData)
                {
                    var archoPath = "Things/Item/ArchoStacks/";
                    var stackPath = "Things/Item/NeuralStacks/";
                    if (this.StyleDef != null)
                    {
                        stackPath = this.StyleDef.graphicData.texPath.Replace("FriendlyStack", "");
                    }
                    if (neuralData.guestStatusInt == GuestStatus.Slave)
                    {
                        return GetStackGraphic(ref slaveGraphic, ref slaveGraphicData,
                            archoPath + "SlaveArchoStack", stackPath + "SlaveStack");
                    }
                    else if (neuralData.faction == Faction.OfPlayer)
                    {
                        return GetStackGraphic(ref friendlyGraphic, ref friendlyGraphicData,
                            archoPath + "FriendlyArchoStack", stackPath + "FriendlyStack");
                    }
                    else if (neuralData.faction != null && neuralData.faction.HostileTo(Faction.OfPlayer))
                    {
                        return GetStackGraphic(ref hostileGraphic, ref hostileGraphicData,
                            archoPath + "HostileArchoStack", stackPath + "HostileStack");
                    }
                    else
                    {
                        return GetStackGraphic(ref strangerGraphic, ref strangerGraphicData,
                            archoPath + "NeutralArchoStack", stackPath + "NeutralStack");
                    }
                }
                else
                {
                    return base.Graphic;
                }
            }
        }

        private Graphic GetStackGraphic(ref Graphic graphic, ref GraphicData graphicData, string archotechStackTexPath, string stackTexPath)
        {
            if (graphic is null)
            {
                if (graphicData is null)
                {
                    var path = this.IsArchotechStack ? archotechStackTexPath : stackTexPath;
                    graphicData = GetGraphicDataWithOtherPath(path);
                }
                graphic = graphicData.GraphicColoredFor(this);
            }
            return graphic;
        }

        public void ResetStackGraphics()
        {
            this.slaveGraphic = this.friendlyGraphic = this.hostileGraphic = this.strangerGraphic = null;
            this.slaveGraphicData = this.friendlyGraphicData = this.hostileGraphicData = this.strangerGraphicData = null;
        }

        public override string LabelNoCount
        {
            get
            {
                var label = base.LabelNoCount;
                if (this.IsActiveStack)
                {
                    label += " (" + this.NeuralData.PawnNameColored.ToStringSafe() + ")";
                }
                return label;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            NeuralData.AppendInfoStack(stringBuilder);
            stringBuilder.Append(base.GetInspectString());
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void Tick()
        {
            base.Tick();
            if (this.Spawned && this.IsArchotechStack)
            {
                var edifice = this.Position.GetEdifice(Map);
                if (edifice != null && this.Position.Walkable(Map) is false)
                {
                    var map = this.Map;
                    var pos = this.Position;
                    this.DeSpawn();
                    GenPlace.TryPlaceThing(this, pos, map, ThingPlaceMode.Near);
                    FleckMaker.Static(this.Position, this.Map, AC_DefOf.PsycastAreaEffect, 3f);
                }

                if (hediff?.skipAbility != null && hediff.pawn.health.hediffSet.hediffs.Contains(hediff))
                {
                    hediff.pawn.positionInt = Position;
                    hediff.pawn.mapIndexOrState = (sbyte)Find.Maps.IndexOf(Map);
                }
                else
                {
                    casterPawn = NeuralData.DummyPawn;
                    casterPawn.positionInt = Position;
                    casterPawn.mapIndexOrState = (sbyte)Find.Maps.IndexOf(Map);
                    PawnComponentsUtility.AddComponentsForSpawn(casterPawn);
                    PawnComponentsUtility.AddAndRemoveDynamicComponents(casterPawn);
                    hediff = casterPawn.health.AddHediff(AC_DefOf.AC_ArchotechStack) as Hediff_NeuralStack;
                }
                casterPawn.jobs.JobTrackerTick();
            }
        }

        public bool IsActiveStack => this.def == AC_DefOf.AC_ActiveNeuralStack || this.def == AC_DefOf.AC_ActiveArchotechStack;
        public bool IsArchotechStack => this.def == AC_DefOf.AC_EmptyArchotechStack || this.def == AC_DefOf.AC_ActiveArchotechStack;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            try
            {
                if (!respawningAfterLoad && !NeuralData.ContainsData && IsActiveStack)
                {
                    CreateInnerPawn();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception spawning " + this + ": " + ex);
            }
            if (def == AC_DefOf.AC_ActiveNeuralStack && stackCount != 1)
            {
                stackCount = 1;
            }
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public void CreateInnerPawn()
        {
            GenerateNeural();
            NeuralData.stackGroupID = AlteredCarbonManager.Instance.GetStackGroupID(this);
            AlteredCarbonManager.Instance.RegisterStack(this);
        }

        public TargetingParameters ForPawn()
        {
            TargetingParameters targetingParameters = new TargetingParameters
            {
                canTargetPawns = true,
                validator = (TargetInfo x) => x.Thing is Pawn pawn
            };
            return targetingParameters;
        }

        private Pawn casterPawn;
        private Hediff_NeuralStack hediff;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (AC_Utils.stackRecipesByDef.TryGetValue(this.def, out var installInfo))
            {
                var installStack = new Command_Action
                {
                    defaultLabel = installInfo.installLabel,
                    defaultDesc = installInfo.installDesc,
                    activateSound = SoundDefOf.Tick_Tiny,
                    icon = installInfo.installIcon,
                    action = delegate ()
                    {
                        Find.Targeter.BeginTargeting(ForPawn(), delegate (LocalTargetInfo x)
                        {
                            if (AC_Utils.CanImplantStackTo(AC_Utils.stackRecipesByDef[this.def].recipe.addsHediff, x.Pawn, this, true))
                            {
                                InstallStackRecipe(x.Pawn, installInfo.recipe);
                            }
                        });
                    }
                };
                var assignedPawnForInstalling = AssignedPawnForInstalling;
                if (assignedPawnForInstalling != null && this.stackCount == 1)
                {
                    installStack.Disable("AC.AlreadyMarkedForInstalling".Translate());
                }
                yield return installStack;
                if (IsArchotechStack && IsActiveStack)
                {
                    hediff.pawn = NeuralData.DummyPawn;
                    hediff.skipAbility.pawn = hediff.pawn;
                    NeuralData.hostPawn = null;
                    hediff.NeuralData = NeuralData;
                    hediff.skipAbility.archoStackForAbility = this;
                    var gizmo = hediff.skipAbility.GetGizmo() as VEF.Abilities.Command_Ability;
                    var archoSkip = new VEF.Abilities.Command_Ability(hediff.pawn, hediff.skipAbility)
                    {
                        defaultLabel = gizmo.defaultLabel,
                        defaultDesc = gizmo.defaultDesc,
                        activateSound = gizmo.activateSound,
                        icon = gizmo.icon,
                        action = delegate ()
                        {
                            Find.Targeter.BeginTargeting(ForPawn(), delegate (LocalTargetInfo x)
                            {
                                if (AC_Utils.CanImplantStackTo(AC_Utils.stackRecipesByDef[this.def].recipe.addsHediff,
                                    x.Pawn, this, true) && Ability_ArchotechStackSkip.Validate(x, true))
                                {
                                    hediff.pawn.mapIndexOrState = (sbyte)Find.Maps.IndexOf(Map);
                                    hediff.pawn.pather = new Verse.AI.Pawn_PathFollower(hediff.pawn);
                                    hediff.skipAbility.CreateCastJob(x);
                                }
                            });
                        }
                    };
                    yield return archoSkip;
                }
                if (DebugSettings.ShowDevGizmos)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "DEV: Instant implant",
                        action = delegate
                        {
                            Find.Targeter.BeginTargeting(new TargetingParameters
                            {
                                canTargetHumans = true,
                                canTargetPawns = true,
                                canTargetAnimals = false,
                                canTargetCorpses = false,
                                canTargetMechs = false,
                                validator = (TargetInfo x) => x.Thing is Pawn pawn

                            }, delegate (LocalTargetInfo x)
                            {
                                if (AC_Utils.CanImplantStackTo(installInfo.recipe.addsHediff, x.Pawn, this, true))
                                {
                                    Recipe_InstallNeuralStack.ApplyNeuralStack(installInfo.recipe.addsHediff, x.Pawn, x.Pawn.GetNeck(), this);
                                    var stack = this.SplitOff(1) as NeuralStack;
                                    stack.DestroyNoKill();
                                }
                            });
                        }
                    };
                    if (IsActiveStack)
                    {
                        yield return new Command_Action
                        {
                            defaultLabel = "DEV: Duplicate stack",
                            action = () =>
                            {
                                Recipe_DuplicateNeuralStack.DuplicateStack(Position, Map, NeuralData);
                            }
                        };
                        yield return new Command_Action
                        {
                            defaultLabel = "DEV: Duplicate pawn",
                            action = () =>
                            {
                                var copy = AC_Utils.ClonePawn(NeuralData.DummyPawn);
                                var copyStack = Recipe_DuplicateNeuralStack.DuplicateStack(Position, Map, NeuralData);
                                Recipe_InstallActiveNeuralStack.ApplyNeuralStack(installInfo.recipe.addsHediff, copy, copy.GetNeck(), copyStack);
                                copyStack.DestroyNoKill();
                                GenPlace.TryPlaceThing(copy, Position, Map, ThingPlaceMode.Near);
                            }
                        };
                    }
                }
            }

            if (this.IsActiveStack)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "AC.AutoLoad".Translate(),
                    defaultDesc = "AC.AutoLoadDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Gizmos/AutoLoadStack"),
                    isActive = () => autoLoad,
                    toggleAction = delegate
                    {
                        autoLoad = !autoLoad;
                    }
                };
            }
        }


        private Pawn assignedPawnForInstalling;

        public Pawn AssignedPawnForInstalling
        {
            get
            {
                if (assignedPawnForInstalling is not null)
                {
                    if (assignedPawnForInstalling.Dead is false && assignedPawnForInstalling.Destroyed is false
                        && assignedPawnForInstalling.BillStack.bills.Any(x => x is Bill_InstallStack install && install.stackToInstall == this))
                    {
                        return assignedPawnForInstalling;
                    }
                    assignedPawnForInstalling = null;
                }
                return null;
            }
        }
        public void InstallStackRecipe(Pawn patient, RecipeDef recipe)
        {
            if (patient.HasNeuralStack(out var stackHediff) && (stackHediff.def == recipe.addsHediff || stackHediff.def == AC_DefOf.AC_ArchotechStack))
            {
                if (stackHediff.def != recipe.addsHediff)
                {
                    Messages.Message("AC.PawnStackCannotDowngrade".Translate(patient.Named("PAWN")), MessageTypeDefOf.CautionInput);
                }
                else if (stackHediff.def == AC_DefOf.AC_NeuralStack)
                {
                    Messages.Message("AC.PawnAlreadyHasStack".Translate(patient.Named("PAWN")), MessageTypeDefOf.CautionInput);
                }
                else
                {
                    Messages.Message("AC.PawnAlreadyHasArchotechStack".Translate(patient.Named("PAWN")), MessageTypeDefOf.CautionInput);
                }
            }
            else if (recipe.Worker.GetPartsToApplyOn(patient, recipe).FirstOrDefault() is null)
            {
                Messages.Message("AC.CannotInstallStackNeckIsMissingOrAnotherImplantInstalled".Translate(), MessageTypeDefOf.CautionInput);
            }
            else
            {
                patient.BillStack.Bills.RemoveAll(x => x is Bill_InstallStack);
                if (patient.IsAndroid())
                {
                    recipe = ModCompatibility.GetRecipeForAndroid(recipe);
                }
                Bill_InstallStack bill_Medical = new Bill_InstallStack(recipe, this);
                patient.BillStack.AddBill(bill_Medical);
                bill_Medical.Part = recipe.Worker.GetPartsToApplyOn(patient, recipe).First();
                var compForbiddable = this.GetComp<CompForbiddable>();
                if (compForbiddable.Forbidden)
                {
                    compForbiddable.Forbidden = false;
                }
                if (recipe.conceptLearned != null)
                {
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
                }
                Map map = patient.Map;
                if (!map.mapPawns.FreeColonists.Any((Pawn col) => recipe.PawnSatisfiesSkillRequirements(col)))
                {
                    Bill.CreateNoPawnsWithSkillDialog(recipe);
                }
                if (!patient.InBed() && patient.RaceProps.IsFlesh)
                {
                    if (patient.RaceProps.Humanlike)
                    {
                        if (!map.listerBuildings.allBuildingsColonist.Any((Building x) => x is Building_Bed && RestUtility.CanUseBedEver(patient, x.def) && ((Building_Bed)x).Medical))
                        {
                            Messages.Message("MessageNoMedicalBeds".Translate(), patient, MessageTypeDefOf.CautionInput, historical: false);
                        }
                    }
                    else if (!map.listerBuildings.allBuildingsColonist.Any((Building x) => x is Building_Bed && RestUtility.CanUseBedEver(patient, x.def)))
                    {
                        Messages.Message("MessageNoAnimalBeds".Translate(), patient, MessageTypeDefOf.CautionInput, historical: false);
                    }
                }
                if (patient.Faction != null && !patient.Faction.Hidden && !patient.Faction.HostileTo(Faction.OfPlayer) && recipe.Worker.IsViolationOnPawn(patient, bill_Medical.Part, Faction.OfPlayer))
                {
                    Messages.Message("MessageMedicalOperationWillAngerFaction".Translate(patient.HomeFaction), patient, MessageTypeDefOf.CautionInput, historical: false);
                }
                if (!HealthCardUtility.CanDoRecipeWithMedicineRestriction(patient, recipe))
                {
                    Messages.Message("MessageWarningNoMedicineForRestriction".Translate(patient.Named("PAWN"), patient.playerSettings.medCare.GetLabel().Named("RESTRICTIONLABEL")), patient, MessageTypeDefOf.CautionInput, historical: false);
                }
                recipe.Worker.CheckForWarnings(patient);
                assignedPawnForInstalling = patient;
            }
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (Destroyed)
            {
                KillInnerPawn();
            }
        }

        public void DestroyNoKill()
        {
            Thing.allowDestroyNonDestroyable = true;
            this.dontKillThePawn = true;
            this.Destroy();
            Thing.allowDestroyNonDestroyable = false;
        }

        public void DestroyWithConfirmation()
        {
            Find.WindowStack.Add(new Dialog_MessageBox("AC.DestroyStackConfirmation".Translate(),
                    "No".Translate(), null,
                    "Yes".Translate(), delegate ()
            {
                Destroy();
            }, null, false, null, null));
        }

        public bool dontKillThePawn = false;
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (this.IsArchotechStack && allowDestroyNonDestroyable is false)
            {
                return;
            }
            if (Needlecasting)
            {
                needleCastingInto.EndNeedlecasting(ConnectStatus.StackDestroyed);
            }
            base.Destroy(mode);
            if (NeuralData.ContainsData && dontKillThePawn is false)
            {
                KillInnerPawn();
            }
            var group = NeuralData.StackGroupData;
            group.copiedStacks.Remove(this);
            if (group.originalStack == this)
            {
                group.originalStack = null;
            }
        }

        public void KillInnerPawn(bool affectFactionRelationship = false, Pawn affecter = null)
        {
            if (NeuralData.ContainsData)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer));
                NeuralData.OverwritePawn(pawn);
                if (affectFactionRelationship)
                {
                    NeuralData.faction.TryAffectGoodwillWith(affecter.Faction, NeuralData.faction.GoodwillToMakeHostile(affecter.Faction), canSendMessage: true, reason: AC_DefOf.AC_ErasedStackEvent);
                }
                if (NeuralData.isFactionLeader)
                {
                    pawn.Faction.leader = pawn;
                }
                try
                {
                    pawn.DisableKillEffects();
                    pawn.Kill(null);
                    pawn.EnableKillEffects();
                    if (pawn.Faction == Faction.OfPlayer)
                    {
                        var label = "Death".Translate() + ": " + pawn.LabelShortCap;
                        var desc = "AC.PawnDiedOfErasingStack".Translate(pawn.LabelShortCap, pawn.Named("PAWN"));
                        Find.LetterStack.ReceiveLetter(label, desc, LetterDefOf.Death, pawn, null, null);
                    }
                }
                catch { }
                NeuralData = null;
            }
        }
        public void EmptyStack(Pawn affecter, bool affectFactionRelationship = false)
        {
            Thing newStack = ThingMaker.MakeThing(this.GetEmptyStackVariant());
            GenPlace.TryPlaceThing(newStack, affecter.Position, affecter.Map, ThingPlaceMode.Near);
            var style = this.StyleDef;
            if (style != null)
            {
                var emptyStyle = DefDatabase<ThingStyleDef>.GetNamedSilentFail(style.defName.Replace("Active", "Empty"));
                if (emptyStyle != null)
                {
                    style = emptyStyle;
                }
                newStack.StyleDef = style;
            }
            if (NeuralData.hostPawn != null)
            {
                AlteredCarbonManager.Instance.StacksIndex.Remove(NeuralData.PawnID);
            }
            KillInnerPawn(affectFactionRelationship, affecter);
            foreach (Pawn otherPawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists)
            {
                otherPawn?.needs?.mood?.thoughts.memories.TryGainMemory(AC_DefOf.AC_ErasedStack);
            }
            Destroy();
        }

        public Hediff_RemoteStack needleCastingInto;
        public Hediff_RemoteStack NeedleCastingInto => needleCastingInto;
        public bool Needlecasting => needleCastingInto != null;

        public Dictionary<Pawn, ConnectStatus> GetAllConnectablePawns()
        {
            return AC_Utils.GetAllConnectablePawnsFor(this);
        }

        public void NeedlecastTo(LocalTargetInfo target)
        {
            var pawnTarget = target.Pawn;
            needleCastingInto = pawnTarget.GetRemoteStack();
            needleCastingInto.Needlecast(this);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref casterPawn, "casterPawn");
            Scribe_References.Look(ref hediff, "hediff");
            Scribe_References.Look(ref needleCastingInto, "needleCastingInto");
            Scribe_References.Look(ref assignedPawnForInstalling, "assignedPawnForInstalling");
        }
    }
}
