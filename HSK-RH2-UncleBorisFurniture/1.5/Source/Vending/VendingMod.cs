using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Vending
{
    public class CompProperties_VendingMachine : CompProperties
    {
        public TraderKindDef traderKind;
        public int refreshStockInTicks;
        public CompProperties_VendingMachine()
        {
            compClass = typeof(CompVendingMachine);
        }
    }

    public class CompVendingMachine : ThingComp
    {
        public CompProperties_VendingMachine Props => props as CompProperties_VendingMachine;
        public int lastRestockTick;
        public PerpetualTrader trader;
        public CompPowerTrader powerComp;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.TryGetComp<CompPowerTrader>();
            if (!respawningAfterLoad)
            {
                Restock();
            }
        }
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption opt in base.CompFloatMenuOptions(selPawn))
            {
                yield return opt;
            }
            yield return powerComp != null && !powerComp.PowerOn
                ? new FloatMenuOption("CannotUseNoPower".Translate(), null)
                : !selPawn.CanReach(parent, PathEndMode.OnCell, Danger.Deadly)
                    ? new FloatMenuOption("CannotTrade".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null)
                    : selPawn.skills.GetSkill(SkillDefOf.Social).TotallyDisabled
                                    ? new FloatMenuOption("CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap), null)
                                    : FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("TradeWith".Translate(trader.TraderKind.label),
                                                delegate
                                                {
                                                    Job job5 = JobMaker.MakeJob(RedHorseDefOf.RedHorse_TradeWith, parent);
                                                    job5.playerForced = true;
                                                    selPawn.jobs.TryTakeOrderedJob(job5, JobTag.Misc);
                                                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.InteractingWithTraders, KnowledgeAmount.Total);
                                                }, MenuOptionPriority.InitiateSocial, null, parent), selPawn, parent);
        }
        private void Restock()
        {
            trader ??= new PerpetualTrader(parent, Props.traderKind);
            trader.Restock();
            lastRestockTick = Find.TickManager.TicksGame;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame > (lastRestockTick + Props.refreshStockInTicks))
            {
                Restock();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastRestockTick, "lastRestockTick");
            Scribe_Deep.Look(ref trader, "trader");
        }
    }
}
