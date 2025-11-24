using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace AlteredCarbon
{
    [HotSwappable]
    public class CompNeuralCache : CompThingContainer, INotifyHauledTo
    {
        public bool allowColonistNeuralStacks = true;
        public bool allowStrangerNeuralStacks = true;
        public bool allowHostileNeuralStacks = true;
        public List<NeuralStack> StoredStacks => innerContainer.OfType<NeuralStack>().ToList();

        public override bool Accepts(Thing thing)
        {
            if (thing is NeuralStack stack && stack.IsActiveStack && stack.autoLoad && Full is false)
            {
                if (this.allowColonistNeuralStacks && stack.NeuralData.Faction != null && stack.NeuralData.Faction == Faction.OfPlayer)
                {
                    return true;
                }
                if (this.allowHostileNeuralStacks && stack.NeuralData.Faction.HostileTo(Faction.OfPlayer))
                {
                    return true;
                }
                if (this.allowStrangerNeuralStacks && (stack.NeuralData.Faction is null || stack.NeuralData.Faction != Faction.OfPlayer && !stack.NeuralData.Faction.HostileTo(Faction.OfPlayer)))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool Accepts(ThingDef thingDef)
        {
            return false;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (innerContainer.Count > 0 && (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize))
            {
                EjectContents(ejectNeedlecasting: true);
            }
            innerContainer.ClearAndDestroyContents(DestroyMode.Vanish);
            base.PostDestroy(mode, previousMap);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            var matrix = this.parent.GetConnectedMatrix();
            if (matrix != null)
            {
                foreach (var g in matrix.GetManageMatrix())
                {
                    yield return g;
                }
            }
            if (innerContainer.Any())
            {
                var ejectAll = new Command_Action();
                ejectAll.defaultLabel = "AC.EjectAll".Translate();
                ejectAll.defaultDesc = "AC.EjectAllNeuralStacksDesc".Translate();
                ejectAll.icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EjectAllStacks");
                ejectAll.action = delegate
                {
                    EjectContents();
                };
                yield return ejectAll;
            }

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Fill with new stacks",
                    action = delegate
                    {
                        for (int i = TotalStackCount; i < Props.stackLimit; i++)
                        {
                            var stack = ThingMaker.MakeThing(AC_DefOf.AC_ActiveNeuralStack) as NeuralStack;
                            stack.CreateInnerPawn();
                            innerContainer.TryAdd(stack);
                        }
                    }
                };
            }

        }

        public void EjectContents(bool ejectNeedlecasting = false)
        {
            var droppableStacks = ejectNeedlecasting ? StoredStacks : StoredStacks.Where(x => x.needleCastingInto is null).ToList();
            foreach (var stack in droppableStacks)
            {
                innerContainer.TryDrop(stack, parent.InteractionCell, parent.Map, ThingPlaceMode.Near, out _);
            }
        }

        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder("AC.NeuralStacksStored".Translate(innerContainer.Count(), Props.stackLimit) + "\n");
            if (innerContainer.OfType<NeuralStack>().Any())
            {
                sb.AppendLine("CasketContains".Translate() + ": " + innerContainer.OfType<NeuralStack>()
                    .Select(x => x.NeuralData.name.ToStringFull).ToStringSafeEnumerable());
            }
            return sb.ToString().TrimEndNewlines();
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            yield break;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.allowColonistNeuralStacks, "allowColonistNeuralStacks", true);
            Scribe_Values.Look(ref this.allowHostileNeuralStacks, "allowHostileNeuralStacks", true);
            Scribe_Values.Look(ref this.allowStrangerNeuralStacks, "allowStrangerNeuralStacks", true);
        }

        public void Notify_HauledTo(Pawn hauler, Thing thing, int count)
        {
            if (thing is NeuralStack stack && stack.NeuralData.Faction == Faction.OfPlayer)
            {
                var matrix = this.parent.GetConnectedMatrix();
                if (matrix != null)
                {
                    stack.NeuralData.trackedToMatrix = matrix;
                }
            }
        }
    }
}
