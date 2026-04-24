using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RedHorse
{
	public class Comp_MusicPlayer : ThingComp
	{
		public enum State
		{
			off,
			on
		}

		private const float ListenRadius = 7.9f;

		private const int counterWoundMax = 20000;

		private static List<IntVec3> listenableCells = new List<IntVec3>();

		private bool autoPlay;

		private int counter;

		private TuneDef currentTuneDef;

		private bool destroyedFlag;

		private float duration = -1f;

		private TuneDef nextTuneDef;

		protected Sustainer playingSong;

		private List<TuneDef> playlist = new List<TuneDef>();

		private CompPowerTrader powerTrader;

		private TuneDef prevTuneDef;

		private int rareTickWorker = 250;

		private State state = State.off;

		private State stateOld = State.on;

		private readonly WorldComponent_Tunes tuneScape = Find.World.GetComponent<WorldComponent_Tunes>();

		private readonly string txtOff = "Off";

		private readonly string txtOn = "On";

		private readonly string txtPlaying = "Now Playing:";

		private readonly string txtStatus = "Status";

		public CompProperties_MusicPlayer Props => props as CompProperties_MusicPlayer;

		public bool isRadio => Props.isRadio;

		public TuneDef CurrentTune
		{
			get
			{
				return currentTuneDef;
			}
			set
			{
				currentTuneDef = value;
			}
		}

		public TuneDef NextTune
		{
			get
			{
				return nextTuneDef;
			}
			set
			{
				nextTuneDef = value;
			}
		}

		public TuneDef PreviousTune
		{
			get
			{
				return prevTuneDef;
			}
			set
			{
				prevTuneDef = value;
			}
		}

		public State CurrentState
		{
			get
			{
				return state;
			}
			set
			{
				state = value;
			}
		}

		public IEnumerable<IntVec3> ListenableCells => ListenableCellsAround(parent.PositionHeld, parent.MapHeld);

		public bool IsOn()
		{
			if (state == State.on)
			{
				return true;
			}
			return false;
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			listenableCells = ListenableCellsAround(parent.PositionHeld, parent.MapHeld);
			TryResolvePowerTrader();
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref autoPlay, "autoPlay", defaultValue: false);
			Scribe_Values.Look(ref state, "state", State.off);
			Scribe_Values.Look(ref counter, "counter", 0);
			Scribe_Defs.Look(ref prevTuneDef, "prevTuneDef");
			Scribe_Defs.Look(ref currentTuneDef, "currentTuneDef");
			Scribe_Defs.Look(ref nextTuneDef, "nextTuneDef");
			Scribe_Collections.Look(ref playlist, "playlist", LookMode.Def);
			stateOld = state;
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				autoPlay = false;
				StopMusic();
			}
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			StopMusic();
			destroyedFlag = true;
			base.PostDestroy(mode, previousMap);
		}

		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (!string.IsNullOrEmpty(base.CompInspectStringExtra()))
			{
				stringBuilder.AppendLine(base.CompInspectStringExtra());
			}
			stringBuilder.Append(txtStatus + " ");
			if (state == State.off)
			{
				stringBuilder.Append(txtOff);
			}
			if (state == State.on)
			{
				stringBuilder.Append(txtOn);
				stringBuilder.AppendLine();
				stringBuilder.Append(txtPlaying + " ");
				stringBuilder.Append(currentTuneDef);
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}

		public virtual void PlayMusic(Pawn activator)
		{
			if (activator != null && this != null)
			{
				if (currentTuneDef == null)
				{
					currentTuneDef = tuneScape.TuneDefCache.RandomElement();
				}
				if (state == State.off)
				{
					state = State.on;
					StartMusic();
				}
			}
		}

		private bool TryResolvePowerTrader()
		{
			if (powerTrader == null)
			{
				powerTrader = parent?.TryGetComp<CompPowerTrader>();
				if (powerTrader != null)
				{
					return true;
				}
				return false;
			}
			return true;
		}

		private void SwitchTracks()
		{
			TuneDef def = null;
			if (!TryResolveNextTrack(out def))
			{
				Log.Error("Could not resolve next track.");
				return;
			}
			NextTune = def;
			TuneDef currentTune = CurrentTune;
			TuneDef nextTune = NextTune;
			StopMusic();
			PreviousTune = currentTune;
			CurrentTune = nextTune;
			StartMusic();
		}

		private bool TryCreatePlaylist()
		{
			if (tuneScape.TuneDefCache == null)
			{
				return false;
			}
			if (tuneScape.TuneDefCache.Count == 0)
			{
				return false;
			}
			List<TuneDef> source = tuneScape.TuneDefCache.ToList();
			playlist = new List<TuneDef>(source.InRandomOrder());
			return true;
		}

		private bool TryResolveNextTrack(out TuneDef def)
		{
			def = null;
			if (playlist.Count == 0 && !TryCreatePlaylist())
			{
				Log.Error("Unable to create playlist!");
				return false;
			}
			TuneDef result = null;
			for (int i = 0; i < 999; i++)
			{
				if (playlist.TryRandomElement(out result) && result != CurrentTune)
				{
					break;
				}
			}
			if (result != null)
			{
				def = result;
				return true;
			}
			return false;
		}

		public virtual void StartMusic(TuneDef parmDef = null)
		{
			if (state == State.off)
			{
				state = State.on;
			}
			duration = Time.time + currentTuneDef.durationTime;
			playingSong = null;
			SoundInfo info = SoundInfo.InMap(parent);
			SoundDef soundDef = currentTuneDef;
			if (parmDef != null)
			{
				soundDef = parmDef;
			}
			playingSong = soundDef.TrySpawnSustainer(info);
		}

		public void StopMusic()
		{
			if (state == State.on)
			{
				state = State.off;
				duration = -1f;
				playingSong?.End();
			}
		}

		public static List<IntVec3> ListenableCellsAround(IntVec3 pos, Map map)
		{
			listenableCells.Clear();
			if (!pos.InBounds(map))
			{
				return listenableCells;
			}
			Region region = pos.GetRegion(map, RegionType.Normal | RegionType.Portal);
			if (region == null)
			{
				return listenableCells;
			}
			RegionTraverser.BreadthFirstTraverse(region, (Region from, Region to) => to.door == null, delegate(Region to)
			{
				foreach (IntVec3 cell in to.Cells)
				{
					if (cell.InHorDistOf(pos, 7.9f))
					{
						listenableCells.Add(cell);
					}
				}
				return false;
			}, 12, RegionType.Normal | RegionType.Portal);
			return listenableCells;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			IEnumerator<Gizmo> enumerator = base.CompGetGizmosExtra().GetEnumerator();
			while (enumerator.MoveNext())
			{
				yield return enumerator.Current;
			}
			if (isRadio && powerTrader != null)
			{
				Command_Toggle toggleDef = new Command_Toggle
				{
					hotKey = KeyBindingDefOf.Command_TogglePower,
					icon = ContentFinder<Texture2D>.Get("UI/Icons/Commands/Autoplay"),
					defaultLabel = "Autoplay",
					defaultDesc = "Enables automatic playing of music through the radio.",
					isActive = () => autoPlay,
					toggleAction = delegate
					{
						autoPlay = !autoPlay;
					},
					disabled = true
				};
				if (powerTrader.PowerOn)
				{
					toggleDef.disabled = false;
				}
				yield return toggleDef;
			}
		}

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			if (!selPawn.CanReserve(parent, 16))
			{
				FloatMenuOption item = new FloatMenuOption((string)"CannotUseReserved".Translate(), null);
				return new List<FloatMenuOption> { item };
			}
			if (!ReachabilityUtility.CanReach(selPawn, (LocalTargetInfo)parent, PathEndMode.InteractionCell, Danger.Some, false, false, TraverseMode.ByPawn))
			{
				FloatMenuOption item2 = new FloatMenuOption((string)"CannotUseNoPath".Translate(), null);
				return new List<FloatMenuOption> { item2 };
			}
			if (!selPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				FloatMenuOption item3 = new FloatMenuOption((string)"CannotUseReason".Translate("IncapableOfCapacity".Translate(PawnCapacityDefOf.Manipulation.label)), (Action)null, MenuOptionPriority.Default);
				return new List<FloatMenuOption> { item3 };
			}
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			IntVec3 vec = selPawn.Position;
			Building t2 = null;
			if (IsOn())
			{
				list.Add(new FloatMenuOption("Listen to " + parent.Label, (Action)Action0, MenuOptionPriority.Default));
				list.Add(new FloatMenuOption("Turn off " + parent.Label, (Action)Action0A, MenuOptionPriority.Default));
			}
			if (tuneScape != null && tuneScape.TuneDefCache.Count > 0)
			{
				foreach (TuneDef def in tuneScape.TuneDefCache)
				{
					Action action = delegate
					{
						Job job3 = new Job(RedHorseDefOf.RedHorse_PlayMusicPlayer, parent)
						{
							targetA = parent
						};
						currentTuneDef = def;
						if (!selPawn.jobs.TryTakeOrderedJob(job3, JobTag.Misc))
						{
						}
					};
					list.Add(new FloatMenuOption((string)("Play " + def.LabelCap), action, MenuOptionPriority.Default));
				}
			}
			return list;
			void Action0()
			{
				Job job2 = null;
				if (ListenBuildingUtility.TryFindBestListenCell(parent, selPawn, desireSit: true, out vec, out t2))
				{
					job2 = new Job(RedHorseDefOf.RedHorse_ListenToMusicPlayer, parent, vec, t2);
				}
				else if (ListenBuildingUtility.TryFindBestListenCell(parent, selPawn, desireSit: false, out vec, out t2))
				{
					job2 = new Job(RedHorseDefOf.RedHorse_ListenToMusicPlayer, parent, vec, t2);
				}
				if (job2 != null)
				{
					job2.targetB = vec;
					job2.targetC = t2;
					if (!selPawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc))
					{
					}
				}
			}
			void Action0A()
			{
				Job job = new Job(RedHorseDefOf.RedHorse_TurnOffMusicPlayer, parent)
				{
					targetA = parent
				};
				if (!selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc))
				{
				}
			}
		}

		public override void CompTickRare()
		{
			if (!destroyedFlag)
			{
				base.CompTickRare();
				DoTickerWork(250);
			}
		}

		public override void CompTick()
		{
			if (!destroyedFlag)
			{
				base.CompTick();
				DoTickerWork(1);
			}
		}

		private void DoTickerWork(int tickerAmount)
		{
			stateOld = state;
			rareTickWorker--;
			if (isRadio && rareTickWorker <= 0)
			{
				rareTickWorker = 250;
				if (!TryResolvePowerTrader())
				{
					Log.Error("Radio Error: Cannot resolve power trader comp.");
					return;
				}
				if (!powerTrader.PowerOn)
				{
					StopMusic();
				}
			}
			if (duration == -1f || state != State.on || !(Time.time >= duration))
			{
				return;
			}
			if (isRadio)
			{
				if (!TryResolvePowerTrader())
				{
					Log.Error("Radio Error: Cannot resolve power trader comp.");
					return;
				}
				if (powerTrader.PowerOn)
				{
					if (autoPlay)
					{
						SwitchTracks();
					}
					else
					{
						StopMusic();
					}
					return;
				}
				if (autoPlay)
				{
					autoPlay = false;
				}
				StopMusic();
			}
			else
			{
				StopMusic();
			}
		}
	}
}
