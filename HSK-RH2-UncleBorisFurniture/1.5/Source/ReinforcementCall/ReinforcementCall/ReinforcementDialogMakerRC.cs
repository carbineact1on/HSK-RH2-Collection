using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ReinforcementCall
{
	// Token: 0x0200000C RID: 12
	public static class ReinforcementDialogMakerRC
	{
		// Token: 0x06000025 RID: 37 RVA: 0x00002D98 File Offset: 0x00000F98
		public static DiaNode FactionDialogFor(Pawn negotiator, Faction faction)
		{
			Map map = negotiator.Map;
			bool flag = faction.leader != null;
			Pawn pawn;
			string value;
			if (flag)
			{
				pawn = faction.leader;
				value = faction.leader.Name.ToStringFull;
			}
			else
			{
				Log.Error("Faction " + ((faction != null) ? faction.ToString() : null) + " has no leader.");
				pawn = negotiator;
				value = faction.Name;
			}
			bool flag2 = faction.PlayerRelationKind == FactionRelationKind.Hostile;
			DiaNode diaNode;
			if (flag2)
			{
				bool flag3 = !faction.def.permanentEnemy && "FactionGreetingHostileAppreciative".CanTranslate();
				string key;
				if (flag3)
				{
					key = "FactionGreetingHostileAppreciative";
				}
				else
				{
					key = "FactionGreetingHostile";
				}
				diaNode = new DiaNode(key.Translate(value).AdjustedFor(pawn, "PAWN", true));
			}
			else
			{
				bool flag4 = faction.PlayerRelationKind == FactionRelationKind.Neutral;
				if (flag4)
				{
					diaNode = new DiaNode("FactionGreetingWary".Translate(value, negotiator.LabelShort, negotiator.Named("NEGOTIATOR"), pawn.Named("LEADER")).AdjustedFor(pawn, "PAWN", true));
				}
				else
				{
					diaNode = new DiaNode("FactionGreetingWarm".Translate(value, negotiator.LabelShort, negotiator.Named("NEGOTIATOR"), pawn.Named("LEADER")).AdjustedFor(pawn, "PAWN", true));
				}
			}
			bool flag5 = map != null && map.IsPlayerHome;
			if (flag5)
			{
				diaNode.options.Add(ReinforcementDialogMakerRC.RequestFreeReinforcements(map, faction, negotiator));
				bool flag6 = faction.def.techLevel >= TechLevel.Industrial;
				if (flag6)
				{
					diaNode.options.Add(ReinforcementDialogMakerRC.RequestQuickReactionReinforcements(map, faction, negotiator));
					diaNode.options.Add(ReinforcementDialogMakerRC.RequestStrongQuickReactionReinforcements(map, faction, negotiator));
				}
			}
			DiaOption diaOption = new DiaOption("(" + "Disconnect".Translate() + ")");
			diaOption.resolveTree = true;
			diaNode.options.Add(diaOption);
			return diaNode;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002FC8 File Offset: 0x000011C8
		public static DiaOption RequestFreeReinforcements(Map map, Faction faction, Pawn negotiator)
		{
			string text = "RequestFreeMilitaryAidRC".Translate();
			bool flag = faction.PlayerRelationKind != FactionRelationKind.Ally;
			DiaOption result;
			if (flag)
			{
				DiaOption diaOption = new DiaOption(text);
				diaOption.Disable("MustBeAlly".Translate());
				result = diaOption;
			}
			else
			{
				bool flag2 = !faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f).Includes(map.mapTemperature.SeasonalTemp);
				if (flag2)
				{
					DiaOption diaOption2 = new DiaOption(text);
					diaOption2.Disable("BadTemperature".Translate());
					result = diaOption2;
				}
				else
				{
					int num = faction.lastMilitaryAidRequestTick + 30000 - Find.TickManager.TicksGame;
					bool flag3 = num > 0;
					if (flag3)
					{
						DiaOption diaOption3 = new DiaOption(text);
						diaOption3.Disable("WaitTime".Translate(num.ToStringTicksToPeriod(true, false, true, true)));
						result = diaOption3;
					}
					else
					{
						bool flag4 = NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction);
						if (flag4)
						{
							DiaOption diaOption4 = new DiaOption(text);
							diaOption4.Disable("HostileVisitorsPresent".Translate());
							result = diaOption4;
						}
						else
						{
							DiaOption diaOption5 = new DiaOption(text);
							IEnumerable<Faction> source = (from x in map.attackTargetsCache.TargetsHostileToColony
							where GenHostility.IsActiveThreatToPlayer(x)
							select ((Thing)x).Faction into x
							where x != null && !x.HostileTo(faction)
							select x).Distinct<Faction>();
							bool flag5 = source.Any<Faction>();
							if (flag5)
							{
								DiaNode diaNode = new DiaNode("MilitaryAidConfirmMutualEnemy".Translate(faction.Name, GenText.ToCommaList(from fa in source
								select fa.Name, true)));
								DiaOption diaOption6 = new DiaOption("CallConfirm".Translate());
								diaOption6.action = delegate()
								{
									ReinforcementDialogMakerRC.CallForSmallAid(map, faction);
								};
								diaOption6.resolveTree = true;
								DiaOption diaOption7 = new DiaOption("CallCancel".Translate());
								diaOption7.linkLateBind = ReinforcementDialogMakerRC.ResetToRoot(faction, negotiator);
								diaNode.options.Add(diaOption6);
								diaNode.options.Add(diaOption7);
								diaOption5.link = diaNode;
							}
							else
							{
								diaOption5.action = delegate()
								{
									ReinforcementDialogMakerRC.CallForSmallAid(map, faction);
								};
								diaOption5.resolveTree = true;
							}
							result = diaOption5;
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000032CC File Offset: 0x000014CC
		public static DiaOption RequestQuickReactionReinforcements(Map map, Faction faction, Pawn negotiator)
		{
			int silverToPay = 800;
			int num = 25;
			string text = "RequestPaidMilitaryAidRC".Translate(silverToPay);
			bool flag = faction.PlayerRelationKind != FactionRelationKind.Ally && faction.PlayerGoodwill < num;
			DiaOption result;
			if (flag)
			{
				DiaOption diaOption = new DiaOption(text);
				diaOption.Disable("NeedGoodwill".Translate(num.ToString("F0")));
				result = diaOption;
			}
			else
			{
				bool flag2 = ReinforcementDialogMakerRC.AmountSendableSilver(map) < silverToPay;
				if (flag2)
				{
					DiaOption diaOption2 = new DiaOption(text);
					diaOption2.Disable("NeedSilverLaunchable".Translate(silverToPay.ToString()));
					result = diaOption2;
				}
				else
				{
					bool flag3 = !faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f).Includes(map.mapTemperature.SeasonalTemp);
					if (flag3)
					{
						DiaOption diaOption3 = new DiaOption(text);
						diaOption3.Disable("BadTemperature".Translate());
						result = diaOption3;
					}
					else
					{
						int num2 = faction.lastMilitaryAidRequestTick + 60000 - Find.TickManager.TicksGame;
						bool flag4 = num2 > 0;
						if (flag4)
						{
							DiaOption diaOption4 = new DiaOption(text);
							diaOption4.Disable("WaitTime".Translate(num2.ToStringTicksToPeriod(true, false, true, true)));
							result = diaOption4;
						}
						else
						{
							bool flag5 = NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction);
							if (flag5)
							{
								DiaOption diaOption5 = new DiaOption(text);
								diaOption5.Disable("HostileVisitorsPresent".Translate());
								result = diaOption5;
							}
							else
							{
								DiaOption diaOption6 = new DiaOption(text);
								IEnumerable<Faction> source = (from x in map.attackTargetsCache.TargetsHostileToColony
								where GenHostility.IsActiveThreatToPlayer(x)
								select ((Thing)x).Faction into x
								where x != null && !x.HostileTo(faction)
								select x).Distinct<Faction>();
								bool flag6 = source.Any<Faction>();
								if (flag6)
								{
									DiaNode diaNode = new DiaNode("MilitaryAidConfirmMutualEnemy".Translate(faction.Name, GenText.ToCommaList(from fa in source
									select fa.Name, true)));
									DiaOption diaOption7 = new DiaOption("CallConfirm".Translate());
									diaOption7.action = delegate()
									{
										ReinforcementDialogMakerRC.CallForMediumAid(map, faction);
									};
									diaOption7.resolveTree = true;
									DiaOption diaOption8 = new DiaOption("CallCancel".Translate());
									diaOption8.linkLateBind = ReinforcementDialogMakerRC.ResetToRoot(faction, negotiator);
									diaNode.options.Add(diaOption7);
									diaNode.options.Add(diaOption8);
									diaOption6.link = diaNode;
								}
								else
								{
									diaOption6.action = delegate()
									{
										TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, silverToPay, map, null);
										ReinforcementDialogMakerRC.CallForMediumAid(map, faction);
									};
									diaOption6.resolveTree = true;
								}
								result = diaOption6;
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x0000365C File Offset: 0x0000185C
		public static DiaOption RequestStrongQuickReactionReinforcements(Map map, Faction faction, Pawn negotiator)
		{
			int silverToPay = 1500;
			int num = 25;
			string text = "RequestHeavyPaidMilitaryAidRC".Translate(silverToPay);
			bool flag = faction.PlayerRelationKind != FactionRelationKind.Ally && faction.PlayerGoodwill < num;
			DiaOption result;
			if (flag)
			{
				DiaOption diaOption = new DiaOption(text);
				diaOption.Disable("NeedGoodwill".Translate(num.ToString("F0")));
				result = diaOption;
			}
			else
			{
				bool flag2 = ReinforcementDialogMakerRC.AmountSendableSilver(map) < silverToPay;
				if (flag2)
				{
					DiaOption diaOption2 = new DiaOption(text);
					diaOption2.Disable("NeedSilverLaunchable".Translate(silverToPay.ToString()));
					result = diaOption2;
				}
				else
				{
					bool flag3 = !faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f).Includes(map.mapTemperature.SeasonalTemp);
					if (flag3)
					{
						DiaOption diaOption3 = new DiaOption(text);
						diaOption3.Disable("BadTemperature".Translate());
						result = diaOption3;
					}
					else
					{
						int num2 = faction.lastMilitaryAidRequestTick + 90000 - Find.TickManager.TicksGame;
						bool flag4 = num2 > 0;
						if (flag4)
						{
							DiaOption diaOption4 = new DiaOption(text);
							diaOption4.Disable("WaitTime".Translate(num2.ToStringTicksToPeriod(true, false, true, true)));
							result = diaOption4;
						}
						else
						{
							bool flag5 = NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction);
							if (flag5)
							{
								DiaOption diaOption5 = new DiaOption(text);
								diaOption5.Disable("HostileVisitorsPresent".Translate());
								result = diaOption5;
							}
							else
							{
								DiaOption diaOption6 = new DiaOption(text);
								IEnumerable<Faction> source = (from x in map.attackTargetsCache.TargetsHostileToColony
								where GenHostility.IsActiveThreatToPlayer(x)
								select ((Thing)x).Faction into x
								where x != null && !x.HostileTo(faction)
								select x).Distinct<Faction>();
								bool flag6 = source.Any<Faction>();
								if (flag6)
								{
									DiaNode diaNode = new DiaNode("MilitaryAidConfirmMutualEnemy".Translate(faction.Name, GenText.ToCommaList(from fa in source
									select fa.Name, true)));
									DiaOption diaOption7 = new DiaOption("CallConfirm".Translate());
									diaOption7.action = delegate()
									{
										ReinforcementDialogMakerRC.CallForStrongAid(map, faction);
									};
									diaOption7.resolveTree = true;
									DiaOption diaOption8 = new DiaOption("CallCancel".Translate());
									diaOption8.linkLateBind = ReinforcementDialogMakerRC.ResetToRoot(faction, negotiator);
									diaNode.options.Add(diaOption7);
									diaNode.options.Add(diaOption8);
									diaOption6.link = diaNode;
								}
								else
								{
									diaOption6.action = delegate()
									{
										TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, silverToPay, map, null);
										ReinforcementDialogMakerRC.CallForStrongAid(map, faction);
									};
									diaOption6.resolveTree = true;
								}
								result = diaOption6;
							}
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000039EC File Offset: 0x00001BEC
		private static int AmountSendableSilver(Map map)
		{
			return (from t in TradeUtility.AllLaunchableThingsForTrade(map, null)
			where t.def == ThingDefOf.Silver
			select t).Sum((Thing t) => t.stackCount);
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00003A50 File Offset: 0x00001C50
		private static void CallForSmallAid(Map map, Faction faction)
		{
			IncidentParms incidentParms = new IncidentParms();
			incidentParms.target = map;
			incidentParms.faction = faction;
			incidentParms.points = (float)Rand.RangeInclusive(1000, 1500);
			incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;
			faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
			IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms);
			RCDefsOf.FreeReinforcementsOneShotRC.PlayOneShotOnCamera(null);
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00003AC0 File Offset: 0x00001CC0
		private static void CallForMediumAid(Map map, Faction faction)
		{
			IncidentParms incidentParms = new IncidentParms();
			incidentParms.target = map;
			incidentParms.faction = faction;
			incidentParms.points = (float)Rand.RangeInclusive(2000, 3500);
			incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
			faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
			IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms);
			RCDefsOf.MediumReinforcementsOneShotRC.PlayOneShotOnCamera(null);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00003B30 File Offset: 0x00001D30
		private static void CallForStrongAid(Map map, Faction faction)
		{
			IncidentParms incidentParms = new IncidentParms();
			incidentParms.target = map;
			incidentParms.faction = faction;
			incidentParms.points = (float)Rand.RangeInclusive(4000, 5000);
			incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeDrop;
			faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
			IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms);
			RCDefsOf.StrongReinforcementsOneShotRC.PlayOneShotOnCamera(null);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00003BA0 File Offset: 0x00001DA0
		private static Func<DiaNode> ResetToRoot(Faction faction, Pawn negotiator)
		{
			return () => ReinforcementDialogMakerRC.FactionDialogFor(negotiator, faction);
		}
	}
}
