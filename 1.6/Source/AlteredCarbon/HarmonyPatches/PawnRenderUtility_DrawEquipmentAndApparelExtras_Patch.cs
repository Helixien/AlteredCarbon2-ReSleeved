using HarmonyLib;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AlteredCarbon
{
    [HotSwappable]
    [HarmonyPatch(typeof(PawnRenderUtility), "DrawEquipmentAndApparelExtras")]
    public static class PawnRenderUtility_DrawEquipmentAndApparelExtras_Patch
    {
        public static void Postfix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
        {
            if (pawn.equipment?.Primary is null)
            {
                if (!pawn.Dead && pawn.RaceProps.Humanlike && pawn.Spawned)
                {
                    var verb = PawnRenderUtility_DrawEquipmentAiming_Patch.GetCurrentVerb(pawn);
                    if (verb?.HediffCompSource is HediffComp_MeleeWeapon hediffComp)
                    {
                        foreach (var hediff in pawn.health.hediffSet.hediffs)
                        {
                            hediffComp = hediff.TryGetComp<HediffComp_MeleeWeapon>();
                            if (hediffComp != null)
                            {
                                DrawHediffComp(pawn, drawPos, facing, flags, hediffComp);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawHediffComp(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags, 
            HediffComp_MeleeWeapon hediffComp)
        {
            Job curJob = pawn.CurJob;
            if (curJob != null && curJob.def?.neverShowWeapon == false)
            {
                //Stance_Busy stance_Busy = pawn.stances?.curStance as Stance_Busy;
                float equipmentDrawDistanceFactor = pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
                float num = 0f;
                //if (!flags.HasFlag(PawnRenderFlags.NeverAimWeapon) && stance_Busy != null && !stance_Busy.neverAimWeapon && stance_Busy.focusTarg.IsValid)
                //{
                //    Vector3 vector = (stance_Busy.focusTarg.HasThing ? stance_Busy.focusTarg.Thing.DrawPos : stance_Busy.focusTarg.Cell.ToVector3Shifted());
                //    if ((vector - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
                //    {
                //        num = (vector - pawn.DrawPos).AngleFlat();
                //    }
                //    Verb currentEffectiveVerb = pawn.CurrentEffectiveVerb;
                //    if (currentEffectiveVerb != null && currentEffectiveVerb.AimAngleOverride.HasValue)
                //    {
                //        num = currentEffectiveVerb.AimAngleOverride.Value;
                //    }
                //    drawPos += new Vector3(0f, 0f, 0.4f).RotatedBy(num) * equipmentDrawDistanceFactor;
                //    PawnRenderUtility_DrawEquipmentAiming_Patch.DrawHediffMelee(drawPos, num, pawn, hediffComp);
                //}
                //else if (PawnRenderUtility.CarryWeaponOpenly(pawn))
                {
                    num = 143f;
                    switch (facing.AsInt)
                    {
                        case 0:
                            drawPos += PawnRenderUtility.EqLocNorth * equipmentDrawDistanceFactor;
                            break;
                        case 1:
                            drawPos += PawnRenderUtility.EqLocEast * equipmentDrawDistanceFactor;
                            break;
                        case 2:
                            drawPos += PawnRenderUtility.EqLocSouth * equipmentDrawDistanceFactor;
                            break;
                        case 3:
                            drawPos += PawnRenderUtility.EqLocWest * equipmentDrawDistanceFactor;
                            num = 217f;
                            break;
                    }
                    PawnRenderUtility_DrawEquipmentAiming_Patch.DrawHediffMelee(drawPos, num, pawn, hediffComp);
                }
            }
        }
    }
}

