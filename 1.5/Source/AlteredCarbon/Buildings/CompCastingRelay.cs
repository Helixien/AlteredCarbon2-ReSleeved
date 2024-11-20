using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AlteredCarbon
{

    public class CompProperties_CastingRelay : CompProperties
    {
        public float retuneSeconds = 3f;

        public float tuneSeconds = 5f;

        public int powerConsumptionIdle = 100;

        public EffecterDef untunedEffect;

        public EffecterDef tuningEffect;

        public EffecterDef tunedEffect;

        public EffecterDef retuningEffect;

        public SoundDef tuningCompleteSound;

        public CompProperties_CastingRelay()
        {
            compClass = typeof(CompCastingRelay);
        }
    }

    [HotSwappable]
    [StaticConstructorOnStartup]
    public class CompCastingRelay : ThingComp
    {
        private static readonly Material UntunedMaterial = SolidColorMaterials.SimpleSolidColorMaterial(Color.red);

        private static readonly Material TuningMaterial = SolidColorMaterials.SimpleSolidColorMaterial(Color.yellow);

        private static readonly Material TunedMaterial = SolidColorMaterials.SimpleSolidColorMaterial(Color.green);

        private static readonly Vector2 TuningBarSize = new Vector3(0.255f, 0.035f);

        private const float TuningBarYOffset = -0.4f;

        public Building_NeuralMatrix tunedTo;

        public int tuningTimeLeft;

        public Building_NeuralMatrix tuningTo;

        private Effecter effecter;

        private CompPowerTrader PowerTrader => parent.TryGetComp<CompPowerTrader>();

        public CompProperties_CastingRelay Props => (CompProperties_CastingRelay)props;

        private int RetuneTimeTicks => (int)(Props.retuneSeconds * 60f);

        private int TuningTimeTicks => (int)(Props.tuneSeconds * 60f);

        private BandNodeState State
        {
            get
            {
                if (tunedTo != null && tuningTo != null)
                {
                    return BandNodeState.Retuning;
                }
                if (tuningTo != null)
                {
                    return BandNodeState.Tuning;
                }
                if (tunedTo != null)
                {
                    return BandNodeState.Tuned;
                }
                return BandNodeState.Untuned;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!ModLister.CheckBiotech("Band node"))
            {
                parent.Destroy();
            }
            else
            {
                base.PostSpawnSetup(respawningAfterLoad);
            }
        }

        public override void PostExposeData()
        {
            Scribe_References.Look(ref tunedTo, "tunedTo");
            Scribe_References.Look(ref tuningTo, "tuningTo");
            Scribe_Values.Look(ref tuningTimeLeft, "tuningTimeLeft", 0);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action command_Action = new Command_Action();
            command_Action.defaultLabel = ((tunedTo == null) ? ("BandNodeTuneTo".Translate() + "...") : ("BandNodeRetuneTo".Translate() + "..."));
            command_Action.defaultDesc = ((tunedTo == null) ? "AC.StackRelayTuningDesc".Translate("PeriodSeconds".Translate(Props.tuneSeconds)) : "AC.StackRelayRetuningDesc".Translate("PeriodSeconds".Translate(Props.retuneSeconds)));
            command_Action.onHover = (Action)Delegate.Combine(command_Action.onHover, (Action)delegate
            {
                Building_NeuralMatrix matrix = ((tuningTo != null) ? tuningTo : tunedTo);
                if (matrix != null)
                {
                    GenDraw.DrawLineBetween(parent.DrawPos, matrix.DrawPos);
                }
            });
            bool flag = false;
            foreach (Building_NeuralMatrix item in parent.Map.listerThings.ThingsOfDef(AC_DefOf.AC_NeuralMatrix))
            {
                if (tunedTo != item && tuningTo != item)
                {
                    flag = true;
                    break;
                }
            }
            command_Action.Disabled = !flag;
            command_Action.icon = ContentFinder<Texture2D>.Get("UI/Gizmos/CastingArrayTuning");
            command_Action.action = (Action)Delegate.Combine(command_Action.action, (Action)delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (Building_NeuralMatrix matrix in parent.Map.listerThings.ThingsOfDef(AC_DefOf.AC_NeuralMatrix))
                {
                    if (matrix.tunedCastingRelays.Count >= Building_NeuralMatrix.MaxCastingRelaysCount || matrix.Powered is false)
                    {
                        continue;
                    }
                    IEnumerable<CompCastingRelay> bandNodes = from t in Find.Selector.SelectedObjects
                                                              where t is Thing thing && thing.TryGetComp<CompCastingRelay>() != null
                                                              select ((Thing)t).TryGetComp<CompCastingRelay>() into n
                                                              where n.tunedTo != matrix && n.tuningTo != matrix
                                                              select n;
                    if (bandNodes.Any())
                    {
                        Building_NeuralMatrix localMatrix = matrix;
                        string text = matrix.LabelCap;
                        if (bandNodes.All((CompCastingRelay b) => b.tunedTo == null) || bandNodes.All((CompCastingRelay b) => b.tunedTo != null))
                        {
                            text = ((tunedTo != null) ? (text + " (" + Props.retuneSeconds + " " + "SecondsLower".Translate() + ")") : ((string)(text + (" (" + Props.tuneSeconds + " " + "SecondsLower".Translate() + ")"))));
                        }
                        list.Add(new FloatMenuOption(text, delegate
                        {
                            foreach (CompCastingRelay item2 in bandNodes)
                            {
                                item2.TuneTo(localMatrix);
                            }
                        }));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(list));
            });
            yield return command_Action;
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action2 = new Command_Action();
                command_Action2.defaultLabel = "DEV: complete tuning";
                command_Action2.action = delegate
                {
                    tuningTimeLeft = 0;
                };
                yield return command_Action2;
            }
        }

        public void TuneTo(Building_NeuralMatrix matrix)
        {
            tuningTimeLeft = ((tunedTo == null) ? TuningTimeTicks : RetuneTimeTicks);
            tuningTo = matrix;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Material material = State switch
            {
                BandNodeState.Tuned => TunedMaterial,
                BandNodeState.Untuned => UntunedMaterial,
                _ => TuningMaterial,
            };
            Vector3 s = new Vector3(TuningBarSize.x, 1f, TuningBarSize.y);
            Vector3 pos = parent.DrawPos + new Vector3(0f, 0f, -0.4f);
            pos.y = parent.def.altitudeLayer.AltitudeFor() + 1f / 26f;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(pos, parent.Rotation.AsQuat, s);
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        public override void CompTick()
        {
            PowerTrader.PowerOutput = ((tunedTo == null && tuningTo == null) ? ((float)(-Props.powerConsumptionIdle)) : (0f - PowerTrader.Props.PowerConsumption));
            if (tunedTo != null && (tunedTo.Destroyed || tunedTo.tunedCastingRelays.Count >= Building_NeuralMatrix.MaxCastingRelaysCount))
            {
                tunedTo = null;
            }
            if (tuningTo != null && (tuningTo.Destroyed || tuningTo.tunedCastingRelays.Count >= Building_NeuralMatrix.MaxCastingRelaysCount))
            {
                tuningTo = null;
            }
            if (PowerTrader != null && !PowerTrader.PowerOn)
            {
                effecter?.Cleanup();
                effecter = null;
                return;
            }
            if (tuningTo != null)
            {
                tuningTimeLeft--;
                if (tuningTimeLeft <= 0)
                {
                    tunedTo = tuningTo;
                    tuningTo = null;
                    if (Props.tuningCompleteSound != null)
                    {
                        Props.tuningCompleteSound.PlayOneShot(parent);
                    }
                }
            }

            if (tuningTo == null && tunedTo != null && !tunedTo.tunedCastingRelays.Contains(this.parent))
            {
                tunedTo.tunedCastingRelays.Add(this.parent);
            }

            if (State == BandNodeState.Untuned)
            {
                if (effecter == null || effecter.def != Props.untunedEffect)
                {
                    effecter?.Cleanup();
                    effecter = Props.untunedEffect.Spawn();
                }
            }
            else if (State == BandNodeState.Tuned)
            {
                if (effecter == null || effecter.def != Props.tunedEffect)
                {
                    effecter?.Cleanup();
                    effecter = Props.tunedEffect.Spawn();
                }
            }
            else if (State == BandNodeState.Tuning)
            {
                if (effecter == null || effecter.def != Props.tuningEffect)
                {
                    effecter?.Cleanup();
                    effecter = Props.tuningEffect.Spawn();
                }
            }
            else if (State == BandNodeState.Retuning)
            {
                if (effecter == null || effecter.def != Props.retuningEffect)
                {
                    effecter?.Cleanup();
                    effecter = Props.retuningEffect.Spawn();
                }
            }
            else
            {
                effecter?.Cleanup();
                effecter = null;
            }
            if (effecter != null)
            {
                effecter.EffectTick(parent, parent);
            }
        }

        public override string CompInspectStringExtra()
        {
            string text = null;
            if (!PowerTrader.PowerOn)
            {
                text = "\n" + "Unpowered".Translate().CapitalizeFirst().Resolve();
            }
            if (tuningTo != null)
            {
                return "BandNodeTuningTo".Translate() + ": " + tuningTo.LabelCap + " - " + tuningTimeLeft.ToStringTicksToPeriod() + text;
            }
            return "BandNodeTunedTo".Translate() + ": " + ((tunedTo == null) ? "Nobody".Translate().Resolve() : tunedTo.LabelCap) + text;
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (tunedTo != null)
            {
                tunedTo.tunedCastingRelays.Remove(this.parent);
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if (tunedTo != null)
            {
                tunedTo.tunedCastingRelays.Remove(this.parent);
            }
        }
    }
}