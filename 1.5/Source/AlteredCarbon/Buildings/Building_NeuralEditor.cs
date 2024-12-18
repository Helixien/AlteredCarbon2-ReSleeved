﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    public class CommandInfo
    {
        public string icon;
        public Action<LocalTargetInfo> action;
        public List<ResearchProjectDef> lockedProjects;
        public RecipeDef recipe;
        public string defaultLabelCancel, defaultDescCancel;
        public bool enableArchostacks;
        public Building building;
        public bool neuralConnectorIntegration;
    }

    public interface IMatrixConnectable
    {
        Building_NeuralMatrix ConnectedMatrix { get; }
    }

    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Building_NeuralEditor : Building_WorkTable, IMatrixConnectable
    {
        public bool autoRestoreIsEnabled = true;
        public Building_NeuralMatrix ConnectedMatrix => CompAffectedByFacilities.LinkedFacilitiesListForReading.OfType<Building_NeuralMatrix>().FirstOrDefault();
        private CompAffectedByFacilities _compAffectedByFacilities;
        public CompAffectedByFacilities CompAffectedByFacilities =>
            _compAffectedByFacilities ??= GetComp<CompAffectedByFacilities>();
        public bool Powered => this.compPower.PowerOn;
        private CompPowerTrader compPower;
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPower = GetComp<CompPowerTrader>();
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            foreach (var g in GetCommands<Command_ActionOnStack>(new CommandInfo
            {
                icon = "UI/Gizmos/WipeStack",
                action = InstallWipeStackBill,
                lockedProjects = new List<ResearchProjectDef> { AC_DefOf.AC_NeuralEditing },
                recipe = AC_DefOf.AC_WipeActiveNeuralStack,
                defaultLabelCancel = "AC.CancelStackReset".Translate(),
                defaultDescCancel = "AC.CancelStackResetDesc".Translate(),
            }))
            {
                yield return g;
            }

            foreach (var g in GetCommands<Command_ActionOnStack>(new CommandInfo
            {
                icon = "UI/Gizmos/DuplicateStack",
                action = InstallDuplicateStackBill,
                lockedProjects = new List<ResearchProjectDef> { AC_DefOf.AC_NeuralEditing },
                recipe = AC_DefOf.AC_DuplicateNeuralStack,
                defaultLabelCancel = "AC.CancelStackDuplication".Translate(),
                defaultDescCancel = "AC.CancelStackDuplicationDesc".Translate(),
                neuralConnectorIntegration = true
            }))
            {
                yield return g;
            }
            foreach (var g in GetCommands<Command_ActionOnStack>(new CommandInfo
            {
                icon = "UI/Gizmos/EditStack",
                action = InstallEditBill,
                lockedProjects = AC_DefOf.AC_EditActiveNeuralStack.researchPrerequisites,
                recipe = AC_DefOf.AC_EditActiveNeuralStack,
                defaultLabelCancel = "AC.CancelStackEdit".Translate(),
                defaultDescCancel = "AC.CancelStackEditDesc".Translate(),
                enableArchostacks = AC_Utils.editStacksSettings.enableArchostackEditing,
                neuralConnectorIntegration = true
            }))
            {
                yield return g;
            }


            foreach (var g in GetCommands<Command_ActionOnBiocoding>(new CommandInfo
            {
                icon = "UI/Gizmos/ResetBiocoding",
                action = InstallResetBiocodingBill,
                recipe = AC_DefOf.AC_ResetBiocodedThings,
                defaultLabelCancel = "AC.CancelBiocodingReset".Translate(),
                defaultDescCancel = "AC.CancelBiocodingResetDesc".Translate(),
            }))
            {
                yield return g;
            }
            foreach (var g in GetCommands<Command_ActionOnPersona>(new CommandInfo
            {
                icon = "UI/Gizmos/UnbondWeapon",
                action = InstallUnboundPersonaBill,
                recipe = AC_DefOf.AC_UnboundPersona,
                defaultLabelCancel = "AC.CancelUnbondingPersona".Translate(),
                defaultDescCancel = "AC.CancelUnbondingPersonaDesc".Translate(),
            }))
            {
                yield return g;
            }
            var matrix = ConnectedMatrix;
            if (matrix != null)
            {
                foreach (var g in matrix.GetManageMatrix())
                {
                    yield return g;
                }
            }
        }

        private IEnumerable<Gizmo> GetCommands<T>(CommandInfo info) where T : Command_ActionOnThing
        {
            var command = (T)Activator.CreateInstance(typeof(T), new object[]
            { this, info });
            command.defaultLabel = info.recipe.label;
            command.defaultDesc = info.recipe.description;
            info.building = this;
            yield return command;
            var cancel = GetCancelGizmo(info);
            if (cancel != null)
            {
                yield return cancel;
            }
        }
        public Command_Action GetCancelGizmo(CommandInfo info)
        {
            return GetCancelGizmo(info.defaultLabelCancel, info.defaultDescCancel, info.recipe);
        }

        public Command_Action GetCancelGizmo(string label, string desc, RecipeDef recipe)
        {
            var bills = this.billStack.Bills.Where(x => x.recipe.label == recipe.label).ToList();
            if (bills.Any())
            {
                return new Command_Action
                {
                    defaultLabel = label,
                    defaultDesc = desc,
                    activateSound = SoundDefOf.Tick_Tiny,
                    icon = UIHelper.CancelIcon,
                    action = delegate ()
                    {
                        foreach (var bill in bills)
                        {
                            this.billStack.Delete(bill);
                        }
                    }
                };
            }
            return null;
        }

        public bool CanAddOperationOn(Thing thing)
        {
            var bill = this.billStack.Bills.OfType<Bill_OperateOnThing>().Where(x => x.targetThing == thing).FirstOrDefault();
            if (bill != null)
            {
                if (bill.recipe == AC_DefOf.AC_WipeActiveNeuralStack)
                {
                    Messages.Message("AC.AlreadyOrderedToWipeStack".Translate(), MessageTypeDefOf.CautionInput);
                }
                else if (bill.recipe == AC_DefOf.AC_EditActiveNeuralStack || bill.recipe == AC_DefOf.AC_EditActiveNeuralStackPawn)
                {
                    Messages.Message("AC.AlreadyOrderedToEditStack".Translate(), MessageTypeDefOf.CautionInput);
                }
                else if (bill.recipe == AC_DefOf.AC_DuplicateNeuralStack || bill.recipe == AC_DefOf.AC_DuplicateNeuralStackPawn)
                {
                    Messages.Message("AC.AlreadyOrderedToDuplicateStack".Translate(), MessageTypeDefOf.CautionInput);
                }
                else if (bill.recipe == AC_DefOf.AC_ResetBiocodedThings)
                {
                    Messages.Message("AC.AlreadyOrderedToResetBiocodedThing".Translate(), MessageTypeDefOf.CautionInput);
                }
                else if (bill.recipe == AC_DefOf.AC_UnboundPersona)
                {
                    Messages.Message("AC.AlreadyOrderedToUnboundPersonaThing".Translate(), MessageTypeDefOf.CautionInput);
                }
                return false;
            }
            return true;
        }

        public void InstallResetBiocodingBill(LocalTargetInfo x)
        {
            if (CanAddOperationOn(x.Thing))
            {
                var thing = x.Thing;
                billStack.AddBill(new Bill_OperateOnThing(thing, AC_DefOf.AC_ResetBiocodedThings, null));
            }
        }
        public void InstallUnboundPersonaBill(LocalTargetInfo x)
        {
            if (CanAddOperationOn(x.Thing))
            {
                var thing = x.Thing;
                billStack.AddBill(new Bill_OperateOnThing(thing, AC_DefOf.AC_UnboundPersona, null));
            }
        }

        public void InstallWipeStackBill(LocalTargetInfo x)
        {
            HandleStackBill(x, "AC.WipingFriendlyStackWarning", () =>
            {
                billStack.AddBill(new Bill_OperateOnStack(x.Thing, AC_DefOf.AC_WipeActiveNeuralStack, null));
            });
        }

        public void InstallDuplicateStackBill(LocalTargetInfo x)
        {
            HandleStackBill(x, "AC.DuplicatingFriendlyStackWarning", () =>
            {
                billStack.AddBill(new Bill_OperateOnStack(x.Thing, x.Thing is Pawn
                    ? AC_DefOf.AC_DuplicateNeuralStackPawn : AC_DefOf.AC_DuplicateNeuralStack, null));
                PostInstall(x);
            });
        }

        public void PostInstall(LocalTargetInfo x)
        {
            if (x.Thing is Pawn pawn && ConnectedMatrix != null)
            {
                var connectors = ConnectedMatrix.LinkedBuildings.OfType<Building_NeuralConnector>();
                foreach (var connector in connectors)
                {
                    if (connector.SelectedPawn is null && connector.CanAcceptPawn(pawn))
                    {
                        connector.SelectPawn(pawn);
                        break;
                    }
                }
            }
        }

        private void InstallEditBill(LocalTargetInfo x)
        {
            HandleStackBill(x, "AC.EditingFriendlyStackWarning", () =>
            {
                Find.WindowStack.Add(new Window_StackEditor(this, x.Thing));
            });
        }

        private void HandleStackBill(LocalTargetInfo x, string warningKey, Action action)
        {
            var neuralData = x.Thing.GetNeuralData();
            if (neuralData != null && CanAddOperationOn(x.Thing))
            {
                if (neuralData.Friendly)
                {
                    Find.WindowStack.Add(new Dialog_MessageBox(warningKey.Translate(), "Cancel".Translate(), null,
                    "Confirm".Translate(), delegate ()
                    {
                        action.Invoke();
                    }, null, false, null, null));
                }
                else
                {
                    action.Invoke();
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.autoRestoreIsEnabled, "autoRestoreIsEnabled", true);
        }
    }
}