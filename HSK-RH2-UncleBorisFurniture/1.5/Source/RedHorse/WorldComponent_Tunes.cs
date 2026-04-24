using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RedHorse
{
	internal class WorldComponent_Tunes : WorldComponent
	{
		private bool AreTunesReady = false;

		public List<TuneDef> TuneDefCache = new List<TuneDef>();

		public WorldComponent_Tunes(World world)
			: base(world)
		{
		}

		public TuneDef GetCache(TuneDef tune)
		{
			if (TuneDefCache == null)
			{
				TuneDefCache = new List<TuneDef>();
			}
			foreach (TuneDef item in TuneDefCache)
			{
				if (item == tune)
				{
					return item;
				}
			}
			TuneDefCache.Add(tune);
			return tune;
		}

		public void GenerateTunesList()
		{
			if (AreTunesReady)
			{
				return;
			}
			foreach (TuneDef allDef in DefDatabase<TuneDef>.AllDefs)
			{
				GetCache(allDef);
			}
			AreTunesReady = true;
		}

		public override void WorldComponentTick()
		{
			base.WorldComponentTick();
			GenerateTunesList();
		}

		public override void ExposeData()
		{
			Scribe_Collections.Look(ref TuneDefCache, "TuneDefCache", LookMode.Def);
			base.ExposeData();
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				GenerateTunesList();
			}
		}
	}
}
