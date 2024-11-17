using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    [HotSwappable]
    public class Hediff_RemoteStack : Hediff_Implant
    {
        public NeuralData originalPawnData;
        public Hediff_NeuralStack sourceHediff;
        public NeuralStack sourceStack;
        private bool wasEmptySleeve;
        public INeedlecastable Source
        {
            get
            {
                if (sourceStack != null)
                {
                    return sourceStack;
                }
                return sourceHediff;
            }
        }

        public static bool lookingForConnectStatus;
        public ConnectStatus GetConnectStatus(INeedlecastable needlecastable)
        {
            lookingForConnectStatus = true;
            var excludedHediffs = new List<HediffDef>
            {
                AC_DefOf.AC_EmptySleeve, AC_DefOf.AC_CryptoStasis
            };
            var removedHedifs = new List<Hediff>();
            foreach (var hediffDef in excludedHediffs)
            {
                var hediff = pawn.GetHediff(hediffDef);
                if (hediff != null)
                {
                    removedHedifs.Add(hediff);
                    pawn.health.hediffSet.hediffs.Remove(hediff);
                }
            }
            pawn.health.capacities.Notify_CapacityLevelsDirty();
            var status = GetStatusInternal(needlecastable);
            foreach (var removed in removedHedifs)
            {
                pawn.health.hediffSet.hediffs.Add(removed);
            }
            pawn.health.capacities.Notify_CapacityLevelsDirty();
            lookingForConnectStatus = false;
            return status;
        }

        private ConnectStatus GetStatusInternal(INeedlecastable needlecastable)
        {
            if (pawn.Faction != Faction.OfPlayer && pawn.IsPrisonerOfColony is false
                && pawn.IsSlaveOfColony is false && pawn.IsEmptySleeve() is false)
            {
                return ConnectStatus.SwitchedFaction;
            }
            if (pawn.health.capacities.CanBeAwake is false)
            {
                return ConnectStatus.LowConscioussness;
            }
            if (pawn.Dead)
            {
                return ConnectStatus.TargetDead;
            }
            else if (pawn.IsLost())
            {
                return ConnectStatus.Lost;
            }
            if (needlecastable is Hediff_NeuralStack hediff)
            {
                if (hediff.pawn.Dead)
                {
                    return ConnectStatus.CasterDead;
                }
                else if (hediff.pawn.IsLost())
                {
                    return ConnectStatus.Lost;
                }
            }
            else if (needlecastable is NeuralStack stack)
            {
                if (stack.ParentHolder is not CompNeuralCache comp)
                {
                    return ConnectStatus.ConnectionDisrupted;
                }
                else if (stack.Destroyed)
                {
                    return ConnectStatus.ConnectionDisrupted;
                }
                else
                {
                    var matrix = comp.parent.GetConnectedMatrix();
                    if (matrix is null || stack.NeuralData.trackedToMatrix != matrix)
                    {
                        return ConnectStatus.ConnectionDisrupted;
                    }
                }
            }
            if (Needlecasted)
            {
                var matrix = GetSourceMatrix();
                if (matrix is null || needlecastable.NeuralData.trackedToMatrix != matrix)
                {
                    return ConnectStatus.NoConnectedMatrix;
                }
                else if (matrix.Powered is false)
                {
                    return ConnectStatus.MatrixHasNoPower;
                }
            }

            if (!InNeedlecastingRange(needlecastable.ThingHolder))
            {
                return ConnectStatus.OutOfRange;
            }

            return ConnectStatus.Connectable;
        }

        public bool InNeedlecastingRange(Thing source)
        {
            var matrices = Find.Maps.SelectMany(x => x.listerThings.GetThingsOfType<Building_NeuralMatrix>()).ToList();
            var pawnTile = GetPawnTile();
            if (pawnTile != -1)
            {
                foreach (var mat in matrices)
                {
                    if (mat.Powered)
                    {
                        var rangeCovered = mat.NeedleCastRange();
                        var distance = Find.WorldGrid.ApproxDistanceInTiles(mat.Tile, pawnTile);
                        var distance2 = Find.WorldGrid.ApproxDistanceInTiles(mat.Tile, source.Tile);
                        if (distance <= rangeCovered && distance2 <= rangeCovered)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return true;
        }

        public int GetPawnTile()
        {
            if (ModCompatibility.VehicleFrameworkIsActive && ModCompatibility.TryGetVehicleTile(pawn, out var tile))
            {
                return tile;
            }
            return pawn.Tile;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref originalPawnData, "originalPawnData");
            Scribe_References.Look(ref sourceHediff, "sourceHediff");
            Scribe_References.Look(ref sourceStack, "sourceStack");
            Scribe_Values.Look(ref wasEmptySleeve, "wasEmptySleeve");
            Scribe_Deep.Look(ref pawnOwnership, "pawnOwnership", pawn);
            if (Source is Hediff_NeuralStack stack)
            {
                Scribe_Deep.Look(ref sourceOwnership, "sourceOwnership", stack.pawn);
            }
        }

        public bool Needlecasted => sourceHediff != null || sourceStack != null;

        public override void PostRemoved()
        {
            base.PostRemoved();
            if (Needlecasted)
            {
                EndNeedlecasting(ConnectStatus.OutOfRange);
            }
        }

        public void Needlecast(INeedlecastable needlecastable)
        {
            AC_Utils.DebugMessage("Starting needlecasting from " + needlecastable + " to " + pawn.GetFullName());
            HediffSet_DirtyCache_Patch.looking = true;
            wasEmptySleeve = pawn.IsEmptySleeve();
            if (pawn.jobs?.curDriver != null && pawn.jobs.curDriver.asleep is false 
                && pawn.CurrentBed() is not Building_SleeveCasket)
            {
                pawn.jobs.StopAll();
            }
            if (wasEmptySleeve)
            {
                Messages.Message("AC.PawnStartedNeedlecastingIntoEmptySleeve".Translate(needlecastable.NeuralData.name.ToStringShort), pawn, MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message("AC.PawnStartedNeedlecastingIntoOtherPawn".Translate(needlecastable.NeuralData.name.ToStringShort, pawn), pawn, MessageTypeDefOf.NeutralEvent);
            }
            var needlePawnData = new NeuralData();
            originalPawnData = new NeuralData();
            AC_Utils.DebugMessage("Copying remote pawn");
            originalPawnData.CopyFromPawn(pawn, needlecastable.NeuralData.sourceStack);
            AC_Utils.DebugMessage("Done copying remote pawn");
            pawnOwnership = pawn.ownership.Clone();
            pawn.ownership.UnclaimAll();

            if (needlecastable is Hediff_NeuralStack source)
            {
                this.sourceHediff = source;
                needlePawnData.CopyFromPawn(source.Pawn, source.SourceStack);
            }
            else if (needlecastable is NeuralStack stack)
            {
                this.sourceStack = stack;
                needlePawnData.CopyDataFrom(stack.NeuralData);
            }
            AC_Utils.DebugMessage("Overwriting remote pawn");
            needlePawnData.OverwritePawn(pawn);
            AC_Utils.DebugMessage("Done overwriting remote pawn");

            Recipe_InstallNeuralStack.ApplyThoughts(pawn, needlePawnData);
            HediffSet_DirtyCache_Patch.looking = false;

            if (needlecastable is Hediff_NeuralStack source2)
            {
                sourceOwnership = source2.Pawn.ownership.Clone();
                source2.Pawn.ownership.UnclaimAll();
                TransferOwnership(sourceOwnership, pawn.ownership);
            }

            //pawn.health.hediffSet.DirtyCache();
        }


        private Pawn_Ownership pawnOwnership;
        private Pawn_Ownership sourceOwnership;

        public void TransferOwnership(Pawn_Ownership from, Pawn_Ownership to)
        {
            if (from is null || to is null) return;

            if (from.OwnedBed != null)
            {
                to.ClaimBedIfNonMedical(from.OwnedBed);
            }
            if (from.AssignedThrone != null)
            {
                to.ClaimThrone(from.AssignedThrone);
            }
            if (from.AssignedMeditationSpot != null)
            {
                to.ClaimMeditationSpot(from.AssignedMeditationSpot);
            }
            if (from.AssignedDeathrestCasket != null)
            {
                to.ClaimDeathrestCasket(from.AssignedDeathrestCasket);
            }
        }

        public void EndNeedlecasting(ConnectStatus connectStatus)
        {
            GiveNeedlecastEndingMessage(connectStatus);
            AC_Utils.DebugMessage("Ending needlecasting from " + Source + " to " + pawn.GetFullName());
            HediffSet_DirtyCache_Patch.looking = true;
            UpdateSourcePawn();
            originalPawnData.OverwritePawn(pawn);
            var source = Source;
            pawn.ownership.UnclaimAll();
            TransferOwnership(pawnOwnership, pawn.ownership);
            pawnOwnership = null;

            if (source is Hediff_NeuralStack hediff)
            {
                var coma = hediff.pawn.GetHediff(AC_DefOf.AC_NeedlecastingStasis);
                hediff.needleCastingInto = null;
                hediff.pawn.health.RemoveHediff(coma);
                hediff.pawn.health.AddHediff(AC_DefOf.AC_NeedlecastingSickness);
                sourceHediff = null;
                hediff.pawn.ownership.UnclaimAll();
                TransferOwnership(sourceOwnership, hediff.pawn.ownership);
                sourceOwnership = null;
            }
            else if (source is NeuralStack stack)
            {
                stack.needleCastingInto = null;
                sourceStack = null;
            }

            originalPawnData = null;
            if (wasEmptySleeve)
            {
                pawn.MakeEmptySleeve();
            }
            else
            {
                pawn.health.AddHediff(AC_DefOf.AC_NeedlecastingSickness);
            }
            HediffSet_DirtyCache_Patch.looking = false;
        }

        public void GiveNeedlecastEndingMessage(ConnectStatus connectStatus)
        {
            if (connectStatus == ConnectStatus.StoppedManually)
            {
                Messages.Message("AC.PawnNoLongerNeedlecasting".Translate(Source.NeuralData.name.ToStringShort),
                    Source.ThingHolder, MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                var cause = GetCause(connectStatus);
                if (cause != null)
                {
                    Messages.Message("AC.PawnStoppedNeedlecastingCause".Translate(Source.NeuralData.name.ToStringShort, cause),
                        Source.ThingHolder, MessageTypeDefOf.NegativeEvent);
                }
            }
        }

        public string GetCause(ConnectStatus connectStatus)
        {
            switch (connectStatus)
            {
                case ConnectStatus.LowConscioussness: return "AC.LowConscioussness".Translate();
                case ConnectStatus.TargetDead: return "AC.SleeveHasDied".Translate();
                case ConnectStatus.MatrixHasNoPower: return "AC.MatrixHasNoPower".Translate();
                case ConnectStatus.OutOfRange: return "AC.SleeveNoLongerReachable".Translate();
                case ConnectStatus.NoConnectedMatrix:
                    {
                        if (Source.ThingHolder is Pawn pawn)
                        {
                            return "AC.WasRemovedFromCryptoCasket".Translate(pawn);
                        }
                        return "AC.ConnectionDistrupted".Translate();
                    }
                case ConnectStatus.Lost: return "AC.ControlledSleeveLost".Translate();
                case ConnectStatus.SwitchedFaction: return "AC.NoLongerPartOfPlayerFaction".Translate(Faction.OfPlayer.name);
                case ConnectStatus.ConnectionDisrupted: return "AC.ConnectionDistrupted".Translate();
                case ConnectStatus.CasterDead: return "AC.CasterHasDied".Translate();
                case ConnectStatus.StackDestroyed: return "AC.CasterStackDestroyed".Translate();
            }
            return null;
        }

        public override void Tick()
        {
            base.Tick();
            if (pawn.IsHashIntervalTick(30))
            {
                if (Needlecasted)
                {
                    var connectStatus = GetConnectStatus(Source);
                    if (connectStatus != ConnectStatus.Connectable)
                    {
                        EndNeedlecasting(connectStatus);
                    }
                    else
                    {
                        UpdateSourcePawn();
                    }
                }
            }
        }

        public Building_NeuralMatrix GetSourceMatrix()
        {
            if (Source is Hediff_NeuralStack hediff && hediff.pawn.ParentHolder is Building_CryptosleepCasket casket)
            {
                return casket.GetConnectedMatrix(checkForPower: false);
            }
            else if (Source is NeuralStack stack && stack.ParentHolder is CompNeuralCache compCache)
            {
                return compCache.parent.GetConnectedMatrix(checkForPower: false);
            }
            return null;
        }

        private void UpdateSourcePawn()
        {
            var source = Source;
            if (source is Hediff_NeuralStack hediff)
            {
                hediff.NeuralData.CopyFromPawn(pawn, hediff.SourceStack);
                hediff.NeuralData.OverwritePawn(hediff.pawn);
            }
            else if (source is NeuralStack stack)
            {
                stack.NeuralData.CopyFromPawn(pawn, stack.def);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (Needlecasted)
            {
                yield return new Command_Action
                {
                    defaultLabel = "AC.EndNeedlecasting".Translate(),
                    defaultDesc = "AC.EndNeedlecastingDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EndNeedlecasting"),
                    action = delegate
                    {
                        var matrix = GetSourceMatrix();
                        if (matrix.Tile != pawn.Tile)
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("AC.NeedlecastingDisconnectWarning".Translate(pawn), delegate
                            {
                                EndNeedlecasting_UI();
                            }));
                        }
                        else
                        {
                            EndNeedlecasting_UI();
                        }
                    }
                };
            }
        }

        public override string GetInspectString()
        {
            var sb = new StringBuilder(base.GetInspectString());
            if (Needlecasted)
            {
                Source.NeuralData.AppendDebugData(sb, pawn);
            }
            return sb.ToString().TrimEndNewlines();
        }

        private void EndNeedlecasting_UI()
        {
            AlteredCarbonManager.Instance.actionsToDo.Add((Time.frameCount + 1, delegate
            {
                EndNeedlecasting(ConnectStatus.StoppedManually);
            }));
        }
    }
}