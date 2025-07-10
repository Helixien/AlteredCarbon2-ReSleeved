using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;
using System.Linq;
using UnityEngine;
using LudeonTK;
using RimWorld.Planet;
namespace AlteredCarbon
{
    [HotSwappable]
    public class QuestPart_DamagePawns : QuestPart
    {
        public string inSignal;

        public List<Pawn> pawns = new List<Pawn>();

        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                foreach (GlobalTargetInfo questLookTarget in base.QuestLookTargets)
                {
                    yield return questLookTarget;
                }
                foreach (Pawn questLookTarget2 in PawnsArriveQuestPartUtility.GetQuestLookTargets(pawns))
                {
                    yield return questLookTarget2;
                }
            }
        }


        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);
            if (!(signal.tag == inSignal))
            {
                return;
            }
            for (int i = 0; i < pawns.Count; i++)
            {
                var value = pawns[i];
                DamageWithInfections(value);
                value.inventory.TryAddItemNotForSale(ThingMaker.MakeThing(AC_DefOf.AC_EmptyNeuralStack));
                value.inventory.TryAddItemNotForSale(ThingMaker.MakeThing(AC_DefOf.AC_NanoStorageDrive));
            }
        }


        [DebugAction("Pawns", null, false, false, false, false, false, 0, false, actionType =
            DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = 1000)]
        public static void DamageWithInfections(Pawn value)
        {
            IEnumerable<BodyPartRecord> source = from x in HealthUtility.HittablePartsViolence(value.health.hediffSet)
                                                 where !value.health.hediffSet.hediffs.Any((Hediff y) =>
                                                 y.Part == x && y.CurStage != null
                                                 && y.CurStage.partEfficiencyOffset < 0f)
                                                 select x;

            Pawn p = value;
            HediffSet hediffSet = p.health.hediffSet;
            int infectionsCount = Rand.RangeInclusive(1, 3);
            int totalInjuries = infectionsCount;

            List<BodyPartRecord> injuredParts = new List<BodyPartRecord>();
            int attempts = 0;
            for (int i = 0; i < totalInjuries && source.Any(); i++)
            {
                attempts++;
                BodyPartRecord bodyPartRecord = source.RandomElementByWeight((BodyPartRecord x) => x.coverageAbs);
                int num2 = Mathf.RoundToInt(hediffSet.GetPartHealth(bodyPartRecord));
                float statValue = p.GetStatValue(StatDefOf.IncomingDamageFactor);
                DamageDef damageDef = ((bodyPartRecord.depth != BodyPartDepth.Outside) ? DamageDefOf.Blunt
                    : ((bodyPartRecord.def.bleedRate > 0f) ? DamageDefOf.Cut : DamageDefOf.Blunt));
                float woundDamage = Rand.Range(num2 * 0.3f, num2 * 0.6f);

                DamageInfo dinfo = new DamageInfo(damageDef, woundDamage, 999f, -1f, null, bodyPartRecord, null,
                                                  DamageInfo.SourceCategory.ThingOrUnknown, null, instigatorGuilty: true, spawnFilth: true,
                                                  QualityCategory.Normal, checkForJobOverride: false);
                dinfo.SetAllowDamagePropagation(val: false);
                HediffDef hediffDefFromDamage = HealthUtility.GetHediffDefFromDamage(damageDef, p, bodyPartRecord);
                if (attempts < 100 && (p.health.WouldBeDownedAfterAddingHediff(hediffDefFromDamage, bodyPartRecord, woundDamage)
                    || p.health.WouldDieAfterAddingHediff(hediffDefFromDamage, bodyPartRecord, woundDamage)))
                {
                    i--;
                    continue;
                }
                p.TakeDamage(dinfo);
                injuredParts.Add(bodyPartRecord);
            }

            foreach (var injury in p.health.hediffSet.hediffs.OfType<Hediff_Injury>())
            {
                injury.Tended(quality: Rand.Range(0.5f, 1f), 1f, 1);
            }

            injuredParts = injuredParts.InRandomOrder().ToList();
            int appliedInfections = 0;
            foreach (var bodyPartRecord in injuredParts)
            {
                if (appliedInfections >= infectionsCount)
                {
                    break;
                }

                Hediff infection = HediffMaker.MakeHediff(HediffDefOf.WoundInfection, p, bodyPartRecord);
                infection.Severity = Rand.Range(0.6f, 0.85f);
                if (p.health.WouldBeDownedAfterAddingHediff(infection) || p.health.WouldDieAfterAddingHediff(infection))
                {
                    continue;
                }

                p.health.AddHediff(infection);
                p.health.immunity.TryAddImmunityRecord(infection.def, infection.def);
                ImmunityRecord immunityRecord = p.health.immunity.GetImmunityRecord(infection.def);
                if (immunityRecord != null)
                {
                    immunityRecord.immunity = Rand.Range(0.2f, 0.3f);
                }

                appliedInfections++;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref inSignal, "inSignal");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
        }

        public override bool QuestPartReserves(Pawn p)
        {
            return pawns.Contains(p);
        }

        public override void ReplacePawnReferences(Pawn replace, Pawn with)
        {
            pawns.Replace(replace, with);
        }
    }

    public class QuestNode_DamageAndGiveItems : QuestNode
    {
        public SlateRef<Pawn> pawn;

        public override void RunInt()
        {
            Slate slate = QuestGen.slate;
            var value = pawn.GetValue(slate);
            var part = new QuestPart_DamagePawns();
            part.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(QuestGen.slate.Get<string>("inSignal"));
            part.pawns.Add(value);
            QuestGen.quest.AddPart(part);
        }

        public override bool TestRunInt(Slate slate)
        {
            return true;
        }
    }
}

