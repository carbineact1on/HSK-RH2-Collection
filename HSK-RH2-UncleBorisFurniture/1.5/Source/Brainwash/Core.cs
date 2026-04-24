using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Brainwash
{
    [StaticConstructorOnStartup]
    public static class Core
    {
        public static List<ThingDef> allTelevisions = new();
        static Core()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.building?.joyKind == BrainwashDefOf.Television)
                {
                    allTelevisions.Add(def);
                }
                else if (def.race?.Humanlike ?? false)
                {
                    def.comps.Add(new CompProperties_ChangePersonality
                    {
                        traitsToExclude = new List<TraitDef>
                        {
                            BrainwashDefOf.Tough,
                            TraitDefOf.AnnoyingVoice,
                            TraitDefOf.CreepyBreathing,
                            BrainwashDefOf.Beauty,
                            BrainwashDefOf.Immunity,
                            BrainwashDefOf.PsychicSensitivity,
                        }
                    });
                }
            }
        }

        public static bool CanBeBrainwashedBy(this Pawn pawn, Pawn leader)
        {
            if (pawn == leader)
                return false;
            if (pawn.RaceProps.Humanlike && pawn.InAggroMentalState is false && (pawn.IsPrisonerOfColony 
                || pawn.IsSlaveOfColony || pawn.IsWildMan()))
            {
                var interactionMode = pawn.guest.interactionMode;
                if (interactionMode == BrainwashDefOf.RedHorse_Brainwash
                        && pawn.CurJobDef != BrainwashDefOf.RedHorse_WatchBrainwashTelevision
                        && pawn.CurJobDef != BrainwashDefOf.RedHorse_StartBrainwashTelevision
                        && (pawn.guest.will > 0 || pawn.guest.resistance > 0))
                {
                    return !pawn.Downed && leader.CanReserve(pawn)
                        && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight)
                        && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing);
                }
            }
            return false;

        }
    }

    [HarmonyPatch(typeof(WorkGiver_Warden_TakeToBed), "TakeToPreferredBedJob")]
    public static class WorkGiver_Warden_TakeToBed_TakeToPreferredBedJob_Patch
    {
        public static void Postfix(ref Job __result, Pawn prisoner)
        {
            if (prisoner.CurJobDef == BrainwashDefOf.RedHorse_WatchBrainwashTelevision || prisoner.CurJobDef == BrainwashDefOf.RedHorse_StartBrainwashTelevision)
            {
                __result = null;
            }
        }
    }

    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState", null)]
    public static class MentalStateHandler_Patch
    {
        public static bool Prefix(Pawn ___pawn, ref bool __result)
        {
            if (___pawn.CurJobDef == BrainwashDefOf.RedHorse_WatchBrainwashTelevision 
                || ___pawn.CurJobDef == BrainwashDefOf.RedHorse_StartBrainwashTelevision)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
    public static class FloatMenuMakerMap_AddHumanlikeOrders_Patch
    {
        public static void Postfix(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
        {
            IntVec3 c = IntVec3.FromVector3(clickPos);
            List<Thing> thingList = c.GetThingList(pawn.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing t = thingList[i];
                if (Core.allTelevisions.Contains(t.def))
                {
                    var comp = pawn.GetComp<CompChangePersonality>();
                    if (comp.SuitableForBrainwashing(t))
                    {
                        var chair = comp.FindChair(pawn, t);
                        if (chair != null)
                        {
                            opts.Add(new FloatMenuOption("Brainwash_ChangeOwnPersonality".Translate(), delegate
                            {
                                if (comp.TryGetNearbyTelevisionAndChair(pawn, out var televisionAndChair))
                                {
                                    Find.WindowStack.Add(new Window_ChangePersonality(comp, delegate
                                    {
                                        Job job = JobMaker.MakeJob(BrainwashDefOf.RedHorse_StartBrainwashTelevision, televisionAndChair.television, televisionAndChair.chair);
                                        job.count = 1;
                                        pawn.jobs.TryTakeOrderedJob(job);
                                    }));
                                }
                            }));
                        }
                    }
                }
            }
        }
    }
}
