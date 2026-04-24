using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RedHorse
{
	public class Building_Refrigerator : Building_Storage, IStoreSettingsParent
	{
		private const float IdealTempDefault = -10f;

		private float currentTemp = float.MinValue;

		private float idealTemp = float.MinValue;

		private bool operatingAtHighPower;

		private StorageSettings curStorageSettings;

		private CompPowerTrader powerTrader;

		private CompGlower glower;

		private const float lowPowerConsumptionFactor = 0.1f;

		private const float temperatureChangeRate = 38f / 325f;

		private const float energyPerSecond = 12f;

		public CompPowerTrader PowerTrader
		{
			get
			{
				return powerTrader;
			}
			set
			{
				powerTrader = value;
			}
		}

		public CompGlower Glower
		{
			get
			{
				return glower;
			}
			set
			{
				glower = value;
			}
		}

		public float IdealTemp
		{
			get
			{
				if (idealTemp == float.MinValue)
				{
					idealTemp = -10f;
				}
				return idealTemp;
			}
			set
			{
				idealTemp = value;
			}
		}

		public float CurrentTemp
		{
			get
			{
				if (currentTemp == float.MinValue)
				{
					currentTemp = base.PositionHeld.GetTemperature(base.MapHeld);
				}
				return currentTemp;
			}
			set
			{
				currentTemp = value;
			}
		}

		public float BasePowerConsumption => 0f - powerTrader.Props.basePowerConsumption;

		public override void SpawnSetup(Map map, bool bla)
		{
			base.SpawnSetup(map, bla);
			powerTrader = GetComp<CompPowerTrader>();
			glower = GetComp<CompGlower>();
			curStorageSettings = new StorageSettings();
			curStorageSettings.CopyFrom(def.building.fixedStorageSettings);
			foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs.Where((ThingDef x) => x.HasComp(typeof(CompRottable))))
			{
				if (!curStorageSettings.filter.Allows(item))
				{
					curStorageSettings.filter.SetAllow(item, allow: true);
				}
			}
		}

		StorageSettings IStoreSettingsParent.GetParentStoreSettings()
		{
			return curStorageSettings;
		}

		public override void TickRare()
		{
			base.TickRare();
			MakeAllHeldThingsBetterCompRottable();
			ResolveTemperature();
		}

		private void ResolveTemperature()
		{
			if (!base.Spawned || powerTrader == null || !powerTrader.PowerOn)
			{
				EqualizeWithRoomTemperature();
				return;
			}
			glower.UpdateLit(base.MapHeld);
			IntVec3 positionHeld = base.PositionHeld;
			float num = 38f / 325f;
			float energyUsed = 0f;
			float num2 = 12f * num * 4.16666651f;
			bool flag = IsUsingHighPower(num2, out energyUsed);
			if (flag)
			{
				GenTemperature.PushHeat(positionHeld, base.MapHeld, (0f - num2) * 1.25f);
				energyUsed += BasePowerConsumption;
				num *= 0.8f;
			}
			else
			{
				energyUsed = BasePowerConsumption * 0.1f;
				num *= 1.1f;
			}
			if (!Mathf.Approximately(CurrentTemp, IdealTemp))
			{
				CurrentTemp += ((CurrentTemp > IdealTemp) ? (0f - num) : num);
			}
			if (CurrentTemp.ToStringTemperature("F0") == IdealTemp.ToStringTemperature("F0"))
			{
				flag = false;
			}
			operatingAtHighPower = flag;
			powerTrader.PowerOutput = energyUsed;
		}

		private void EqualizeWithRoomTemperature()
		{
			float temperature = base.PositionHeld.GetTemperature(base.MapHeld);
			if (CurrentTemp > temperature)
			{
				CurrentTemp += -38f / 325f;
			}
			else if (CurrentTemp < temperature)
			{
				CurrentTemp += 38f / 325f;
			}
		}

		private bool IsUsingHighPower(float energyLimit, out float energyUsed)
		{
			float a = IdealTemp - CurrentTemp;
			energyUsed = 0f;
			if (energyLimit > 0f)
			{
				energyUsed = Mathf.Min(a, energyLimit);
				energyUsed = Mathf.Max(energyUsed, 0f);
			}
			else
			{
				energyUsed = Mathf.Max(a, energyLimit);
				energyUsed = Mathf.Min(energyUsed, 0f);
			}
			return Mathf.Approximately(energyUsed, 0f);
		}

		private void MakeAllHeldThingsBetterCompRottable()
		{
			foreach (Thing thing in base.PositionHeld.GetThingList(base.Map))
			{
				ThingWithComps thingWithComps = thing as ThingWithComps;
				if (thingWithComps != null)
				{
					CompRottable compRottable = thing.TryGetComp<CompRottable>();
					if (compRottable != null && !(compRottable is CompBetterRottable))
					{
						CompBetterRottable compBetterRottable = new CompBetterRottable();
						thingWithComps.AllComps.Remove(compRottable);
						thingWithComps.AllComps.Add(compBetterRottable);
						compBetterRottable.props = compRottable.props;
						compBetterRottable.parent = thingWithComps;
						compBetterRottable.RotProgress = compRottable.RotProgress;
					}
				}
			}
		}

		private float RoundedToCurrentTempModeOffset(float celsiusTemp)
		{
			float f = GenTemperature.CelsiusToOffset(celsiusTemp, Prefs.TemperatureMode);
			f = Mathf.RoundToInt(f);
			return GenTemperature.ConvertTemperatureOffset(f, Prefs.TemperatureMode, TemperatureDisplayMode.Celsius);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			float offset2 = RoundedToCurrentTempModeOffset(-10f);
			yield return new Command_Action
			{
				action = delegate
				{
					InterfaceChangeTargetTemperature(offset2);
				},
				defaultLabel = offset2.ToStringTemperatureOffset("F0"),
				defaultDesc = "CommandLowerTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc5,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower")
			};
			float offset3 = RoundedToCurrentTempModeOffset(-1f);
			yield return new Command_Action
			{
				action = delegate
				{
					InterfaceChangeTargetTemperature(offset3);
				},
				defaultLabel = offset3.ToStringTemperatureOffset("F0"),
				defaultDesc = "CommandLowerTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc4,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower")
			};
			yield return new Command_Action
			{
				action = delegate
				{
					idealTemp = 21f;
					SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
					ThrowCurrentTemperatureText();
				},
				defaultLabel = "CommandResetTemp".Translate(),
				defaultDesc = "CommandResetTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc1,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset")
			};
			float offset4 = RoundedToCurrentTempModeOffset(1f);
			yield return new Command_Action
			{
				action = delegate
				{
					InterfaceChangeTargetTemperature(offset4);
				},
				defaultLabel = "+" + offset4.ToStringTemperatureOffset("F0"),
				defaultDesc = "CommandRaiseTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc2,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise")
			};
			float offset = RoundedToCurrentTempModeOffset(10f);
			yield return new Command_Action
			{
				action = delegate
				{
					InterfaceChangeTargetTemperature(offset);
				},
				defaultLabel = "+" + offset.ToStringTemperatureOffset("F0"),
				defaultDesc = "CommandRaiseTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc3,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise")
			};
		}

		private void InterfaceChangeTargetTemperature(float offset)
		{
			if (offset > 0f)
			{
				SoundDefOf.Thunder_OnMap.PlayOneShotOnCamera();
			}
			else
			{
				SoundDefOf.Thunder_OnMap.PlayOneShotOnCamera();
			}
			idealTemp += offset;
			idealTemp = Mathf.Clamp(idealTemp, -270f, 2000f);
			ThrowCurrentTemperatureText();
		}

		private void ThrowCurrentTemperatureText()
		{
			MoteMaker.ThrowText(this.TrueCenter() + new Vector3(0.5f, 0f, 0.5f), base.MapHeld, idealTemp.ToStringTemperature("F0"), Color.white);
		}

		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Temperature".Translate() + ": ");
			stringBuilder.AppendLine(CurrentTemp.ToStringTemperature("F0"));
			stringBuilder.Append("TargetTemperature".Translate() + ": ");
			stringBuilder.AppendLine(IdealTemp.ToStringTemperature("F0"));
			stringBuilder.Append("PowerConsumptionMode".Translate() + ": ");
			if (operatingAtHighPower)
			{
				stringBuilder.Append("PowerConsumptionHigh".Translate());
			}
			else
			{
				stringBuilder.Append("PowerConsumptionLow".Translate());
			}
			return stringBuilder.ToString();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref currentTemp, "currentTemp", float.MinValue);
			Scribe_Values.Look(ref idealTemp, "idealTemp", float.MinValue);
		}
	}
}
