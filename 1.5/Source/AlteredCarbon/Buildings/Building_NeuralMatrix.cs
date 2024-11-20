using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace AlteredCarbon
{
    [HotSwappable]
    public class Building_NeuralMatrix : Building
    {
        public CompPowerTrader compPower;
        public CompNeuralCache compCache;
        public CompFacility compFacility;
        public string name;
        public const int MaxCastingRelaysCount = 6;
        public List<Thing> tunedCastingRelays = new List<Thing>();
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compPower = this.TryGetComp<CompPowerTrader>();
            compCache = this.TryGetComp<CompNeuralCache>();
            compFacility = this.TryGetComp<CompFacility>();
            if (respawningAfterLoad && name.NullOrEmpty())
            {
                GenerateName();
                var castingRelays = map.listerThings.ThingsOfDef(AC_DefOf.AC_CastingRelay).OrderBy(x => x.Position.DistanceTo(Position)).ToList();
                while (castingRelays.Any() && tunedCastingRelays.Count < MaxCastingRelaysCount)
                {
                    var castingRelay = castingRelays.PopFront();
                    var comp = castingRelay.TryGetComp<CompCastingRelay>();
                    comp.tunedTo = this;
                }
            }
        }

        public bool Powered => this.compPower.PowerOn;
        public override string Label => base.Label + (", " + name).Colorize(ColoredText.SubtleGrayColor);

        public override void PostMake()
        {
            base.PostMake();
            GenerateName();
        }

        public float NeedleCastRange()
        {
            var range = 1f;
            if (Powered)
            {
                foreach (var linked in tunedCastingRelays.Where(x => x.TryGetComp<CompPowerTrader>().PowerOn))
                {
                    range += 5f;
                }
            }
            return range;
        }
        public IEnumerable<NeuralStack> StoredNeuralStacks => compCache.innerContainer.OfType<NeuralStack>();
        public IEnumerable<NeuralStack> AllNeuralStacks => AllNeuralCaches.SelectMany(x => x.innerContainer.OfType<NeuralStack>());
        public List<Thing> LinkedBuildings => compFacility.LinkedBuildings;
        public IEnumerable<CompNeuralCache> AllNeuralCaches => LinkedBuildings.Select(x => x.TryGetComp<CompNeuralCache>()).Concat(compCache).Where(x => x != null);
        public override IEnumerable<Gizmo> GetGizmos()
        {
            yield return new CastingRangeGizmo(this);
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }
            if (Faction == Faction.OfPlayer)
            {
                foreach (var g in EjectAll())
                {
                    yield return g;
                }
                foreach (var g in GetManageMatrix())
                {
                    yield return g;
                }
            }
        }

        public IEnumerable<Gizmo> GetManageMatrix()
        {
            var manageNeuralMatrix = new Command_Action
            {
                defaultLabel = "AC.ManageNeuralMatrix".Translate(),
                defaultDesc = "AC.ManageNeuralMatrixDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Gizmos/ManageNeuralMatrix"),
                action = delegate
                {
                    Find.WindowStack.Add(new Window_NeuralMatrixManagement(this));
                }
            };
            manageNeuralMatrix.TryDisableCommand(new CommandInfo
            {
                lockedProjects = new List<ResearchProjectDef> { AC_DefOf.AC_NeuralDigitalization },
                building = this
            });
            yield return manageNeuralMatrix;
        }

        private IEnumerable<Gizmo> EjectAll()
        {
            var stacks = StoredNeuralStacks.ToList();
            if (stacks.Any())
            {
                var ejectAll = new Command_Action();
                ejectAll.defaultLabel = "AC.EjectAll".Translate();
                ejectAll.defaultDesc = "AC.EjectAllNeuralStacksDesc".Translate();
                ejectAll.icon = ContentFinder<Texture2D>.Get("UI/Gizmos/EjectAllStacks");
                ejectAll.action = delegate
                {
                    compCache.EjectContents();
                };
                yield return ejectAll;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref name, "name");
            Scribe_Collections.Look(ref tunedCastingRelays, "tunedCastingRelays", LookMode.Reference);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                tunedCastingRelays ??= new List<Thing>();
            }
        }

        private void GenerateName()
        {
            GrammarRequest request = default(GrammarRequest);
            request.Includes.Add(AC_DefOf.AC_NeuralMatrixNameMaker);
            name = GrammarResolver.Resolve("root", request);
        }
    }
}