using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Brainwash
{
    public abstract class JobDriver_WatchBrainwashBase : JobDriver
    {
        protected Building Television => (Building)job.GetTarget(TargetIndex.A).Thing;
        protected Building Chair => (Building)job.GetTarget(TargetIndex.B).Thing;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, job.def.joyMaxParticipants, 0, null, errorOnFailed)
                && pawn.ReserveSittableOrSpot(Chair.Position, job, errorOnFailed);
        }
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.Goto(TargetIndex.B, PathEndMode.OnCell);
            this.FailOn(() => Television.TryGetComp<CompPowerTrader>() != null && !Television.TryGetComp<CompPowerTrader>().PowerOn);
            Toil toil = new()
            {
                tickAction = delegate
                {
                    pawn.rotationTracker.FaceTarget(TargetA);
                },
                handlingFacing = true
            };
            if (TargetA.Thing.def.building != null && TargetA.Thing.def.building.effectWatching != null)
            {
                toil.WithEffect(() => TargetA.Thing.def.building.effectWatching, EffectTargetGetter);
            }
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = 5000;
            toil.PlaySustainerOrSound(BrainwashDefOf.RedHorse_Propaganda);
            toil.AddFinishAction(delegate
            {
                if (ticksLeftThisToil <= 0)
                {
                    BrainwashEffect();
                }
            });
            toil.WithProgressBarToilDelay(TargetIndex.B);
            yield return toil;
            LocalTargetInfo EffectTargetGetter()
            {
                return TargetA.Thing.OccupiedRect().RandomCell + IntVec3.North.RotatedBy(TargetA.Thing.Rotation);
            }
        }

        public virtual void BrainwashEffect()
        {

        }

        public override object[] TaleParameters()
        {
            return new object[2]
            {
                pawn,
                TargetA.Thing.def
            };
        }
    }
}
