using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ReinforcementCall
{
	// Token: 0x0200000A RID: 10
	public class Building_RedPhoneRC : Building
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000018 RID: 24 RVA: 0x000027EC File Offset: 0x000009EC
		public bool CanUseCommsNow
		{
			get
			{
				bool flag = base.Spawned && base.Map.gameConditionManager.ConditionIsActive(MBDefsOf.SolarFlare);
				return !flag && powerComp.PowerOn;
			}
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002832 File Offset: 0x00000A32
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			powerComp = base.GetComp<CompPowerTrader>();
			LessonAutoActivator.TeachOpportunity(ConceptDefOf.OpeningComms, OpportunityType.GoodToKnow);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002858 File Offset: 0x00000A58
		[Obsolete]
		private FloatMenuOption GetFailureReason(Pawn myPawn)
		{
			bool flag = !ReachabilityUtility.CanReach(myPawn, this, PathEndMode.InteractionCell, Danger.Some, false, false, TraverseMode.ByPawn);
			FloatMenuOption result;
			if (flag)
			{
				result = new FloatMenuOption("CannotUseNoPath".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
			}
			else
			{
				bool flag2 = base.Spawned && base.Map.gameConditionManager.ConditionIsActive(MBDefsOf.SolarFlare);
				if (flag2)
				{
					result = new FloatMenuOption("CannotUseSolarFlare".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
				}
				else
				{
					bool flag3 = !powerComp.PowerOn;
					if (flag3)
					{
						result = new FloatMenuOption("CannotUseNoPower".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
					}
					else
					{
						bool flag4 = !myPawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking);
						if (flag4)
						{
							result = new FloatMenuOption("CannotUseReason".Translate("IncapableOfCapacity".Translate(PawnCapacityDefOf.Talking.label)), null, MenuOptionPriority.Default, null, null, 0f, null, null);
						}
						else
						{
							bool totallyDisabled = myPawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled;
							if (totallyDisabled)
							{
								result = new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap), null, MenuOptionPriority.Default, null, null, 0f, null, null);
							}
							else
							{
								bool flag5 = !CanUseCommsNow;
								if (flag5)
								{
									Log.Error((myPawn?.ToString()) + " could not use comm console for unknown reason.");
									result = new FloatMenuOption("Cannot use now", null, MenuOptionPriority.Default, null, null, 0f, null, null);
								}
								else
								{
									result = null;
								}
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002A2C File Offset: 0x00000C2C
		public IEnumerable<Faction> GetCommTargets(Pawn myPawn)
		{
			IEnumerable<Faction> source = from f in Find.FactionManager.AllFactionsVisibleInViewOrder
										  where !f.IsPlayer
										  select f;
			return from f in source
				   where (f.PlayerRelationKind == FactionRelationKind.Neutral && f.PlayerGoodwill > 0) || (f.PlayerRelationKind == FactionRelationKind.Ally && !f.IsPlayer)
				   select f;
		}

		[Obsolete]
		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
		{
			FloatMenuOption failureReason = GetFailureReason(myPawn);
			bool flag = failureReason != null;
			if (flag)
			{
				yield return failureReason;
			}
			else
			{
				IEnumerable<Faction> factionsToCall = GetCommTargets(myPawn);
				bool flag2 = factionsToCall == null;
				if (flag2)
				{
					yield return new FloatMenuOption("NoFriendlyFactionRC".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
				}
				else
				{
					bool flag3 = factionsToCall.Count<Faction>() < 1;
					if (flag3)
					{
						yield return new FloatMenuOption("NoFriendlyFactionRC".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
					}
				}
				foreach (Faction commTarget in factionsToCall)
				{
					FloatMenuOption option = CommFloatMenuOption(commTarget, myPawn);
					bool flag4 = option != null;
					if (flag4)
					{
						yield return option;
					}
				}
			}
			yield break;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002AAC File Offset: 0x00000CAC
		public FloatMenuOption CommFloatMenuOption(Faction commTarget, Pawn negotiator)
		{
			bool isPlayer = commTarget.IsPlayer;
			FloatMenuOption result;
			if (isPlayer)
			{
				result = null;
			}
			else
			{
				string text = "CallOnRadio".Translate(commTarget.GetCallLabel());
				string text2 = text;
				text = string.Concat(new string[]
				{
					text2,
					" (",
					commTarget.PlayerRelationKind.GetLabel(),
					", ",
					commTarget.PlayerGoodwill.ToStringWithSign(),
					")"
				});
				bool flag = !LeaderIsAvailableToTalk(commTarget);
				if (flag)
				{
					bool flag2 = commTarget.leader != null;
					string str = flag2
						? (string)"LeaderUnavailable".Translate(commTarget.leader.LabelShort, commTarget.leader)
						: (string)"LeaderUnavailableNoLeader".Translate();
					result = new FloatMenuOption(text + " (" + str + ")", null, MenuOptionPriority.Default, null, null, 0f, null, null);
				}
				else
				{
					result = FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate ()
					{
						GiveUseCommsJob(negotiator, commTarget);
					}, MenuOptionPriority.InitiateSocial, null, null, 0f, null, null), negotiator, this, "ReservedBy");
				}
			}
			return result;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002C40 File Offset: 0x00000E40
		public bool LeaderIsAvailableToTalk(Faction faction)
		{
			bool flag = faction.leader == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool spawned = faction.leader.Spawned;
				if (spawned)
				{
					bool flag2 = faction.leader.Downed || faction.leader.IsPrisoner || !faction.leader.Awake() || faction.leader.InMentalState;
					if (flag2)
					{
						return false;
					}
				}
				result = true;
			}
			return result;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002CBC File Offset: 0x00000EBC
		public void GiveUseCommsJob(Pawn negotiator, Faction target)
		{
			Job job = new Job(RCDefsOf.UseRedPhoneRC, this)
			{
				commTarget = target
			};
			negotiator.jobs.TryTakeOrderedJob(job, JobTag.Misc);
			PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.OpeningComms, KnowledgeAmount.Total);
		}

		// Token: 0x04000011 RID: 17
		private CompPowerTrader powerComp;
	}
}
