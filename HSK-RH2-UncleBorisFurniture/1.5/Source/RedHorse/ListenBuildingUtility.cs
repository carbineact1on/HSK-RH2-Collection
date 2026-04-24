using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RedHorse
{
	public static class ListenBuildingUtility
	{
		public static bool TryFindBestListenCell(Thing toListen, Pawn pawn, bool desireSit, out IntVec3 result, out Building chair)
		{
			IntVec3 invalid = IntVec3.Invalid;
			Comp_MusicPlayer comp_MusicPlayer = toListen.TryGetComp<Comp_MusicPlayer>();
			if (comp_MusicPlayer != null)
			{
				IEnumerable<IntVec3> listenableCells = comp_MusicPlayer.ListenableCells;
				Random random = new Random();
				IEnumerable<IntVec3> enumerable = listenableCells.OrderBy((IntVec3 order) => random.Next()).ToList();
				foreach (IntVec3 item in enumerable)
				{
					bool flag = false;
					Building building = null;
					if (desireSit)
					{
						building = item.GetEdifice(pawn.Map);
						if (building != null && building.def.building.isSittable && pawn.CanReserve(building))
						{
							flag = true;
						}
					}
					else if (!item.IsForbidden(pawn) && pawn.CanReserve(item))
					{
						flag = true;
					}
					if (flag)
					{
						result = item;
						chair = building;
						return true;
					}
				}
			}
			result = IntVec3.Invalid;
			chair = null;
			return false;
		}

		public static bool CanListenFromBed(Pawn pawn, Building_Bed bed, Thing toListen)
		{
			if (!pawn.Position.Standable(pawn.Map) || pawn.Position.GetEdifice(pawn.Map) is Building_Bed)
			{
				return false;
			}
			Comp_MusicPlayer comp_MusicPlayer = toListen.TryGetComp<Comp_MusicPlayer>();
			if (comp_MusicPlayer == null)
			{
				return false;
			}
			IEnumerable<IntVec3> listenableCells = comp_MusicPlayer.ListenableCells;
			return listenableCells.Any((IntVec3 current) => current == pawn.Position);
		}
	}
}
