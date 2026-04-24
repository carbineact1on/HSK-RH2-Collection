using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace RedHorse
{
	public class CompBetterRottable : CompRottable
	{
		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			switch (base.Stage)
			{
			case RotStage.Fresh:
				stringBuilder.AppendLine("RotStateFresh".Translate());
				break;
			case RotStage.Rotting:
				stringBuilder.AppendLine("RotStateRotting".Translate());
				break;
			case RotStage.Dessicated:
				stringBuilder.AppendLine("RotStateDessicated".Translate());
				break;
			}
			float num = (float)base.PropsRot.TicksToRotStart - base.RotProgress;
			if (num > 0f)
			{
				float f = GenTemperature.GetTemperatureForCell(parent.PositionHeld, parent.Map);
				List<Thing> thingList = parent.PositionHeld.GetThingList(parent.Map);
				for (int i = 0; i < thingList.Count; i++)
				{
					if (thingList[i] is Building_Refrigerator)
					{
						Building_Refrigerator building_Refrigerator = thingList[i] as Building_Refrigerator;
						f = building_Refrigerator.CurrentTemp;
						break;
					}
				}
				f = Mathf.RoundToInt(f);
				float num2 = GenTemperature.RotRateAtTemperature(f);
				int ticksUntilRotAtCurrentTemp = base.TicksUntilRotAtCurrentTemp;
				if (num2 < 0.001f)
				{
					stringBuilder.Append("CurrentlyFrozen".Translate() + ".");
				}
				else if (num2 < 0.999f)
				{
					stringBuilder.Append("CurrentlyRefrigerated".Translate(ticksUntilRotAtCurrentTemp.ToStringTicksToPeriodVague()) + ".");
				}
				else
				{
					stringBuilder.Append("NotRefrigerated".Translate(ticksUntilRotAtCurrentTemp.ToStringTicksToPeriodVague()) + ".");
				}
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}

		public override void CompTickRare()
		{
			if (parent.MapHeld == null || parent.Map == null)
			{
				return;
			}
			float rotProgress = base.RotProgress;
			float num = 1f;
			float temperature = GenTemperature.GetTemperatureForCell(parent.PositionHeld, parent.MapHeld);
			List<Thing> list = parent.MapHeld.thingGrid.ThingsListAtFast(parent.PositionHeld);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] is Building_Refrigerator)
				{
					Building_Refrigerator building_Refrigerator = list[i] as Building_Refrigerator;
					temperature = building_Refrigerator.CurrentTemp;
					break;
				}
			}
			num *= GenTemperature.RotRateAtTemperature(temperature);
			base.RotProgress += Mathf.Round(num * 250f);
			if (base.Stage == RotStage.Rotting && base.PropsRot.rotDestroys)
			{
				if (parent.GetSlotGroup() != null)
				{
					Messages.Message("MessageRottedAwayInStorage".Translate(parent.Label).CapitalizeFirst(), MessageTypeDefOf.SilentInput);
					LessonAutoActivator.TeachOpportunity(ConceptDefOf.SpoilageAndFreezers, OpportunityType.GoodToKnow);
				}
				parent.Destroy();
			}
			else if (Mathf.FloorToInt(rotProgress / 60000f) != Mathf.FloorToInt(base.RotProgress / 60000f))
			{
				if (base.Stage == RotStage.Rotting && base.PropsRot.rotDamagePerDay > 0f)
				{
					parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, (float)GenMath.RoundRandom(base.PropsRot.rotDamagePerDay), 1f, -1f, (Thing)null, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
				}
				else if (base.Stage == RotStage.Dessicated && base.PropsRot.dessicatedDamagePerDay > 0f && ShouldTakeDessicateDamage())
				{
					parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, (float)GenMath.RoundRandom(base.PropsRot.dessicatedDamagePerDay), 1f, -1f, (Thing)null, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
				}
			}
		}

		private bool ShouldTakeDessicateDamage()
		{
			if (parent.ParentHolder != null)
			{
				Thing thing = parent.ParentHolder as Thing;
				if (thing != null && thing.def.category == ThingCategory.Building && thing.def.building.preventDeteriorationInside)
				{
					return false;
				}
			}
			return true;
		}

		private void StageChanged()
		{
			(parent as Corpse)?.RotStageChanged();
		}
	}
}
