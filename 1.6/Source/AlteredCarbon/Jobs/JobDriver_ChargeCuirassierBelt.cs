using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using VEF.Apparels;

namespace AlteredCarbon
{
    public class JobDriver_ChargeCuirassierBelt : JobDriver
    {
        public Building Building => TargetA.Thing as Building;
        public Apparel Apparel => TargetB.Thing as Apparel;
        public CompPowerTrader _apparelPowerComp;
        public CompPowerTrader ApparelPowerComp => _apparelPowerComp ??= MakePowerComp(Apparel);
        public int chargeDuration;
        public static CompPowerTrader MakePowerComp(Apparel apparel)
        {
            var comp = new CompPowerTrader();
            comp.Initialize(new CompProperties_Power
            {
                compClass = typeof(CompPowerTrader),
                basePowerConsumption = 10,
            });
            comp.parent = apparel;
            comp.powerOnInt = true;
            comp.SetUpPowerVars();
            return comp;
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job);
        }

        public static bool CanDoWork(Pawn pawn, Apparel apparel, Building building, CompPowerTrader compPowerTrader)
        {
            if (apparel.Wearer != pawn)
            {
                return false;
            }
            var comp = building?.PowerComp;
            if (comp is null)
            {
                return false;
            }
            if (building.PowerComp.PowerNet.CanPowerNow(compPowerTrader) is false)
            {
                return false;
            }
            return true;
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            var comp = Apparel.GetComp<CompShieldBubble>();
            chargeDuration = (int)((comp.EnergyMax - comp.Energy) * 10f);
        }

        private Sustainer sustainerCharging;

        private Mote moteCharging;

        private Mote moteCablePulse;

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => CanDoWork(pawn, Apparel, Building, ApparelPowerComp) is false);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            if (TargetA.Thing.def.hasInteractionCell)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            }
            else
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            }
            Toil doWork = Toils_General.Wait(chargeDuration, TargetIndex.A);
            doWork.initAction = () =>
            {
                AddPowerComp();
                SoundDefOf.MechChargerStart.PlayOneShot(Building);
                var comp = Apparel.GetComp<CompShieldBubble>();
                if (comp.Energy <= 0 && comp.Props.resetSound != null)
                {
                    comp.Props.resetSound.PlayOneShot(new TargetInfo(comp.Pawn.Position, comp.Pawn.Map));
                }
                pawn.rotationTracker.FaceCell(TargetThingA.Position);
            };
            doWork.handlingFacing = true;
            doWork.AddPreTickAction(() =>
            {
                if (moteCablePulse == null || moteCablePulse.Destroyed)
                {
                    moteCablePulse = MoteMaker.MakeInteractionOverlay(ThingDefOf.Mote_ChargingCablesPulse, Building,
                        new TargetInfo(pawn.Position, base.Map));
                }
                moteCablePulse?.Maintain();
                var comp = Apparel.GetComp<CompShieldBubble>();
                comp.Energy = Mathf.Min(comp.EnergyMax, comp.Energy + 0.1f);
                if (Building.PowerComp.PowerNet.powerComps.Any(x => x.parent == Apparel) is false)
                {
                    AddPowerComp();
                }

                if (sustainerCharging == null)
                {
                    sustainerCharging = SoundDefOf.MechChargerCharging.TrySpawnSustainer(SoundInfo.InMap(Building));
                }
                sustainerCharging.Maintain();
                if (moteCharging == null || moteCharging.Destroyed)
                {
                    moteCharging = MoteMaker.MakeAttachedOverlay(pawn, ThingDefOf.Mote_MechCharging, Vector3.zero);
                }
                moteCharging?.Maintain();
            });
            ToilEffects.WithProgressBarToilDelay(doWork, TargetIndex.A, false, -0.5f);
            ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(doWork, TargetIndex.A);
            yield return doWork;
            yield return new Toil
            {
                initAction = delegate ()
                {
                    Building.PowerComp.PowerNet.powerComps.RemoveAll(x => x.parent == Apparel);
                    if (sustainerCharging != null)
                    {
                        sustainerCharging.End();
                        sustainerCharging = null;
                    }
                }
            };
        }

        private void AddPowerComp()
        {
            Building.PowerComp.PowerNet.powerComps.Add(ApparelPowerComp);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref chargeDuration, "chargeDuration");
        }
    }
}

