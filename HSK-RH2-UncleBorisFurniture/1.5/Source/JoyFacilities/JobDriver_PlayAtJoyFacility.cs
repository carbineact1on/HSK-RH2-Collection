using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace JoyFacilities
{
	public class JobDriver_PlayAtJoyFacility : JobDriver_WatchBuilding
	{
		private JobExtension jobExtension;
		public JobExtension JobExtension
        {
            get
            {
				if (jobExtension is null)
				{
					jobExtension = this.pawn.CurJobDef.GetModExtension<JobExtension>();
				}
				return jobExtension;
            }
        }

		private Effecter effecter;
		protected override void WatchTickAction()
		{
			var jobExtension = JobExtension;
			if (jobExtension.throwIntervalTicks != -1 && jobExtension.moteThrowObject != null && pawn.IsHashIntervalTick(jobExtension.throwIntervalTicks))
			{
				ThrowObjectAt(pawn, TargetLocA, jobExtension.moteThrowObject);
				if (jobExtension.throwSoundDef != null)
                {
					jobExtension.throwSoundDef.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
				}
			}
			if (jobExtension.pawnEffecterDef != null)
            {
				if (effecter is null)
                {
					effecter = jobExtension.pawnEffecterDef.Spawn();
				}
                else
                {
					effecter.EffectTick(pawn, new TargetInfo(pawn.Rotation.FacingCell + pawn.Position, pawn.Map));
				}
			}
			base.WatchTickAction();
		}

        protected override IEnumerable<Toil> MakeNewToils()
        {
            foreach (var t in base.MakeNewToils())
            {
				yield return t;
            }
            this.AddFinishAction(delegate
			{
				if (this.effecter != null)
                {
					this.effecter.Cleanup();
					this.effecter = null;
                }
            });
        }
        private static void ThrowObjectAt(Pawn thrower, IntVec3 targetCell, FleckDef fleck)
		{
			if (thrower.Position.ShouldSpawnMotesAt(thrower.Map))
			{
				float num = Rand.Range(3.8f, 5.6f);
				Vector3 vector = targetCell.ToVector3Shifted() + Vector3Utility.RandomHorizontalOffset((1f - (float)thrower.skills.GetSkill(SkillDefOf.Shooting).Level / 20f) * 1.8f);
				vector.y = thrower.DrawPos.y;
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(thrower.DrawPos, thrower.Map, fleck);
				dataStatic.rotationRate = Rand.Range(-300, 300);
				dataStatic.velocityAngle = (vector - dataStatic.spawnPosition).AngleFlat();
				dataStatic.velocitySpeed = num;
				dataStatic.airTimeLeft = Mathf.RoundToInt((dataStatic.spawnPosition - vector).MagnitudeHorizontal() / num);
				thrower.Map.flecks.CreateFleck(dataStatic);
			}
		}
	}

	public class JobDriver_PlayAtJoyFacilityPowered : JobDriver_PlayAtJoyFacility
    {
		protected override void WatchTickAction()
		{
			if (!((Building)base.TargetA.Thing).TryGetComp<CompPowerTrader>().PowerOn)
			{
				EndJobWith(JobCondition.Incompletable);
			}
			else
			{
				base.WatchTickAction();
			}
		}
	}
}
